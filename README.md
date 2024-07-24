# Crow

![Crow logo, designed with Ai](./crow-logo.png)

Crow is a lightweight command-line tool designed to help developers reclaim valuable disk space by identifying and deleting unnecessary build files. This utility efficiently scans for build files in specified directories and offers an easy way to remove them, ensuring your development environment remains clean and more space left for your video games.

## Usage

```sh
Usage:      --lf <file> --in <directory> ... --rm <file> ...
```

## Example

```sh
crow --lf *csproj --in C:\Users\YourUserName\source\repos --rm bin obj
```

```sh
crow --lf package.json --in C:\Users\YourUserName\jsWorks --rm node_modules
```

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
