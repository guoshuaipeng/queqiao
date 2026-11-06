$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient, System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Ui5 {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
[void][Ui5]::SetProcessDPIAware()
function Save([IntPtr]$hwnd, [string]$out) {
  $rect = New-Object Ui5+RECT
  [void][Ui5]::GetWindowRect($hwnd, [ref]$rect)
  $w = $rect.Right - $rect.Left
  $h = $rect.Bottom - $rect.Top
  if ($w -lt 20 -or $h -lt 20) { return }
  $bmp = New-Object System.Drawing.Bitmap $w, $h
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc(); [void][Ui5]::PrintWindow($hwnd, $hdc, 2); $g.ReleaseHdc($hdc)
  $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  Write-Output "saved $out"
}
function Find-Name($root, $name) {
  $cond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty, $name)
  return $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
}
function Invoke-Name($root, $name) {
  $el = Find-Name $root $name
  if (-not $el) { return $false }
  $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
  return $true
}
function Wait-Name($root, $name, $seconds) {
  $deadline = (Get-Date).AddSeconds($seconds)
  do {
    if (Find-Name $root $name) { return $true }
    Start-Sleep -Milliseconds 300
  } while ((Get-Date) -lt $deadline)
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
$dataHome = Join-Path $env:TEMP "queqiao-cancel-home"
New-Item -ItemType Directory -Force -Path $dataHome | Out-Null
[System.IO.File]::WriteAllText((Join-Path $dataHome "settings.json"), '{"relay":"127.0.0.1:17991","trust":"ask"}')
$hostp = Start-Queqiao $dataHome
$helpp = Start-Queqiao $dataHome
[void][Ui5]::ShowWindow($hostp.MainWindowHandle, 9)
[void][Ui5]::ShowWindow($helpp.MainWindowHandle, 9)
[void][Ui5]::SetWindowPos($hostp.MainWindowHandle, [IntPtr]::Zero, 20, 20, 0, 0, 0x0001 -bor 0x0004)
[void][Ui5]::SetWindowPos($helpp.MainWindowHandle, [IntPtr]::Zero, 900, 20, 0, 0, 0x0001 -bor 0x0004)
Start-Sleep -Milliseconds 700
$hroot = [System.Windows.Automation.AutomationElement]::FromHandle($hostp.MainWindowHandle)
$groot = [System.Windows.Automation.AutomationElement]::FromHandle($helpp.MainWindowHandle)
$shot = "E:\IdeaProjects\remote-assist-wt-tauri\docs\tauri-migration\acceptance"
if (-not (Invoke-Name $hroot "开启接受协助")) { Write-Output "no-start"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 1 }
$waiting = Wait-Name $hroot "复制连接凭证" 15
Write-Output "waiting=$waiting"
if (-not $waiting) { Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 2 }
if (Has-Secret $hroot) { Write-Output "before-not-saved" } else { Save $hostp.MainWindowHandle (Join-Path $shot "desktop-before-cancel.png") }
Invoke-Name $hroot "复制连接凭证" | Out-Null
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
$gedit = $groot.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $editCond)
$gedit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($payload) | Out-Null
if (-not (Invoke-Name $groot "连接")) { Write-Output "no-connect"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 4 }
$asked = Wait-Name $hroot "有人想协助这台电脑" 15
$secretDuringDialog = Has-Secret $hroot
Write-Output "asked=$asked secretDuringDialog=$secretDuringDialog"
if (-not $asked) { Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 5 }
if ($secretDuringDialog) { Write-Output "dialog-not-saved" } else { Save $hostp.MainWindowHandle (Join-Path $shot "desktop-cancel-dialog.png") }
if (-not (Invoke-Name $groot "取消")) { Write-Output "no-cancel"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 6 }
$deadline = (Get-Date).AddSeconds(8)
$stillAsk = $true
do {
  Start-Sleep -Milliseconds 300
  $stillAsk = [bool](Find-Name $hroot "有人想协助这台电脑")
} while ($stillAsk -and (Get-Date) -lt $deadline)
Start-Sleep -Milliseconds 400
$stillWait = [bool](Find-Name $hroot "等待对方连接")
$stillCopy = [bool](Find-Name $hroot "复制连接凭证")
$hostSession = [bool](Find-Name $hroot "正在接受协助 · 已连接")
$helperSession = [bool](Find-Name $groot "正在协助对方 · 已连接")
$helperCancel = [bool](Find-Name $groot "取消")
$helperConnect = [bool](Find-Name $groot "连接")
Write-Output "stillAsk=$stillAsk stillWait=$stillWait stillCopy=$stillCopy hostSession=$hostSession helperSession=$helperSession helperCancel=$helperCancel helperConnect=$helperConnect"
Clear-Secret $hroot
Clear-Secret $groot
Start-Sleep -Milliseconds 400
if (Has-Secret $hroot) { Write-Output "host-after-not-saved" } else { Save $hostp.MainWindowHandle (Join-Path $shot "desktop-after-cancel.png") }
if (Has-Secret $groot) { Write-Output "helper-after-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-after-cancel-helper.png") }
Stop-Process -Id $hostp.Id,$helpp.Id -Force
if (-not ($asked -and -not $secretDuringDialog -and -not $stillAsk -and $stillWait -and $stillCopy -and -not $hostSession -and -not $helperSession -and -not $helperCancel -and $helperConnect)) { exit 7 }
Write-Output "cancel-ok"
