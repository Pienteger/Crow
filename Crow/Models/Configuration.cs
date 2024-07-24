using System.Text;

namespace Crow.Models;

public class Configuration
{
    public required string LookFor { get; set; }
    public required IReadOnlyList<string> RemoveCandidates { get; set; }
    public required IReadOnlyList<string> LookIns { get; set; }

    public static Configuration Parse(string argsStr)
    {
        var args = argsStr.Split(' ');
        var lfIndex = args.IndexOf(Flags.LF);
        string lfValue =
            lfIndex != -1 && !args[lfIndex + 1].StartsWith('-')
                ? args[lfIndex + 1]
                : throw new ArgumentException($"{Flags.LF} value is required.", Flags.LF);

        List<string> rmValues = ParseFlagParts(Flags.RM, args);
        List<string> inValues = ParseFlagParts(Flags.IN, args);

        return new Configuration
        {
            LookFor = lfValue,
            RemoveCandidates = rmValues,
            LookIns = inValues
        };
    }

    private static List<string> ParseFlagParts(string flag, string[] args)
    {
        var sIndex = args.IndexOf(flag);
        if (sIndex == -1)
        {
            throw new ArgumentException($"{flag} value is required.", flag);
        }

        var values = new List<string>();

        for (int i = sIndex + 1; i < args.Length && !args[i].StartsWith('-'); i++)
        {
            if (flag == Flags.RM && args[i] == ".")
            {
                throw new ArgumentException($"{flag} cannot contain '.'.", flag);
            }

            values.Add(args[i] == "." ? Environment.CurrentDirectory : ParseComposite(ref i, args));
        }

        if (values.Count == 0)
            throw new ArgumentException($"{flag} value is required.", flag);

        return values.Distinct().ToList();
    }

    private static string ParseComposite(ref int index, string[] args)
    {
        char marker = args[index][0];
        if (marker != '\"' && marker != '\'')
        {
            return args[index];
        }
        var sb = new StringBuilder();
        sb.Append(args[index]);
        while (!args[index].EndsWith(marker) && ++index < args.Length)
        {
            sb.Append(' ').Append(args[index]);
        }
        return sb.ToString().RemoveWrappingQuotes();
    }
}
