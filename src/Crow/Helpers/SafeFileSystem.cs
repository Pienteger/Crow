using System.Collections.Concurrent;
using System.IO.Enumeration;

namespace Crow.Helpers;

/// <summary>
/// File system walking that never throws. Inaccessible entries are skipped and
/// reparse points (junctions, symlinks, OneDrive placeholders) are never followed.
/// Walks run on a pool of dedicated threads that share one queue of directories,
/// so one huge folder does not leave the other threads idle.
/// </summary>
public static class SafeFileSystem
{
    private static readonly int WorkerCount = Math.Clamp(Environment.ProcessorCount * 2, 4, 64);

    private static readonly EnumerationOptions SingleLevelOptions = new()
    {
        RecurseSubdirectories = false,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint,
        BufferSize = 64 * 1024
    };

    // Links are listed (so they can be removed) but never descended into.
    private static readonly EnumerationOptions DeleteOptions = new()
    {
        RecurseSubdirectories = false,
        IgnoreInaccessible = true,
        AttributesToSkip = 0,
        BufferSize = 64 * 1024
    };

    public readonly record struct Entry(
        string Name,
        string FullPath,
        bool IsDirectory,
        long Length,
        DateTime LastWriteTimeUtc,
        DateTime CreationTimeUtc
    );

    public sealed class SizeCounter
    {
        private long size;
        private int count;

        public long Size => Interlocked.Read(ref size);
        public int Count => Volatile.Read(ref count);

        public void Add(long bytes, int files)
        {
            Interlocked.Add(ref size, bytes);
            Interlocked.Add(ref count, files);
        }
    }

    /// <summary>
    /// Visits every directory under <paramref name="roots"/> in parallel.
    /// <paramref name="visit"/> receives a directory and its state and returns
    /// the subdirectories to descend into; children inherit the parent's state.
    /// </summary>
    public static void ParallelWalk<TState>(
        IEnumerable<(string Path, TState State)> roots,
        Func<string, TState, IEnumerable<string>> visit
    )
    {
        var queue = new ConcurrentQueue<(string Path, TState State)>();
        foreach (var root in roots)
        {
            queue.Enqueue(root);
        }

        var pending = queue.Count;
        if (pending == 0)
            return;

        using var available = new SemaphoreSlim(pending);

        void Work()
        {
            while (true)
            {
                available.Wait();

                // Only the final shutdown release finds the queue empty.
                if (!queue.TryDequeue(out var item))
                    return;

                foreach (var child in SafeVisit(item))
                {
                    Interlocked.Increment(ref pending);
                    queue.Enqueue((child, item.State));
                    available.Release();
                }

                if (Interlocked.Decrement(ref pending) == 0)
                    available.Release(WorkerCount);
            }
        }

        IEnumerable<string> SafeVisit((string Path, TState State) item)
        {
            try
            {
                return visit(item.Path, item.State).ToList();
            }
            catch
            {
                return [];
            }
        }

        var workers = Enumerable
            .Range(0, WorkerCount)
            .Select(_ => Task.Factory.StartNew(Work, TaskCreationOptions.LongRunning))
            .ToArray();
        Task.WaitAll(workers);
    }

    /// <summary>
    /// Lists the direct children of a directory. Returns what was read before any error.
    /// File paths are only built when <paramref name="withFilePaths"/> is set; otherwise
    /// <see cref="Entry.FullPath"/> is empty for files.
    /// </summary>
    public static List<Entry> List(string path, bool withFilePaths = true)
    {
        return withFilePaths ? Read(path, static (ref FileSystemEntry e) => ToEntry(ref e, true))
            : Read(path, static (ref FileSystemEntry e) => ToEntry(ref e, false));
    }

    private static Entry ToEntry(ref FileSystemEntry e, bool withFilePath)
    {
        return new Entry(
            e.FileName.ToString(),
            e.IsDirectory || withFilePath ? e.ToFullPath() : string.Empty,
            e.IsDirectory,
            e.Length,
            e.LastWriteTimeUtc.UtcDateTime,
            e.CreationTimeUtc.UtcDateTime
        );
    }

