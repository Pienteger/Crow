namespace Crow;

internal static class Texts
{
    public static readonly string HELP_TEXT = $"""
            Usage:      {Flags.LF} <file> {Flags.IN} <directory> ... {Flags.RM} <file> ...
            Example:    [gray]{Flags.LF} *csproj {Flags.IN} C:\Users\{Environment.UserName}\source\repos {Flags.RM} bin obj[/]
                        [gray]{Flags.LF} *csproj {Flags.IN} C:\Users\{Environment.UserName}\source\repos {Flags.RM} bin obj {Flags.IG} AppData[/]

            Flags:
            [yellow]{Flags.LF}[/]         Files or directories to look for. Must be single input. Supports Regex.
            [yellow]{Flags.IN}[/]         Directories to scan. Supports multiple input.
            [yellow]{Flags.RM}[/]         Files or directories to remove. Supports multiple input. Supports Regex.
            [yellow]{Flags.IG}[/]         Files or directories to ignore. Supports multiple input.
            [red]{Flags.FORCE}[/]      Removes files without asking for confirmation. Use this at your own risk.

            Misc:
            [yellow]help[/]         Display help message
            [yellow]version[/]      Display app version
              
            Note: 
            1. All flags except {Flags.FORCE} are [red]required[/].
            2. Flags are case insensitive. 
            3. Using [red]{Flags.FORCE}[/] is not recommended.

            Credits:
            By  Mahmudul Hasan (https://mahmudx.com)
            For Pienteger® (https://pienteger.com) Open-Source Softworks
        """;
    public const string NOT_TARGET_FOUND = "[Cyan]0 potential target found. You are good to go.[/]";
    public const string THANK_YOU = "Thanks for using our app.";
    public const string INVALID_ARGUMENT =
        "Invalid arguments. To see help, pass the argument 'help' or 'h'.";
}
