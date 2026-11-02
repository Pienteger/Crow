# Installs or updates Crow from the latest GitHub release.
#
#   irm https://raw.githubusercontent.com/Pienteger/Crow/master/install.ps1 | iex
#
# Downloads the zip for this machine, checks its SHA-256, copies crow.exe to the
# same folder setup.ps1 uses and adds that folder to the user PATH.
# Wrapped in a script block so nothing leaks into the caller's session and a
# failure never closes their shell.

& {
    $ErrorActionPreference = 'Stop'
    $ProgressPreference = 'SilentlyContinue' # Invoke-WebRequest is very slow with the progress bar on.

    # Windows PowerShell 5.1 may still default to TLS 1.0, which GitHub refuses.
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    $installDir = 'C:\Users\Public\Pienteger'

    try {
        $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
    }
    catch {
        $arch = $env:PROCESSOR_ARCHITECTURE
    }
    $rid = if ($arch -match 'Arm64') { 'win-arm64' } else { 'win-x64' }

    $name = "Crow-$rid"
    $baseUrl = 'https://github.com/Pienteger/Crow/releases/latest/download'
    $temp = Join-Path ([IO.Path]::GetTempPath()) "crow-install-$([guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $temp | Out-Null

    try {
        Write-Host "Downloading $name.zip..."
        $zip = Join-Path $temp "$name.zip"
        Invoke-WebRequest -UseBasicParsing -Uri "$baseUrl/$name.zip" -OutFile $zip
        $expected = (Invoke-WebRequest -UseBasicParsing -Uri "$baseUrl/$name.zip.sha256").Content

        # The .sha256 file is "<hash>  <file name>". Content may arrive as bytes.
        if ($expected -is [byte[]]) { $expected = [Text.Encoding]::ASCII.GetString($expected) }
        $expected = ($expected.Trim() -split '\s+')[0]
        $actual = (Get-FileHash $zip -Algorithm SHA256).Hash
        if ($actual -ne $expected) {
            throw "Checksum mismatch for $name.zip. Expected $expected, got $actual."
        }

        Expand-Archive -Path $zip -DestinationPath $temp -Force

        if (-not (Test-Path $installDir)) {
            New-Item -ItemType Directory -Path $installDir | Out-Null
        }
        Copy-Item (Join-Path $temp "$name\crow.exe") (Join-Path $installDir 'crow.exe') -Force
    }
    catch {
        Write-Host "Crow was not installed: $($_.Exception.Message)" -ForegroundColor Red
        return
    }
    finally {
        Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue
    }

    $userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
    $entries = @($userPath -split ';' | Where-Object { $_ })
    if ($entries.TrimEnd('\') -notcontains $installDir) {
        [Environment]::SetEnvironmentVariable('Path', (($entries + $installDir) -join ';'), 'User')
        Write-Host "Added $installDir to your PATH."
    }

    # Make crow usable in this window too, without reopening the terminal.
    if (($env:Path -split ';').TrimEnd('\') -notcontains $installDir) {
        $env:Path = "$env:Path;$installDir"
    }

    $version = & (Join-Path $installDir 'crow.exe') version
    Write-Host "Crow $version installed. Run 'crow help' to get started." -ForegroundColor Green
}
