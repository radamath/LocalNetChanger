#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $Root "LocalNetChanger\LocalNetChanger.csproj"
$MsiProject = Join-Path $Root "installer\msi\LocalNetChanger.Installer.wixproj"
$IssFile = Join-Path $Root "installer\LocalNetChanger.iss"
$PublishDir = Join-Path $Root "publish\win-x64"
$DistDir = Join-Path $Root "dist"
$Version = "1.0.8"
$MsiOutput = Join-Path $DistDir "LocalNetChanger-Setup-$Version.msi"
$ExeOutput = Join-Path $DistDir "LocalNetChanger-Setup-$Version.exe"

$Wix = Join-Path $env:USERPROFILE ".dotnet\tools\wix.exe"
$Iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

Write-Host "==> LocalNetChanger yayinlaniyor (framework-dependent win-x64)..." -ForegroundColor Cyan
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }

dotnet publish $Project `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o $PublishDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish basarisiz oldu." }

New-Item -ItemType Directory -Force -Path $DistDir | Out-Null

if (Test-Path $Wix) {
    & $Wix eula accept wix7 2>$null | Out-Null

    Write-Host "==> WiX MSI kurulum paketi olusturuluyor..." -ForegroundColor Cyan
    dotnet build $MsiProject -c Release
    if ($LASTEXITCODE -ne 0) { throw "MSI derlemesi basarisiz oldu." }

    $builtMsi = @(
        (Join-Path $DistDir "tr-TR\LocalNetChanger-Setup-$Version.msi"),
        (Join-Path $DistDir "en-US\LocalNetChanger-Setup-$Version.msi"),
        (Join-Path $DistDir "LocalNetChanger-Setup-$Version.msi")
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1

    if (-not $builtMsi) { throw "MSI dosyasi bulunamadi." }

    if ($builtMsi -ne $MsiOutput) {
        Copy-Item $builtMsi $MsiOutput -Force
    }

    $sizeMb = [math]::Round((Get-Item $MsiOutput).Length / 1MB, 2)
    Write-Host ""
    Write-Host "MSI kurulum paketi hazir." -ForegroundColor Green
    Write-Host "Dosya: $MsiOutput" -ForegroundColor Green
    Write-Host "Boyut: $sizeMb MB" -ForegroundColor Green
    Write-Host ""
    Write-Host "MSI kurulumu:" -ForegroundColor Cyan
    Write-Host "  - Windows yerel kurulum formati" -ForegroundColor White
    Write-Host "  - GPL lisans sayfasi (Turkce sihirbaz)" -ForegroundColor White
    Write-Host "  - Program Ekle/Kaldir entegrasyonu" -ForegroundColor White
    Write-Host "  - .NET 8 Desktop Runtime kontrolu" -ForegroundColor White
}

if ($Iscc) {
    Write-Host "==> Inno Setup EXE paketi olusturuluyor..." -ForegroundColor Cyan
    & $Iscc $IssFile
    if ($LASTEXITCODE -eq 0 -and (Test-Path $ExeOutput)) {
        $exeMb = [math]::Round((Get-Item $ExeOutput).Length / 1MB, 2)
        Write-Host "Inno EXE: $ExeOutput ($exeMb MB)" -ForegroundColor Green
    }
}

if (-not (Test-Path $MsiOutput) -and -not (Test-Path $ExeOutput)) {
    throw "Hicbir kurulum paketi olusturulamadi. WiX veya Inno Setup gerekli."
}
