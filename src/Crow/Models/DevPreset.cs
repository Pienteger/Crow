namespace Crow.Models;

/// <summary>
/// A project type: the marker file that identifies its root and the
/// regenerable folders next to that marker.
/// </summary>
public sealed record DevPreset(string Name, string LookFor, IReadOnlyList<string> RemoveCandidates);

public static class DevPresets
{
    private static readonly string[] PythonCaches =
        ["__pycache__", ".pytest_cache", ".mypy_cache", ".ruff_cache", ".tox"];

    public static readonly IReadOnlyList<DevPreset> All =
    [
        new(".NET", "*.csproj", ["bin", "obj"]),
        new(".NET", "*.fsproj", ["bin", "obj"]),
        new(".NET", "*.vbproj", ["bin", "obj"]),
        new("Node.js", "package.json", ["node_modules", ".next", ".nuxt", ".turbo", ".parcel-cache", ".svelte-kit", ".angular"]),
        new("Rust", "Cargo.toml", ["target"]),
        new("Maven", "pom.xml", ["target"]),
        new("Gradle", "build.gradle", ["build", ".gradle"]),
        new("Gradle", "build.gradle.kts", ["build", ".gradle"]),
        new("Python", "pyproject.toml", PythonCaches),
        new("Python", "requirements.txt", PythonCaches),
        new("Flutter", "pubspec.yaml", ["build", ".dart_tool"]),
        new("CMake", "CMakeLists.txt", ["cmake-build-debug", "cmake-build-release"]),
    ];
}
