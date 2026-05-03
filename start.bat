@echo off
chcp 65001 >nul
title 会场大屏播放控制 - 启动器

echo ============================================
echo   会场大屏播放控制软件 - 开发启动脚本
echo ============================================
echo.

set PROJECT_DIR=%~dp0src\ShowPlayer.App
set OUTPUT_DIR=%PROJECT_DIR%\bin\Debug\net8.0-windows

echo [1/2] 正在编译项目...
dotnet build "%PROJECT_DIR%" --nologo -q
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [错误] 编译失败，请检查代码后重试。
    pause
    exit /b 1
)
echo         编译成功!

echo [2/2] 正在启动程序...
start "" "%OUTPUT_DIR%\ShowPlayer.App.exe"

echo         程序已启动，请在中控台窗口操作。
echo.
echo 提示: 展示窗口已自动打开，可拖拽到扩展屏幕并双击全屏。
echo.
pause
