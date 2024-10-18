namespace Crow.Models;

public sealed class Configuration
{
    public required string LookFor { get; init; }
    public required IReadOnlyList<string> RemoveCandidates { get; init; }
    public required IReadOnlyList<string> LookIns { get; init; }
    public required IReadOnlyList<string> Ignores { get; init; }

    public static Configuration Parse(string argsStr)
    {
        // Validate the argument string
        if (string.IsNullOrWhiteSpace(argsStr))
        {
            throw new ArgumentException(
                "Argument string cannot be null or empty.",
                nameof(argsStr)
            );
        }

        var args = argsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var lfValue = ParseLookForValue(args);
        var rmValues = ParseFlagParts(Flags.RM, args);
        var inValues = ParseFlagParts(Flags.IN, args);
        var igValues = ParseFlagParts(Flags.IG, args);

        return new Configuration
        {
            LookFor = lfValue,
            RemoveCandidates = rmValues,
            LookIns = inValues,
            Ignores = igValues
        };
    }

    private static string ParseLookForValue(string[] args)
    {
        var lfIndex = args.IndexOf(Flags.LF);
        if (lfIndex == -1 || lfIndex + 1 >= args.Length || args[lfIndex + 1].StartsWith('-'))
        {
            throw new ArgumentException($"{Flags.LF} value is required.", nameof(args));
        }

        return args[lfIndex + 1];
    }

    private static List<string> ParseFlagParts(string flag, string[] args)
    {
        var sIndex = args.IndexOf(flag);

        if (sIndex == -1 && flag != Flags.IG)
        {
            throw new ArgumentException($"{flag} value is required.", flag);
        }

        var values = new List<string>();

        for (var i = sIndex + 1; i < args.Length && !args[i].StartsWith('-'); i++)
        {
            ValidateFlag(flag, args[i]);
            var value = ParseComposite(ref i, args);

            // if the value is empty, skip it
            if (string.IsNullOrWhiteSpace(value))
                continue;

            // if flag is not RM, resolve relative path
            if (flag != Flags.RM && !Path.IsPathRooted(value))
            {
                value = Path.GetFullPath(value);
            }

            values.Add(value);
        }

        if (values.Count == 0 && flag != Flags.IG)
            throw new ArgumentException($"{flag} value is required.", flag);

        return values.Distinct().ToList();
    }

    private static string ParseComposite(ref int index, string[] args)
    {
        // Extract the first argument
        var path = args[index];
        var marker = path[0];

        // Handle quoted arguments
        if (marker is not ('\"' or '\''))
            return path;

        var sb = new StringBuilder();
        sb.Append(path);

        // Concatenate arguments until we find one that ends with the same marker
        while (index < args.Length - 1 && !args[index].EndsWith(marker))
        {
            index++;
            sb.Append(' ').Append(args[index]);
        }

        // Remove wrapping quotes
        path = sb.ToString().RemoveWrappingQuotes();

        return path;
    }

    private static void ValidateFlag(string flag, string value)
    {
        if (flag == Flags.RM && value == ".")
        {
            throw new ArgumentException($"{flag} cannot contain relative path.", flag);
        }
    }
}
