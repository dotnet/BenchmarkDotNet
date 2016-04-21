@echo off

echo Starting Initial Cleanup
echo -----------------------------

if exist "testsOutput" rmdir /s /q "testsOutput"
if exist "artifacts" rmdir /s /q "artifacts"

echo -----------------------------
echo Initial Cleanup finished
echo -----------------------------
echo Starting Build
echo -----------------------------

set _msbuildexe="%ProgramFiles%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% (
	echo Error: Could not find MSBuild.exe.
	exit /B
)

call %_msbuildexe% BenchmarkDotNet.sln /t:build /property:Configuration=Release
if NOT %ERRORLEVEL% == 0 (	
    echo Error: Build has failed
    exit /B
)

echo -----------------------------
echo Build finished
echo -----------------------------
echo Starting Copying files
echo -----------------------------

mkdir testsOutput
call build/batchcopy.cmd "artifacts/bin/BenchmarkDotNet/Release/net45/*.*" "testsOutput"
call build/batchcopy.cmd "artifacts/bin/BenchmarkDotNet.Diagnostics.Windows/Release/net40/*.*" "testsOutput"
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
echo -----------------------------
echo Running Tests for .NET 4.5
echo -----------------------------

call "testsOutput/xunit.console.exe" "testsOutput/BenchmarkDotNet.Tests.dll" "testsOutput/BenchmarkDotNet.IntegrationTests.dll" "testsOutput/BenchmarkDotNet.IntegrationTests.Classic.exe" 2> failedTests.txt

if NOT %ERRORLEVEL% == 0 (	
    echo Error: .NET 4.5 tests has failed:
	type failedTests.txt	
    goto cleanup
)

echo Running Tests for .NET 4.6
echo -----------------------------

call build/batchcopy.cmd "artifacts/bin/BenchmarkDotNet/Release/net46/*.*" "testsOutput"

call "testsOutput/xunit.console.exe" "testsOutput/BenchmarkDotNet.Tests.dll" "testsOutput/BenchmarkDotNet.IntegrationTests.dll" "testsOutput/BenchmarkDotNet.IntegrationTests.Classic.exe" 2> failedTests.txt

if NOT %ERRORLEVEL% == 0 (	
    echo Error: .NET 4.6 tests has failed:
	type failedTests.txt
    goto cleanup
)

echo -----------------------------
echo All tests has passed for both 4.5 and 4.6
echo -----------------------------

:cleanup
del failedTests.txt