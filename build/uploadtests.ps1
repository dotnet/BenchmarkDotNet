param(
    [string]
    $ResultsFile,

    [string]
    $ResultsType = "xunit"
)

$ResultsFile = Resolve-Path $ResultsFile
$Url = "https://ci.appveyor.com/api/testresults/$ResultsType/$($env:APPVEYOR_JOB_ID)"

if(-Not (Test-Path $ResultsFile))
{
    Write-Host "File '$ResultsFile' not found"
    exit(1)
}

# upload results to AppVeyor
try
{
    $wc = New-Object 'System.Net.WebClient'
    $wc.UploadFile($Url, $ResultsFile)
    Write-Host "Tests result uploaded correctly"
}
catch
{
    Write-Host "Error uploading tests results to $Url"
    $Exception = $_.Exception
    
    while($null -ne $Exception)
    {
        Write-Host "Error: $($Exception.Message)"
        $Exception = $Exception.InnerException
    }
    
    exit(2)
}
