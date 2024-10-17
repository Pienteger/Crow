using System.Diagnostics;
using Crow.Helpers;
using Spectre.Console;

namespace Crow;

public static class Program
{
    private static bool isForceFlagEnabled;
    private static Configuration? parsedConfig;

    public static void Main(string[] args)
    {
        if (!ValidateArguments(args))
        {
            return;
        }

        if (parsedConfig is null)
        {
            return;
        }

        isForceFlagEnabled = args.IndexOf(Flags.FORCE) >= 0;

        try
        {
            DisplayOperationInfoTable(parsedConfig);
            var junkYards = PerformScanOperation(parsedConfig).ToList();

            if (junkYards.Count != 0)
            {
                DisplayJunkYardTable(junkYards);
                if (isForceFlagEnabled || AskForRemovalConfirmation())
                {
                    ExecuteRemoveOperation(junkYards);
                }
            }
            else
            {
                AnsiConsole.MarkupLine(Texts.NOT_TARGET_FOUND);
            }

            AnsiConsole.MarkupLine(Texts.THANK_YOU);
        }
        catch (Exception e)
        {
            DisplayExceptionMessages(e);
        }
    }

    private static bool ValidateArguments(string[] args)
    {
        if (args.Length == 0)
        {
            DisplayInvalidArgumentsMessage();
            return false;
        }

        if (Commands.HelpAliases.IndexOf(args[0]) >= 0)
        {
            DisplayHelpMessage();
            return false;
        }

        if (Commands.VersionAliases.IndexOf(args[0]) >= 0)
        {
            DisplayVersionMessage();
            return false;
        }

        try
        {
            // We make the args a single string because the shell fails to parse
            // into the valid segments when there is an argument that ends with \"
            // So we do segmentation manually.
            parsedConfig = Configuration.Parse(string.Join(" ", args));
        }
        catch (ArgumentException ae)
        {
            AnsiConsole.MarkupLine($"[red]Invalid Argument: [/]{ae.Message}");
            return false;
        }
        catch
        {
            DisplayInvalidArgumentsMessage();
            return false;
        }

        return true;
    }

    #region Display Things

    private static void DisplayInvalidArgumentsMessage()
    {
        AnsiConsole.MarkupLine(Texts.INVALID_ARGUMENT);
    }

    private static void DisplayHelpMessage()
    {
        AnsiConsole.MarkupLine(Texts.HELP_TEXT);
        AnsiConsole.WriteLine();
    }

    private static void DisplayVersionMessage()
    {
        const string message = "1.1.0";
        Console.WriteLine(message);
    }

    private static void DisplayExceptionMessages(Exception e)
    {
        var sb = new StringBuilder();
        sb.AppendLine(e.Message);
        while (e.InnerException is not null)
        {
            sb.AppendLine(e.InnerException.Message);
            e = e.InnerException;
        }

        AnsiConsole.MarkupLine(sb.ToString());
    }

    private static void DisplayOperationInfoTable(Configuration config)
    {
        static string EscapeBrackets(string input) => input.Replace("[", "[[").Replace("]", "]]");

        var table = new Table();
        table.AddColumns(string.Empty, string.Empty);
        // Add rows, escaping special characters as needed
        table.AddRow("Looking for", EscapeBrackets(config.LookFor));
        table.AddRow("In", EscapeBrackets(string.Join(", ", config.LookIns)));
        table.AddRow("To delete", EscapeBrackets(string.Join(", ", config.RemoveCandidates)));

        // Only add "Ignoring" row if there are any ignores
        if (config.Ignores.Any())
        {
            table.AddRow("Ignoring", EscapeBrackets(string.Join(", ", config.Ignores)));
        }

        table.AddRow("Force delete", isForceFlagEnabled ? "Enabled" : "Disabled");
        table.HideHeaders();

        AnsiConsole.Write(table);
    }

    private static void DisplayJunkYardTable(IEnumerable<JunkYard> junkYards)
    {
        var table = new Table();
        table.AddColumn("🔢");
        table.AddColumn("Path");
        table.AddColumn("Size");

        var c = 0;
        long totalSize = 0;
        foreach (var item in junkYards)
        {
            table.AddRow($"{++c}", $"{item.Icon} {item.Path}", item.Size.ToUnitText());
            totalSize += item.Size;
        }

        table.AddEmptyRow();
        var sizeSumWithUnit = totalSize.ToUnitText();
        table.AddRow(string.Empty, "[yellow]Total[/]", $"[yellow]~{sizeSumWithUnit}[/]");
        AnsiConsole.Write(table);
    }

    #endregion

    #region Do Things

    private static IEnumerable<JunkYard> PerformScanOperation(Configuration config)
    {
        List<JunkYard> junkYards = [];
        var stopwatch = new Stopwatch();

        AnsiConsole
            .Status()
            .Start(
                "Scanning...",
                _ =>
                {
                    stopwatch.Start();
                    junkYards = config.ScanJunkYards().ToList();
                }
            );

        stopwatch.Stop();
        var time = stopwatch.ElapsedMilliseconds.ToTimeText();
        var rule = new Rule($"[green]Scan finished. {time} taken[/]")
        {
            Justification = Justify.Left
        };

        AnsiConsole.Write(rule);

        return junkYards.Distinct();
    }

    private static bool AskForRemovalConfirmation()
    {
        return AnsiConsole.Confirm("❗❗❗Remove these items?", false);
    }

    private static void ExecuteRemoveOperation(IEnumerable<JunkYard> junkYards)
    {
        AnsiConsole
            .Status()
            .Start(
                "Removing junks...",
                _ =>
                {
                    junkYards.CleanJunkYards();
                }
            );
        AnsiConsole.MarkupLine($"[green]Done![/]");
    }

    #endregion
}
