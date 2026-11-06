<#
.SYNOPSIS
  亲友主路径：publish net48 App+MCP → 编译 Inno Setup（FW4.8 依赖小包）。

.EXAMPLE
  .\scripts\build-installer-net48.ps1
  .\scripts\build-installer-net48.ps1 -SkipPublish
#>
param(
    [switch]$SkipPublish,
    [string]$Configuration = "Release",
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not $root) { $root = (Get-Location).Path }
$env:PATH = "C:\Users\user\AppData\Local\Microsoft\dotnet;" + $env:PATH

$appOut = Join-Path $root "artifacts\app-net48-fd"
$iss = Join-Path $root "installer\RemoteAssist-Net48.iss"
$installerOut = Join-Path $root "artifacts\installer"

if (-not $SkipPublish) {
    Write-Host "==> Building net48 App + MCP (framework-dependent)..."
    & (Join-Path $PSScriptRoot "build-net48.ps1") -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} else {
    Write-Host "==> SkipPublish: using existing $appOut"
}

$exe = Join-Path $appOut "RemoteAssist.Net48.exe"
$mcp = Join-Path $appOut "RemoteAssist.Mcp.exe"
if (-not (Test-Path $exe)) {
    Write-Error "Missing $exe — run without -SkipPublish or .\scripts\build-net48.ps1 first."
    exit 1
}
if (-not (Test-Path $mcp)) {
    Write-Host "MCP missing in app out; publishing..."
    & (Join-Path $PSScriptRoot "publish-mcp-net48.ps1") -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Copy-Item (Join-Path $root "artifacts\mcp-net48-fd\*") $appOut -Recurse -Force
}

function Find-ISCC {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    foreach ($c in $candidates) {
        if ($c -and (Test-Path $c)) { return $c }
    }
    $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

$iscc = Find-ISCC
if (-not $iscc) {
    Write-Host "未找到 ISCC.exe。请安装 Inno Setup 6：https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host "App+MCP 产物已在: $appOut"
    exit 2
}

New-Item -ItemType Directory -Force -Path $installerOut | Out-Null
Write-Host "==> Compiling net48 Setup with $iscc"
& $iscc "/DAppSource=$appOut" "/DMyAppVersion=$Version" $iss
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$setup = Get-ChildItem $installerOut -Filter "RemoteAssist-Net48-Setup-*.exe" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if (-not $setup) {
    Write-Error "Setup.exe not found under $installerOut"
    exit 1
}

$hash = (Get-FileHash $setup.FullName -Algorithm SHA256).Hash
Write-Host ""
Write-Host ("OK: {0}" -f $setup.FullName)
Write-Host ("bytes={0}  SHA256={1}" -f $setup.Length, $hash)
Write-Host "Requires: .NET Framework 4.8（旁见 installer/net48-runtime-note.txt）"
Write-Host "兼容备用（勿主推亲友）: .\scripts\build-installer.ps1 → RemoteAssist-Setup-*.exe (net8 SC)"
