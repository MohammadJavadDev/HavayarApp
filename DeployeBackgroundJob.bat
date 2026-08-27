@echo off
chcp 65001 >nul
cd /d "%~dp0"

echo ========================================
echo  Havayar BackgroundJob Deploy
echo ========================================
echo.

REM -NoLogo -NoProfile: بدون کش پروفایل قدیمی
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0DeployeBackgroundJob.ps1" %*

set EXITCODE=%ERRORLEVEL%
echo.
if %EXITCODE% neq 0 (
    echo Deploy FAILED. Exit code: %EXITCODE%
) else (
    echo Deploy finished.
)

echo.
pause
exit /b %EXITCODE%
