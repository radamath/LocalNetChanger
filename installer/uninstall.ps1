#Requires -Version 5.1
param([switch]$Silent)

$ErrorActionPreference = "Stop"
$AppName = "LocalNetChanger"
$InstallDir = Join-Path $env:ProgramFiles $AppName

function Test-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]$identity
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Administrator)) {
    Start-Process powershell.exe -Verb RunAs -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $PSCommandPath, "-Silent") | Out-Null
    exit
}

Get-Process -Name "LocalNetChanger" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

$programs = [Environment]::GetFolderPath("Programs")
$startMenuDir = Join-Path $programs $AppName
$desktop = [Environment]::GetFolderPath("Desktop")
$startup = [Environment]::GetFolderPath("Startup")

@(
    (Join-Path $startMenuDir "$AppName.lnk"),
    (Join-Path $startMenuDir "Lisans (GPL v3).lnk"),
    (Join-Path $desktop "$AppName.lnk"),
    (Join-Path $startup "$AppName.lnk")
) | ForEach-Object { if (Test-Path $_) { Remove-Item $_ -Force } }

if (Test-Path $startMenuDir) { Remove-Item $startMenuDir -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path $InstallDir) { Remove-Item $InstallDir -Recurse -Force }

$uninstallKey = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$AppName"
if (Test-Path $uninstallKey) { Remove-Item $uninstallKey -Recurse -Force }

Write-Host "$AppName kaldirildi." -ForegroundColor Green
