<#
.SYNOPSIS
  Publish RemoteAssist.Helper as a Windows x64 CLI bundle (optional).

.EXAMPLE
  .\scripts\publish-helper.ps1
  .\scripts\publish-helper.ps1 -FrameworkDependent
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

$project = Join-Path $root "src\RemoteAssist.Helper\RemoteAssist.Helper.csproj"
$tag = if ($FrameworkDependent) { "fd" } else { "sc" }
$outDir = Join-Path $root "artifacts\helper-$Runtime-$tag"

Write-Host "Publishing Helper -> $outDir"
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

$exe = Get-ChildItem $outDir -Filter "RemoteAssist.Helper.exe" | Select-Object -First 1
if (-not $exe) {
    Write-Error "RemoteAssist.Helper.exe not found in $outDir"
    exit 1
}

$sizeMb = [math]::Round($exe.Length / 1MB, 2)
Write-Host ""
Write-Host "OK: $($exe.FullName) ($sizeMb MB)"
Write-Host "用法: $($exe.Name) pair --payload ""remoteassist:pair?v=1&host=..."""
