@echo off

echo -----------------------------
echo Running dotnet restore
echo -----------------------------

call dotnet restore

if NOT %ERRORLEVEL% == 0 (
    echo Dotnet restore has failed
    goto end
)

cd BenchmarkDotNet.IntegrationTests

echo -----------------------------
echo Running Core tests
echo -----------------------------

call dotnet test --configuration Release --framework netcoreapp1.0 2> failedCoreTests.txt

if NOT %ERRORLEVEL% == 0 (
    echo CORE tests has failed
	type failedCoreTests.txt
    goto restoreCurrentFolder
)

echo -----------------------------
echo All tests has passed for Core
echo -----------------------------


:restoreCurrentFolder
cd ..

:end
del failedCoreTests.txt	
