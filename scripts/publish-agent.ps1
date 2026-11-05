<#
.SYNOPSIS
  Publish RemoteAssist.Agent as a Windows x64 bundle for friends/family copy.

.EXAMPLE
  .\scripts\publish-agent.ps1
  .\scripts\publish-agent.ps1 -FrameworkDependent
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

$project = Join-Path $root "src\RemoteAssist.Agent\RemoteAssist.Agent.csproj"
$tag = if ($FrameworkDependent) { "fd" } else { "sc" }
$outDir = Join-Path $root "artifacts\agent-$Runtime-$tag"

Write-Host "Publishing Agent -> $outDir"
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

$exe = Get-ChildItem $outDir -Filter "RemoteAssist.Agent.exe" | Select-Object -First 1
if (-not $exe) {
    Write-Error "RemoteAssist.Agent.exe not found in $outDir"
    exit 1
}

$sizeMb = [math]::Round($exe.Length / 1MB, 2)
Write-Host ""
Write-Host "OK: $($exe.FullName) ($sizeMb MB)"
if (-not $FrameworkDependent) {
    Write-Host "给亲友：拷贝整个目录 artifacts\agent-$Runtime-sc\（至少 exe；自包含通常无需再装 .NET）"
} else {
    Write-Host "框架依赖版：目标机需安装 .NET 8 Desktop Runtime (Windows x64)"
    Write-Host "  https://dotnet.microsoft.com/download/dotnet/8.0"
}
Write-Host "运行: $($exe.Name)   （可加 --headless / --yes / --port）"
