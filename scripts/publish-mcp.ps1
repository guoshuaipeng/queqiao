<#
.SYNOPSIS
  Publish RemoteAssist.Mcp as a Windows x64 bundle for Cursor / stdio hosts.

.EXAMPLE
  .\scripts\publish-mcp.ps1
  .\scripts\publish-mcp.ps1 -FrameworkDependent
#>
param(
    [switch]$FrameworkDependent,
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not $root) { $root = (Get-Location).Path }

$env:PATH = "C:\Users\user\AppData\Local\Microsoft\dotnet;" + $env:PATH

$project = Join-Path $root "src\RemoteAssist.Mcp\RemoteAssist.Mcp.csproj"
$tag = if ($FrameworkDependent) { "fd" } else { "sc" }
$outDir = Join-Path $root "artifacts\mcp-$Runtime-$tag"

Write-Host "Publishing MCP -> $outDir"
Write-Host "  self-contained: $(-not $FrameworkDependent)"

$dotnetArgs = @(
    "publish", $project,
    "-c", $Configuration,
    "-r", $Runtime,
    "-o", $outDir,
    "/p:PublishSingleFile=true",
    "/p:IncludeNativeLibrariesForSelfExtract=true",
    "--nologo"
)

if ($FrameworkDependent) {
    $dotnetArgs += @("--self-contained", "false")
} else {
    $dotnetArgs += @(
        "--self-contained", "true",
        "/p:EnableCompressionInSingleFile=true"
    )
}

& dotnet @dotnetArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$exe = Get-ChildItem $outDir -Filter "RemoteAssist.Mcp.exe" | Select-Object -First 1
if (-not $exe) {
    Write-Error "RemoteAssist.Mcp.exe not found in $outDir"
    exit 1
}

$sizeMb = [math]::Round($exe.Length / 1MB, 2)
Write-Host ""
Write-Host "OK: $($exe.FullName) ($sizeMb MB)"
if (-not $FrameworkDependent) {
    Write-Host "Cursor mcp.json 可用 command 指向该 exe（见 docs/MCP.md）。"
} else {
    Write-Host "框架依赖版：本机需安装 .NET 8 Runtime (Windows x64)"
    Write-Host "  https://dotnet.microsoft.com/download/dotnet/8.0"
}
Write-Host "冒烟: $($exe.Name) --help"
