@echo off
setlocal enabledelayedexpansion

:: ========== 配置 ==========
set "PROJECT_DIR=%~dp0"
set "GAME_DIR=D:\EFT"
set "BUILD_CONFIG=Release"

:: ========== 颜色定义 ==========
set "RESET=0"
set "BOLD=1"
set "RED=31"
set "GREEN=32"
set "YELLOW=33"
set "BLUE=34"
set "MAGENTA=35"
set "CYAN=36"
set "WHITE=37"

:: ========== 函数 ==========
:print
set "text=%~1"
set "color=%~2"
echo.
echo  \033[%color%m%text%\033[0m
goto :eof

:header
call :print "========================================" %CYAN%
call :print "     Better Load - Auto Deploy Script" %BOLD%
call :print "========================================" %CYAN%
echo.
goto :eof

:success
call :print "  [OK] %~1" %GREEN%
goto :eof

:error
call :print "  [ERROR] %~1" %RED%
goto :eof

:info
call :print "  [INFO] %~1" %BLUE%
goto :eof

:: ========== 主流程 ==========
cd /d "%PROJECT_DIR%"

call :header

:: 1. Restore
call :info "Restoring NuGet packages..." %BLUE%
dotnet restore >nul 2>&1
if errorlevel 1 (
    call :error "Restore failed!" %RED%
    echo.
    pause
    exit /b 1
)
call :success "Restore complete" %GREEN%

:: 2. Build
echo.
call :info "Building project..." %BLUE%
dotnet build BetterLoad.csproj -c %BUILD_CONFIG% -v q >nul 2>&1
if errorlevel 1 (
    call :error "Build failed!" %RED%
    echo.
    pause
    exit /b 1
)
call :success "Build complete" %GREEN%

:: 3. Deploy
echo.
call :info "Deploying to game directory..." %BLUE%

if not exist "%GAME_DIR%\BepInEx\plugins" (
    call :error "Plugins directory not found: %GAME_DIR%\BepInEx\plugins" %RED%
    echo.
    pause
    exit /b 1
)

copy /Y "BetterLoad\BetterLoad.dll" "%GAME_DIR%\BepInEx\plugins\">nul 2>&1
if errorlevel 1 (
    call :error "Failed to copy BetterLoad.dll" %RED%
    echo.
    pause
    exit /b 1
)

copy /Y "com.betterload.plugin.jsonc" "%GAME_DIR%\BepInEx\plugins\">nul 2>&1
if errorlevel 1 (
    call :error "Failed to copy com.betterload.plugin.jsonc" %RED%
    echo.
    pause
    exit /b 1
)

:: ========== 完成 ==========
echo.
call :print "========================================" %GREEN%
call :print "          Deploy Complete!" %BOLD%
call :print "========================================" %GREEN%
echo.
echo  \033[36m  DLL:    \033[37m%D:\EFT%\BepInEx\plugins\BetterLoad.dll
echo  \033[36m  Config: \033[37m%D:\EFT%\BepInEx\plugins\com.betterload.plugin.jsonc
echo.
call :print "  Ready to use! Press any key to exit..." %YELLOW%
echo.
pause >nul