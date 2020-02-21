$DotNetInstallerUri = 'https://dot.net/v1/dotnet-install.ps1';
$DotNetUnixInstallerUri = 'https://dot.net/v1/dotnet-install.sh'
$DotNetChannel = 'LTS'
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent

[string] $CakeVersion = ''
[string] $DotNetVersion= ''
foreach($line in Get-Content "$PSScriptRoot\build.config")
{
  if ($line -like 'CAKE_VERSION=*') {
      $CakeVersion = $line.SubString(13)
  }
  elseif ($line -like 'DOTNET_VERSION=*') {
      $DotNetVersion =$line.SubString(15)
  }
}


if ([string]::IsNullOrEmpty($CakeVersion) -or [string]::IsNullOrEmpty($DotNetVersion)) {
    'Failed to parse Cake / .NET Core SDK Version'
    exit 1
}

# Make sure tools folder exists
$ToolPath = Join-Path $PSScriptRoot "tools"
if (!(Test-Path $ToolPath)) {
    Write-Verbose "Creating tools directory..."
    New-Item -Path $ToolPath -Type Directory -Force | out-null
}


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

# Get .NET Core CLI path if installed.
$FoundDotNetCliVersion = $null;
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $FoundDotNetCliVersion = dotnet --version;
}

if($FoundDotNetCliVersion -ne $DotNetVersion) {
    $InstallPath = Join-Path $PSScriptRoot ".dotnet"
    if (!(Test-Path $InstallPath)) {
        New-Item -Path $InstallPath -ItemType Directory -Force | Out-Null;
    }

    if ($IsMacOS -or $IsLinux) {
        (New-Object System.Net.WebClient).DownloadFile($DotNetUnixInstallerUri, "$InstallPath\dotnet-install.sh");
        & bash $InstallPath\dotnet-install.sh --version "$DotNetVersion" --install-dir "$InstallPath" --channel "$DotNetChannel" --no-path
    }
    else {
        (New-Object System.Net.WebClient).DownloadFile($DotNetInstallerUri, "$InstallPath\dotnet-install.ps1");
        & $InstallPath\dotnet-install.ps1 -Channel $DotNetChannel -Version $DotNetVersion -InstallDir $InstallPath;
    }

    Remove-PathVariable "$InstallPath"
    $env:PATH = "$InstallPath;$env:PATH"
}

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
$env:DOTNET_CLI_TELEMETRY_OPTOUT=1
$env:DOTNET_MULTILEVEL_LOOKUP=0

###########################################################################
# INSTALL CAKE
###########################################################################

# Make sure Cake has been installed.
[string] $CakeExePath = ''
[string] $CakeInstalledVersion = Get-Command dotnet-cake -ErrorAction SilentlyContinue  | % {&$_.Source --version}

if ($CakeInstalledVersion -eq $CakeVersion) {
    # Cake found locally
    $CakeExePath = (Get-Command dotnet-cake).Source
}
else {
    $CakePath = Join-Path $ToolPath ".store\cake.tool\$CakeVersion"
    $CakeExePath = (Get-ChildItem -Path $ToolPath -Filter "dotnet-cake*" -File| ForEach-Object FullName | Select-Object -First 1)


    if ((!(Test-Path -Path $CakePath -PathType Container)) -or (!(Test-Path $CakeExePath -PathType Leaf))) {

        if ((![string]::IsNullOrEmpty($CakeExePath)) -and (Test-Path $CakeExePath -PathType Leaf))
        {
            & dotnet tool uninstall --tool-path $ToolPath Cake.Tool
        }

        & dotnet tool install --tool-path $ToolPath --version $CakeVersion Cake.Tool
        if ($LASTEXITCODE -ne 0)
        {
            'Failed to install cake'
            exit 1
        }
        $CakeExePath = (Get-ChildItem -Path $ToolPath -Filter "dotnet-cake*" -File| ForEach-Object FullName | Select-Object -First 1)
    }
}

###########################################################################
# RUN BUILD SCRIPT
###########################################################################
& "$CakeExePath" ./build.cake --bootstrap
if ($LASTEXITCODE -eq 0)
{
    & "$CakeExePath" ./build.cake $args
}
exit $LASTEXITCODE