param(
    [string]
    $Version = $env:APPVEYOR_BUILD_VERSION
)

$projects = Get-ChildItem -Path $Path -Directory |
                    foreach { "$($_.Fullname)\project.json" } |
                    Where-Object { Test-Path $_ }

function Set-DotnetProjectVersion
{
    param(
        $project,
        $version
    )

    $changed = $false
    $json = Get-Content -Raw -Path $project | ConvertFrom-Json
    if($json.version)
    {
        Write-Host "Setting version $version on project $project"
        $json.version = $version
        $changed = $true
    }
    
    if($json.dependencies.BenchmarkDotNet.version)
    {
        $json.dependencies.BenchmarkDotNet.version = $version
        $changed = $true
    }
    
    if($changed)
    {
        $json | ConvertTo-Json -depth 999 | Out-File $project
    }
}

if(-not ([string]::IsNullOrEmpty($Version)))
{
    Write-Host "Building version $Version"

    foreach($project in $projects)
    {
        Set-DotnetProjectVersion $project $Version
    }
}
