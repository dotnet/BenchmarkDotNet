@echo off

set SRC=%1
set DST=%2

set SRC=%SRC:/=\%
set DST=%DST:/=\%

if not exist %SRC% goto end

echo run: xcopy /F /Y /I %SRC% %DST%
xcopy /F /Y /I %SRC% %DST%

:end
