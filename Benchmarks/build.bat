rmdir /S /Q bin\Builds

mkdir bin\Builds\v3.5-x86
MsBuild /t:Rebuild /p:Configuration=Release;PlatformTarget=x86;TargetFrameworkVersion=v3.5
copy bin\Release\* bin\Builds\v3.5-x86\*

mkdir bin\Builds\v4.0-x86
MsBuild /t:Rebuild /p:Configuration=Release;PlatformTarget=x86;TargetFrameworkVersion=v4.0
copy bin\Release\* bin\Builds\v4.0-x86\*

mkdir bin\Builds\v4.5-x86
MsBuild /t:Rebuild /p:Configuration=Release;PlatformTarget=x86;TargetFrameworkVersion=v4.5
copy bin\Release\* bin\Builds\v4.5-x86\*

mkdir bin\Builds\v3.5-x64
MsBuild /t:Rebuild /p:Configuration=Release;PlatformTarget=x64;TargetFrameworkVersion=v3.5
copy bin\Release\* bin\Builds\v3.5-x64\*

mkdir bin\Builds\v4.0-x64
MsBuild /t:Rebuild /p:Configuration=Release;PlatformTarget=x64;TargetFrameworkVersion=v4.0
copy bin\Release\* bin\Builds\v4.0-x64\*

mkdir bin\Builds\v4.5-x64
MsBuild /t:Rebuild /p:Configuration=Release;PlatformTarget=x64;TargetFrameworkVersion=v4.5
copy bin\Release\* bin\Builds\v4.5-x64\*