@echo off

echo Starting Initial Cleanup
echo -----------------------------

if exist "output" rmdir /s /q "output"

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

call %_msbuildexe% ../BenchmarkDotNet.sln /t:build /property:Configuration=Release
if NOT %ERRORLEVEL% == 0 (	
    echo Error: Build has failed
    exit /B
)

echo -----------------------------
echo Build finished
echo -----------------------------
echo Starting Copying files
echo -----------------------------

mkdir "output"
call ../build/batchcopy.cmd "../src/BenchmarkDotNet/bin/Release/net45/*.*" "output"
call ../build/batchcopy.cmd "../src/BenchmarkDotNet.Core/bin/Release/net45/*.*" "output"
call ../build/batchcopy.cmd "../src/BenchmarkDotNet.Toolchains.Roslyn/bin/Release/net45/*.*" "output"
call ../build/batchcopy.cmd "../src/BenchmarkDotNet.Diagnostics.Windows/bin/Release/net45/*.*" "output"
call ../build/batchcopy.cmd "BenchmarkDotNet.IntegrationTests/bin/Release/net451/*.*" "output"
call ../build/batchcopy.cmd "BenchmarkDotNet.Tests/bin/Release/net451/*.*" "output"
call ../build/batchcopy.cmd "BenchmarkDotNet.IntegrationTests.Classic/bin/Release/*.*" "output"
call ../build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/Microsoft.Diagnostics.Tracing.TraceEvent/1.0.41/lib/net40" "output"
call ../build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/xunit.runner.console/2.2.0-beta2-build3300/tools" "output"
call ../build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/xunit.extensibility.execution/2.2.0-beta2-build3300/lib/net45" "output"
call ../build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/xunit.extensibility.core/2.2.0-beta2-build3300/lib/net45" "output"
call ../build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/xunit.assert/2.2.0-beta2-build3300/lib/netstandard1.0" "output"

echo -----------------------------
echo Copying files ended
echo -----------------------------
echo Running Tests for Classic Desktop CLR
echo -----------------------------

call "output/xunit.console.exe" "output/BenchmarkDotNet.Tests.dll" "output/BenchmarkDotNet.IntegrationTests.dll" "output/BenchmarkDotNet.IntegrationTests.Classic.dll" -noshadow 2> failedTests.txt

if NOT %ERRORLEVEL% == 0 (	
	type failedTests.txt	
    goto cleanup
)

echo -----------------------------
echo All classic tests has passed
echo -----------------------------

:cleanup
del failedTests.txt