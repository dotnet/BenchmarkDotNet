@ECHO OFF
PowerShell -NoProfile -NoLogo -ExecutionPolicy unrestricted -Command "$ErrorActionPreference='Stop';[System.Threading.Thread]::CurrentThread.CurrentCulture = ''; [System.Threading.Thread]::CurrentThread.CurrentUICulture = '';& '%~dp0build.ps1' %*; exit $LASTEXITCODE"
