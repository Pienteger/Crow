using Crow.Models;

namespace Crow.Helpers;

public static class Lasterd
{
    private static long GetFileSize(string filePath)
    {
        if (File.Exists(filePath))
            return new FileInfo(filePath).Length;
        return default;
    }

    private static long GetDirectorySize(DirectoryInfo directory)
    {
        var startDirectorySize = default(long);
        if (directory is not { Exists: true })
            return startDirectorySize; //Return 0 while Directory does not exist.

        //Add size of files in the Current Directory to main size.
        foreach (FileInfo fileInfo in directory.GetFiles())
        {
            Interlocked.Add(ref startDirectorySize, fileInfo.Length);
        }

        Parallel.ForEach(
            directory.GetDirectories(),
            subDirectory => Interlocked.Add(ref startDirectorySize, GetDirectorySize(subDirectory))
        );

        return startDirectorySize; //Return full Size of this Directory.
    }

    public static IEnumerable<JunkYard> GetJunkYardsFromConfig(
        this string? directory,
        Configuration config
    )
    {
        if (string.IsNullOrEmpty(directory))
            yield break;
        var directoryInfo = new DirectoryInfo(directory);

        foreach (JunkYard junk in directoryInfo.GetJunkYardsFromConfig(config))
        {
            yield return junk;
        }
    }

    private static IEnumerable<JunkYard> GetJunkYardsFromConfig(
        this DirectoryInfo? directoryInfo,
        Configuration config
    )
    {
        if (directoryInfo is null)
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

        foreach (FileInfo file in files)
        {
            if (!Sherlock.MatchesPattern(config.LookFor, file.Name.AsSpan(), MatchType.Win32))
                continue;

            isJunkFolder = true;

            foreach (string target in config.RemoveCandidates)
            {
                foreach (DirectoryInfo directory in directories)
                {
                    bool matches = Sherlock.MatchesPattern(
                        target,
                        directory.Name.AsSpan(),
                        MatchType.Win32
                    );
                    if (!matches)
                        continue;
                    yield return new JunkYard(
                        directory.FullName,
                        GetDirectorySize(new DirectoryInfo(directory.FullName)),
                        JunkType.Directory
                    );
                }

                foreach (FileInfo fileInfo in files)
                {
                    bool matches = Sherlock.MatchesPattern(
                        target,
                        fileInfo.Name.AsSpan(),
                        MatchType.Win32
                    );

                    if (!matches)
                        continue;

                    yield return new JunkYard(
                        fileInfo.FullName,
                        GetFileSize(fileInfo.FullName),
                        JunkType.File
                    );
                }
            }
            break;
        }

        if (isJunkFolder)
            yield break;

        foreach (DirectoryInfo directory in directories)
        {
            foreach (JunkYard junk in directory.GetJunkYardsFromConfig(config))
            {
                yield return junk;
            }
        }
    }
}
