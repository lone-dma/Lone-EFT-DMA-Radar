@ECHO OFF
ECHO - Checking for admin rights...
NET SESSION >nul 2>&1
if %errorlevel% NEQ 0 (
    ECHO ERROR: This script requires administrative privileges. Please run as administrator.
    PAUSE
    EXIT 1
)
ECHO - Installing VC 2015+ Redist...
winget install Microsoft.VCRedist.2015+.x64
PAUSE
EXIT 0