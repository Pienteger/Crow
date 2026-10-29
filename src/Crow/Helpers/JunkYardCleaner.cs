using Spectre.Console;

namespace Crow.Helpers;

public static class JunkYardCleaner
{
    /// <summary>
    /// Removes every junkyard, keeps going past failures, and returns the totals.
    /// </summary>
    public static CleanResult CleanJunkYards(this IEnumerable<JunkYard> junkYards)
    {
        long freed = 0;
        int deleted = 0, skipped = 0;

        foreach (var junkYard in junkYards)
        {
            var path = Markup.Escape(junkYard.Path);
            CleanResult result;

            if (junkYard.JunkType == JunkType.File)
            {
                AnsiConsole.MarkupLine($"Removing file: {path}");
                result = SafeFileSystem.TryDeleteFile(junkYard.Path)
                    ? new CleanResult(junkYard.Size, 1, 0)
                    : new CleanResult(0, 0, 1);
            }
            else
            {
                AnsiConsole.MarkupLine($"Removing directory: {path}");
                result = SafeFileSystem.DeleteTree(junkYard.Path);
            }

            if (result.SkippedCount > 0)
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]  {result.SkippedCount:N0} item(s) could not be removed from {path}[/]");
            }

            freed += result.FreedBytes;
            deleted += result.DeletedCount;
            skipped += result.SkippedCount;
        }

        return new CleanResult(freed, deleted, skipped);
    }
}
