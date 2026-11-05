<#
.SYNOPSIS
  Publish RemoteAssist.Agent.Cli as framework-dependent small packages (linux/osx).

.EXAMPLE
  .\scripts\publish-agent-cli.ps1
  .\scripts\publish-agent-cli.ps1 -Runtime linux-x64
#>
param(
    [string]$Configuration = "Release",
    [string[]]$Runtimes = @("linux-x64", "osx-x64", "osx-arm64")
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not $root) { $root = (Get-Location).Path }

$env:PATH = "C:\Users\user\AppData\Local\Microsoft\dotnet;" + $env:PATH

$project = Join-Path $root "src\RemoteAssist.Agent.Cli\RemoteAssist.Agent.Cli.csproj"
$readme = @"
鹊桥远程协助（framework-dependent 小包）

这个程序比较小，但需要先安装「.NET 8 运行时」。装一次就可以，它不是开发工具。

- Linux：用发行版软件源或微软提供的 .NET 8 运行时包安装（不要选 SDK）。
- macOS：用官方安装包或 Homebrew 安装 .NET 8 运行时（不要选 SDK）。
- 下载：https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0 （「运行时」那一栏）

启动后终端会打印「鹊桥远程协助已启动，正在等待对方连接。」以及两行会话码 / 验证密码。
写操作确认标题为「对方想在这台电脑执行操作」；请输入 允许 或 拒绝（也可以输入 y 或 n）；超时不自动允许。

macOS 包：已出包；未经真机启动时交付说明会写明「已出包、未上真机」。
"@

foreach ($Runtime in $Runtimes) {
    $outDir = Join-Path $root "artifacts\agent-cli-$Runtime-fd"
    Write-Host "Publishing Agent.Cli -> $outDir"
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

    $files = Get-ChildItem $outDir -File | Sort-Object Length -Descending
    $total = ($files | Measure-Object Length -Sum).Sum
    $totalMb = [math]::Round($total / 1MB, 2)
    Write-Host "OK: $outDir  total=$totalMb MB"
    $files | Select-Object -First 8 Name, @{n='KB';e={[math]::Round($_.Length/1KB,1)}} | Format-Table -AutoSize
}

# MCP linux/osx FD 同步小包（已是 net8.0）
$mcpProject = Join-Path $root "src\RemoteAssist.Mcp\RemoteAssist.Mcp.csproj"
foreach ($Runtime in $Runtimes) {
    $outDir = Join-Path $root "artifacts\mcp-$Runtime-fd"
    Write-Host "Publishing MCP -> $outDir"
    if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
    & dotnet publish $mcpProject `
        -c $Configuration `
        -r $Runtime `
        -o $outDir `
        --self-contained false `
        /p:PublishSingleFile=false `
        --nologo
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Set-Content -Encoding utf8 (Join-Path $outDir "运行说明.txt") $readme
    $total = ((Get-ChildItem $outDir -File | Measure-Object Length -Sum).Sum)
    Write-Host "OK MCP: $outDir  total=$([math]::Round($total/1MB,2)) MB"
}
