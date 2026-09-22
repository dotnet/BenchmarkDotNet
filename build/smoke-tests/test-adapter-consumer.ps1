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

# Invoke-Dotnet checks $LASTEXITCODE itself, and prints the log before it throws. Leaving this on would make a failing
# `dotnet` throw at the call itself on PowerShell Core - which is what the workflow runs - so the log would never be
# printed and a broken restore or build would report nothing at all.
$PSNativeCommandUseErrorActionPreference = $false

$project = [System.IO.Path]::Combine($PSScriptRoot, 'TestAdapterConsumer', 'TestAdapterConsumer.csproj')
$targetFramework = 'net10.0'

# $IsWindows only exists on PowerShell Core, where it is the only way to tell; Windows PowerShell is Windows by definition.
$onWindows = ($null -eq $IsWindows) -or $IsWindows

# build.cmd installs the SDK the repository is pinned to into .dotnet, and only puts it on PATH for its own run.
$dotnet = [System.IO.Path]::Combine($PSScriptRoot, '..', '..', '.dotnet', $(if ($onWindows) { 'dotnet.exe' } else { 'dotnet' }))
if (-not (Test-Path $dotnet)) {
    $dotnet = 'dotnet'
}

if (-not (Test-Path -LiteralPath $ArtifactsDirectory)) {
    throw "The artifacts directory '$ArtifactsDirectory' does not exist. Run 'build.cmd pack' first."
}

# Both the package this reads the version from and the source the restore below resolves it through, so it has to be
# the same absolute path in both: a relative one would otherwise be resolved against the consuming project.
$ArtifactsDirectory = (Resolve-Path -LiteralPath $ArtifactsDirectory).ProviderPath

# `build.cmd pack` never cleans, so the folder can hold several versions. The newest is the one that was just packed,
# which is the one worth smoke testing.
$packages = @(Get-ChildItem -Path $ArtifactsDirectory -Filter 'BenchmarkDotNet.TestAdapter.*.nupkg' |
    Where-Object { $_.Name -notlike '*.symbols.nupkg' } |
    Sort-Object -Property LastWriteTime -Descending)

if ($packages.Count -eq 0) {
    throw "No BenchmarkDotNet.TestAdapter package was found in '$ArtifactsDirectory'. Run 'build.cmd pack' first."
}

$package = $packages[0]

if ($packages.Count -gt 1) {
    Write-Output "'$ArtifactsDirectory' holds $($packages.Count) BenchmarkDotNet.TestAdapter packages, taking the most recently written one."
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

# The project restores into this folder rather than into the global one, see its .csproj. NuGet never re-extracts a
# version it already has, and the version does not change between runs, so the packages this repository produces are
# dropped before the restore; everything else in there is an ordinary cache and is left alone.
$packagesDirectory = [System.IO.Path]::Combine($PSScriptRoot, 'packages')
if (Test-Path -LiteralPath $packagesDirectory) {
    Get-ChildItem -Path $packagesDirectory -Directory -Filter 'benchmarkdotnet*' | Remove-Item -Recurse -Force
}

Write-Output '##[group]Restoring the consuming project'
# The project assigns RestoreAdditionalProjectSources too, but a global property wins over that assignment, which is
# what makes a custom -ArtifactsDirectory restore from the folder the version was read off. Only the restore needs it:
# nuget.g.props bakes the result in for every later invocation.
Invoke-Dotnet restore $project "-p:BenchmarkDotNetVersion=$version" `
    "-p:RestoreAdditionalProjectSources=$ArtifactsDirectory" '-tl:off' | Write-Output
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
