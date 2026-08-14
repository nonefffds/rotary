@echo off
setlocal
set ISCC="%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
"%ISCC%" "%~dp0rotary.iss"
if errorlevel 1 goto :eof
echo.
echo Installer built: host\winui\installer\output\RotarySetup.exe
