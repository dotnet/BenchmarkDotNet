:: Run only from the folder where the batch file is located!

dotnet build BenchmarkDotNet.Templates.csproj -c Release
dotnet pack BenchmarkDotNet.Templates.csproj -c Release

:: If we install the templates via a folder path, then it will have a different ID (ID=folder path).
:: It will conflict with BDN templates from nuget.
:: We need to install the templates via a FILE path in order to update the template from nuget.
::
:: https://stackoverflow.com/questions/47450531/batch-write-output-of-dir-to-a-variable
for /f "delims=" %%a in ('dir /s /b BenchmarkDotNet.Templates*.nupkg') do set "nupkg_path=%%a"

dotnet new --uninstall "BenchmarkDotNet.Templates"
dotnet new --install "%nupkg_path%"