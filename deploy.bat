@echo off
setlocal enabledelayedexpansion

set "PROJECT_DIR=%~dp0"
set "GAME_DIR=D:\EFT"
set "BUILD_CONFIG=Release"

cd /d "%PROJECT_DIR%"

cls
echo.
echo  ========================================
echo   Better Load - Auto Deploy Script
echo  ========================================
echo.

echo  [1/3] Restoring NuGet packages...
dotnet restore >nul 2>&1
if errorlevel 1 (
    echo  [ERROR] Restore failed!
    pause
    exit /b 1
)
echo  [OK] Restore complete

echo.
echo  [2/3] Building project...
dotnet build BetterLoad.csproj -c %BUILD_CONFIG% -v q >nul 2>&1
if errorlevel 1 (
    echo  [ERROR] Build failed!
    pause
    exit /b 1
)
echo  [OK] Build complete

echo.
echo  [3/3] Deploying to game directory...

if not exist "%GAME_DIR%\BepInEx\plugins" (
    echo  [ERROR] Plugins directory not found!
    pause
    exit /b 1
)

copy /Y "BetterLoad\BetterLoad.dll" "%GAME_DIR%\BepInEx\plugins\" >nul 2>&1
if errorlevel 1 (
    echo  [ERROR] Failed to copy BetterLoad.dll
    pause
    exit /b 1
)

if not exist "%GAME_DIR%\BepInEx\plugins\zh-cn" (
    mkdir "%GAME_DIR%\BepInEx\plugins\zh-cn" >nul 2>&1
)

copy /Y "com.betterload.plugin.jsonc" "%GAME_DIR%\BepInEx\plugins\zh-cn\" >nul 2>&1
if errorlevel 1 (
    echo  [ERROR] Failed to copy com.betterload.plugin.jsonc
    pause
    exit /b 1
)

echo.
echo  ========================================
echo           Deploy Complete!
echo  ========================================
echo.
echo    DLL:    %GAME_DIR%\BepInEx\plugins\BetterLoad.dll
echo    Config: %GAME_DIR%\BepInEx\plugins\zh-cn\com.betterload.plugin.jsonc
echo.
echo    Ready to use!
echo.
pause