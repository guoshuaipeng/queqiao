# 构建鹊桥 MCP stdio 服务 → artifacts\mcp-win-x64-sc\RemoteAssist.Mcp.exe
# 用法：.\scripts\publish-mcp.ps1 [-Release]
param(
    [switch]$Release
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$profile = if ($Release) { "release" } else { "release" }
Write-Host "cargo build -p queqiao-mcp --$profile"
cargo build -p queqiao-mcp --$profile
if ($LASTEXITCODE -ne 0) {
    throw "cargo build failed: $LASTEXITCODE"
}

$src = Join-Path $root "target\$profile\remote_assist_mcp.exe"
if (-not (Test-Path $src)) {
    throw "missing build output: $src"
}

$destDir = Join-Path $root "artifacts\mcp-win-x64-sc"
New-Item -ItemType Directory -Force -Path $destDir | Out-Null
$dest = Join-Path $destDir "RemoteAssist.Mcp.exe"
Copy-Item -Force $src $dest
$hash = (Get-FileHash -Algorithm SHA256 $dest).Hash
Write-Host "OK $dest"
Write-Host "SHA256 $hash"
