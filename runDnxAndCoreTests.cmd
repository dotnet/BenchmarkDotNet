@echo off

echo Rembember to build solution first

cd BenchmarkDotNet.IntegrationTests

echo Running Dnx tests
echo -----------------------------

call dnvm run default -r clr --configuration RELEASE test

echo -----------------------------
echo Running Core tests
echo -----------------------------

call dnvm run default -r coreclr --configuration RELEASE test