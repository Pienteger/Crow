using System.Diagnostics;
using System.Reflection;
using Crow.Helpers;
using Spectre.Console;

namespace Crow;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;

            if (TryExecuteBasicRequest(args))
            {
                return;
            }

            if (TryExecuteSystemJunkDeleteRequest(args))
            {
                return;
            }

            if (TryExecuteDevCleanRequest(args))
            {
                return;
            }

            if (TryParseScanDeleteConfiguration(args: args, out var parsedConfig))
            {
                if (parsedConfig is null)
                {
                    DisplayInvalidArgumentsMessage();
                    return;
                }

                DisplayOperationInfoTable(parsedConfig);
                var junkYards = PerformScanOperation(parsedConfig.ScanJunkYards).ToList();
                ReviewAndRemove(junkYards, parsedConfig.IsForceEnabled);
            }
        }
        catch (Exception e)
        {
            DisplayExceptionMessages(e);
        }

        AnsiConsole.MarkupLine(Texts.THANK_YOU);
    }

    private static bool TryExecuteBasicRequest(string[] args)
    {
        if (args.Length == 0)
        {
            DisplayInvalidArgumentsMessage();
            return true;
        }

        if (Commands.HelpAliases.IndexOf(args[0]) >= 0)
        {
            DisplayHelpMessage();
            return true;
        }

        if (Commands.VersionAliases.IndexOf(args[0]) >= 0)
        {
            DisplayVersionMessage();
            return true;
        }

        return false;
    }

    private static bool TryParseScanDeleteConfiguration(string[] args, out ScanDeleteConfiguration? parsedConfig)
    {
        parsedConfig = null;
        try
        {
            // We make the args a single string because the shell fails to parse
            // into the valid segments when there is an argument that ends with \"
            // So we do segmentation manually.
            parsedConfig = ScanDeleteConfiguration.Parse(string.Join(" ", args));
            return true;
        }
        catch (ArgumentException ae)
        {
            DisplayInvalidArgumentMessage(ae);
            return false;
        }
        catch
        {
            DisplayInvalidArgumentsMessage();
            return false;
        }
    }

    private static bool TryExecuteSystemJunkDeleteRequest(string[] args)
    {
        if (Commands.DeleteSystemJunkAliases.IndexOf(args[0]) < 0)
        {
            return false;
        }

        SystemJunkConfiguration config;
        try
        {
            config = SystemJunkConfiguration.Parse(args);
        }
        catch (ArgumentException ae)
        {
            DisplayInvalidArgumentMessage(ae);
            return true;
        }

        var scans = PerformSystemJunkScanOperation(config);
        if (scans.Count == 0)
        {
            AnsiConsole.MarkupLine(Texts.NO_TARGET_FOUND);
            return true;
        }

        DisplaySystemJunkTable(scans);

        if (config.IsDryRun)
        {
            AnsiConsole.MarkupLine(Texts.DRY_RUN);
            return true;
        }

        var selected = SelectSystemJunk(scans, config);
        if (selected.Count == 0)
        {
            AnsiConsole.MarkupLine(Texts.NOTHING_SELECTED);
            return true;
        }

        if (config.IsForceEnabled || AskForRemovalConfirmation())
        {
            ExecuteSystemJunkCleanOperation(selected);
        }

        return true;
    }

    private static bool TryExecuteDevCleanRequest(string[] args)
    {
        if (Commands.DevCleanAliases.IndexOf(args[0]) < 0)
        {
            return false;
        }

        DevScanConfiguration config;
        try
        {
            config = DevScanConfiguration.Parse(string.Join(" ", args));
        }
        catch (ArgumentException ae)
        {
            DisplayInvalidArgumentMessage(ae);
            return true;
        }

        DisplayDevOperationInfoTable(config);
        var junkYards = PerformScanOperation(config.ScanJunkYards).ToList();
        ReviewAndRemove(junkYards, config.IsForceEnabled);
        return true;
    }

    #region Display Things

    private static void DisplayInvalidArgumentsMessage()
    {
        AnsiConsole.MarkupLine(Texts.INVALID_ARGUMENT);
    }

    private static void DisplayInvalidArgumentMessage(ArgumentException ae)
    {
        AnsiConsole.MarkupLine($"[red]Invalid Argument: [/]{Markup.Escape(ae.Message)}");
    }

    private static void DisplayHelpMessage()
    {
        AnsiConsole.MarkupLine(Texts.HELP_TEXT);
        AnsiConsole.WriteLine();
    }

    private static void DisplayVersionMessage()
    {
        // Set by <Version> in the csproj, or -p:Version=... in the release workflow.
        var version = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";

        // Drop the "+<commit sha>" suffix the SDK appends.
        Console.WriteLine(version.Split('+')[0]);
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

        AnsiConsole.MarkupLine(Markup.Escape(sb.ToString()));
    }

    private static void DisplayOperationInfoTable(ScanDeleteConfiguration config)
    {
        var table = new Table();
        table.AddColumns(string.Empty, string.Empty);
        // Add rows, escaping special characters as needed
        table.AddRow("Looking for", Markup.Escape(config.LookFor));
        table.AddRow("In", Markup.Escape(string.Join(", ", config.LookIns)));
        table.AddRow("To delete", Markup.Escape(string.Join(", ", config.RemoveCandidates)));

        // Only add "Ignoring" row if there are any ignores
        if (config.Ignores.Any())
        {
            table.AddRow("Ignoring", Markup.Escape(string.Join(", ", config.Ignores)));
        }

        table.AddRow("Force delete", config.IsForceEnabled ? "Enabled" : "Disabled");
        table.HideHeaders();

        AnsiConsole.Write(table);
    }

    private static void DisplayDevOperationInfoTable(DevScanConfiguration config)
    {
        var table = new Table();
        table.AddColumns(string.Empty, string.Empty);
        table.AddRow("Project types", string.Join(", ", config.Presets.Select(p => p.Name).Distinct()));
        table.AddRow("In", Markup.Escape(string.Join(", ", config.LookIns)));

        if (config.Ignores.Any())
        {
            table.AddRow("Ignoring", Markup.Escape(string.Join(", ", config.Ignores)));
        }

        table.AddRow("Force delete", config.IsForceEnabled ? "Enabled" : "Disabled");
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
            var label = item.Label is null ? string.Empty : $" [grey]({Markup.Escape(item.Label)})[/]";
            table.AddRow($"{++c}", $"{item.Icon} {Markup.Escape(item.Path)}{label}", item.Size.ToUnitText());
            totalSize += item.Size;
        }

        table.AddEmptyRow();
        var sizeSumWithUnit = totalSize.ToUnitText();
        table.AddRow(string.Empty, "[yellow]Total[/]", $"[yellow]~{sizeSumWithUnit}[/]");
        AnsiConsole.Write(table);
    }

    private static void DisplaySystemJunkTable(IReadOnlyList<SystemJunkScanResult> scans)
    {
        var table = new Table();
        table.AddColumns("🔢", "Category", "Location", "Files", "Size", "Default");

        var c = 0;
        long totalSize = 0;
        long defaultSize = 0;
        foreach (var scan in scans)
        {
            var location = scan.Location;
            var admin = location.RequiresAdmin ? " [grey](admin)[/]" : string.Empty;
            table.AddRow(
                $"{++c}",
                Markup.Escape(location.Category),
                $"{Markup.Escape(location.Name)}{admin}",
                scan.FileCount.ToString("N0"),
                scan.Size.ToUnitText(),
                location.SelectedByDefault ? "[green]✓[/]" : "[grey]-[/]"
            );
            totalSize += scan.Size;
            defaultSize += location.SelectedByDefault ? scan.Size : 0;
        }

        table.AddEmptyRow();
        table.AddRow(string.Empty, string.Empty, "[yellow]Total[/]", string.Empty, $"[yellow]~{totalSize.ToUnitText()}[/]", string.Empty);
        table.AddRow(string.Empty, string.Empty, "[green]Default selection[/]", string.Empty, $"[green]~{defaultSize.ToUnitText()}[/]", string.Empty);
        AnsiConsole.Write(table);
    }

    private static void DisplayCleanResultTable(IReadOnlyList<(SystemJunkScanResult Scan, CleanResult Result)> results)
    {
        var table = new Table();
        table.AddColumns("Location", "Freed", "Removed", "Skipped");

        long totalFreed = 0;
        foreach (var (scan, result) in results.OrderByDescending(r => r.Result.FreedBytes))
        {
            table.AddRow(
                Markup.Escape(scan.Location.Name),
                result.FreedBytes.ToUnitText(),
                result.DeletedCount.ToString("N0"),
                result.SkippedCount == 0 ? "0" : $"[yellow]{result.SkippedCount:N0}[/]"
            );
            totalFreed += result.FreedBytes;
        }

        table.AddEmptyRow();
        table.AddRow("[green]Total freed[/]", $"[green]{totalFreed.ToUnitText()}[/]", string.Empty, string.Empty);
        AnsiConsole.Write(table);
    }

    private static void DisplaySavedSpaceMessage(CleanResult total, TimeSpan elapsed, IEnumerable<string> paths)
    {
        var lines = new List<string>
        {
            total.FreedBytes > 0
                ? $"[bold green]🎉 You just freed {total.FreedBytes.ToUnitText()} of disk space![/]"
                : "[yellow]No disk space was freed.[/]",
            $"Removed {total.DeletedCount:N0} item(s) in {((long)elapsed.TotalMilliseconds).ToTimeText()}."
        };

        foreach (var drive in GetDrives(paths))
        {
            lines.Add($"Drive {Markup.Escape(drive.Name)} now has [green]{drive.AvailableFreeSpace.ToUnitText()}[/] free of {drive.TotalSize.ToUnitText()}.");
        }

        if (total.SkippedCount > 0)
        {
            lines.Add(string.Empty);
            lines.Add(string.Format(Texts.SKIPPED_FILES_HINT, total.SkippedCount));
        }

        var panel = new Panel(new Markup(string.Join(Environment.NewLine, lines)))
        {
            Header = new PanelHeader(" 🐦 Crow "),
            Border = BoxBorder.Rounded,
            Padding = new Padding(2, 1, 2, 1)
        };
        panel.BorderColor(total.SkippedCount > 0 ? Color.Yellow : Color.Green);

        AnsiConsole.Write(panel);
    }

    private static IEnumerable<DriveInfo> GetDrives(IEnumerable<string> paths)
    {
        foreach (var root in paths.Select(Path.GetPathRoot).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            DriveInfo? drive = null;
            try
            {
                drive = new DriveInfo(root);
                _ = drive.AvailableFreeSpace;
            }
            catch
            {
                drive = null;
            }

            if (drive is not null)
                yield return drive;
        }
    }

    #endregion

    #region Do Things

    private static IEnumerable<JunkYard> PerformScanOperation(Func<IEnumerable<JunkYard>> scan)
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
                    junkYards = scan().ToList();
                }
            );

        stopwatch.Stop();
        DisplayScanFinishedRule(stopwatch);

        return junkYards.Distinct().OrderByDescending(j => j.Size);
    }

    private static List<SystemJunkScanResult> PerformSystemJunkScanOperation(SystemJunkConfiguration config)
    {
        List<SystemJunkScanResult> scans = [];
        var stopwatch = new Stopwatch();

        AnsiConsole
            .Status()
            .Start(
                "Scanning system junk...",
                _ =>
                {
                    stopwatch.Start();
                    scans = SystemJunkCleaner
                        .MeasureAll(SystemJunkCatalog.GetLocations(), location => config.OlderThan ?? location.MinAge)
                        .Where(scan => scan.FileCount > 0)
                        .OrderByDescending(scan => scan.Size)
                        .ToList();
                }
            );

        stopwatch.Stop();
        DisplayScanFinishedRule(stopwatch);

        return scans;
    }

    private static void DisplayScanFinishedRule(Stopwatch stopwatch)
    {
        var time = stopwatch.ElapsedMilliseconds.ToTimeText();
        var rule = new Rule($"[green]Scan finished. {time} taken[/]") { Justification = Justify.Left };

        AnsiConsole.Write(rule);
    }

    private static List<SystemJunkScanResult> SelectSystemJunk(
        IReadOnlyList<SystemJunkScanResult> scans,
        SystemJunkConfiguration config
    )
    {
        var defaults = scans.Where(s => config.IncludeAll || s.Location.SelectedByDefault).ToList();
        if (config.IsForceEnabled)
        {
            return defaults;
        }

        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            AnsiConsole.MarkupLine(Texts.NOT_INTERACTIVE);
            return [];
        }

        var prompt = new MultiSelectionPrompt<SystemJunkScanResult>()
            .Title("Select what to clean:")
            .PageSize(20)
            .NotRequired()
            .InstructionsText("[grey](space to toggle, enter to accept)[/]")
            .UseConverter(s => Markup.Escape($"{s.Location.Name} ({s.Size.ToUnitText()})"))
            .AddChoices(scans);

        foreach (var scan in defaults)
        {
            prompt.Select(scan);
        }

        return AnsiConsole.Prompt(prompt);
    }

    private static void ReviewAndRemove(List<JunkYard> junkYards, bool isForceEnabled)
    {
        if (junkYards.Count == 0)
        {
            AnsiConsole.MarkupLine(Texts.NO_TARGET_FOUND);
            return;
        }

        DisplayJunkYardTable(junkYards);
        if (isForceEnabled || AskForRemovalConfirmation())
        {
            ExecuteRemoveOperation(junkYards);
        }
    }

    private static bool AskForRemovalConfirmation()
    {
        return AnsiConsole.Confirm("❗❗❗Remove these items?", false);
    }

    private static void ExecuteRemoveOperation(IReadOnlyList<JunkYard> junkYards)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CleanResult(0, 0, 0);
        AnsiConsole
            .Status()
            .Start(
                "Removing junks...",
                _ => { result = junkYards.CleanJunkYards(); }
            );
        stopwatch.Stop();

        DisplaySavedSpaceMessage(result, stopwatch.Elapsed, junkYards.Select(j => j.Path));
    }

    private static void ExecuteSystemJunkCleanOperation(IReadOnlyList<SystemJunkScanResult> scans)
    {
        List<(SystemJunkScanResult Scan, CleanResult Result)> results = [];
        var stopwatch = Stopwatch.StartNew();

        AnsiConsole
            .Status()
            .Start(
                "Removing junks...",
                ctx =>
                {
                    foreach (var scan in scans)
                    {
                        ctx.Status($"Cleaning {Markup.Escape(scan.Location.Name)}...");
                        results.Add((scan, scan.Clean()));
                    }
                }
            );

        stopwatch.Stop();

        DisplayCleanResultTable(results);

        var total = new CleanResult(
            results.Sum(r => r.Result.FreedBytes),
            results.Sum(r => r.Result.DeletedCount),
            results.Sum(r => r.Result.SkippedCount));
        DisplaySavedSpaceMessage(total, stopwatch.Elapsed, scans.SelectMany(s => s.Location.Paths));
    }

    #endregion
}
