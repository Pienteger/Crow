using Crow.Helpers;
using Crow.Models;
using Spectre.Console;
using System.Diagnostics;
using System.Text;

namespace Crow;

class Program
{
    private static bool IsForceFlagEnabled;
    private static Configuration? ParsedConfig;

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            DisplayInvalidArgumentsMessage();
        }
        else if (Commands.HelpAliases.IndexOf(args[0]) >= 0)
        {
            DisplayHelpMessage();
        }
        else if (Commands.VersionAliases.IndexOf(args[0]) >= 0)
        {
            DisplayVersionMessage();
        }
        else
        {
            try
            {
                // We make the args a single string because the shell fails to parse 
                // into the valid segments when there is an argument ends with \"
                // So we do segmentation manually.
                ParsedConfig = Configuration.Parse(string.Join(" ", args));
            }
            catch (ArgumentException ae)
            {
                AnsiConsole.MarkupLine($"[red]Invalid Argument: [/]{ae.Message}");
            }
            catch
            {
                DisplayInvalidArgumentsMessage();
            }
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
                    ExecuteRemoveOperation(junkYards.Select(x => x.Path));
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[Cyan]0 potential target found. You are good to go.[/]");
            }
            AnsiConsole.MarkupLine("Thanks for using our app.");
        }
        catch (Exception e)
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
    }

    #region Display Things

    private static void DisplayInvalidArgumentsMessage()
    {
        const string message = "Invalid arguments. To see help, pass the argument 'help' or 'h'.";
        Console.WriteLine(message);
    }

    private static void DisplayHelpMessage()
    {
        string message = $"""
            Usage:      {Flags.LF} <file> {Flags.IN} <directory> ... {Flags.RM} <file> ...
            Example:    [gray]{Flags.LF} *csproj {Flags.IN} C:\Users\{Environment.UserName}\source\repos {Flags.RM} bin obj[/]

            Flags:
              [yellow]{Flags.LF}[/]         Files or directories to look for. Must be single input. Supports Regex.
              [yellow]{Flags.IN}[/]         Directories to scan. Supports multiple input.
              [yellow]{Flags.RM}[/]         Files or directories to remove. Supports multiple input. Supports Regex.
              [red]{Flags.Force}[/]      Removes files without asking for confirmation. Use this at your own risk.

            Misc:
              [yellow]help[/]         Display help message
              [yellow]version[/]      Display app version
              
            Note: 
              1. All flags except {Flags.Force} are [red]required[/].
              2. Flags are case insensitive. 
              3. Using [red]{Flags.Force}[/] is not recommended.

            Credits:
              By  Mahmudul Hasan (https://mahmudx.com)
              For Pienteger® (https://pienteger.com) Open-Source Softworks
            """;
        AnsiConsole.MarkupLine(message);
        AnsiConsole.WriteLine();
    }

    private static void DisplayVersionMessage()
    {
        const string message = "1.0.0";
        Console.WriteLine(message);
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
        foreach (var item in junkYards)
        {
            table.AddRow($"{++c}", $"{item.Icon} {item.Path}", item.Size.GetSizeWithUnit());
        }

        table.AddEmptyRow();
        var sizeSumWithUnit = junkYards.Sum(x => x.Size).GetSizeWithUnit();
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
                    foreach (var item in config.LookIns)
                    {
                        var yards = item.GetJunkYardsFromConfig(config);
                        if (yards != null)
                        {
                            junkYards.AddRange(yards);
                        }
                    }
                }
            );

        stopwatch.Stop();
        var time = stopwatch.ElapsedMilliseconds.GetTimeFromMs();
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

    private static void ExecuteRemoveOperation(IEnumerable<string> paths)
    {
        AnsiConsole
            .Progress()
            .Start(ctx =>
            {
                // Define tasks
                var task1 = ctx.AddTask("[red]Removing items:[/]");
                var length = paths.Count();
                foreach (var rmTarget in paths)
                {
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
                    task1.Increment(100 / length);
                }
            });

        AnsiConsole.MarkupLine($"[green]Done![/]");
    }

    #endregion
}
