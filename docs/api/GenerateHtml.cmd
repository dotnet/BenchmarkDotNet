@echo off
call vsvars32.bat
msbuild BenchmarkDotNet.shfbproj /v:m 
