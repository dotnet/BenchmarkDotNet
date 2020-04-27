dotnet build -c Release BenchmarkDotNet.Templates.csproj
dotnet pack -c Release BenchmarkDotNet.Templates.csproj
dotnet new -u BenchmarkDotNet.Templates
dotnet new -i BenchmarkDotNet.Templates::0.0.0-* --nuget-source .\bin\Release\