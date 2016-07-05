@echo off

echo Starting Initial Cleanup
echo -----------------------------

if exist "testsOutput" rmdir /s /q "testsOutput"

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
call build/batchcopy.cmd "BenchmarkDotNet/bin/Release/net45/*.*" "testsOutput"
call build/batchcopy.cmd "BenchmarkDotNet.Diagnostics.Windows/bin/Release/net45/*.*" "testsOutput"
call build/batchcopy.cmd "BenchmarkDotNet.IntegrationTests/bin/Release/net451/*.*" "testsOutput"
call build/batchcopy.cmd "BenchmarkDotNet.Tests/bin/Release/net451/*.*" "testsOutput"
call build/batchcopy.cmd "BenchmarkDotNet.IntegrationTests.Classic/bin/Release/*.*" "testsOutput"
call build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/Microsoft.Diagnostics.Tracing.TraceEvent/1.0.41/lib/net40" "testsOutput"
call build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/xunit.runner.console/2.2.0-beta2-build3300/tools" "testsOutput"
call build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/xunit.extensibility.execution/2.2.0-beta2-build3300/lib/net45" "testsOutput"
call build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/xunit.extensibility.core/2.2.0-beta2-build3300/lib/net45" "testsOutput"
call build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/xunit.assert/2.2.0-beta2-build3300/lib/netstandard1.0" "testsOutput"

echo -----------------------------
echo Copying files ended
echo -----------------------------
echo Running Tests for Classic Desktop CLR
echo -----------------------------

call "testsOutput/xunit.console.exe" "testsOutput/BenchmarkDotNet.Tests.dll" "testsOutput/BenchmarkDotNet.IntegrationTests.dll" "testsOutput/BenchmarkDotNet.IntegrationTests.Classic.exe" -noshadow 2> failedTests.txt

if NOT %ERRORLEVEL% == 0 (	
	type failedTests.txt	
    goto cleanup
)

echo -----------------------------
echo All classic tests has passed
echo -----------------------------

:cleanup
del failedTests.txt