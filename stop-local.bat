@echo off
echo Stopping MAF Studio (Local Development)...
echo.

echo Stopping all running processes...
taskkill /F /IM dotnet.exe 2>nul
taskkill /F /IM node.exe 2>nul

echo.
echo All services have been stopped.
echo.
echo To start services again, run: start-local.bat
echo.