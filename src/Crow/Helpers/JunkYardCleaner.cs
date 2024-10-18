using Spectre.Console;

namespace Crow.Helpers;

public static class JunkYardCleaner
{
    public static void CleanJunkYards(this IEnumerable<JunkYard> junkYards)
    {
        foreach (var junkYard in junkYards.Select(j=>j.Path))
        {
            try
            {
                if (File.Exists(junkYard))
                {
                    AnsiConsole.MarkupLine($"Removing file: {junkYard}");
                    File.Delete(junkYard);
                }
                else if (Directory.Exists(junkYard))
                {
                    AnsiConsole.MarkupLine($"Removing directory: {junkYard}");
                    Directory.Delete(junkYard, true);
                }
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"[red]Error: {e.Message}.[/]");
                AnsiConsole.MarkupLine($"Ignoring {junkYard}");
            }
        }
    }
}
