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
echo Running Core 1.1 Unit tests
echo -----------------------------

call dotnet test "BenchmarkDotNet.Tests\BenchmarkDotNet.Tests.csproj" --configuration Release --framework netcoreapp1.1

if NOT %ERRORLEVEL% == 0 (
    echo CORE 1.1 Unit tests has failed
    goto end
)

echo -----------------------------
echo Running Core 2.0 Unit tests
echo -----------------------------

call dotnet test "BenchmarkDotNet.Tests\BenchmarkDotNet.Tests.csproj" --configuration Release --framework netcoreapp2.0

if NOT %ERRORLEVEL% == 0 (
    echo CORE 2.0 Unit tests has failed
    goto end
)


echo -----------------------------
echo Running Core 1.1 Integration tests
echo -----------------------------

call dotnet test "BenchmarkDotNet.IntegrationTests\BenchmarkDotNet.IntegrationTests.csproj" --configuration Release --framework netcoreapp1.1

if NOT %ERRORLEVEL% == 0 (
    echo CORE 1.1 Integration tests has failed
    goto end
)

echo -----------------------------
echo Running Core 2.0 Integration tests
echo -----------------------------

call dotnet test "BenchmarkDotNet.IntegrationTests\BenchmarkDotNet.IntegrationTests.csproj" --configuration Release --framework netcoreapp2.0

if NOT %ERRORLEVEL% == 0 (
    echo CORE 2.0 Integration tests has failed
    goto end
)

echo -----------------------------
echo All tests has passed for Core
echo -----------------------------

:end