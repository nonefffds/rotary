@echo off
setlocal
set NUGET_PACKAGES=D:\nuget
set DOTNET_CLI_TELEMETRY_OPTOUT=1

D:\dotnet\dotnet.exe publish -c Release -r win-x64 -p:Platform=x64 -o dist
if errorlevel 1 goto :eof

rem WinAppSDK's publish step misses the compiled XAML (.xbf) files - copy them from bin
set BIN=bin\x64\Release\net8.0-windows10.0.19041.0\win-x64
copy /y "%BIN%\App.xbf" dist\ >nul
copy /y "%BIN%\MainWindow.xbf" dist\ >nul
copy /y "%BIN%\SensorWindow.xbf" dist\ >nul

echo.
echo Published to dist\ - fully self-contained (WinUI runtime + .NET bundled,
echo no install needed on the target PC).
echo Copy the whole dist\ folder to any x64 Windows 10/11 machine.
