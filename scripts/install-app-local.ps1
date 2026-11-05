<#
.SYNOPSIS
  无 Inno 时的本机「软安装」：复制统一 App publish 产物到 LocalAppData\Programs\RemoteAssist。

.EXAMPLE
  .\scripts\install-app-local.ps1
  .\scripts\install-app-local.ps1 -DesktopShortcut
#>
param(
    [switch]$SkipPublish,
    [switch]$DesktopShortcut,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$env:PATH = "C:\Users\user\AppData\Local\Microsoft\dotnet;" + $env:PATH

$src = Join-Path $root "artifacts\app-win-x64-sc"
$dst = Join-Path $env:LOCALAPPDATA "Programs\RemoteAssist"

if (-not $SkipPublish) {
    & (Join-Path $PSScriptRoot "publish-app.ps1") -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$exe = Join-Path $src "RemoteAssist.exe"
if (-not (Test-Path $exe)) { throw "Missing $exe" }

New-Item -ItemType Directory -Force -Path $dst | Out-Null
Copy-Item -Path (Join-Path $src "*") -Destination $dst -Recurse -Force

$target = Join-Path $dst "RemoteAssist.exe"
$programs = [Environment]::GetFolderPath("Programs")
$startDir = Join-Path $programs "远端协助"
New-Item -ItemType Directory -Force -Path $startDir | Out-Null

$ws = New-Object -ComObject WScript.Shell
$lnk = $ws.CreateShortcut((Join-Path $startDir "远端协助.lnk"))
$lnk.TargetPath = $target
$lnk.WorkingDirectory = $dst
$lnk.Description = "远端协助（被控/控制）"
$lnk.Save()

$uninstallPs1 = Join-Path $dst "Uninstall-RemoteAssist.ps1"
@"
`$ErrorActionPreference = 'Stop'
`$app = '$dst'
Remove-Item (Join-Path `$env:APPDATA 'Microsoft\Windows\Start Menu\Programs\远端协助') -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path ([Environment]::GetFolderPath('Desktop')) '远端协助.lnk') -Force -ErrorAction SilentlyContinue
Remove-Item 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\RemoteAssist' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item `$app -Recurse -Force
Write-Host '已卸载远端协助'
"@ | Set-Content -Path $uninstallPs1 -Encoding UTF8

$ulnk = $ws.CreateShortcut((Join-Path $startDir "卸载 远端协助.lnk"))
$ulnk.TargetPath = "powershell.exe"
$ulnk.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$uninstallPs1`""
$ulnk.WorkingDirectory = $dst
$ulnk.Save()

if ($DesktopShortcut) {
    $dl = $ws.CreateShortcut((Join-Path ([Environment]::GetFolderPath("Desktop")) "远端协助.lnk"))
    $dl.TargetPath = $target
    $dl.WorkingDirectory = $dst
    $dl.Save()
}

$reg = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\RemoteAssist"
New-Item -Path $reg -Force | Out-Null
New-ItemProperty -Path $reg -Name "DisplayName" -Value "远端协助" -PropertyType String -Force | Out-Null
New-ItemProperty -Path $reg -Name "DisplayVersion" -Value "0.1.0" -PropertyType String -Force | Out-Null
New-ItemProperty -Path $reg -Name "Publisher" -Value "RemoteAssist" -PropertyType String -Force | Out-Null
New-ItemProperty -Path $reg -Name "InstallLocation" -Value $dst -PropertyType String -Force | Out-Null
New-ItemProperty -Path $reg -Name "DisplayIcon" -Value $target -PropertyType String -Force | Out-Null
New-ItemProperty -Path $reg -Name "UninstallString" -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$uninstallPs1`"" -PropertyType String -Force | Out-Null
New-ItemProperty -Path $reg -Name "NoModify" -Value 1 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path $reg -Name "NoRepair" -Value 1 -PropertyType DWord -Force | Out-Null

Write-Host "已安装到: $dst"
Write-Host "开始菜单: 远端协助"
Write-Host "运行: $target"