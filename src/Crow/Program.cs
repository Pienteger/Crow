using System.Diagnostics;
using Crow.Helpers;
using Spectre.Console;

namespace Crow;

class Program
{
    private static bool IsForceFlagEnabled;
    private static Configuration? ParsedConfig;

    static void Main(string[] args)
    {
        if (!ValidateArguments(args))
        {
            return;
        }

        if (ParsedConfig is null)
        {
            return;
        }

        IsForceFlagEnabled = args.IndexOf(Flags.Force) >= 0;

        try
        {
            DisplayOperationInfoTable(ParsedConfig);
            var junkYards = PerformScanOperation(ParsedConfig);

            if (junkYards.Any())
            {
                DisplayJunkYardTable(junkYards);
                if (IsForceFlagEnabled || AskForRemovalConfirmation())
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
            ParsedConfig = Configuration.Parse(string.Join(" ", args));
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
        const string message = "1.0.0";
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
        var table = new Table();
        table.AddColumns(string.Empty, string.Empty);
        table.AddRow("Looking for", config.LookFor.Replace("[", "[[").Replace("]", "]]"));
        table.AddRow("In", string.Join(", ", config.LookIns).Replace("[", "[[").Replace("]", "]]"));
        table.AddRow(
            "To delete",
            string.Join(", ", config.RemoveCandidates).Replace("[", "[[").Replace("]", "]]")
        );
        table.AddRow("Force delete", IsForceFlagEnabled ? "Enabled" : "Disabled");
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
                ctx =>
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
                ctx =>
                {
                    junkYards.CleanJunkYards();
                }
            );
        AnsiConsole.MarkupLine($"[green]Done![/]");
    }

    #endregion
}
