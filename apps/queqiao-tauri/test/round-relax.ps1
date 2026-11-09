$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient, System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Ui9 {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
[void][Ui9]::SetProcessDPIAware()
$timeoutMarker = Join-Path $env:TEMP "queqiao-timeout-marker.txt"
Remove-Item -Force $timeoutMarker -ErrorAction SilentlyContinue
function Save([IntPtr]$hwnd, [string]$out) {
  $rect = New-Object Ui9+RECT
  [void][Ui9]::GetWindowRect($hwnd, [ref]$rect)
  $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
  if ($w -lt 20 -or $h -lt 20) { return }
  $bmp = New-Object System.Drawing.Bitmap $w, $h
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc(); [void][Ui9]::PrintWindow($hwnd, $hdc, 2); $g.ReleaseHdc($hdc)
  $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  Write-Output "saved $out"
}
function All-Els($root) { return $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition) }
function Find-Button($root, $name) {
  foreach ($el in (All-Els $root)) {
    if ($el.Current.ControlType.ProgrammaticName -eq "ControlType.Button" -and $el.Current.Name -eq $name) { return $el }
  }
  return $null
}
function Find-ButtonPrefix($root, $prefix) {
  foreach ($el in (All-Els $root)) {
    if ($el.Current.ControlType.ProgrammaticName -eq "ControlType.Button" -and $el.Current.Name.StartsWith($prefix)) { return $el }
  }
  return $null
}
function Invoke-Button($root, $name) {
  $el = Find-Button $root $name
  if (-not $el) { return $false }
  $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
  return $true
}
function Result-Has($root, $needle) {
  foreach ($el in (All-Els $root)) {
    if ($el.Current.ControlType.ProgrammaticName -eq "ControlType.Edit") { continue }
    $text = $el.Current.Name
    if ($text -and $text.Contains($needle)) { return $true }
  }
  return $false
}
function Has-Text($root, $needle) {
  foreach ($el in (All-Els $root)) {
    $text = $el.Current.Name
    if ($text -and $text.Contains($needle)) { return $true }
  }
  return $false
}
function Wait-Text($root, $name, $seconds) {
  $deadline = (Get-Date).AddSeconds($seconds)
  do { if (Has-Text $root $name) { return $true }; Start-Sleep -Milliseconds 300 } while ((Get-Date) -lt $deadline)
  return $false
}
function Wait-Button($root, $name, $seconds) {
  $deadline = (Get-Date).AddSeconds($seconds)
  do { if (Find-Button $root $name) { return $true }; Start-Sleep -Milliseconds 300 } while ((Get-Date) -lt $deadline)
  return $false
}
function Has-Secret($root) {
  $editCond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
  foreach ($ed in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)) {
    try {
      $val = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
      if ($val -and $val.StartsWith("remoteassist:")) { return $true }
    } catch {}
  }
  return $false
}
function Clear-Secret($root) {
  $editCond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
  foreach ($ed in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)) {
    try {
      $pat = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
      if ($pat.Current.Value -and $pat.Current.Value.StartsWith("remoteassist:")) { $pat.SetValue("") | Out-Null }
    } catch {}
  }
}
function Edit-Values($root) {
  $editCond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
  $list = @()
  foreach ($ed in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)) {
    try { $list += $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value } catch {}
  }
  return $list
}
function Put-Payload($root, $payload) {
  $editCond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
  foreach ($ed in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)) {
    try {
      $pat = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
      $cur = $pat.Current.Value
      if (-not $cur -or $cur.StartsWith("remoteassist:")) { $pat.SetValue($payload) | Out-Null; return }
    } catch {}
  }
}
function Start-Queqiao([string]$dataDir) {
  $exe = "E:\IdeaProjects\remote-assist-wt-tauri\apps\queqiao-tauri\src-tauri\target\release\queqiao-tauri.exe"
  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName = $exe
  $psi.UseShellExecute = $false
  $psi.EnvironmentVariables["QUEQIAO_HOME"] = $dataDir
  $proc = [System.Diagnostics.Process]::Start($psi)
  $deadline = (Get-Date).AddSeconds(12)
  do { Start-Sleep -Milliseconds 200; $proc.Refresh() } while ($proc.MainWindowHandle -eq [IntPtr]::Zero -and (Get-Date) -lt $deadline)
  return $proc
}
function Fill-Propose($root, $summary, $command) {
  $editCond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
  $pats = @()
  foreach ($ed in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)) {
    try {
      $pat = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
      $cur = $pat.Current.Value
      if (-not $cur -or -not $cur.StartsWith("remoteassist:")) { $pats += $pat }
    } catch {}
  }
  if ($pats.Count -lt 2) { return $false }
  $pats[$pats.Count - 2].SetValue($summary) | Out-Null
  $pats[$pats.Count - 1].SetValue($command) | Out-Null
  return $true
}
$dataHome = Join-Path $env:TEMP "queqiao-relax-home"
New-Item -ItemType Directory -Force -Path $dataHome | Out-Null
[System.IO.File]::WriteAllText((Join-Path $dataHome "settings.json"), '{"relay":"127.0.0.1:17991","trust":"relaxed"}')
$hostp = Start-Queqiao $dataHome
$helpp = Start-Queqiao $dataHome
[void][Ui9]::ShowWindow($hostp.MainWindowHandle, 9)
[void][Ui9]::ShowWindow($helpp.MainWindowHandle, 9)
[void][Ui9]::SetWindowPos($hostp.MainWindowHandle, [IntPtr]::Zero, 20, 20, 0, 0, 0x0001 -bor 0x0004)
[void][Ui9]::SetWindowPos($helpp.MainWindowHandle, [IntPtr]::Zero, 900, 20, 0, 0, 0x0001 -bor 0x0004)
Start-Sleep -Milliseconds 700
$hroot = [System.Windows.Automation.AutomationElement]::FromHandle($hostp.MainWindowHandle)
$groot = [System.Windows.Automation.AutomationElement]::FromHandle($helpp.MainWindowHandle)
$shot = "E:\IdeaProjects\remote-assist-wt-tauri\docs\tauri-migration\acceptance"
if (-not (Invoke-Button $hroot "开启接受协助")) { Write-Output "no-start"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 1 }
if (-not (Wait-Button $hroot "复制连接凭证" 15)) { Write-Output "no-wait"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 2 }
Invoke-Button $hroot "复制连接凭证" | Out-Null
Start-Sleep -Milliseconds 700
$editCond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
$payload = ""
foreach ($ed in $hroot.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)) {
  try {
    $val = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
    if ($val -and $val.StartsWith("remoteassist:pair") -and $val.Contains("127.0.0.1")) { $payload = $val; break }
  } catch {}
}
Write-Output "capturedPayload=$($payload.Length -gt 40)"
if ($payload.Length -le 40) { Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 3 }
Put-Payload $groot $payload
if (-not (Invoke-Button $groot "连接")) { Write-Output "no-connect"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 4 }
if (-not (Wait-Text $hroot "有人想协助这台电脑" 15)) { Write-Output "no-ask"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 5 }
$allowPair = Find-ButtonPrefix $hroot "允许协助"
if (-not $allowPair) { Write-Output "no-pair-allow"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 6 }
$allowPair.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
if (-not (Wait-Text $groot "正在协助对方 · 已连接" 12)) { Write-Output "no-session"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 7 }
if (-not (Wait-Text $hroot "当前是「宽松」" 8)) { Write-Output "no-loose"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 8 }
Clear-Secret $hroot
Start-Sleep -Milliseconds 200
if (Has-Secret $hroot) { Write-Output "host-not-saved" } else { Save $hostp.MainWindowHandle (Join-Path $shot "desktop-relaxed-host.png") }
if (-not (Invoke-Button $groot "提议一项改动")) { Write-Output "no-propose"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 9 }
Start-Sleep -Milliseconds 300
if (-not (Fill-Propose $groot "打一句问候" "echo queqiao-relaxed-ok")) { Write-Output "no-fields"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 10 }
Clear-Secret $groot
Start-Sleep -Milliseconds 200
if (Has-Secret $groot) { Write-Output "before-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-relaxed-before.png") }
if (-not (Invoke-Button $groot "发给对方确认")) { Write-Output "no-send"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 11 }
$plainAsk = $false
$sawPlain = $false
$deadline = (Get-Date).AddSeconds(12)
do {
  if (Has-Text $hroot "对方想在这台电脑执行操作") { $plainAsk = $true }
  if (Result-Has $groot "queqiao-relaxed-ok") { $sawPlain = $true; break }
  Start-Sleep -Milliseconds 250
} while ((Get-Date) -lt $deadline)
$ran = Has-Text $hroot "已执行"
Write-Output "plainAsk=$plainAsk sawPlain=$sawPlain ran=$ran"
Clear-Secret $groot
Start-Sleep -Milliseconds 200
if (Has-Secret $groot) { Write-Output "plain-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-relaxed-ok.png") }
$risk = "del /q " + (Join-Path $env:TEMP "queqiao-relaxed-never.txt")
if (-not (Fill-Propose $groot "删一个不存在的文件" $risk)) { Write-Output "no-risk-fields"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 12 }
if (-not (Invoke-Button $groot "发给对方确认")) { Write-Output "no-risk-send"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 13 }
if (-not (Wait-Text $hroot "对方想在这台电脑执行操作" 12)) { Write-Output "no-risk-ask"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 14 }
Clear-Secret $hroot
Start-Sleep -Milliseconds 200
if (Has-Secret $hroot) { Write-Output "risk-not-saved" } else { Save $hostp.MainWindowHandle (Join-Path $shot "desktop-relaxed-risk.png") }
if (-not (Invoke-Button $hroot "拒绝")) { Write-Output "no-reject"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 15 }
$denied = Wait-Text $groot "已拒绝，未做任何改动" 8
$riskFile = Test-Path (Join-Path $env:TEMP "queqiao-relaxed-never.txt")
Write-Output "denied=$denied riskFile=$riskFile"
$probe = Join-Path $env:TEMP "queqiao-relax-probe"
New-Item -ItemType Directory -Force -Path $probe | Out-Null
$log = Join-Path $probe "app.log"
$lines = 1..100 | ForEach-Object {
  if ($_ -eq 1) { "queqiao-log-head" }
  elseif ($_ -eq 100) { "queqiao-log-tail" }
  else { "line-$_" }
}
Set-Content -Path $log -Value $lines -Encoding ascii
function Set-EmptyOrPath($root, $value) {
  $editCond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
  foreach ($ed in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)) {
    try {
      $pat = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
      $cur = $pat.Current.Value
      if (-not $cur -or $cur.StartsWith($probe)) { $pat.SetValue($value) | Out-Null; return $true }
    } catch {}
  }
  return $false
}
if (-not (Invoke-Button $groot "查看日志")) { Write-Output "no-log-btn"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 16 }
Start-Sleep -Milliseconds 300
if (-not (Set-EmptyOrPath $groot $log)) { Write-Output "no-path"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 17 }
$go = Find-Button $groot "查看"
if (-not $go) { Write-Output "no-go"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 18 }
$box = $go.Current.BoundingRectangle
$wide = $box.Width -ge 48 -and $box.Width -gt $box.Height
Write-Output "viewW=$([math]::Round($box.Width)) viewH=$([math]::Round($box.Height)) wide=$wide"
Clear-Secret $groot
Start-Sleep -Milliseconds 200
if (Has-Secret $groot) { Write-Output "path-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-log-button.png") }
$go.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
$logAsk = $false
$sawTail = $false
$sawHead = $false
$sawCount = $false
$deadline = (Get-Date).AddSeconds(12)
do {
  if (Has-Text $hroot "对方想在这台电脑执行操作") { $logAsk = $true }
  if (Result-Has $groot "queqiao-log-tail") { $sawTail = $true; $sawHead = Result-Has $groot "queqiao-log-head"; $sawCount = Result-Has $groot "只显示了末尾 80 行。"; break }
  Start-Sleep -Milliseconds 250
} while ((Get-Date) -lt $deadline)
$logNote = Has-Text $hroot "查看日志"
Write-Output "logAsk=$logAsk sawTail=$sawTail sawHead=$sawHead sawCount=$sawCount logNote=$logNote"
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 400
if (Has-Secret $groot) { Write-Output "log-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-log-tail.png") }
Stop-Process -Id $hostp.Id,$helpp.Id -Force
Remove-Item -Recurse -Force $probe -ErrorAction SilentlyContinue
if (-not ($sawPlain -and $ran -and -not $plainAsk -and $denied -and -not $riskFile -and $wide -and $sawTail -and -not $sawHead -and $sawCount -and $logNote -and -not $logAsk)) { exit 19 }
Write-Output "relax-ok"
