<#
.SYNOPSIS
  Publish RemoteAssist.Agent.Avalonia dual-mode as framework-dependent packages (linux/osx).
  Optionally packs linux-x64 into a .tar.gz with 运行说明.txt.
#>
param(
    [string]$Configuration = "Release",
    [string[]]$Runtimes = @("linux-x64", "osx-x64", "osx-arm64"),
    [switch]$SkipTarball
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$env:PATH = "C:\Users\user\AppData\Local\Microsoft\dotnet;" + $env:PATH
$project = Join-Path $root "src\RemoteAssist.Agent.Avalonia\RemoteAssist.Agent.Avalonia.csproj"

$readme = @"
鹊桥远程协助（Avalonia 双模式 · framework-dependent 小包）

这个程序比较小，但需要先安装「.NET 8 运行时」。装一次就可以，它不是开发工具。

- Linux：用发行版软件源或微软提供的 .NET 8 运行时包安装（不要选 SDK）。
- macOS：用官方安装包或 Homebrew 安装 .NET 8 运行时（不要选 SDK）。
- 下载：https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0 （「运行时」那一栏）

打开后首页可选「接受协助」或「去协助别人」。
- 接受协助：点「复制码+密码」，把复制出来的两行发给对方。
- 去协助别人：粘贴对方发来的会话码和验证密码，点「连接」。

写确认标题为「对方想在这台电脑执行操作」；按钮「允许 / 拒绝」；超时不自动允许。
关窗不等于结束：要结束请点「结束这次协助」。

macOS 包：已出包；未经真机启动时交付说明会写明「已出包、未上真机」。
"@

$sizes = @{}
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
    $sizes[$Runtime] = $totalMb
    Write-Host "OK: $outDir  total=$totalMb MB"
    $files | Select-Object -First 12 Name, @{n='KB';e={[math]::Round($_.Length/1KB,1)}} | Format-Table -AutoSize

    if ($Runtime -eq 'linux-x64' -and -not $SkipTarball) {
        $tarPath = Join-Path $root "artifacts\queqiao-linux-x64-fd.tar.gz"
        if (Test-Path $tarPath) { Remove-Item $tarPath -Force }
        # Prefer tar.exe (Windows 10+); pack folder contents under queqiao/
        $staging = Join-Path $root "artifacts\_queqiao-linux-stage"
        if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
        New-Item -ItemType Directory -Path (Join-Path $staging "鹊桥远程协助") | Out-Null
        Copy-Item -Path (Join-Path $outDir "*") -Destination (Join-Path $staging "鹊桥远程协助") -Recurse -Force
        Push-Location $staging
        try {
            & tar -czf $tarPath "鹊桥远程协助"
            if ($LASTEXITCODE -ne 0) { throw "tar failed" }
            $tarMb = [math]::Round((Get-Item $tarPath).Length / 1MB, 2)
            Write-Host "Tarball: $tarPath ($tarMb MB)"
        }
        finally { Pop-Location }
        Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Sizes summary:"
$sizes.GetEnumerator() | ForEach-Object { Write-Host ("  {0}: {1} MB" -f $_.Key, $_.Value) }
