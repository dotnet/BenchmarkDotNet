SETLOCAL
rem @echo off

echo -----------------------------
echo Enabling Developer Mode
echo -----------------------------

rem dism /online /add-capability /capabilityName:Tools.DeveloperMode.Core~~~~0.0.1.0

echo -----------------------------
echo Setting Device Portal Credentials to uap_integration/uap_integration
echo -----------------------------
rem webmanagement.exe -Credentials uap_integration uap_integration

echo -----------------------------
echo Call Dev Cmd 2017
echo -----------------------------

rem TODO: Add detection of VS2017
SET VS150COMNTOOLS=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\Tools\
call "%VS150COMNTOOLS%VsDevCmd.bat"

echo -----------------------------
echo Running dotnet restore
echo -----------------------------

pushd ..\samples

msbuild /t:Restore "BenchmarkDotNet.Samples.Uap\BenchmarkDotNet.Samples.Uap.csproj"
msbuild /t:Build "BenchmarkDotNet.Samples.Uap\BenchmarkDotNet.Samples.Uap.csproj" /p:configuration=Debug

if NOT %ERRORLEVEL% == 0 (
    echo Dotnet restore has failed
    rem goto end
)
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

pushd ..\samples\BenchmarkDotNet.Samples.Uap\bin\Debug\net46\win7-x86
echo %UAP_BIN%

BenchmarkDotNet.Samples.Uap.exe "%UAP_BIN%"
popd
REM echo -----------------------------
REM echo Running .NET 4.6 Host Integration tests
REM echo -----------------------------

REM call dotnet test "BenchmarkDotNet.IntegrationTests.Uap\BenchmarkDotNet.IntegrationTests.Uap.csproj" --configuration Release --framework net46

REM if NOT %ERRORLEVEL% == 0 (
    REM echo .NET 4.6 Integration tests has failed
    REM goto end
REM )

REM echo -----------------------------
REM echo Running Core Host Integration tests
REM echo -----------------------------

REM call dotnet test "BenchmarkDotNet.IntegrationTests.Uap\BenchmarkDotNet.IntegrationTests.Uap.csproj" --configuration Release --framework netcoreapp1.1

REM if NOT %ERRORLEVEL% == 0 (
    REM echo CORE Integration tests has failed
    REM goto end
REM )

REM echo -----------------------------
REM echo All tests has passed for Core
REM echo -----------------------------

:end
ENDLOCAL