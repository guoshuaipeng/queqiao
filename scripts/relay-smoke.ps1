#Requires -Version 5.1
param(
  [string]$Addr = "127.0.0.1:17891",
  [string]$RelayDir = (Join-Path $PSScriptRoot "..\relay")
)

$ErrorActionPreference = "Stop"
$go = $null
if (Get-Command go -ErrorAction SilentlyContinue) { $go = (Get-Command go).Source }
elseif (Test-Path "C:\Program Files\Go\bin\go.exe") { $go = "C:\Program Files\Go\bin\go.exe" }
else { throw "go not found; install Go or refresh PATH" }

Push-Location $RelayDir
try {
  New-Item -ItemType Directory -Force -Path .\data | Out-Null
  Write-Host "Running unit tests..."
  & $go test .\internal\relay\ -count=1
  if ($LASTEXITCODE -ne 0) { throw "go test failed" }

  $env:RELAY_ADDR = "127.0.0.1:17891"
  $env:RELAY_DATA = (Join-Path (Get-Location) "data\relay-smoke.db")
  $proc = Start-Process -FilePath $go -ArgumentList @("run",".\cmd\relay") -PassThru -NoNewWindow -RedirectStandardOutput "$env:TEMP\relay-smoke-out.log" -RedirectStandardError "$env:TEMP\relay-smoke-err.log"
  Start-Sleep -Seconds 2
  try {
    & $go run .\cmd\smoketest -addr 127.0.0.1:17891
    if ($LASTEXITCODE -ne 0) { throw "smoketest failed" }
    Write-Host "relay-smoke.ps1 OK"
  }
  finally {
    if ($proc -and !$proc.HasExited) { Stop-Process -Id $proc.Id -Force }
  }
}
finally {
  Pop-Location
}
