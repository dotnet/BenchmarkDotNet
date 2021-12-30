dotnet build -c Release BenchmarkDotNet.Templates.csproj
dotnet pack -c Release BenchmarkDotNet.Templates.csproj
dotnet new -u BenchmarkDotNet.Templates
dotnet new -i bin/Release/BenchmarkDotNet.Templates.*