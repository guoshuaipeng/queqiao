<#
.SYNOPSIS
  Publish Agent + compile Inno Setup installer (Setup.exe).

.EXAMPLE
  .\scripts\build-installer.ps1
  .\scripts\build-installer.ps1 -SkipPublish
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

$agentOut = Join-Path $root "artifacts\agent-win-x64-sc"
$iss = Join-Path $root "installer\RemoteAssist.iss"
$installerOut = Join-Path $root "artifacts\installer"

if (-not $SkipPublish) {
    Write-Host "==> Publishing Agent (self-contained)..."
    & (Join-Path $PSScriptRoot "publish-agent.ps1") -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} else {
    Write-Host "==> SkipPublish: using existing $agentOut"
}

$exe = Join-Path $agentOut "RemoteAssist.Agent.exe"
if (-not (Test-Path $exe)) {
    Write-Error "Missing $exe — run without -SkipPublish or .\scripts\publish-agent.ps1 first."
    exit 1
}

function Find-ISCC {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe"
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
    Write-Host ""
    Write-Host "未找到 Inno Setup 编译器 ISCC.exe。" -ForegroundColor Yellow
    Write-Host "请安装 Inno Setup 6："
    Write-Host "  https://jrsoftware.org/isdl.php"
    Write-Host "  或 GitHub: https://github.com/jrsoftware/issrc/releases"
    Write-Host "安装后重新运行本脚本。脚本与 .iss 已就绪：$iss"
    Write-Host ""
    Write-Host "也可用便携安装（当前用户，无需 Inno）:"
    Write-Host "  .\scripts\install-agent-local.ps1"
    exit 2
}

New-Item -ItemType Directory -Force -Path $installerOut | Out-Null

# 同步版本到 iss（简单替换 OutputBaseFilename 已含宏；此处用 /D 覆盖 AgentSource）
Write-Host "==> Compiling installer with $iscc"
& $iscc "/DAgentSource=$agentOut" "/DMyAppVersion=$Version" $iss
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$setup = Get-ChildItem $installerOut -Filter "RemoteAssist-Agent-Setup-*.exe" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $setup) {
    Write-Error "Setup.exe not found under $installerOut"
    exit 1
}

Write-Host ""
Write-Host "OK: $($setup.FullName) ($([math]::Round($setup.Length/1MB,2)) MB)"
Write-Host "发给亲友：双击 Setup 安装 → 开始菜单打开「远端协助」。"
