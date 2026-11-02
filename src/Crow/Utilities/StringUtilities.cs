namespace Crow.Utilities;

public static class StringUtilities
{
    /// <summary>
    /// Removes wrapping quotes (both single and double quotes) from
    /// the beginning and end of the specified string.
    /// </summary>
    /// <param name="text">The string from which to remove wrapping quotes.</param>
    /// <returns>
    /// A string with the wrapping quotes removed,
    /// or the original string if it does not start and end with quotes.
    /// </returns>
    public static string RemoveWrappingQuotes(this string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length < 2)
            return text;

        // Check if both the start and end have matching quotes
        if (
            (text.StartsWith('"') && text.EndsWith('"'))
            || (text.StartsWith('\'') && text.EndsWith('\''))
        )
        {
            // Remove both the starting and ending quote
            text = text[1..^1];
        }

        return text;
    }

    /// <summary>
    /// Undoes what Windows does to a quoted path that ends with a backslash.
    /// <c>"C:\My Projects\" --force</c> arrives as the single arg
    /// <c>C:\My Projects" --force</c>, because <c>\"</c> is read as an escaped quote.
    /// Paths cannot contain <c>"</c>, so the text before it is the path and the
    /// rest holds the args it swallowed.
    /// </summary>
    /// <param name="args">The args as passed to Main.</param>
    /// <returns>The args with the path and the swallowed args split apart again.</returns>
    public static string[] RepairQuotedArgs(this string[] args)
    {
        var result = new List<string>(args.Length);
        foreach (var arg in args)
        {
            // An empty arg such as --in "" carries no value.
            if (string.IsNullOrWhiteSpace(arg))
                continue;

            var quote = arg.IndexOf('"');
            if (quote == -1)
            {
                result.Add(arg);
                continue;
            }

            if (quote > 0)
                result.Add(arg[..quote]);

            result.AddRange(arg[(quote + 1)..].Split([' ', '"'], StringSplitOptions.RemoveEmptyEntries));
        }

        return result.ToArray();
    }

    /// <summary>
    /// Compares two strings for equality, ignoring case differences.
    /// </summary>
    /// <param name="str1">The first string to compare.</param>
    /// <param name="str2">The second string to compare.</param>
    /// <returns>True if the strings are equal ignoring case, otherwise false.</returns>
    public static bool IsEqual(this string str1, string str2)
    {
        return str1.Equals(str2, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Finds the index of the specified value in the string array,
    /// using case-insensitive comparison.
    /// </summary>
    /// <param name="array">The array to search.</param>
    /// <param name="value">The value to find in the array.</param>
    /// <returns>The index of the value in the array if found; otherwise, -1.</returns>
    public static int IndexOf(this string[] array, string value)
    {
        if (array is null || array.Length == 0)
            return -1;

        for (var i = 0; i < array.Length; i++)
        {
            if (array[i].IsEqual(value))
                return i;
        }
        return -1;
    }
}
