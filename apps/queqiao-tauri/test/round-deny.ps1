$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient, System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Ui7 {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
[void][Ui7]::SetProcessDPIAware()
$marker = Join-Path $env:TEMP "queqiao-deny-marker.txt"
Remove-Item -Force $marker -ErrorAction SilentlyContinue
function Save([IntPtr]$hwnd, [string]$out) {
  $rect = New-Object Ui7+RECT
  [void][Ui7]::GetWindowRect($hwnd, [ref]$rect)
  $w = $rect.Right - $rect.Left
  $h = $rect.Bottom - $rect.Top
  if ($w -lt 20 -or $h -lt 20) { return }
  $bmp = New-Object System.Drawing.Bitmap $w, $h
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc(); [void][Ui7]::PrintWindow($hwnd, $hdc, 2); $g.ReleaseHdc($hdc)
  $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  Write-Output "saved $out"
}
function All-Els($root) {
  return $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
}
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
function Wait-Button($root, $name, $seconds) {
  $deadline = (Get-Date).AddSeconds($seconds)
  do { if (Find-Button $root $name) { return $true }; Start-Sleep -Milliseconds 300 } while ((Get-Date) -lt $deadline)
  return $false
}
function Has-Text($root, $name) {
  foreach ($el in (All-Els $root)) {
    if ($el.Current.Name -and $el.Current.Name.Contains($name)) { return $true }
  }
  return $false
}
function Wait-Text($root, $name, $seconds) {
  $deadline = (Get-Date).AddSeconds($seconds)
  do { if (Has-Text $root $name) { return $true }; Start-Sleep -Milliseconds 300 } while ((Get-Date) -lt $deadline)
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
function Put-Payload($root, $payload) {
  $editCond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
  foreach ($ed in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)) {
    try {
      $pat = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
      $cur = $pat.Current.Value
      if (-not $cur -or $cur.StartsWith("remoteassist:") -or $cur -eq "") {
        if (-not $cur -or $cur.StartsWith("remoteassist:")) { $pat.SetValue($payload) | Out-Null; return }
      }
    } catch {}
  }
  $ed = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $editCond)
  $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($payload) | Out-Null
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
$dataHome = Join-Path $env:TEMP "queqiao-deny-home"
New-Item -ItemType Directory -Force -Path $dataHome | Out-Null
[System.IO.File]::WriteAllText((Join-Path $dataHome "settings.json"), '{"relay":"127.0.0.1:17991","trust":"ask"}')
$hostp = Start-Queqiao $dataHome
$helpp = Start-Queqiao $dataHome
[void][Ui7]::ShowWindow($hostp.MainWindowHandle, 9)
[void][Ui7]::ShowWindow($helpp.MainWindowHandle, 9)
[void][Ui7]::SetWindowPos($hostp.MainWindowHandle, [IntPtr]::Zero, 20, 20, 0, 0, 0x0001 -bor 0x0004)
[void][Ui7]::SetWindowPos($helpp.MainWindowHandle, [IntPtr]::Zero, 900, 20, 0, 0, 0x0001 -bor 0x0004)
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
$allow = Find-ButtonPrefix $hroot "允许协助"
if (-not $allow) { Write-Output "no-allow"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 6 }
$allow.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
if (-not (Wait-Text $groot "正在协助对方 · 已连接" 12)) { Write-Output "no-session"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 7 }
if (Has-Secret $hroot) { Write-Output "session-not-saved" } else { Save $hostp.MainWindowHandle (Join-Path $shot "desktop-before-deny.png") }
if (-not (Invoke-Button $groot "提议一项改动")) { Write-Output "no-propose"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 8 }
Start-Sleep -Milliseconds 400
$empties = @()
foreach ($ed in $groot.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)) {
  try {
    $pat = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    $cur = $pat.Current.Value
    if (-not $cur) { $empties += $pat }
  } catch {}
}
if ($empties.Count -lt 2) { Write-Output "no-fields $($empties.Count)"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 9 }
$empties[0].SetValue("写一个标记文件") | Out-Null
$cmd = 'echo queqiao-deny-test> "%TEMP%\queqiao-deny-marker.txt"'
$empties[1].SetValue($cmd) | Out-Null
if (-not (Invoke-Button $groot "发给对方确认")) { Write-Output "no-send"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 10 }
$asked = Wait-Text $hroot "对方想在这台电脑执行操作" 12
Write-Output "writeAsked=$asked"
if (-not $asked) { Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 11 }
if (Has-Secret $hroot) { Write-Output "dialog-not-saved" } else { Save $hostp.MainWindowHandle (Join-Path $shot "desktop-deny-dialog.png") }
if (-not (Invoke-Button $hroot "拒绝")) { Write-Output "no-reject"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 12 }
$hostDenied = Wait-Text $hroot "已拒绝，未做任何改动" 8
$helpDenied = Wait-Text $groot "已拒绝，未做任何改动" 8
Start-Sleep -Milliseconds 500
$ran = Test-Path $marker
Write-Output "hostDenied=$hostDenied helpDenied=$helpDenied markerRan=$ran"
if (Has-Secret $hroot) { Write-Output "host-after-not-saved" } else { Save $hostp.MainWindowHandle (Join-Path $shot "desktop-after-deny.png") }
Clear-Secret $groot
Start-Sleep -Milliseconds 200
if (Has-Secret $groot) { Write-Output "helper-after-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-after-deny-helper.png") }
if (-not (Invoke-Button $hroot "结束本次协助")) { Write-Output "no-end"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 13 }
if (-not (Wait-Button $hroot "结束协助" 6)) { Write-Output "no-end-confirm"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 14 }
Invoke-Button $hroot "结束协助" | Out-Null
Start-Sleep -Seconds 1
Put-Payload $groot $payload
Invoke-Button $groot "连接" | Out-Null
$notWaiting = Wait-Text $groot "对方现在没有在等待协助" 10
$networkOnly = Has-Text $groot "没能连上，请检查网络后重试。"
Write-Output "notWaiting=$notWaiting networkOnly=$networkOnly"
Clear-Secret $groot
Start-Sleep -Milliseconds 200
if (Has-Secret $groot) { Write-Output "away-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-not-waiting.png") }
Stop-Process -Id $hostp.Id,$helpp.Id -Force
Remove-Item -Force $marker -ErrorAction SilentlyContinue
if (-not ($hostDenied -and $helpDenied -and -not $ran -and $notWaiting -and -not $networkOnly)) { exit 15 }
Write-Output "deny-ok"
