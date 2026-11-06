<#
.SYNOPSIS
  Publish net48 Framework-dependent MCP host (stdio) for Cursor / R047 Setup.

.EXAMPLE
  .\scripts\publish-mcp-net48.ps1
#>
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not $root) { $root = (Get-Location).Path }
$env:PATH = "C:\Users\user\AppData\Local\Microsoft\dotnet;" + $env:PATH

$project = Join-Path $root "src-net48\RemoteAssist.Mcp\RemoteAssist.Mcp.csproj"
$outDir = Join-Path $root "artifacts\mcp-net48-fd"

Write-Host "Publishing net48 MCP (FW4.8 dependent) -> $outDir"
if (Test-Path $outDir) { Get-ChildItem $outDir -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

& dotnet publish $project -c $Configuration -o $outDir --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$exe = Get-Item (Join-Path $outDir "RemoteAssist.Mcp.exe") -ErrorAction Stop
Write-Host ""
Write-Host ("OK: {0} ({1:N1} KB)" -f $exe.FullName, ($exe.Length/1KB))
Write-Host "Requires: .NET Framework 4.8 (Windows). Not self-contained."
Write-Host "Smoke: & `"$($exe.FullName)`" --help"
