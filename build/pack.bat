dotnet pack .\src\BenchmarkDotNet\BenchmarkDotNet.csproj -c Release
dotnet pack .\src\BenchmarkDotNet.Diagnostics.Windows\BenchmarkDotNet.Diagnostics.Windows.csproj -c Release
dotnet pack .\src\BenchmarkDotNet.Tools.Disassembler.x64\BenchmarkDotNet.Tools.Disassembler.x64.csproj -c Release
dotnet pack .\src\BenchmarkDotNet.Tools.Disassembler.x86\BenchmarkDotNet.Tools.Disassembler.x86.csproj -c Release
rmdir artifacts /s /q
mkdir artifacts
for /R %%x in (BenchmarkDotNet*.*nupkg) do copy "%%x" "artifacts/" /Y