@echo off

echo -----------------------------
echo Starting Initial Cleanup
echo -----------------------------

if exist "artifacts" rmdir /s /q "artifacts"

echo -----------------------------
echo Initial Cleanup finished
echo -----------------------------

echo -----------------------------
echo Running dotnet restore to make sure that all projects are compatible with dotnet cli
echo -----------------------------

call dotnet restore

if NOT %ERRORLEVEL% == 0 (
    echo Dotnet restore has failed
    goto cleanup
)

echo -----------------------------
echo Running dnu restore to make sure that all projects are compatible with dnx toolchain
echo -----------------------------

call dnu restore

if NOT %ERRORLEVEL% == 0 (
    echo dnu restore has failed
    goto cleanup
)

cd BenchmarkDotNet.IntegrationTests

echo -----------------------------
echo Running Dnx tests
echo -----------------------------

call dnvm run default -r clr --configuration RELEASE test 2> failedDnxTests.txt

if NOT %ERRORLEVEL% == 0 (
    echo DNX tests has failed
	type failedDnxTests.txt	
    goto cleanup
)

echo -----------------------------
echo Running Core tests for x64
echo -----------------------------

call dnvm run default -r coreclr --configuration RELEASE -arch x64 test 2> failedCoreTests.txt

if NOT %ERRORLEVEL% == 0 (
    echo CORE tests has failed
	type failedCoreTests.txt	
    goto cleanup
)

echo -----------------------------
echo All Classic tests has passed for both Dnx and Core
echo -----------------------------

:cleanup
del failedDnxTests.txt
del failedCoreTests.txt
FOR /D /R %cd%\.. %%X IN (*_*) DO RMDIR /S /Q "%%X"
cd ..