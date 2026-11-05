<#
.SYNOPSIS
  Publish unified RemoteAssist App (accept + control modes) as Windows x64 single-file.

.EXAMPLE
  .\scripts\publish-app.ps1
  .\scripts\publish-app.ps1 -FrameworkDependent
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

$project = Join-Path $root "src\RemoteAssist.App\RemoteAssist.App.csproj"
$tag = if ($FrameworkDependent) { "fd" } else { "sc" }
$outDir = Join-Path $root "artifacts\app-$Runtime-$tag"

Write-Host "Publishing App -> $outDir"
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

$exe = Get-ChildItem $outDir -Filter "RemoteAssist.exe" | Select-Object -First 1
if (-not $exe) {
    Write-Error "RemoteAssist.exe not found in $outDir"
    exit 1
}

$sizeMb = [math]::Round($exe.Length / 1MB, 2)
Write-Host ""
Write-Host "OK: $($exe.FullName) ($sizeMb MB)"
Write-Host "双击打开后选择：接受协助（被控） / 去协助别人（控制）"
Write-Host "直达：RemoteAssist.exe --accept   或   RemoteAssist.exe --control"