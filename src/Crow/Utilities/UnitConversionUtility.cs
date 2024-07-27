namespace Crow.Utilities;

public static class UnitConversionUtility
{
    /// <summary>
    /// Converts a size in bytes to a human-readable
    /// string representation with appropriate units (B, KiB, MiB, GiB).
    /// </summary>
    /// <param name="size">The size in bytes to be converted.</param>
    /// <returns>
    /// A string representing the size in the most suitable unit,
    /// rounded to two decimal places.
    /// </returns>
    public static string ToUnitText(this long size)
    {
        return size switch
        {
            < 1024 => $"{size} B",
            < 1024 * 1024 => $"{Math.Round(size / 1024.0, 2)} KiB",
            < 1024 * 1024 * 1024 => $"{Math.Round(size / 1024.0 / 1024.0, 2)} MiB",
            _ => $"{Math.Round(size / 1024.0 / 1024.0 / 1024.0, 2)} GiB"
        };
    }
}
