param(
  [ValidateSet("Debug","Release")]
  [string]$Configuration = "Release"
)
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$env:PATH = "C:\Users\user\AppData\Local\Microsoft\dotnet;" + $env:PATH

Write-Host "== Building RemoteAssist.Net48 ($Configuration) ==" -ForegroundColor Cyan
Push-Location $root
try {
  dotnet restore .\RemoteAssist.Net48.sln
  if ($LASTEXITCODE -ne 0) { throw "restore failed" }
  dotnet build .\RemoteAssist.Net48.sln -c $Configuration --no-restore
  if ($LASTEXITCODE -ne 0) { throw "build failed" }

  $outDir = Join-Path $root "artifacts\app-net48-fd"
  New-Item -ItemType Directory -Force -Path $outDir | Out-Null
  Get-ChildItem $outDir -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
  dotnet publish .\src-net48\RemoteAssist.App\RemoteAssist.App.csproj -c $Configuration -o $outDir
  if ($LASTEXITCODE -ne 0) { throw "publish failed" }

  Write-Host ""
  Write-Host "Output: $outDir" -ForegroundColor Green
  Get-ChildItem $outDir -File | Sort-Object Length -Descending |
    Select-Object Name, @{N="KB";E={[math]::Round($_.Length/1KB,1)}} |
    Format-Table -AutoSize
  $exe = Get-Item (Join-Path $outDir "RemoteAssist.Net48.exe")
  $total = (Get-ChildItem $outDir -File | Measure-Object Length -Sum).Sum
  Write-Host ("EXE: {0:N1} KB | folder: {1:N1} KB ({2:N2} MB)" -f ($exe.Length/1KB), ($total/1KB), ($total/1MB))

  # R047/⑤：把 net48 MCP 宿主打进同一分发目录（亲友主路径）
  Write-Host "== Publishing net48 MCP into App output ==" -ForegroundColor Cyan
  & (Join-Path $PSScriptRoot "publish-mcp-net48.ps1") -Configuration $Configuration
  if ($LASTEXITCODE -ne 0) { throw "publish-mcp-net48 failed" }
  $mcpSrc = Join-Path $root "artifacts\mcp-net48-fd"
  Copy-Item (Join-Path $mcpSrc "*") $outDir -Recurse -Force
  Write-Host "MCP copied into $outDir"
  Write-Host "Run: $($exe.FullName)"
  Write-Host "Requires: .NET Framework 4.8 (usually preinstalled on Win10/11). No self-contained bundle needed."
}
finally {
  Pop-Location
}
