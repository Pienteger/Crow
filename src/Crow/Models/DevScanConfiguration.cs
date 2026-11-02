namespace Crow.Models;

public sealed class DevScanConfiguration
{
    public required IReadOnlyList<DevPreset> Presets { get; init; }
    public required IReadOnlyList<string> LookIns { get; init; }
    public required IReadOnlyList<string> Ignores { get; init; }

    public bool IsForceEnabled { get; init; }

    public static DevScanConfiguration Parse(string argsStr)
    {
        if (string.IsNullOrWhiteSpace(argsStr))
        {
            throw new ArgumentException(
                "Argument string cannot be null or empty.",
                nameof(argsStr)
            );
        }

        return Parse(argsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Parses args as passed to Main, so a single arg may hold a path with spaces.
    /// </summary>
    public static DevScanConfiguration Parse(string[] args)
    {
        // Drop the command itself, otherwise a missing optional flag
        // would read it as a value.
        args = args
            .SkipWhile(a => !a.StartsWith('-') || Commands.DevCleanAliases.IndexOf(a) >= 0)
            .ToArray();

        return new DevScanConfiguration
        {
            Presets = DevPresets.All,
            LookIns = ScanDeleteConfiguration.ParseFlagParts(Flags.IN, args),
            Ignores = ScanDeleteConfiguration.ParseFlagParts(Flags.IG, args),
            IsForceEnabled = args.IndexOf(Flags.FORCE) >= 0
        };
    }
}
