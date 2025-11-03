@ECHO OFF
ECHO - Checking for admin rights...
NET SESSION >nul 2>&1
if %errorlevel% NEQ 0 (
    ECHO ERROR: This script requires administrative privileges. Please run as administrator.
    PAUSE
    EXIT 1
)
ECHO - Disabling Fast Boot
REG ADD "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power" /V HiberbootEnabled /T REG_DWORD /D 0 /F
if %ERRORLEVEL% NEQ 0 (goto error)
ECHO - Disabling Memory Compression...
powershell.exe -Command "try{ Disable-MMAgent -MemoryCompression } catch { Write-Host "WARNING: Memory Compression may be already disabled. Please verify it is set to False below:" -ForegroundColor Yellow ; Get-MMAgent ; PAUSE } ; Exit 0"
if %ERRORLEVEL% NEQ 0 (goto error)
ECHO - Memory Compression Disabled!
SHUTDOWN /r /t 10 /c "Success! Your computer will now reboot..."
EXIT 0

:error
ECHO - An ERROR has occurred!
PAUSE
EXIT 1