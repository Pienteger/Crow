# Crow

![Crow logo, designed with Ai](./crow-logo.png)

Crow is a lightweight command-line tool designed to help developers reclaim valuable disk space by identifying and deleting unnecessary build files. This utility efficiently scans for build files in specified directories and offers an easy way to remove them, ensuring your development environment remains clean and more space left for your video games.

## Installation

1. Download the app file from the [release page](https://github.com/Pienteger/Crow/releases).
2. Set the appropriate PowerShell policy to run the installer:
   - Run `Set-ExecutionPolicy RemoteSigned -Scope CurrentUser` in the shell.
3. Unzip the downloaded file and navigate to the unzipped directory.
4. Run `.\setup.ps1`:
   - Right click on the file and select **Run with PowerShell**
   - This will copy the `.exe` file to `C:\Users\Public\Pienteger\` and add it to the PATH.
5. Re-open the Command Prompt, PowerShell, or terminal.
6. Run `crow help` to verify the installation.

### Caution

To set the execution policy, you may need to run the shell as an admin.

## Example Usage

### Clean system junk

```sh
crow clean
```

Scans temp files, crash dumps, Windows error reports, browser caches (Chrome, Edge, Brave, Firefox), app caches (VS Code, Discord, Slack, Teams) and shader caches, plus opt-in Windows Update downloads and package manager caches (npm, Yarn, pip, NuGet, Gradle). Results are sorted by size. You pick what to clean from a checklist where the safe locations are preselected.

- `--dry-run` only reports what could be freed.
- `--all` preselects every location.
- `--older-than <hours>` only removes files untouched for that long. By default, temp folders keep anything from the last 24 hours.
- `--force` cleans the default selection without asking.

Only the contents of each location are removed. Files in use are skipped, junctions are never followed, and the Recycle Bin is not touched. Close your browsers first to free their caches. Run from an elevated shell to clean the Windows folders fully.

### Clean build output of all your projects

```sh
crow dev --in C:\Users\YourUserName\source
```

Walks the tree once and finds `bin`/`obj` (.NET), `node_modules`/`.next`/`.turbo`/... (Node.js), `target` (Rust, Maven), `build`/`.gradle` (Gradle, Flutter), Python caches and CMake build folders. A folder is only reported when it sits next to its project file, so nested projects in a monorepo are found as well. Results are sorted by size.

### Custom scan

```sh
crow --lf *csproj --in C:\Users\YourUserName\source\repos --rm bin obj
```

```sh
crow --lf package.json --in C:\Users\YourUserName\jsWorks --rm node_modules
```

```sh
crow --lf package.json --in C:\Users\YourUserName\jsWorks --rm node_modules --ig AppData
```

> Make sure crow is in your PATH

### Flags

|Flag|Description|Remark|
|---|---|---|
|`--lf`|Files or directories to look for|Required|
|`--in`|Directories to scan|Required|
|`--rm`|Files or directories to remove|Required|
|`--ig`|Directories to ignore|Optional|
|`--force`|Removes files without asking for confirmation|Optional|

### Misc

- `help`  
  Display help message.

- `version`  
  Display app version.

### Note

1. Flags are case insensitive.
2. Using `--force` is not recommended.
