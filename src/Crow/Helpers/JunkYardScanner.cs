using System.Collections.Concurrent;

namespace Crow.Helpers;

public static class JunkYardScanner
{
    // Never worth descending into: huge, and node_modules is full of package.json markers.
    private static readonly HashSet<string> NeverDescendInto =
        new(["node_modules", ".git", ".hg", ".svn"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Finds and retrieves all junkyards based on the specified configuration.
    /// A junkyard is a file or directory matching one of the remove candidates
    /// that sits next to a file matching the look-for pattern.
    /// </summary>
    /// <param name="config">The configuration object containing the paths to search for junkyards.</param>
    /// <returns>The JunkYard objects found in the specified directories.</returns>
    public static IEnumerable<JunkYard> ScanJunkYards(this ScanDeleteConfiguration config)
    {
        var preset = new DevPreset(string.Empty, config.LookFor, config.RemoveCandidates);
        return Scan([preset], config.LookIns, config.Ignores, labelResults: false);
    }

    /// <summary>
    /// Applies every preset to each directory in a single walk of the tree.
    /// </summary>
    public static IEnumerable<JunkYard> ScanJunkYards(this DevScanConfiguration config)
    {
        return Scan(config.Presets, config.LookIns, config.Ignores, labelResults: true);
    }

    /// <summary>
    /// Walks the tree in parallel, then sizes every found directory in a second
    /// parallel walk. Keeps descending below project roots (so nested projects in a
    /// monorepo are found) but never into anything marked for removal.
    /// </summary>
    private static List<JunkYard> Scan(
        IReadOnlyList<DevPreset> presets,
        IReadOnlyList<string> lookIns,
        IReadOnlyList<string> ignores,
        bool labelResults
    )
    {
        var ignored = ignores.Select(TrimSeparators).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var found = new ConcurrentBag<JunkYard>();

        var roots = lookIns
            .Where(p => !string.IsNullOrEmpty(p) && !ignored.Contains(TrimSeparators(p)))
            .Select(p => (p, 0));

        SafeFileSystem.ParallelWalk(roots, (directory, _) =>
        {
            var entries = SafeFileSystem.List(directory, withFilePaths: false);
            var files = entries.Where(e => !e.IsDirectory).ToList();
            var junk = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var matchingPresets = presets.Where(preset =>
                files.Exists(f => Sherlock.MatchesPattern(preset.LookFor, f.Name.AsSpan(), MatchType.Win32)));

            foreach (var preset in matchingPresets)
            {
                var targets = entries.Where(e => preset.RemoveCandidates.Any(candidate =>
                    Sherlock.MatchesPattern(candidate, e.Name.AsSpan(), MatchType.Win32)));

                foreach (var target in targets.Where(t => junk.Add(t.Name)))
                {
                    var label = labelResults ? preset.Name : null;
                    found.Add(target.IsDirectory
                        ? new JunkYard(target.FullPath, 0, JunkType.Directory, label)
                        : new JunkYard(Path.Combine(directory, target.Name), target.Length, JunkType.File, label));
                }
            }

            return entries
                .Where(e => e.IsDirectory
                            && !junk.Contains(e.Name)
                            && !NeverDescendInto.Contains(e.Name)
                            && !ignored.Contains(e.FullPath))
                .Select(e => e.FullPath);
        });

        var directories = found.Where(j => j.JunkType == JunkType.Directory).ToList();
        var sizes = SafeFileSystem.GetDirectorySizes(directories.Select(d => d.Path).ToList());

        return directories
            .Select((junkYard, i) => junkYard with { Size = sizes[i] })
            .Concat(found.Where(j => j.JunkType == JunkType.File))
            .ToList();
    }

    private static string TrimSeparators(string path)
    {
        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
