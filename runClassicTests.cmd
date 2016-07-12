@echo off

echo Starting Initial Cleanup
echo -----------------------------

if exist "tests/output" rmdir /s /q "tests/output"

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

mkdir "tests/output"
call build/batchcopy.cmd "src/BenchmarkDotNet/bin/Release/net45/*.*" "tests/output"
call build/batchcopy.cmd "src/BenchmarkDotNet.Diagnostics.Windows/bin/Release/net45/*.*" "tests/output"
call build/batchcopy.cmd "tests/BenchmarkDotNet.IntegrationTests/bin/Release/net451/*.*" "tests/output"
call build/batchcopy.cmd "tests/BenchmarkDotNet.Tests/bin/Release/net451/*.*" "tests/output"
call build/batchcopy.cmd "tests/BenchmarkDotNet.IntegrationTests.Classic/bin/Release/*.*" "tests/output"
call build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/Microsoft.Diagnostics.Tracing.TraceEvent/1.0.41/lib/net40" "tests/output"
call build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/xunit.runner.console/2.2.0-beta2-build3300/tools" "tests/output"
call build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/xunit.extensibility.execution/2.2.0-beta2-build3300/lib/net45" "tests/output"
call build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/xunit.extensibility.core/2.2.0-beta2-build3300/lib/net45" "tests/output"
call build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/xunit.assert/2.2.0-beta2-build3300/lib/netstandard1.0" "tests/output"

echo -----------------------------
echo Copying files ended
echo -----------------------------
echo Running Tests for Classic Desktop CLR
echo -----------------------------

call "tests/output/xunit.console.exe" "tests/output/BenchmarkDotNet.Tests.dll" "tests/output/BenchmarkDotNet.IntegrationTests.dll" "tests/output/BenchmarkDotNet.IntegrationTests.Classic.exe" -noshadow 2> failedTests.txt

if NOT %ERRORLEVEL% == 0 (	
	type failedTests.txt	
    goto cleanup
)

echo -----------------------------
echo All classic tests has passed
echo -----------------------------

:cleanup
del failedTests.txt