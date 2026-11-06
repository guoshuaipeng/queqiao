$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient, System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Ui3 {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
function Save([IntPtr]$hwnd, [string]$out) {
  $rect = New-Object Ui3+RECT
  [void][Ui3]::GetWindowRect($hwnd, [ref]$rect)
  $bmp = New-Object System.Drawing.Bitmap ($rect.Right - $rect.Left), ($rect.Bottom - $rect.Top)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc(); [void][Ui3]::PrintWindow($hwnd, $hdc, 2); $g.ReleaseHdc($hdc)
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
$dataHome = Join-Path $env:TEMP "queqiao-stop-home"
New-Item -ItemType Directory -Force -Path $dataHome | Out-Null
[System.IO.File]::WriteAllText((Join-Path $dataHome "settings.json"), '{"relay":"127.0.0.1:17991","trust":"ask"}')
$hostp = Start-Queqiao $dataHome
$helpp = Start-Queqiao $dataHome
[void][Ui3]::ShowWindow($hostp.MainWindowHandle, 9)
[void][Ui3]::ShowWindow($helpp.MainWindowHandle, 9)
[void][Ui3]::SetWindowPos($hostp.MainWindowHandle, [IntPtr]::Zero, 30, 30, 0, 0, 0x0001 -bor 0x0004)
[void][Ui3]::SetWindowPos($helpp.MainWindowHandle, [IntPtr]::Zero, 920, 30, 0, 0, 0x0001 -bor 0x0004)
Start-Sleep -Milliseconds 700
$hroot = [System.Windows.Automation.AutomationElement]::FromHandle($hostp.MainWindowHandle)
$groot = [System.Windows.Automation.AutomationElement]::FromHandle($helpp.MainWindowHandle)
$shot = "E:\IdeaProjects\remote-assist-wt-tauri\docs\tauri-migration\acceptance"
Save $hostp.MainWindowHandle (Join-Path $shot "desktop-before-stop.png")
if (-not (Invoke-Name $hroot "开启接受协助")) { Write-Output "no-start"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 1 }
$waiting = Wait-Name $hroot "复制连接凭证" 15
Write-Output "waiting=$waiting"
if (-not $waiting) {
  $names = @()
  $all = $hroot.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
  foreach ($el in $all) { if ($el.Current.Name -and $el.Current.Name -notlike "remoteassist:*") { $names += $el.Current.Name } }
  Write-Output ("names=" + (($names | Select-Object -Unique) -join " | "))
  Save $hostp.MainWindowHandle (Join-Path $shot "desktop-stop-failed.png")
  Stop-Process -Id $hostp.Id,$helpp.Id -Force
  exit 2
}
Save $hostp.MainWindowHandle (Join-Path $shot "desktop-while-waiting.png")
Invoke-Name $hroot "复制连接凭证" | Out-Null
Start-Sleep -Milliseconds 500
$editCond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
$payload = ""
foreach ($ed in $hroot.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)) {
  try {
    $val = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
    if ($val -and $val.StartsWith("remoteassist:pair") -and $val.Contains("127.0.0.1")) { $payload = $val; break }
  } catch {}
}
Write-Output "capturedPayload=$($payload.Length -gt 40)"
if (-not (Invoke-Name $hroot "停止接受")) { Write-Output "no-stop"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 3 }
$idle = Wait-Name $hroot "开启接受协助" 8
$noCopy = -not (Find-Name $hroot "复制连接凭证")
$noWait = -not (Find-Name $hroot "等待对方连接")
$noSession = -not (Find-Name $hroot "正在接受协助 · 已连接")
$leaked = $false
foreach ($ed in $hroot.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)) {
  try {
    $val = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
    if ($val -and $val.StartsWith("remoteassist:")) { $leaked = $true }
  } catch {}
}
Write-Output "backToIdle=$idle noCopy=$noCopy noWait=$noWait noSession=$noSession secretStillVisible=$leaked"
if ($leaked) {
  Write-Output "after-stop-not-saved"
} else {
  Save $hostp.MainWindowHandle (Join-Path $shot "desktop-after-stop.png")
}
$late = $false
$helperFail = $false
if ($payload.Length -gt 40) {
  $gedit = $groot.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $editCond)
  $gedit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($payload) | Out-Null
  Invoke-Name $groot "连接" | Out-Null
  Start-Sleep -Seconds 3
  $late = [bool](Find-Name $hroot "有人想协助这台电脑") -or [bool](Find-Name $hroot "正在接受协助 · 已连接")
  $helperFail = [bool](Find-Name $groot "没能连上，请检查网络后重试。")
  Write-Output "lateDialogOrSession=$late helperFail=$helperFail"
}
Stop-Process -Id $hostp.Id,$helpp.Id -Force
if (-not ($waiting -and $idle -and $noCopy -and $noWait -and $noSession -and -not $late -and $helperFail -and -not $leaked)) { exit 4 }
Write-Output "stop-ok"
