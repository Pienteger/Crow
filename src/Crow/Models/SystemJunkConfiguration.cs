using System.Globalization;

namespace Crow.Models;

public sealed class SystemJunkConfiguration
{
    public bool IsForceEnabled { get; init; }
    public bool IsDryRun { get; init; }
    public bool IncludeAll { get; init; }

    /// <summary>
    /// Overrides every location's own minimum file age when set.
    /// </summary>
    public TimeSpan? OlderThan { get; init; }

    public static SystemJunkConfiguration Parse(string[] args)
    {
        return new SystemJunkConfiguration
        {
            IsForceEnabled = args.IndexOf(Flags.FORCE) >= 0,
            IsDryRun = args.IndexOf(Flags.DRY_RUN) >= 0,
            IncludeAll = args.IndexOf(Flags.ALL) >= 0,
            OlderThan = ParseOlderThan(args)
        };
    }

    private static TimeSpan? ParseOlderThan(string[] args)
    {
        var index = args.IndexOf(Flags.OLDER_THAN);
        if (index == -1)
            return null;

        if (index + 1 >= args.Length
            || !double.TryParse(args[index + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var hours)
            || hours < 0)
        {
            throw new ArgumentException($"{Flags.OLDER_THAN} expects a number of hours.", Flags.OLDER_THAN);
        }

        return TimeSpan.FromHours(hours);
    }
}
