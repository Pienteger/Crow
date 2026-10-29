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

    private static List<T> Read<T>(string path, FileSystemEnumerable<T>.FindTransform transform)
    {
        var result = new List<T>();
        try
        {
            foreach (var item in new FileSystemEnumerable<T>(path, transform, SingleLevelOptions))
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
