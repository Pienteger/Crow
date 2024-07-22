namespace Crow;

public static class Utilities
{
    public static string GetSizeWithUnit(this long size)
    {
        return size switch
        {
            < 1024 => $"{size} B",
            < 1024 * 1024 => $"{Math.Round(size / 1024.0, 2)} KiB",
            < 1024 * 1024 * 1024 => $"{Math.Round(size / 1024.0 / 1024.0, 2)} MiB",
            _ => $"{Math.Round(size / 1024.0 / 1024.0 / 1024.0, 2)} GiB"
        };
    }

    public static string GetTimeFromMs(this long ms)
    {
        return ms switch
        {
            < 1000 => $"{ms}ms",
            < 1000 * 60 => $"{Math.Round(ms / 1_000.0, 2)}s",
            < 1000 * 60 * 60 => $"{Math.Round(ms / 60_000.0, 2)}min",
            _ => $"{Math.Round(ms / 3_600_000.0, 2)}h",
        };
    }
}
