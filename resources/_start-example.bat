@echo off
cls
:start
echo Starting server...

Server.exe

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
