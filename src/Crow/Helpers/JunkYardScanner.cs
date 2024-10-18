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
        foreach (var path in config.LookIns)
        {
            if (string.IsNullOrEmpty(path))
                continue;

            var directoryInfo = new DirectoryInfo(path);

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
        if (
            directoryInfo is null
            || !directoryInfo.Exists
            || config.Ignores.Contains(directoryInfo.FullName)
        )
            yield break;

        FileInfo[] files;
        DirectoryInfo[] directories;
        try
        {
            files = directoryInfo.GetFiles();
            directories = directoryInfo.GetDirectories();
        }
        catch
        {
            yield break;
        }

        if (files.Length == 0 && directories.Length == 0)
            yield break;

        var isJunkFolder = false;

        // Search through all the files to find target directories or files where
        // certain identifiers exists

        foreach (var file in files)
        {
            if (!Sherlock.MatchesPattern(config.LookFor, file.Name.AsSpan(), MatchType.Win32))
                continue;
            isJunkFolder = true;

            foreach (var target in config.RemoveCandidates)
            {
                foreach (var junkYard in FindJunkYards(directories, files, target))
                {
                    yield return junkYard;
                }
            }

            break;
        }

        if (isJunkFolder)
            yield break;

        foreach (var directory in directories)
        {
            foreach (var junk in directory.GetJunkYards(config))
            {
                yield return junk;
            }
        }
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