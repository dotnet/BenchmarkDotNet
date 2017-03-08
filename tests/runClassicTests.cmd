@echo off

echo Starting Initial Cleanup
echo -----------------------------

if exist "output" rmdir /s /q "output"

echo -----------------------------
echo Initial Cleanup finished
echo -----------------------------
echo Starting Build
echo -----------------------------

call dotnet msbuild ../BenchmarkDotNet.sln /t:build /property:Configuration=Release
if NOT %ERRORLEVEL% == 0 (	
    echo Error: Build has failed
	echo Build the solution manually from VS, new msbuild is having problems with F# and VB paths..
    rem exit /B TODO: uncomment when starts working..
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
call ../build/batchcopy.cmd "BenchmarkDotNet.IntegrationTests/bin/Release/net452/*.*" "output"
call ../build/batchcopy.cmd "BenchmarkDotNet.Tests/bin/Release/net452/*.*" "output"
call ../build/batchcopy.cmd "BenchmarkDotNet.IntegrationTests.Classic/bin/Release/*.*" "output"
call ../build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/Microsoft.Diagnostics.Tracing.TraceEvent/1.0.41/lib/net40" "output"
call ../build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/xunit.runner.console/2.2.0/tools" "output"
call ../build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/xunit.extensibility.execution/2.2.0/lib/net452" "output"
call ../build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/xunit.extensibility.core/2.2.0/lib/netstandard1.1" "output"
call ../build/batchcopy.cmd "%USERPROFILE%/.nuget/packages/xunit.assert/2.2.0/lib/netstandard1.1" "output"

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