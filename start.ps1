Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  会场大屏播放控制软件 - 开发启动脚本" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$ProjectDir = Join-Path $PSScriptRoot "src\ShowPlayer.App"

Push-Location $ProjectDir

Write-Host "[1/2] 正在编译项目..." -ForegroundColor Yellow
dotnet build --nologo -q
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Host ""
    Write-Host "[错误] 编译失败，请检查代码后重试。" -ForegroundColor Red
    pause
    exit 1
}
Write-Host "        编译成功!" -ForegroundColor Green

Pop-Location

$ExePath = Join-Path $ProjectDir "bin\Debug\net8.0-windows\ShowPlayer.App.exe"
Write-Host "[2/2] 正在启动程序..." -ForegroundColor Yellow
Start-Process -FilePath $ExePath

Write-Host "        程序已启动，请在中控台窗口操作。" -ForegroundColor Green
Write-Host ""
Write-Host "提示: 展示窗口已自动打开，可拖拽到扩展屏幕并双击全屏。" -ForegroundColor Gray
Write-Host ""
pause
