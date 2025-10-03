@ECHO OFF
set OUT_DIR="%TEMP%\eftdma_%RANDOM%"
set CLIENT_DIR="%USERPROFILE%\Desktop\dmz\eft-dma-radar-lite"

ECHO - Creating Temporary Output Dir...
MKDIR %OUT_DIR%
if %ERRORLEVEL% NEQ 0 (goto ERROR)

ECHO - Publishing EFT DMA Radar Lite...
dotnet publish "src\eft-dma-radar-lite.csproj" ^
    --configuration Release ^
    --framework net10.0-windows ^
    --runtime win-x64 ^
    --no-self-contained ^
    /p:PublishSingleFile=true ^
    /p:DebugSymbols=false ^
    /p:DebugType=none ^
    --output %OUT_DIR%
if %ERRORLEVEL% NEQ 0 (goto ERROR)

ECHO - Copying output to destination(s)...
CD %OUT_DIR%
7z a "%USERPROFILE%\Downloads\eft-dma-radar-lite.zip" * -r -aoa
if %ERRORLEVEL% NEQ 0 (goto ERROR)
XCOPY * %CLIENT_DIR% /E /H /C /Y
if %ERRORLEVEL% NEQ 0 (goto ERROR)

ECHO - Build OK
CD %USERPROFILE%
RD /S /Q %OUT_DIR%
PAUSE
EXIT 0

:ERROR
ECHO - ERROR!
CD %USERPROFILE%
RD /S /Q %OUT_DIR%
PAUSE
EXIT 1