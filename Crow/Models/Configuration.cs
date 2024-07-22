namespace Crow.Models;

public class Configuration
{
    public required string LookFor { get; set; }
    public required IReadOnlyList<string> RemoveCandidates { get; set; }
    public required IReadOnlyList<string> Paths { get; set; }
    public bool IsForceDeleteEnabled { get; set; }

    public static Configuration Parse(string[] args)
    {
        var lfIndex = Array.IndexOf(args, Flags.LF);
        string lfValue =
            lfIndex != -1 && !args[lfIndex + 1].StartsWith('-')
                ? args[lfIndex + 1]
                : throw new ArgumentException($"{Flags.LF} value is required.", Flags.LF);

        List<string> rmValues = ParseFlagParts(Flags.RM, args);
        List<string> inValues = ParseFlagParts(Flags.IN, args);

        var forceIndex = Array.IndexOf(args, Flags.Force);

        return new Configuration
        {
            LookFor = lfValue,
            RemoveCandidates = rmValues,
            Paths = inValues,
            IsForceDeleteEnabled = forceIndex != -1
        };
    }

    private static List<string> ParseFlagParts(string flag, string[] args)
    {
        var sIndex = Array.IndexOf(args, flag);
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

            values.Add(args[i] == "." ? Environment.CurrentDirectory : args[i]);
        }

        if (values.Count == 0)
            throw new ArgumentException($"{flag} value is required.", flag);

        return values.Distinct().ToList();
    }
}
