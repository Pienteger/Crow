namespace Crow.Models;

public static class Commands
{
    public const string History = "history";
    public const string List = "list";
    public const string Remove = "remove";
    public const string Repeat = "repeat";

    public static readonly string[] HelpAliases = ["help", "h", "-h", "--help"];
    public static readonly string[] VersionAliases = ["version", "v", "-v", "--version"];
}