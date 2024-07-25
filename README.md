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

## Usage

```sh
--lf <file> --in <directory> ... --rm <file> ...
```

## Example

```sh
crow --lf *csproj --in C:\Users\YourUserName\source\repos --rm bin obj
```

```sh
crow --lf package.json --in C:\Users\YourUserName\jsWorks --rm node_modules
```
> Make sure crow is in your PATH

### Flags

- `--lf`  
  Files or directories to look for. Must be a single input. Supports Regex.

- `--in`  
  Directories to scan. Supports multiple input.

- `--rm`  
  Files or directories to remove. Supports multiple input. Supports Regex.

- `--force`  
  Removes files without asking for confirmation. Use this at your own risk.

### Misc

- `help`  
  Display help message.

- `version`  
  Display app version.

### Note

1. All flags except `--force` are required.
2. Flags are case insensitive.
3. Using `--force` is not recommended.
