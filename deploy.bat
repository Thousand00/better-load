@echo off
setlocal enabledelayedexpansion

set "PROJECT_DIR=%~dp0"
set "GAME_DIR=D:\EFT"
set "BUILD_CONFIG=Release"

echo ========================================
echo   Better Load - Auto Deploy
echo ========================================
echo.

cd /d "%PROJECT_DIR%"

echo [1/3] Restoring NuGet packages...
dotnet restore
if errorlevel 1 (
    echo [ERROR] Restore failed!
    pause
    exit /b 1
)

echo.
echo [2/3] Building project...
dotnet build BetterLoad.csproj -c %BUILD_CONFIG%
if errorlevel 1 (
    echo [ERROR] Build failed!
    pause
    exit /b 1
)

echo.
echo [3/3] Deploying to game directory...

if not exist "%GAME_DIR%\BepInEx\plugins" (
    echo [ERROR] Game BepInEx plugins directory not found: %GAME_DIR%\BepInEx\plugins
    pause
    exit /b 1
)

copy /Y "BetterLoad\BetterLoad.dll" "%GAME_DIR%\BepInEx\plugins\"
if errorlevel 1 (
    echo [ERROR] Failed to copy BetterLoad.dll
    pause
    exit /b 1
)

copy /Y "com.betterload.plugin.jsonc" "%GAME_DIR%\BepInEx\plugins\"
if errorlevel 1 (
    echo [ERROR] Failed to copy com.betterload.plugin.jsonc
    pause
    exit /b 1
)

echo.
echo ========================================
echo   Deploy Complete!
echo ========================================
echo.
echo DLL:    %GAME_DIR%\BepInEx\plugins\BetterLoad.dll
echo Config: %GAME_DIR%\BepInEx\plugins\com.betterload.plugin.jsonc
echo.
pause