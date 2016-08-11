@echo off
REM Uncomment next line if msbuild isn't in the path but vs.net's common7\tools folder is. 
REM call vsvars32.bat
msbuild BenchmarkDotNet.shfbproj /v:m 
