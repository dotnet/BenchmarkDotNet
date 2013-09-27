SET NAME=Benchmarks.exe
SET RUNFOLDER=bin\Runs\%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%
mkdir %RUNFOLDER%
cd %RUNFOLDER%
..\..\Builds\v3.5-x86\%Name% %* -of=v3.5-x86.txt
..\..\Builds\v4.0-x86\%Name% %* -of=v4.0-x86.txt
..\..\Builds\v4.5-x86\%Name% %* -of=v4.5-x86.txt
..\..\Builds\v3.5-x64\%Name% %* -of=v3.5-x64.txt
..\..\Builds\v4.0-x64\%Name% %* -of=v4.0-x64.txt
..\..\Builds\v4.5-x64\%Name% %* -of=v4.5-x64.txt

echo Total Results > total.txt
echo. >> total.txt

echo v3.5-x86 >> total.txt
cat v3.5-x86.txt >> total.txt
echo. >> total.txt
echo v4.0-x86 >> total.txt
cat v4.0-x86.txt >> total.txt
echo. >> total.txt
echo v4.5-x86 >> total.txt
cat v4.5-x86.txt >> total.txt
echo. >> total.txt

echo v3.5-x64 >> total.txt
cat v3.5-x64.txt >> total.txt
echo. >> total.txt
echo v4.0-x64 >> total.txt
cat v4.0-x64.txt >> total.txt
echo. >> total.txt
echo v4.5-x64 >> total.txt
cat v4.5-x64.txt >> total.txt
echo. >> total.txt

cd ..\..\..