SET NAME=Benchmarks.exe
SET RUNFOLDER=bin\Runs\%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%
mkdir %RUNFOLDER%
cd %RUNFOLDER%
..\..\Builds\v3.5-x86\%Name% %* -of=ms-v3.5-x86.txt
..\..\Builds\v4.0-x86\%Name% %* -of=ms-v4.0-x86.txt
..\..\Builds\v4.5-x86\%Name% %* -of=ms-v4.5-x86.txt
..\..\Builds\v3.5-x64\%Name% %* -of=ms-v3.5-x64.txt
..\..\Builds\v4.0-x64\%Name% %* -of=ms-v4.0-x64.txt
..\..\Builds\v4.5-x64\%Name% %* -of=ms-v4.5-x64.txt
mono ..\..\Builds\v3.5-x86\%Name% %* -of=mono-v3.5-x86.txt
mono ..\..\Builds\v4.0-x86\%Name% %* -of=mono-v4.0-x86.txt
mono ..\..\Builds\v4.5-x86\%Name% %* -of=mono-v4.5-x86.txt
mono ..\..\Builds\v3.5-x64\%Name% %* -of=mono-v3.5-x64.txt
mono ..\..\Builds\v4.0-x64\%Name% %* -of=mono-v4.0-x64.txt
mono ..\..\Builds\v4.5-x64\%Name% %* -of=mono-v4.5-x64.txt

echo Total Results for '%*' > total.txt
echo. >> total.txt

echo ms-v3.5-x86 >> total.txt
cat ms-v3.5-x86.txt >> total.txt
echo. >> total.txt
echo ms-v4.0-x86 >> total.txt
cat ms-v4.0-x86.txt >> total.txt
echo. >> total.txt
echo ms-v4.5-x86 >> total.txt
cat ms-v4.5-x86.txt >> total.txt
echo. >> total.txt

echo ms-v3.5-x64 >> total.txt
cat ms-v3.5-x64.txt >> total.txt
echo. >> total.txt
echo ms-v4.0-x64 >> total.txt
cat ms-v4.0-x64.txt >> total.txt
echo. >> total.txt
echo ms-v4.5-x64 >> total.txt
cat ms-v4.5-x64.txt >> total.txt
echo. >> total.txt

echo mono-v3.5-x86 >> total.txt
cat mono-v3.5-x86.txt >> total.txt
echo. >> total.txt
echo mono-v4.0-x86 >> total.txt
cat mono-v4.0-x86.txt >> total.txt
echo. >> total.txt
echo mono-v4.5-x86 >> total.txt
cat mono-v4.5-x86.txt >> total.txt
echo. >> total.txt

echo mono-v3.5-x64 >> total.txt
cat mono-v3.5-x64.txt >> total.txt
echo. >> total.txt
echo mono-v4.0-x64 >> total.txt
cat mono-v4.0-x64.txt >> total.txt
echo. >> total.txt
echo mono-v4.5-x64 >> total.txt
cat mono-v4.5-x64.txt >> total.txt
echo. >> total.txt

Rscript ..\..\..\plot.R

cd ..\..\..