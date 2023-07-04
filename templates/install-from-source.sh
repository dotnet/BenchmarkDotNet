# Run only from the folder where the shell script is located!

dotnet build BenchmarkDotNet.Templates.csproj -c Release
dotnet pack BenchmarkDotNet.Templates.csproj -c Release

#  If we install the templates via a folder path, then it will have a different ID (ID=folder path).
#  It will conflict with BDN templates from nuget.
#  We need to install the templates via a FILE path in order to update the template from nuget.

nupkg_path=$(find . -name "BenchmarkDotNet.Templates*.nupkg")

dotnet new uninstall "BenchmarkDotNet.Templates"
dotnet new install $nupkg_path
