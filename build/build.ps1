#!/usr/bin/env pwsh

#Requires -PSEdition Core

$DotNetInstallerUri = 'https://dot.net/v1/dotnet-install.ps1';
$BuildPath = Split-Path $MyInvocation.MyCommand.Path -Parent
$PSScriptRoot = Split-Path $PSScriptRoot -Parent

###########################################################################
# INSTALL .NET CORE CLI
###########################################################################

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
$env:DOTNET_CLI_TELEMETRY_OPTOUT=1

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

    curl -LsSfo $ScriptPath $DotNetInstallerUri --retry 5 --retry-delay 5

    & $ScriptPath -JSonFile $GlobalJsonPath -InstallDir $InstallPath;

    # Install .NET 8 SDK
    & $ScriptPath -Channel 8.0 -InstallDir $InstallPath -NoPath;
}

Remove-PathVariable "$InstallPath"
$env:PATH = "$InstallPath;$env:PATH"
$env:DOTNET_ROOT=$InstallPath

###########################################################################
# RUN BUILD SCRIPT
###########################################################################
& dotnet run --configuration Release --project build/BenchmarkDotNet.Build/BenchmarkDotNet.Build.csproj -- $args

exit $LASTEXITCODE;