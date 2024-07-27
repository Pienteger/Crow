using Spectre.Console;

namespace Crow.Helpers
{
    public static class JunkYardCleaner
    {
        public static void CleanJunkYards(this IEnumerable<JunkYard> junkYards)
        {
            foreach (var junkYard in junkYards)
            {
                var rmTarget = junkYard.Path;
                try
                {
                    if (File.Exists(rmTarget))
                    {
                        AnsiConsole.MarkupLine($"Removing file: {rmTarget}");
                        File.Delete(rmTarget);
                    }
                    else if (Directory.Exists(rmTarget))
                    {
                        AnsiConsole.MarkupLine($"Removing directory: {rmTarget}");
                        Directory.Delete(rmTarget, true);
                    }
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLine($"[red]Error: {e.Message}.[/]");
                    AnsiConsole.MarkupLine($"Ignoring {rmTarget}");
                }
            }
        }
    }
}
