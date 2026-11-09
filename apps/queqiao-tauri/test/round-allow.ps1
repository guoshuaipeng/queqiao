$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient, System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Ui8 {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
[void][Ui8]::SetProcessDPIAware()
$allowMarker = Join-Path $env:TEMP "queqiao-allow-marker.txt"
$denyMarker = Join-Path $env:TEMP "queqiao-deny2-marker.txt"
Remove-Item -Force $allowMarker, $denyMarker -ErrorAction SilentlyContinue
function Save([IntPtr]$hwnd, [string]$out) {
  $rect = New-Object Ui8+RECT
  [void][Ui8]::GetWindowRect($hwnd, [ref]$rect)
  $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
  if ($w -lt 20 -or $h -lt 20) { return }
  $bmp = New-Object System.Drawing.Bitmap $w, $h
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc(); [void][Ui8]::PrintWindow($hwnd, $hdc, 2); $g.ReleaseHdc($hdc)
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
function Has-Text($root, $name) {
  foreach ($el in (All-Els $root)) { if ($el.Current.Name -and $el.Current.Name.Contains($name)) { return $true } }
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
$dataHome = Join-Path $env:TEMP "queqiao-allow-home"
New-Item -ItemType Directory -Force -Path $dataHome | Out-Null
[System.IO.File]::WriteAllText((Join-Path $dataHome "settings.json"), '{"relay":"127.0.0.1:17991","trust":"ask"}')
$hostp = Start-Queqiao $dataHome
$helpp = Start-Queqiao $dataHome
[void][Ui8]::ShowWindow($hostp.MainWindowHandle, 9)
[void][Ui8]::ShowWindow($helpp.MainWindowHandle, 9)
[void][Ui8]::SetWindowPos($hostp.MainWindowHandle, [IntPtr]::Zero, 20, 20, 0, 0, 0x0001 -bor 0x0004)
[void][Ui8]::SetWindowPos($helpp.MainWindowHandle, [IntPtr]::Zero, 900, 20, 0, 0, 0x0001 -bor 0x0004)
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
if (-not (Invoke-Button $groot "提议一项改动")) { Write-Output "no-propose"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 8 }
Start-Sleep -Milliseconds 300
$allowCmd = "echo queqiao-allow-ok& echo queqiao-allow-ok> $allowMarker"
if (-not (Fill-Propose $groot "回显一句" $allowCmd)) { Write-Output "no-fields"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 9 }
if (-not (Invoke-Button $groot "发给对方确认")) { Write-Output "no-send"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 10 }
if (-not (Wait-Text $hroot "对方想在这台电脑执行操作" 12)) { Write-Output "no-write-ask"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 11 }
$yes = Find-Button $hroot "允许"
if (-not $yes) { Write-Output "no-allow"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 12 }
$yes.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
$sawResult = Wait-Text $groot "queqiao-allow-ok" 12
$hostAllowed = Wait-Text $hroot "已允许" 8
Start-Sleep -Milliseconds 400
$allowFile = Test-Path $allowMarker
Write-Output "sawResult=$sawResult hostAllowed=$hostAllowed allowFile=$allowFile"
Clear-Secret $hroot
Clear-Secret $groot
Start-Sleep -Milliseconds 200
if (Has-Secret $hroot) { Write-Output "host-allow-not-saved" } else { Save $hostp.MainWindowHandle (Join-Path $shot "desktop-allow-host.png") }
if (Has-Secret $groot) { Write-Output "result-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-allow-result.png") }
if (-not (Fill-Propose $groot "不要执行" "echo no> $denyMarker")) { Write-Output "no-fields2"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 13 }
if (-not (Invoke-Button $groot "发给对方确认")) { Write-Output "no-send2"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 14 }
if (-not (Wait-Text $hroot "对方想在这台电脑执行操作" 12)) { Write-Output "no-write-ask2"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 15 }
if (-not (Invoke-Button $hroot "拒绝")) { Write-Output "no-reject"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 16 }
$denied = Wait-Text $groot "已拒绝，未做任何改动" 8
Start-Sleep -Milliseconds 500
$vals = Edit-Values $groot
$stuck = $false
foreach ($v in $vals) { if ($v -and ($v.Contains("不要执行") -or $v.Contains("queqiao-deny2") -or $v.Contains("queqiao-allow-ok"))) { $stuck = $true } }
$denyFile = Test-Path $denyMarker
Write-Output "denied=$denied stuckCommand=$stuck denyFile=$denyFile"
Clear-Secret $groot
Start-Sleep -Milliseconds 200
if (Has-Secret $groot) { Write-Output "clear-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-after-clear.png") }
Stop-Process -Id $hostp.Id,$helpp.Id -Force
Remove-Item -Force $allowMarker, $denyMarker -ErrorAction SilentlyContinue
if (-not ($sawResult -and $hostAllowed -and $allowFile -and $denied -and -not $stuck -and -not $denyFile)) { exit 17 }
Write-Output "allow-ok"
