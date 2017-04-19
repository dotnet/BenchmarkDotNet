SETLOCAL
@echo off

echo -----------------------------
echo Enabling Developer Mode
echo -----------------------------

dism /online /add-capability /capabilityName:Tools.DeveloperMode.Core~~~~0.0.1.0

echo -----------------------------
echo Setting Device Portal Credentials to uap_integration/uap_integration
echo -----------------------------
webmanagement.exe -Credentials uap_integration uap_integration

echo -----------------------------
echo Call Dev Cmd 2017
echo -----------------------------

rem TODO: Add detection of VS2017
SET VS150COMNTOOLS=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\Tools\
call "%VS150COMNTOOLS%VsDevCmd.bat"

echo -----------------------------
echo Building sample executable
echo -----------------------------

pushd ..\samples

msbuild /t:Restore "BenchmarkDotNet.Samples.Uap\BenchmarkDotNet.Samples.Uap.csproj"
msbuild /t:Build "BenchmarkDotNet.Samples.Uap\BenchmarkDotNet.Samples.Uap.csproj" /p:configuration=Release

popd

echo -----------------------------
echo Building UAP 10.0 libraries
echo -----------------------------

pushd ..\src
msbuild /t:Build "BenchmarkDotNet\BenchmarkDotNet.csproj" /p:configuration=Release;TargetFramework=uap10.0
SET UAP_BIN=%CD%\BenchmarkDotNet\bin\Release\uap10.0
popd

echo -----------------------------
echo Running UAP Sample
echo -----------------------------

pushd ..\samples\BenchmarkDotNet.Samples.Uap\bin\Release\net46\win7-x86

BenchmarkDotNet.Samples.Uap.exe "%UAP_BIN%"
if NOT %ERRORLEVEL% == 0 (
    echo Running of samples has failed
    goto end
)
popd

:end
ENDLOCAL