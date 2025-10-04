@ECHO OFF
ECHO - Checking for admin rights...
NET SESSION >nul 2>&1
if %errorlevel% NEQ 0 (
    ECHO ERROR: This script requires administrative privileges. Please run as administrator.
    PAUSE
    EXIT 1
)
ECHO - Installing Required Runtimes, please follow the prompts below:
ECHO - Installing VC 2015+ Redist...
winget install Microsoft.VCRedist.2015+.x64
ECHO - Installing .NET10 Preview Runtimes...
winget install Microsoft.DotNet.DesktopRuntime.Preview --architecture x64
winget install Microsoft.DotNet.AspNetCore.Preview --architecture x64
PAUSE
EXIT 0