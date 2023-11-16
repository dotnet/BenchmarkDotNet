#!/usr/bin/env pwsh

$DotNetInstallerUri = 'https://dot.net/v1/dotnet-install.ps1';
$BuildPath = Split-Path $MyInvocation.MyCommand.Path -Parent
$PSScriptRoot = Split-Path $PSScriptRoot -Parent

if ($PSVersionTable.PSEdition -ne 'Core') {
    # Attempt to set highest encryption available for SecurityProtocol.
    # PowerShell will not set this by default (until maybe .NET 4.6.x). This
    # will typically produce a message for PowerShell v2 (just an info
    # message though)
    try {
        # Set TLS 1.2 (3072), then TLS 1.1 (768), then TLS 1.0 (192), finally SSL 3.0 (48)
        # Use integers because the enumeration values for TLS 1.2 and TLS 1.1 won't
        # exist in .NET 4.0, even though they are addressable if .NET 4.5+ is
        # installed (.NET 4.5 is an in-place upgrade).
        [System.Net.ServicePointManager]::SecurityProtocol = 3072 -bor 768 -bor 192 -bor 48
      } catch {
        Write-Output 'Unable to set PowerShell to use TLS 1.2 and TLS 1.1 due to old .NET Framework installed. If you see underlying connection closed or trust errors, you may need to upgrade to .NET Framework 4.5+ and PowerShell v3'
      }
}

###########################################################################
# INSTALL .NET CORE CLI
###########################################################################

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
$env:DOTNET_CLI_TELEMETRY_OPTOUT=1
$env:DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX=2

Function Remove-PathVariable([string]$VariableToRemove)
{
    $SplitChar = ';'
    if ($IsMacOS -or $IsLinux) {
        $SplitChar = ':'
    }

    $path = [Environment]::GetEnvironmentVariable("PATH", "User")
    if ($path -ne $null)
    {
        $newItems = $path.Split($SplitChar, [StringSplitOptions]::RemoveEmptyEntries) | Where-Object { "$($_)" -inotlike $VariableToRemove }
        [Environment]::SetEnvironmentVariable("PATH", [System.String]::Join($SplitChar, $newItems), "User")
    }

    $path = [Environment]::GetEnvironmentVariable("PATH", "Process")
    if ($path -ne $null)
    {
        $newItems = $path.Split($SplitChar, [StringSplitOptions]::RemoveEmptyEntries) | Where-Object { "$($_)" -inotlike $VariableToRemove }
        [Environment]::SetEnvironmentVariable("PATH", [System.String]::Join($SplitChar, $newItems), "Process")
    }
}

$InstallPath = Join-Path $PSScriptRoot ".dotnet"
$SdkPath = Join-Path $BuildPath "sdk"
$GlobalJsonPath = Join-Path $SdkPath "global.json"
if (!(Test-Path $InstallPath)) {
    New-Item -Path $InstallPath -ItemType Directory -Force | Out-Null;
    $ScriptPath = Join-Path $InstallPath 'dotnet-install.ps1'
    (New-Object System.Net.WebClient).DownloadFile($DotNetInstallerUri, $ScriptPath);
    & $ScriptPath -JSonFile $GlobalJsonPath -InstallDir $InstallPath;
}

Remove-PathVariable "$InstallPath"
$env:PATH = "$InstallPath;$env:PATH"
$env:DOTNET_ROOT=$InstallPath

###########################################################################
# RUN BUILD SCRIPT
###########################################################################
& dotnet run --configuration Release --project build/BenchmarkDotNet.Build/BenchmarkDotNet.Build.csproj -- $args

exit $LASTEXITCODE;