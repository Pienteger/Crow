namespace Crow.Helpers;

public static class JunkYardScanner
{
    /// <summary>
    /// Finds and retrieves all junkyards based on the specified configuration.
    /// Iterates through each path in the configuration's LookIns collection,
    /// skips empty paths, and retrieves junkyards from each valid directory.
    /// </summary>
    /// <param name="config">The configuration object containing the paths to search for junkyards.</param>
    /// <returns>A leaky enumerable collection of JunkYard objects found in the specified directories.</returns>
    public static IEnumerable<JunkYard> ScanJunkYards(this Configuration config)
    {
        var validPaths = config.LookIns
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => new DirectoryInfo(p));
        
        foreach (var directoryInfo  in validPaths)
        {
            foreach (var junk in directoryInfo.GetJunkYards(config))
            {
                yield return junk;
            }
        }
    }

    private static long GetFileSize(string filePath)
    {
        return File.Exists(filePath) ? new FileInfo(filePath).Length : 0;
    }

    private static long CalculateDirectorySize(DirectoryInfo directory)
    {
        if (directory is not {Exists: true})
            return 0;

        long startDirectorySize = 0;

        foreach (var fileInfo in directory.GetFiles())
        {
            Interlocked.Add(ref startDirectorySize, fileInfo.Length);
        }

        Parallel.ForEach(
            directory.GetDirectories(),
            subDirectory =>
                Interlocked.Add(ref startDirectorySize, CalculateDirectorySize(subDirectory))
        );

        return startDirectorySize;
    }

    private static IEnumerable<JunkYard> GetJunkYards(
        this DirectoryInfo? directoryInfo,
        Configuration config
    )
    {
        if (ShouldSkipDirectory(directoryInfo, config))
            yield break;

        if (TryGetDirectoryContents(directoryInfo!, out var files, out var directories))
            yield break;

        if (IsDirectoryEmpty(files, directories))
            yield break;

        if (CheckForJunkFiles(files, directories, config, out var junkYards))
        {
            foreach (var junkYard in junkYards)
            {
                yield return junkYard;
            }

            yield break;
        }

        foreach (var junkYard1 in SearchSubDirectories(config, directories))
        {
            yield return junkYard1;
        }
    }

    private static bool CheckForJunkFiles(
        FileInfo[] files, DirectoryInfo[] directories, Configuration config, out IEnumerable<JunkYard> junkYards)
    {
        var validFiles = files.Where(f => Sherlock.MatchesPattern(config.LookFor, f.Name.AsSpan(), MatchType.Win32))
            .ToList();

        if (validFiles.Count == 0)
        {
            junkYards = [];
            return false;
        }

        junkYards = config.RemoveCandidates.SelectMany(
            target => FindJunkYards(directories, files, target)
        );
        return true;
    }

    private static IEnumerable<JunkYard> SearchSubDirectories(Configuration config, DirectoryInfo[] directories)
    {
        return directories.SelectMany(directory => directory.GetJunkYards(config));
    }

    private static bool TryGetDirectoryContents(DirectoryInfo directoryInfo, out FileInfo[] files,
        out DirectoryInfo[] directories)
    {
        try
        {
            files = directoryInfo.GetFiles();
            directories = directoryInfo.GetDirectories();
        }
        catch
        {
            files = [];
            directories = [];
            return true;
        }

        return false;
    }

    private static bool IsDirectoryEmpty(FileInfo[] files, DirectoryInfo[] directories)
    {
        return files.Length == 0 && directories.Length == 0;
    }

    private static bool ShouldSkipDirectory(DirectoryInfo? directoryInfo, Configuration config)
    {
        return directoryInfo is null
               || !directoryInfo.Exists
               || config.Ignores.Contains(directoryInfo.FullName);
    }

    private static IEnumerable<JunkYard> FindJunkYards(
        DirectoryInfo[] directories,
        FileInfo[] files,
        string target
    )
    {
        var validDirectoryPaths =
            directories.Where(d => Sherlock.MatchesPattern(target, d.Name.AsSpan(), MatchType.Win32));

        foreach (var directory in validDirectoryPaths)
        {
            yield return new JunkYard(
                directory.FullName,
                CalculateDirectorySize(directory),
                JunkType.Directory
            );
        }

        var validFilePaths = files.Where(f => Sherlock.MatchesPattern(target, f.Name.AsSpan(), MatchType.Win32))
            .Select(x => x.FullName);
        foreach (var filePath in validFilePaths)
        {
            yield return new JunkYard(filePath, GetFileSize(filePath), JunkType.File);
        }
    }
}