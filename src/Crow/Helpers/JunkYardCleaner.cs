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
                    AnsiConsole.MarkupLine($"Removing file: {Markup.Escape(junkYard)}");
                    File.Delete(junkYard);
                }
                else if (Directory.Exists(junkYard))
                {
                    AnsiConsole.MarkupLine($"Removing directory: {Markup.Escape(junkYard)}");
                    Directory.Delete(junkYard, true);
                }
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(e.Message)}.[/]");
                AnsiConsole.MarkupLine($"Ignoring {Markup.Escape(junkYard)}");
            }
        }
    }
}
