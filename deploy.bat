@echo off
setlocal enabledelayedexpansion

set "SRC=%~dp0main\BetterLoad"
set "DEST=D:\EFT\BepInEx\plugins\BetterLoad"

if not exist "%DEST%\Plugins" mkdir "%DEST%\Plugins"

copy /Y "%SRC%\BetterLoad.dll" "%DEST%\BetterLoad.dll"
if errorlevel 1 (
    echo [ERROR] Failed to copy BetterLoad.dll
    goto :end
)

for %%F in ("%SRC%\Plugins\*.dll") do (
    if /i "%%~nxF" neq "BetterLoad.dll" (
        copy /Y "%%F" "%DEST%\Plugins\"
    )
)

echo [OK] Deployed to %DEST%

:end
pause
