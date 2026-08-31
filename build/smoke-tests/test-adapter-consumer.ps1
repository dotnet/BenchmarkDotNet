#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Smoke tests the packed BenchmarkDotNet.TestAdapter against a project that consumes it as a NuGet package.

.DESCRIPTION
    Everything in this repository that uses the adapter's build files imports them by path from the project file,
    which MSBuild evaluates before nuget.g.targets. A package consumer gets the opposite order: NuGet imports
    Microsoft.Testing.Platform.MSBuild's targets, which default IsTestingPlatformApplication to true, and only then
    the adapter's, which have to overwrite that default for the opt-outs to work. Nothing in the solution can
    reproduce that order, so this restores the real package and asserts on how the properties resolve.

    Run `build.cmd pack` first, so that the packages exist.

.PARAMETER ArtifactsDirectory
    The directory `build.cmd pack` wrote the packages to.

.PARAMETER Configuration
    The configuration to build the consuming project in.
#>

[CmdletBinding()]
param(
    [string] $ArtifactsDirectory = [System.IO.Path]::Combine($PSScriptRoot, '..', '..', 'artifacts'),
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$project = [System.IO.Path]::Combine($PSScriptRoot, 'TestAdapterConsumer', 'TestAdapterConsumer.csproj')
$targetFramework = 'net10.0'

# $IsWindows only exists on PowerShell Core, where it is the only way to tell; Windows PowerShell is Windows by definition.
$onWindows = ($null -eq $IsWindows) -or $IsWindows

# build.cmd installs the SDK the repository is pinned to into .dotnet, and only puts it on PATH for its own run.
$dotnet = [System.IO.Path]::Combine($PSScriptRoot, '..', '..', '.dotnet', $(if ($onWindows) { 'dotnet.exe' } else { 'dotnet' }))
if (-not (Test-Path $dotnet)) {
    $dotnet = 'dotnet'
}

$package = Get-ChildItem -Path $ArtifactsDirectory -Filter 'BenchmarkDotNet.TestAdapter.*.nupkg' |
    Where-Object { $_.Name -notlike '*.symbols.nupkg' } |
    Select-Object -First 1

if ($null -eq $package) {
    throw "No BenchmarkDotNet.TestAdapter package was found in '$ArtifactsDirectory'. Run 'build.cmd pack' first."
}

$version = $package.BaseName -replace '^BenchmarkDotNet\.TestAdapter\.', ''
Write-Output "Consuming BenchmarkDotNet.TestAdapter $version from $ArtifactsDirectory"

function Invoke-Dotnet {
    param([Parameter(ValueFromRemainingArguments = $true)] [string[]] $Arguments)

    $output = & $dotnet @Arguments 2>&1 | Out-String

    if ($LASTEXITCODE -ne 0) {
        Write-Output $output
        throw "'dotnet $($Arguments -join ' ')' failed with exit code $LASTEXITCODE."
    }

    return $output
}

function Assert-Property {
    param(
        [string] $Name,
        [string] $Expected,
        [string[]] $With = @()
    )

    $arguments = @($project, '-nologo', '-tl:off', "-p:BenchmarkDotNetVersion=$version", "-p:Configuration=$Configuration") + $With + @("-getProperty:$Name")
    $actual = (Invoke-Dotnet msbuild @arguments).Trim()

    $description = if ($With.Count -eq 0) { 'by default' } else { "with $($With -join ' ')" }

    if ($actual -ne $Expected) {
        throw "Expected $Name to be '$Expected' $description, but it was '$actual'."
    }

    Write-Output "  OK: $Name is '$Expected' $description"
}

Write-Output '##[group]Restoring the consuming project'
Invoke-Dotnet restore $project "-p:BenchmarkDotNetVersion=$version" '-tl:off' | Write-Output
Write-Output '##[endgroup]'

Write-Output 'Checking how the packaged build files resolve the test platform:'

# Microsoft.Testing.Platform is the default, and the adapter leaves the entry point to it.
Assert-Property -Name 'IsTestingPlatformApplication' -Expected 'true'
Assert-Property -Name 'GenerateProgramFile' -Expected 'false'

# The two opt-outs have to win over the default Microsoft.Testing.Platform.MSBuild sets in its own targets, which a
# package consumer imports before the adapter's.
Assert-Property -Name 'IsTestingPlatformApplication' -Expected 'false' -With '-p:BenchmarkDotNetUseVSTest=true'
Assert-Property -Name 'IsTestingPlatformApplication' -Expected 'false' -With '-p:GenerateProgramFile=false'

Write-Output '##[group]Building the consuming project'
Invoke-Dotnet build $project '--no-restore' '-c' $Configuration "-p:BenchmarkDotNetVersion=$version" '-tl:off' | Write-Output
Write-Output '##[endgroup]'

Write-Output 'Listing the benchmarks through the entry point Microsoft.Testing.Platform generated:'
$application = [System.IO.Path]::Combine($PSScriptRoot, 'TestAdapterConsumer', 'bin', $Configuration, $targetFramework, 'TestAdapterConsumer.dll')
$listed = Invoke-Dotnet $application '--list-tests' '--no-ansi'
Write-Output $listed

if ($listed -notmatch 'TestAdapterConsumer\.ConsumedBenchmark\.Add') {
    throw 'The packaged adapter did not list the benchmark of the consuming project.'
}

Write-Output 'The packaged BenchmarkDotNet.TestAdapter behaves as expected.'
