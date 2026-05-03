@echo off
chcp 65001 >nul
title 会场大屏播放控制 - 发布打包

echo ============================================
echo   会场大屏播放控制软件 - 自包含发布
echo ============================================
echo.
echo 目标: 生成单个文件夹，内含所有依赖，免安装运行
echo 路径: src\ShowPlayer.App\bin\publish\
echo.

set PROJECT_DIR=%~dp0src\ShowPlayer.App

echo [1/2] 执行自包含发布...
dotnet publish "%PROJECT_DIR%" ^
  --configuration Release ^
  --runtime win-x64 ^
  --self-contained true ^
  --output "%PROJECT_DIR%\bin\publish" ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:DebugType=none ^
  -p:FileVersion=1.0.0.0 ^
  -p:AssemblyVersion=1.0.0.0 ^
  --nologo

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [错误] 发布失败！
    pause
    exit /b 1
)

echo [2/2] 发布完成!
echo.
echo ============================================
echo   输出位置:
echo   %PROJECT_DIR%\bin\publish\ShowPlayer.App.exe
echo.
echo   文件大小请查看上述输出
echo   可将整个 publish 文件夹复制到 U 盘分发
echo ============================================
echo.
pause
