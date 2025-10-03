@ECHO OFF
set OUT_DIR="%TEMP%\eftdma_%RANDOM%"
set CLIENT_DIR="%USERPROFILE%\Desktop\dmz\eft-dma-radar-lite"

ECHO - Creating Temporary Output Dir...
MKDIR %OUT_DIR%
if %ERRORLEVEL% NEQ 0 (goto ERROR)

ECHO - Publishing EFT DMA Radar Lite...
dotnet publish "src\eft-dma-radar-lite.csproj" ^
    -c Release ^
    -r win-x64 ^
    --no-self-contained ^
    /p:PublishSingleFile=true ^
    /p:DebugType=none ^
    -o %OUT_DIR%
if %ERRORLEVEL% NEQ 0 (goto ERROR)

ECHO - Kill existing process (if any)...
TASKKILL /F /IM "eft-dma-radar-lite.exe"
set PROC_KILLED=%ERRORLEVEL%

ECHO - Copying output to destination(s)...
CD %OUT_DIR%
7z a "%USERPROFILE%\Downloads\eft-dma-radar-lite.zip" * -r -aoa
if %ERRORLEVEL% NEQ 0 (goto ERROR)
XCOPY * %CLIENT_DIR% /E /H /C /Y
if %ERRORLEVEL% NEQ 0 (goto ERROR)

ECHO - Build OK
CD %USERPROFILE%
RD /S /Q %OUT_DIR%
if %PROC_KILLED% == 0 (
	ECHO - Restarting process before exiting...
	CD %CLIENT_DIR%
	START "" "eft-dma-radar-lite.exe"
)
EXIT 0

:ERROR
ECHO - ERROR!
CD %USERPROFILE%
RD /S /Q %OUT_DIR%
PAUSE
EXIT 1