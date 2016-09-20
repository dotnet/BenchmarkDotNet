@echo off

echo -----------------------------
echo Running dotnet restore
echo -----------------------------

call dotnet restore

if NOT %ERRORLEVEL% == 0 (
    echo Dotnet restore has failed
    goto end
)

echo -----------------------------
echo Running Core tests
echo -----------------------------

call dotnet test "BenchmarkDotNet.IntegrationTests/" --configuration Release --framework netcoreapp1.0 2> failedCoreTests.txt

if NOT %ERRORLEVEL% == 0 (
    echo CORE tests has failed
	type failedCoreTests.txt
    goto end
)

echo -----------------------------
echo All tests has passed for Core
echo -----------------------------

:end
del failedCoreTests.txt