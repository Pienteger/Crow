namespace Crow.Utilities;

public static class TimeConversionUtility
{
    /// <summary>
    /// Converts a time duration in milliseconds to a human-readable
    /// string representation with appropriate units (ms, s, min, h).
    /// </summary>
    /// <param name="ms">The time duration in milliseconds to be converted.</param>
    /// <returns>
    /// A string representing the time duration in the most suitable unit,
    /// rounded to two decimal places.
    /// </returns>
    public static string ToTimeText(this long ms)
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
