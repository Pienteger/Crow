namespace Crow.Models;

/// <summary>
/// A well-known place where the OS or apps leave disposable files.
/// Only the contents of <see cref="Paths"/> are cleaned, never the folders themselves.
/// </summary>
/// <param name="Name">Display name.</param>
/// <param name="Category">Group the location belongs to.</param>
/// <param name="Paths">Existing directories that make up this location.</param>
/// <param name="MinAge">Files modified more recently than this are left alone.</param>
/// <param name="SelectedByDefault">Whether the location is safe to clean without thinking about it.</param>
/// <param name="RequiresAdmin">Whether cleaning it fully needs an elevated shell.</param>
public sealed record SystemJunkLocation(
    string Name,
    string Category,
    IReadOnlyList<string> Paths,
    TimeSpan MinAge,
    bool SelectedByDefault,
    bool RequiresAdmin = false
);

public sealed record SystemJunkScanResult(
    SystemJunkLocation Location,
    long Size,
    int FileCount,
    TimeSpan MinAge
);

public sealed record CleanResult(long FreedBytes, int DeletedCount, int SkippedCount);
