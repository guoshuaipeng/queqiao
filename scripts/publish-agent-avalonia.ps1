<#
.SYNOPSIS
  Publish RemoteAssist.Agent.Avalonia as framework-dependent small packages (linux/osx).
#>
param(
    [string]$Configuration = "Release",
    [string[]]$Runtimes = @("linux-x64", "osx-x64", "osx-arm64")
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$env:PATH = "C:\Users\user\AppData\Local\Microsoft\dotnet;" + $env:PATH
$project = Join-Path $root "src\RemoteAssist.Agent.Avalonia\RemoteAssist.Agent.Avalonia.csproj"

$readme = @"
鹊桥远程协助（Avalonia 被控窗 · framework-dependent 小包）

这个程序比较小，但需要先安装「.NET 8 运行时」。装一次就可以，它不是开发工具。

- Linux：用发行版软件源或微软提供的 .NET 8 运行时包安装（不要选 SDK）。
- macOS：用官方安装包或 Homebrew 安装 .NET 8 运行时（不要选 SDK）。
- 下载：https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0 （「运行时」那一栏）

打开后点「复制码+密码」，把复制出来的两行发给对方。
写确认标题为「对方想在这台电脑执行操作」；按钮「允许 / 拒绝」；超时不自动允许。
关窗不等于结束：要结束请点「结束这次协助」。

macOS 包：已出包；未经真机启动时交付说明会写明「已出包、未上真机」。
"@

foreach ($Runtime in $Runtimes) {
    $outDir = Join-Path $root "artifacts\agent-avalonia-$Runtime-fd"
    Write-Host "Publishing Agent.Avalonia -> $outDir"
    if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }

    & dotnet publish $project `
        -c $Configuration `
        -r $Runtime `
        -o $outDir `
        --self-contained false `
        /p:PublishSingleFile=false `
        --nologo
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Set-Content -Encoding utf8 (Join-Path $outDir "运行说明.txt") $readme

    $files = Get-ChildItem $outDir -File | Where-Object { $_.Extension -ne '.pdb' } | Sort-Object Length -Descending
    $total = ($files | Measure-Object Length -Sum).Sum
    $totalMb = [math]::Round($total / 1MB, 2)
    $totalKb = [math]::Round($total / 1KB, 1)
    Write-Host "OK: $outDir  total=$totalMb MB ($totalKb KB)"
    $files | Select-Object -First 12 Name, @{n='KB';e={[math]::Round($_.Length/1KB,1)}} | Format-Table -AutoSize
}
