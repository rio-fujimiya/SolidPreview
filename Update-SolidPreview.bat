@echo off
setlocal
cd /d "%~dp0"

if not exist "Install.ps1" (
    echo ERROR: Install.ps1 not found.
    pause
    exit /b 1
)
if not exist "Uninstall.ps1" (
    echo ERROR: Uninstall.ps1 not found.
    pause
    exit /b 1
)

set "PS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
set "RC=0"

echo [1/4] Stopping Explorer and preview host...
"%PS%" -NoProfile -ExecutionPolicy Bypass -Command "$s=(Get-Process -Id $PID).SessionId; Get-Process explorer,prevhost -ErrorAction SilentlyContinue | Where-Object { $_.SessionId -eq $s } | Stop-Process -Force -ErrorAction SilentlyContinue"

echo [2/4] Uninstalling previous version...
"%PS%" -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall.ps1"
if errorlevel 1 goto failed

echo [3/4] Installing new version...
"%PS%" -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1" %*
if errorlevel 1 goto failed
goto restart

:failed
set "RC=%ERRORLEVEL%"
echo ERROR: Update failed with exit code %RC%.

:restart
echo [4/4] Restarting Explorer...
"%PS%" -NoProfile -ExecutionPolicy Bypass -Command "$s=(Get-Process -Id $PID).SessionId; Get-Process explorer,prevhost -ErrorAction SilentlyContinue | Where-Object { $_.SessionId -eq $s } | Stop-Process -Force -ErrorAction SilentlyContinue; Start-Process explorer.exe"
if "%RC%"=="0" echo Update completed.
pause
exit /b %RC%
