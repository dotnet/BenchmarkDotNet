cd ..

rmdir /S /Q build\bin\Builds

mkdir build\bin\Builds\v3.5-x86
rmdir /S /Q bin\Release
MsBuild /t:Rebuild /p:Configuration=Release;PlatformTarget=x86;TargetFrameworkVersion=v3.5
copy bin\Release\* build\bin\Builds\v3.5-x86\*

mkdir build\bin\Builds\v4.0-x86
rmdir /S /Q bin\Release
MsBuild /t:Rebuild /p:Configuration=Release;PlatformTarget=x86;TargetFrameworkVersion=v4.0
copy bin\Release\* build\bin\Builds\v4.0-x86\*

mkdir build\bin\Builds\v4.5-x86
rmdir /S /Q bin\Release
MsBuild /t:Rebuild /p:Configuration=Release;PlatformTarget=x86;TargetFrameworkVersion=v4.5
copy bin\Release\* build\bin\Builds\v4.5-x86\*

mkdir build\bin\Builds\v3.5-x64
rmdir /S /Q bin\Release
MsBuild /t:Rebuild /p:Configuration=Release;PlatformTarget=x64;TargetFrameworkVersion=v3.5
copy bin\Release\* build\bin\Builds\v3.5-x64\*

mkdir build\bin\Builds\v4.0-x64
rmdir /S /Q bin\Release
MsBuild /t:Rebuild /p:Configuration=Release;PlatformTarget=x64;TargetFrameworkVersion=v4.0
copy bin\Release\* build\bin\Builds\v4.0-x64\*

mkdir build\bin\Builds\v4.5-x64
rmdir /S /Q bin\Release
MsBuild /t:Rebuild /p:Configuration=Release;PlatformTarget=x64;TargetFrameworkVersion=v4.5
copy bin\Release\* build\bin\Builds\v4.5-x64\*

cd build