namespace Crow;

internal static class Texts
{
    public static readonly string HELP_TEXT = $"""
            Usage:      crow clean [{Flags.DRY_RUN}] [{Flags.ALL}] [{Flags.OLDER_THAN} <hours>] [{Flags.FORCE}]
                        crow dev {Flags.IN} <directory> ... [{Flags.IG} <directory> ...] [{Flags.FORCE}]
                        crow {Flags.LF} <file> {Flags.IN} <directory> ... {Flags.RM} <file> ...

            Example:    [gray]crow clean[/]
                        [gray]crow clean {Flags.DRY_RUN}[/]
                        [gray]crow dev {Flags.IN} C:\Users\{Environment.UserName}\source[/]
                        [gray]{Flags.LF} *csproj {Flags.IN} C:\Users\{Environment.UserName}\source\repos {Flags.RM} bin obj[/]
                        [gray]{Flags.LF} *csproj {Flags.IN} C:\Users\{Environment.UserName}\source\repos {Flags.RM} bin obj {Flags.IG} AppData[/]

            Commands:
            [yellow]clean[/]        Clean system junk: temp files, crash dumps, error reports, browser and app
                         caches, and (opt-in) Windows Update downloads and package manager caches.
                         You pick what to clean from a list; safe locations are preselected.
            [yellow]dev[/]          Find build output of .NET, Node.js, Rust, Maven, Gradle, Python, Flutter
                         and CMake projects (bin, obj, node_modules, target, build, ...) in one pass.

            Flags:
            [yellow]{Flags.LF}[/]         Files or directories to look for. Must be single input. Supports Regex.
            [yellow]{Flags.IN}[/]         Directories to scan. Supports multiple input.
            [yellow]{Flags.RM}[/]         Files or directories to remove. Supports multiple input. Supports Regex.
            [yellow]{Flags.IG}[/]         Files or directories to ignore. Supports multiple input.
            [yellow]{Flags.DRY_RUN}[/]    clean: only report what could be freed.
            [yellow]{Flags.ALL}[/]        clean: preselect every location, not only the safe ones.
            [yellow]{Flags.OLDER_THAN}[/] clean: only remove files untouched for this many hours.
            [red]{Flags.FORCE}[/]      Removes files without asking for confirmation. Use this at your own risk.

            Misc:
            [yellow]help[/]         Display help message
            [yellow]version[/]      Display app version

            Note:
            1. {Flags.LF}, {Flags.IN} and {Flags.RM} are [red]required[/] for a custom scan.
            2. Flags are case insensitive.
            3. Using [red]{Flags.FORCE}[/] is not recommended.
            4. Close your browsers before [yellow]clean[/] to free their caches; files in use are skipped.

            Credits:
            By  Mahmudul Hasan (https://mahmudx.com)
            For Pienteger® (https://pienteger.com) Open-Source Softworks
        """;
    public const string NO_TARGET_FOUND = "[Cyan]0 potential target found. You are good to go.[/]";
    public const string THANK_YOU = "Thanks for using our app.";
    public const string INVALID_ARGUMENT =
        "Invalid arguments. To see help, pass the argument 'help' or 'h'.";
    public const string DRY_RUN = "[grey]Dry run: nothing was removed.[/]";
    public const string NOTHING_SELECTED = "[grey]Nothing selected. Nothing was removed.[/]";
    public const string NOT_INTERACTIVE =
        "[yellow]The console is not interactive. Re-run with --force to clean the default selection.[/]";
    public const string SKIPPED_FILES_HINT =
        "[grey]Skipped files are in use or need admin rights. Close the apps using them or run from an elevated shell.[/]";
}
