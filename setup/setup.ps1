# PowerShell script to copy executable and add to PATH

# Define variables. Resolve the exe next to this script, so it works from any working directory.
$sourceExe = Join-Path $PSScriptRoot "crow.exe"
$destinationDir = "C:\Users\Public\Pienteger\"
$destinationExe = Join-Path $destinationDir "crow.exe"

# Create the target directory if it doesn't exist
if (-Not (Test-Path -Path $destinationDir)) {
    New-Item -ItemType Directory -Path $destinationDir | Out-Null
}

# Copy the executable to the target directory
Copy-Item -Path $sourceExe -Destination $destinationExe -Force

# Get the current PATH
$envPath = [Environment]::GetEnvironmentVariable("Path", [EnvironmentVariableTarget]::User)

# Check if the new path is already in the PATH
if ($envPath -notlike "*$destinationDir*") {
    # Add the new path to the existing PATH
    $newPath = "$envPath;$destinationDir"
    # Set the new PATH
    [Environment]::SetEnvironmentVariable("Path", $newPath, [EnvironmentVariableTarget]::User)
    # Refresh the PATH in the current session
    $env:Path = $newPath
}

Write-Output "C:\Users\Public\Pienteger\ has been added to the PATH."

# Display success message and wait for any key press to continue
Write-Output "Installation successful. Press any key to continue..."
$x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Exit the script
exit
