@echo off

echo Rembember to build solution first

if not exist "testsOutput" mkdir testsOutput

echo Copying files started
echo -----------------------------

call build/batchcopy.cmd "artifacts/bin/BenchmarkDotNet/Release/net45/*.*" "testsOutput"
call build/batchcopy.cmd "artifacts/bin/BenchmarkDotNet.Diagnostics/Release/net40/*.*" "testsOutput"
call build/batchcopy.cmd "artifacts/bin/BenchmarkDotNet.IntegrationTests/Release/net45/*.*" "testsOutput"
call build/batchcopy.cmd "artifacts/bin/BenchmarkDotNet.Tests/Release/net45/*.*" "testsOutput"
call build/batchcopy.cmd "BenchmarkDotNet.IntegrationTests.Classic/bin/Release" "testsOutput"
call build/batchcopy.cmd "%USERPROFILE%/.dnx/packages/Microsoft.Diagnostics.Tracing.TraceEvent/1.0.41/lib/net40" "testsOutput"
call build/batchcopy.cmd "%USERPROFILE%/.dnx/packages/xunit.runner.console/2.1.0/tools" "testsOutput"
call build/batchcopy.cmd "%USERPROFILE%/.dnx/packages/xunit.extensibility.execution/2.1.0/lib/net45" "testsOutput"
call build/batchcopy.cmd "%USERPROFILE%/.dnx/packages/xunit.extensibility.core/2.1.0/lib/portable-net45+win8+wp8+wpa81" "testsOutput"
call build/batchcopy.cmd "%USERPROFILE%/.dnx/packages/xunit.assert/2.1.0/lib/portable-net45+win8+wp8+wpa81" "testsOutput"

echo -----------------------------
echo Copying files ended

echo Running Tests
echo -----------------------------

call "testsOutput/xunit.console.exe" "testsOutput/BenchmarkDotNet.Tests.dll" "testsOutput/BenchmarkDotNet.IntegrationTests.dll" "testsOutput/BenchmarkDotNet.IntegrationTests.Classic.exe"