    /// <summary>
    /// Sums the size and count of files under each root that were modified before
    /// that root's cutoff, all in one parallel walk. Only directory paths are
    /// allocated, never file names.
    /// </summary>
    public static SizeCounter[] Measure(IReadOnlyList<(string Path, DateTime Cutoff)> roots)
    {
        var counters = roots.Select(_ => new SizeCounter()).ToArray();

        ParallelWalk(
            roots.Select((root, i) => (root.Path, (Counter: counters[i], root.Cutoff))),
            (directory, state) =>
            {
                var (counter, cutoff) = state;
                long size = 0;
                var count = 0;
                var subdirectories = new List<string>();

                var enumerable = new FileSystemEnumerable<string?>(
                    directory,
                    static (ref FileSystemEntry e) => e.IsDirectory ? e.ToFullPath() : null,
                    SingleLevelOptions
                )
                {
                    ShouldIncludePredicate = (ref FileSystemEntry e) =>
                    {
                        if (e.IsDirectory)
                            return true;

                        if (e.LastWriteTimeUtc.UtcDateTime < cutoff)
                        {
                            size += e.Length;
                            count++;
                        }

                        return false;
                    }
                };

                try
                {
                    foreach (var subdirectory in enumerable)
                    {
                        subdirectories.Add(subdirectory!);
                    }
                }
                catch
                {
                    // Keep what was counted before the error.
                }

                counter.Add(size, count);
                return subdirectories;
            }
        );

        return counters;
    }

    /// <summary>
    /// Sizes many directories in one parallel walk.
    /// </summary>
    public static long[] GetDirectorySizes(IReadOnlyList<string> directories)
    {
        return Measure(directories.Select(d => (d, DateTime.MaxValue)).ToList()).Select(c => c.Size).ToArray();
    }

    /// <summary>
    /// Deletes a directory tree in parallel. Unlike <see cref="Directory.Delete(string, bool)"/>
    /// it does not stop at the first failure, clears read-only flags, retries briefly on
    /// transient locks, and removes links (symlinks, junctions) without touching their targets,
    /// including file-type links that point at a folder, which Node creates on Windows.
    /// </summary>
    public static CleanResult DeleteTree(string root)
    {
        long freed = 0;
        int deleted = 0, skipped = 0;
        var directories = new ConcurrentBag<string>();

        ParallelWalk([(root, 0)], (directory, _) =>
        {
            directories.Add(directory);
            var subdirectories = new List<string>();

            var entries = Read(
                directory,
                static (ref FileSystemEntry e) => (
                    Path: e.ToFullPath(),
                    e.IsDirectory,
                    IsLink: (e.Attributes & FileAttributes.ReparsePoint) != 0,
                    e.Length
                ),
                DeleteOptions
            );

            foreach (var entry in entries)
            {
                if (entry.IsDirectory && !entry.IsLink)
                {
                    subdirectories.Add(entry.Path);
                    continue;
                }

                if (TryDeleteEntry(entry.Path, entry.IsDirectory))
                {
                    Interlocked.Add(ref freed, entry.IsLink ? 0 : entry.Length);
                    Interlocked.Increment(ref deleted);
                }
                else
                {
                    Interlocked.Increment(ref skipped);
                }
            }

            return subdirectories;
        });

        // Deepest first, so each folder is empty by the time it is removed.
        foreach (var directory in directories.OrderByDescending(d => d.Length))
        {
            if (!TryDeleteEntry(directory, isDirectory: true))
                skipped++;
        }

        return new CleanResult(freed, deleted, skipped);
    }

    /// <summary>
    /// Deletes one file (or link), clearing read-only and retrying briefly on transient locks.
    /// </summary>
    public static bool TryDeleteFile(string path)
    {
        return TryDeleteEntry(path, isDirectory: false);
    }

    private static bool TryDeleteEntry(string path, bool isDirectory)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            // A link's own kind may not match its target, so try the other kind too.
            if (TryDelete(path, isDirectory) || TryDelete(path, !isDirectory))
                return true;

            ClearReadOnly(path);
            if (!EntryExists(path))
                return true;

            if (attempt < 2)
                Thread.Sleep(25 << attempt); // Antivirus or indexer holding it for a moment.
        }

        return false;
    }

    private static bool TryDelete(string path, bool asDirectory)
    {
        try
        {
            // Non-recursive: on a link this removes the link, never its target.
            if (asDirectory)
                Directory.Delete(path);
            else
                File.Delete(path);

            return !EntryExists(path);
        }
        catch
        {
            return false;
        }
    }

    private static void ClearReadOnly(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            if (attributes.HasFlag(FileAttributes.ReadOnly))
                File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
        }
        catch
        {
            // Gone or not ours. The next delete attempt will tell.
        }
    }

    // File.Exists and Directory.Exists follow links; GetAttributes looks at the entry itself.
    private static bool EntryExists(string path)
    {
        try
        {
            File.GetAttributes(path);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
        catch
        {
            return true;
        }
    }

    private static List<T> Read<T>(
        string path,
        FileSystemEnumerable<T>.FindTransform transform,
        EnumerationOptions? options = null
    )
    {
        var result = new List<T>();
        try
        {
            foreach (var item in new FileSystemEnumerable<T>(path, transform, options ?? SingleLevelOptions))
            {
                result.Add(item);
            }
        }
        catch
        {
            // Missing, inaccessible or vanished mid-read. Keep what was read.
        }

        return result;
    }
}
