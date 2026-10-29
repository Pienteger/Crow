namespace Crow.Helpers;

public static class SystemJunkCatalog
{
    private const string SYSTEM = "System";
    private const string BROWSERS = "Browsers";
    private const string APPS = "Apps";
    private const string DEVELOPER = "Developer";

    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

    private static readonly string[] ChromiumCacheFolders = ["Cache", "Code Cache", "GPUCache"];
    private static readonly string[] ElectronCacheFolders = ["Cache", "Code Cache", "GPUCache", "CachedData"];

    /// <summary>
    /// Returns the junk locations that exist on this machine.
    /// </summary>
    public static IReadOnlyList<SystemJunkLocation> GetLocations()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        List<SystemJunkLocation> locations =
        [
            new("User temp files", SYSTEM, [Path.GetTempPath()], OneDay, true),
            new("Windows temp files", SYSTEM, [Path.Combine(windows, "Temp")], OneDay, true, RequiresAdmin: true),
            new("Crash dumps", SYSTEM, [Path.Combine(local, "CrashDumps")], TimeSpan.Zero, true),
            new(
                "Windows error reports",
                SYSTEM,
                [Path.Combine(local, "Microsoft", "Windows", "WER"), Path.Combine(programData, "Microsoft", "Windows", "WER")],
                TimeSpan.Zero,
                true
            ),
            new("DirectX shader cache", SYSTEM, [Path.Combine(local, "D3DSCache")], TimeSpan.Zero, true),
            new(
                "Windows Update downloads",
                SYSTEM,
                [Path.Combine(windows, "SoftwareDistribution", "Download")],
                OneDay,
                false,
                RequiresAdmin: true
            ),
            new(
                "Internet cache",
                BROWSERS,
                [Path.Combine(local, "Microsoft", "Windows", "INetCache")],
                TimeSpan.Zero,
                true
            ),
            new(
                "Chrome cache",
                BROWSERS,
                ChromiumCaches(Path.Combine(local, "Google", "Chrome", "User Data")),
                TimeSpan.Zero,
                true
            ),
            new(
                "Edge cache",
                BROWSERS,
                ChromiumCaches(Path.Combine(local, "Microsoft", "Edge", "User Data")),
                TimeSpan.Zero,
                true
            ),
            new(
                "Brave cache",
                BROWSERS,
                ChromiumCaches(Path.Combine(local, "BraveSoftware", "Brave-Browser", "User Data")),
                TimeSpan.Zero,
                true
            ),
            new(
                "Firefox cache",
                BROWSERS,
                SubFolders(Path.Combine(local, "Mozilla", "Firefox", "Profiles"), "cache2"),
                TimeSpan.Zero,
                true
            ),
            new(
                "App caches (VS Code, Discord, Slack, Teams)",
                APPS,
                ElectronCaches(roaming, ["Code", "discord", "Slack", Path.Combine("Microsoft", "Teams")]),
                TimeSpan.Zero,
                true
            ),
            new(
                "NVIDIA shader cache",
                APPS,
                [Path.Combine(local, "NVIDIA", "DXCache"), Path.Combine(local, "NVIDIA", "GLCache")],
                TimeSpan.Zero,
                false
            ),
            new("npm cache", DEVELOPER, [Path.Combine(local, "npm-cache")], TimeSpan.Zero, false),
            new("Yarn cache", DEVELOPER, [Path.Combine(local, "Yarn", "Cache")], TimeSpan.Zero, false),
            new("pip cache", DEVELOPER, [Path.Combine(local, "pip", "cache")], TimeSpan.Zero, false),
            new(
                "NuGet HTTP cache",
                DEVELOPER,
                [Path.Combine(local, "NuGet", "v3-cache"), Path.Combine(local, "NuGet", "plugins-cache")],
                TimeSpan.Zero,
                false
            ),
            new("NuGet packages", DEVELOPER, [Path.Combine(home, ".nuget", "packages")], TimeSpan.Zero, false),
            new("Gradle caches", DEVELOPER, [Path.Combine(home, ".gradle", "caches")], TimeSpan.Zero, false),
        ];

        return locations
            .Select(l => l with { Paths = l.Paths.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList() })
            .Where(l => l.Paths.Count > 0)
            .ToList();
    }

    private static List<string> ChromiumCaches(string userDataPath)
    {
        if (!Directory.Exists(userDataPath))
            return [];

        var profiles = Directory
            .EnumerateDirectories(userDataPath)
            .Where(p => Path.GetFileName(p) is var name
                        && (name == "Default" || name.StartsWith("Profile ") || name == "Guest Profile"));

        return profiles
            .SelectMany(profile => ChromiumCacheFolders.Select(cache => Path.Combine(profile, cache)))
            .Append(Path.Combine(userDataPath, "ShaderCache"))
            .Append(Path.Combine(userDataPath, "GrShaderCache"))
            .ToList();
    }

    private static List<string> ElectronCaches(string root, string[] apps)
    {
        return apps
            .SelectMany(app => ElectronCacheFolders.Select(cache => Path.Combine(root, app, cache)))
            .ToList();
    }

    private static List<string> SubFolders(string root, string child)
    {
        return Directory.Exists(root)
            ? Directory.EnumerateDirectories(root).Select(p => Path.Combine(p, child)).ToList()
            : [];
    }
}
