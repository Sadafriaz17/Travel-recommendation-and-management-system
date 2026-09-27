@echo off
title NEXVOY AutoSync - csproj Watcher
color 0E
echo.
echo  =========================================
echo   NEXVOY .csproj Auto-Sync Launcher
echo  =========================================
echo   Keep this window open while working.
echo   New files will be added to the project
echo   automatically within 2 seconds.
echo  =========================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0AutoSync-Csproj.ps1"
pause
