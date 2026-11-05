<#
.SYNOPSIS
  兼容旧名：转发到 install-app-local.ps1（统一 App）。
#>
param(
    [switch]$SkipPublish,
    [switch]$DesktopShortcut,
    [string]$Configuration = "Release"
)
& (Join-Path $PSScriptRoot "install-app-local.ps1") -SkipPublish:$SkipPublish -DesktopShortcut:$DesktopShortcut -Configuration $Configuration
exit $LASTEXITCODE