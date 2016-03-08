@echo off

echo Rembember to build solution first

cd BenchmarkDotNet.IntegrationTests

echo -----------------------------
echo Running Core tests for x64
echo -----------------------------

call dnvm run default -r coreclr --configuration RELEASE -arch x64 test

if NOT %ERRORLEVEL% == 0 (
    echo CORE tests has failed
    goto end
)


echo -----------------------------
echo Running Dnx tests
echo -----------------------------

call dnvm run default -r clr --configuration RELEASE test

if NOT %ERRORLEVEL% == 0 (
    echo DNX tests has failed
    goto end
)

:end