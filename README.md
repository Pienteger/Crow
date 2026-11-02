# Crow

![Crow logo, designed with Ai](./crow-logo.png)

Crow is a small Windows command-line tool that frees disk space. It finds build output (`bin`, `obj`, `node_modules`, `target`, ...) across all your projects, plus system junk like temp files and browser caches. You review the list, then Crow deletes what you picked. That leaves more space for your video games.

- Single ~4 MB exe. No .NET runtime needed.
- Shows sizes first and asks before deleting anything.
- Never follows junctions or symlinks out of the folder being cleaned.

## Install

Run this in PowerShell:

```powershell
irm https://raw.githubusercontent.com/Pienteger/Crow/master/install.ps1 | iex
```

It downloads the latest release for your CPU (x64 or ARM64) and checks its SHA-256. Then it copies `crow.exe` to `C:\Users\Public\Pienteger` and adds that folder to your user PATH. No admin rights or execution policy change needed.

Run the same command again to update.

<details>
<summary>Manual install</summary>

1. Download `Crow-win-x64.zip` (or `Crow-win-arm64.zip` for ARM devices) from the [release page](https://github.com/Pienteger/Crow/releases).
2. Unzip it.
3. Right-click `setup.ps1` and choose **Run with PowerShell**.
   If Windows blocks the script, run `Set-ExecutionPolicy RemoteSigned -Scope CurrentUser` first.
4. Reopen your terminal and run `crow help`.

You can also skip the script: put `crow.exe` in any folder on your PATH.

</details>

<details>
<summary>Uninstall</summary>

```powershell
Remove-Item C:\Users\Public\Pienteger -Recurse
$path = [Environment]::GetEnvironmentVariable('Path', 'User') -split ';' | Where-Object { $_.TrimEnd('\') -ne 'C:\Users\Public\Pienteger' }
[Environment]::SetEnvironmentVariable('Path', ($path -join ';'), 'User')
```

</details>

## Quick start

```sh
crow clean --dry-run                # See how much system junk you have, delete nothing
crow clean                          # Pick what to clean from a checklist
crow dev --in C:\Users\you\source   # Find build output in all your projects
```

## Commands

### `crow clean`: system junk

Scans these locations and sorts them by size:

| Selected by default | Opt-in |
|---|---|
| User and Windows temp files, crash dumps, Windows error reports, DirectX shader cache | Windows Update downloads |
| Internet cache, Chrome, Edge, Brave and Firefox caches | NVIDIA shader cache |
| VS Code, Discord, Slack and Teams caches | npm, Yarn, pip, NuGet and Gradle caches |

You pick what to clean from a checklist. The safe locations are already ticked.

| Flag | What it does |
|---|---|
| `--dry-run` | Only report what could be freed. |
| `--all` | Tick every location, not only the safe ones. |
| `--older-than <hours>` | Only remove files untouched for that long. Temp folders keep the last 24 hours by default. |
| `--force` | Clean the default selection without asking. |

Only the contents of each location are removed, never the folder itself. Files in use are skipped. The Recycle Bin is not touched.

Tips:

- Close your browsers first, or their caches stay locked.
- Run from an elevated shell to clean the Windows folders fully.

### `crow dev`: build output

```sh
crow dev --in C:\Users\you\source
crow dev --in D:\work D:\oss --ig D:\work\legacy-app
```

Walks the tree once and looks for these project types:

| Project | Found by | Removes |
|---|---|---|
| .NET | `*.csproj`, `*.fsproj`, `*.vbproj` | `bin`, `obj` |
| Node.js | `package.json` | `node_modules`, `.next`, `.nuxt`, `.turbo`, `.parcel-cache`, `.svelte-kit`, `.angular` |
| Rust | `Cargo.toml` | `target` |
| Maven | `pom.xml` | `target` |
| Gradle | `build.gradle`, `build.gradle.kts` | `build`, `.gradle` |
| Python | `pyproject.toml`, `requirements.txt` | `__pycache__`, `.pytest_cache`, `.mypy_cache`, `.ruff_cache`, `.tox` |
| Flutter | `pubspec.yaml` | `build`, `.dart_tool` |
| CMake | `CMakeLists.txt` | `cmake-build-debug`, `cmake-build-release` |

A folder is only reported when it sits next to its project file. A `build` folder with no Gradle or Flutter project next to it is left alone. Nested projects in a monorepo are found too.

| Flag | What it does |
|---|---|
| `--in <dir> ...` | Folders to scan. Required. |
| `--ig <dir> ...` | Folders to skip. |
| `--force` | Delete without asking. |

### Custom scan

For anything the presets don't cover:

```sh
crow --lf *.csproj --in C:\Users\you\source\repos --rm bin obj
crow --lf package.json --in C:\Users\you\jsWorks --rm node_modules --ig C:\Users\you\jsWorks\keep-me
```

| Flag | What it does |
|---|---|
| `--lf <pattern>` | Marker file that identifies a project folder. Required. |
| `--in <dir> ...` | Folders to scan. Required. |
| `--rm <pattern> ...` | Files or folders to delete next to the marker. Required. |
| `--ig <path> ...` | Paths to skip. |
| `--force` | Delete without asking. |

### Other

| Command | What it does |
|---|---|
| `crow help` | Show usage. |
| `crow version` | Show the installed version. |

### Notes

- `--lf` and `--rm` take wildcards: `*` and `?`.
- `--in` and `--ig` take full paths. A relative path is resolved against the current folder.
- Flags are case-insensitive.
- `--force` skips the confirmation prompt. Use it with care.

## Build from source

Needs the .NET 10 SDK. From `src`:

```sh
dotnet test
dotnet run --project Crow -- help
```

To build the same native exe as the release (needs the Visual Studio C++ build tools):

```sh
dotnet publish Crow/Crow.csproj -c Release -r win-x64 -p:PublishAot=true -p:OptimizationPreference=Size -p:StripSymbols=true -o ../publish
```

## Releasing

The [Release workflow](.github/workflows/release.yml) runs the tests and builds Native AOT exes for `win-x64` and `win-arm64`. Then it smoke-tests them and publishes the zips with SHA-256 checksums.

```sh
git tag v1.2.0
git push origin v1.2.0
```

Or open **Actions > Release > Run workflow** and enter a version. The workflow creates the tag for you. A version with a suffix, such as `1.3.0-beta.1`, is published as a pre-release.

## License

[MIT](LICENSE.txt)
