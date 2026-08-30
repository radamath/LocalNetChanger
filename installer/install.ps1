#Requires -Version 5.1
param(
    [switch]$Silent,
    [switch]$DesktopShortcut,
    [switch]$StartupShortcut
)

$ErrorActionPreference = "Stop"

$AppName = "LocalNetChanger"
$AppExe = "LocalNetChanger.exe"
$InstallDir = Join-Path $env:ProgramFiles $AppName
$AppSource = Join-Path $PSScriptRoot "app"
$LicenseFile = Join-Path $PSScriptRoot "license.txt"

function Test-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]$identity
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Ensure-Administrator {
    if (Test-Administrator) { return }
    $args = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $PSCommandPath)
    if ($Silent) { $args += "-Silent" }
    if ($DesktopShortcut) { $args += "-DesktopShortcut" }
    if ($StartupShortcut) { $args += "-StartupShortcut" }
    Start-Process powershell.exe -Verb RunAs -ArgumentList $args | Out-Null
    exit
}

function Show-LicenseDialog {
    if ($Silent) { return $true }

    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing

    $form = New-Object System.Windows.Forms.Form
    $form.Text = "$AppName - GPL Lisans Sözleşmesi"
    $form.Size = New-Object System.Drawing.Size(640, 480)
    $form.StartPosition = "CenterScreen"
    $form.FormBorderStyle = "FixedDialog"
    $form.MaximizeBox = $false
    $form.MinimizeBox = $false

    $textBox = New-Object System.Windows.Forms.TextBox
    $textBox.Multiline = $true
    $textBox.ReadOnly = $true
    $textBox.ScrollBars = "Vertical"
    $textBox.Location = New-Object System.Drawing.Point(12, 12)
    $textBox.Size = New-Object System.Drawing.Size(600, 360)
    $textBox.Font = New-Object System.Drawing.Font("Segoe UI", 9)
    $textBox.Text = if (Test-Path $LicenseFile) { Get-Content $LicenseFile -Raw -Encoding UTF8 } else { "GPL v3 lisans metni bulunamadi." }
    $form.Controls.Add($textBox)

    $accept = New-Object System.Windows.Forms.Button
    $accept.Text = "Kabul Ediyorum"
    $accept.Location = New-Object System.Drawing.Point(432, 390)
    $accept.Size = New-Object System.Drawing.Size(90, 28)
    $accept.DialogResult = [System.Windows.Forms.DialogResult]::OK
    $form.Controls.Add($accept)

    $cancel = New-Object System.Windows.Forms.Button
    $cancel.Text = "Iptal"
    $cancel.Location = New-Object System.Drawing.Point(528, 390)
    $cancel.Size = New-Object System.Drawing.Size(84, 28)
    $cancel.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
    $form.Controls.Add($cancel)

    $form.AcceptButton = $accept
    $form.CancelButton = $cancel

    return ($form.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK)
}

function New-Shortcut {
    param([string]$ShortcutPath, [string]$TargetPath, [string]$Description)
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = $TargetPath
    $shortcut.WorkingDirectory = Split-Path $TargetPath
    $shortcut.Description = $Description
    $shortcut.Save()
}

function Register-Uninstall {
    $uninstallKey = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$AppName"
    New-Item -Path $uninstallKey -Force | Out-Null
    Set-ItemProperty $uninstallKey -Name "DisplayName" -Value $AppName
    Set-ItemProperty $uninstallKey -Name "DisplayVersion" -Value "1.0.0"
    Set-ItemProperty $uninstallKey -Name "Publisher" -Value "LocalNetChanger"
    Set-ItemProperty $uninstallKey -Name "DisplayIcon" -Value (Join-Path $InstallDir $AppExe)
    Set-ItemProperty $uninstallKey -Name "InstallLocation" -Value $InstallDir
    Set-ItemProperty $uninstallKey -Name "UninstallString" -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$(Join-Path $InstallDir 'uninstall.ps1')`""
    Set-ItemProperty $uninstallKey -Name "QuietUninstallString" -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$(Join-Path $InstallDir 'uninstall.ps1')`" -Silent"
    Set-ItemProperty $uninstallKey -Name "NoModify" -Value 1 -Type DWord
    Set-ItemProperty $uninstallKey -Name "NoRepair" -Value 1 -Type DWord
}

