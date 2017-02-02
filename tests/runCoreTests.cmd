@echo off

echo -----------------------------
echo Running dotnet restore
echo -----------------------------

call dotnet restore "BenchmarkDotNet.IntegrationTests\BenchmarkDotNet.IntegrationTests.csproj"

if NOT %ERRORLEVEL% == 0 (
    echo Dotnet restore has failed
    goto end
)

echo -----------------------------
echo Running Core tests
echo -----------------------------

call dotnet test "BenchmarkDotNet.IntegrationTests\BenchmarkDotNet.IntegrationTests.csproj" --configuration Release --framework netcoreapp1.1

if NOT %ERRORLEVEL% == 0 (
    echo CORE tests has failed
    goto end
)

echo -----------------------------
echo All tests has passed for Core
echo -----------------------------

:end