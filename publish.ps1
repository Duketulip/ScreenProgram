Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  会场大屏播放控制软件 - 发布打包" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "目标: 生成免安装绿色包，解压即用" -ForegroundColor Gray
Write-Host ""

$ProjectDir = Join-Path $PSScriptRoot "src\ShowPlayer.App"
$PublishDir = Join-Path $ProjectDir "bin\publish"
$ZipPath = Join-Path $PSScriptRoot "ShowPlayer_v1.0.0.zip"

Write-Host "[1/3] 清理旧发布..." -ForegroundColor Yellow
if (Test-Path $PublishDir) { Remove-Item -Path $PublishDir -Recurse -Force }
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }

Write-Host "[2/3] 执行自包含发布..." -ForegroundColor Yellow
Push-Location $ProjectDir
dotnet publish `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output $PublishDir `
  -p:DebugType=none `
  -p:FileVersion=1.0.0.0 `
  -p:AssemblyVersion=1.0.0.0 `
  --nologo
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Host ""
    Write-Host "[错误] 发布失败！" -ForegroundColor Red
    pause
    exit 1
}
Pop-Location

Write-Host "[3/3] 打包为 ZIP ..." -ForegroundColor Yellow
Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath -Force

$ZipSize = (Get-Item $ZipPath).Length / 1MB
$ExeSize = (Get-Item (Join-Path $PublishDir "ShowPlayer.App.exe")).Length / 1MB

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  ✅ 发布完成!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  📦 ZIP 包路径:" -ForegroundColor Yellow
Write-Host "  $ZipPath" -ForegroundColor White
Write-Host "  (ZIP 大小: $('{0:N1}' -f $ZipSize) MB)" 
Write-Host ""
Write-Host "  📁 解压后直接运行:" -ForegroundColor Yellow
Write-Host "  ShowPlayer.App.exe" -ForegroundColor White
Write-Host "  (程序大小: $('{0:N1}' -f $ExeSize) MB)"
Write-Host ""
Write-Host "  💡 使用说明:" -ForegroundColor Gray
Write-Host "  1. 将 ShowPlayer_v1.0.0.zip 发给用户" -ForegroundColor Gray
Write-Host "  2. 解压到任意目录" -ForegroundColor Gray
Write-Host "  3. 双击 ShowPlayer.App.exe 即可运行" -ForegroundColor Gray
Write-Host "  4. 无需安装 .NET 运行时" -ForegroundColor Gray
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
pause
