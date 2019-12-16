:: this script should be executed from the root dir

git clean -xfd
dotnet restore
dotnet build .\src\BenchmarkDotNet\BenchmarkDotNet.csproj -c Release
dotnet build .\src\BenchmarkDotNet.Diagnostics.Windows\BenchmarkDotNet.Diagnostics.Windows.csproj  -c Release
dotnet build .\src\BenchmarkDotNet.Tool\BenchmarkDotNet.Tool.csproj  -c Release
dotnet build .\src\BenchmarkDotNet.Annotations\BenchmarkDotNet.Annotations.csproj  -c Release
dotnet build .\templates\BenchmarkDotNet.Templates.csproj -c Release
dotnet pack .\src\BenchmarkDotNet\BenchmarkDotNet.csproj -c Release --include-symbols -p:SymbolPackageFormat=snupkg
dotnet pack .\src\BenchmarkDotNet.Diagnostics.Windows\BenchmarkDotNet.Diagnostics.Windows.csproj  -c Release --include-symbols -p:SymbolPackageFormat=snupkg
dotnet pack .\src\BenchmarkDotNet.Tool\BenchmarkDotNet.Tool.csproj -c Release --include-symbols -p:SymbolPackageFormat=snupkg
dotnet pack .\src\BenchmarkDotNet.Annotations\BenchmarkDotNet.Annotations.csproj -c Release --include-symbols -p:SymbolPackageFormat=snupkg
dotnet pack .\templates\BenchmarkDotNet.Templates.csproj -c Release
rmdir artifacts /s /q
mkdir artifacts
for /R %%x in (BenchmarkDotNet*.*nupkg) do copy "%%x" "artifacts/" /Y