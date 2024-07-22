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

    /// <summary>
    /// Case insensitive string comparision 
    /// </summary>
    /// <param name="str1"></param>
    /// <param name="str2"></param>
    /// <returns></returns>
    public static bool IsEqual(this string str1, string str2)
    {
        return str1.Equals(str2, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Searches for the specified string ignoring the case and returns the index of its
    /// first occurrence in a one-dimensional array.
    /// </summary>
    /// <param name="array">The one-dimensional, zero-based array to search.</param>
    /// <param name="value">The object to locate in array.</param>
    /// <returns>
    /// The zero-based index of the first occurrence of value in the entire array, if
    /// found; otherwise, -1.
    /// </returns>
    public static int IndexOf(this string[] array, string value)
    {
        if (array is null || array.Length == 0)
            return -1;

        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].IsEqual(value))
                return i;
        }
        return -1;
    }
}
