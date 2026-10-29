using System.Collections.Concurrent;

namespace Crow.Helpers;

public static class SystemJunkCleaner
{
    /// <summary>
    /// Counts the files in the location that are older than <paramref name="minAge"/>.
    /// </summary>
    public static SystemJunkScanResult Measure(this SystemJunkLocation location, TimeSpan minAge)
    {
        return MeasureAll([location], _ => minAge)[0];
    }

    /// <summary>
    /// Measures every location in a single parallel walk, so the slow ones
    /// (package caches with tens of thousands of files) share all threads.
    /// </summary>
    public static List<SystemJunkScanResult> MeasureAll(
        IReadOnlyList<SystemJunkLocation> locations,
        Func<SystemJunkLocation, TimeSpan> minAge
    )
    {
        var now = DateTime.UtcNow;
        var roots = locations
            .SelectMany((location, index) => location.Paths.Select(path => (Index: index, Path: path)))
            .ToList();

        var counters = SafeFileSystem.Measure(
            roots.Select(r => (r.Path, now - minAge(locations[r.Index]))).ToList());

        var totals = roots
            .Select((root, i) => (root.Index, Counter: counters[i]))
            .ToLookup(r => r.Index, r => r.Counter);

        return locations
            .Select((location, index) => new SystemJunkScanResult(
                location,
                totals[index].Sum(c => c.Size),
                totals[index].Sum(c => c.Count),
                minAge(location)))
            .ToList();
    }

    /// <summary>
    /// Deletes the files counted by <see cref="Measure"/>, then removes empty sub folders.
    /// Files that are locked or need elevation are skipped, not retried.
    /// </summary>
    public static CleanResult Clean(this SystemJunkScanResult scan)
    {
        var cutoff = DateTime.UtcNow - scan.MinAge;
        long freed = 0;
        int deleted = 0, skipped = 0;
        var oldDirectories = new ConcurrentBag<string>();

        SafeFileSystem.ParallelWalk(scan.Location.Paths.Select(p => (p, 0)), (directory, _) =>
        {
            var subdirectories = new List<string>();
            foreach (var entry in SafeFileSystem.List(directory))
            {
                if (entry.IsDirectory)
                {
                    // Creation time is used because deleting files bumps the folder's
                    // write time, and fresh folders may belong to a running process.
                    if (entry.CreationTimeUtc < cutoff)
                        oldDirectories.Add(entry.FullPath);

                    subdirectories.Add(entry.FullPath);
                    continue;
                }

                if (entry.LastWriteTimeUtc >= cutoff)
                    continue;

                if (SafeFileSystem.TryDeleteFile(entry.FullPath))
                {
                    Interlocked.Add(ref freed, entry.Length);
                    Interlocked.Increment(ref deleted);
                }
                else
                {
                    Interlocked.Increment(ref skipped);
                }
            }

            return subdirectories;
        });

        foreach (var directory in oldDirectories.OrderByDescending(d => d.Length))
        {
            TryDeleteEmptyDirectory(directory);
        }

        return new CleanResult(freed, deleted, skipped);
    }

    private static void TryDeleteEmptyDirectory(string directory)
    {
        try
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any())
                Directory.Delete(directory);
        }
        catch
        {
            // In use or not ours to delete. Leave it.
        }
    }
}