function Test-DotNetDesktopRuntime {
    $paths = @(
        (Join-Path ${env:ProgramFiles} "dotnet\shared\Microsoft.WindowsDesktop.App"),
        (Join-Path ${env:ProgramFiles(x86)} "dotnet\shared\Microsoft.WindowsDesktop.App"),
        "HKLM:\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App",
        "HKLM:\SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App"
    )

    if (Test-Path $paths[0]) {
        if (Get-ChildItem $paths[0] -Directory | Where-Object { $_.Name -like "8.*" }) { return $true }
    }

    foreach ($regPath in $paths[2..3]) {
        if (Test-Path $regPath) {
            $values = Get-ItemProperty $regPath -ErrorAction SilentlyContinue
            if ($values.PSObject.Properties.Name | Where-Object { $_ -like "8.*" }) { return $true }
        }
    }

    return $false
}

function Ensure-DotNetDesktopRuntime {
    if (Test-DotNetDesktopRuntime) { return }

    Add-Type -AssemblyName System.Windows.Forms
    $answer = [System.Windows.Forms.MessageBox]::Show(
        "LocalNetChanger icin .NET 8 Desktop Runtime gereklidir.`n`nRuntime yuklu degil. Indirme sayfasini acmak ister misiniz?",
        "$AppName Kurulum",
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Warning
    )

    if ($answer -eq [System.Windows.Forms.DialogResult]::Yes) {
        Start-Process "https://dotnet.microsoft.com/download/dotnet/8.0"
    }

    exit 1
}

Ensure-Administrator
Ensure-DotNetDesktopRuntime

if (-not (Show-LicenseDialog)) {
    Write-Host "Kurulum iptal edildi."
    exit 1
}

if (-not (Test-Path $AppSource)) {
    throw "Uygulama dosyalari bulunamadi: $AppSource"
}

Write-Host "$AppName kuruluyor -> $InstallDir"

Get-Process -Name "LocalNetChanger" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

if (Test-Path $InstallDir) {
    Remove-Item $InstallDir -Recurse -Force
}
New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
Copy-Item (Join-Path $AppSource "*") $InstallDir -Recurse -Force
Copy-Item $LicenseFile (Join-Path $InstallDir "LICENSE.txt") -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $PSScriptRoot "info.txt") (Join-Path $InstallDir "README-KURULUM.txt") -Force -ErrorAction SilentlyContinue

$uninstallScript = Join-Path $PSScriptRoot "uninstall.ps1"
if (Test-Path $uninstallScript) {
    Copy-Item $uninstallScript (Join-Path $InstallDir "uninstall.ps1") -Force
}

$exePath = Join-Path $InstallDir $AppExe
$programs = [Environment]::GetFolderPath("Programs")
$startMenuDir = Join-Path $programs $AppName
New-Item -ItemType Directory -Path $startMenuDir -Force | Out-Null
New-Shortcut (Join-Path $startMenuDir "$AppName.lnk") $exePath "$AppName"
New-Shortcut (Join-Path $startMenuDir "Lisans (GPL v3).lnk") (Join-Path $InstallDir "LICENSE.txt") "GPL v3 Lisans"

if ($DesktopShortcut) {
    $desktop = [Environment]::GetFolderPath("Desktop")
    New-Shortcut (Join-Path $desktop "$AppName.lnk") $exePath "$AppName"
}

if ($StartupShortcut) {
    $startup = [Environment]::GetFolderPath("Startup")
    New-Shortcut (Join-Path $startup "$AppName.lnk") $exePath "$AppName"
}

Register-Uninstall

Write-Host "Kurulum tamamlandi." -ForegroundColor Green
if (-not $Silent) {
    $run = Read-Host "LocalNetChanger simdi baslatilsin mi? (E/H)"
    if ($run -match '^[Ee]') {
        Start-Process $exePath -Verb RunAs
    }
}
