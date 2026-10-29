namespace Crow.Models;

public static class Commands
{
    public static readonly string[] HelpAliases = ["help", "h", "-h", "--help"];
    public static readonly string[] VersionAliases = ["version", "v", "-v", "--version"];

    public static readonly string[] DeleteSystemJunkAliases =
        ["clean", "delete_system_junk", "dsj", "--dsj"];

    public static readonly string[] DevCleanAliases = ["dev", "--dev"];
}
