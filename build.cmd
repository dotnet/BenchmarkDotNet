:<<"::CMDLITERAL"
@CALL build\build.bat %*
@GOTO :EOF
::CMDLITERAL
"$(cd "$(dirname "$0")"; pwd)/build/build.sh" "$@"