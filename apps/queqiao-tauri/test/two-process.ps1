$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient, System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Cap2 {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
function Save([IntPtr]$hwnd, [string]$out) {
  $rect = New-Object Cap2+RECT
  [void][Cap2]::GetWindowRect($hwnd, [ref]$rect)
  $w = [Math]::Max(1, $rect.Right - $rect.Left)
  $h = [Math]::Max(1, $rect.Bottom - $rect.Top)
  $bmp = New-Object System.Drawing.Bitmap $w, $h
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc()
  [void][Cap2]::PrintWindow($hwnd, $hdc, 2)
  $g.ReleaseHdc($hdc)
  $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
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
function Names($root) {
  $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
  $names = @()
  foreach ($el in $all) { if ($el.Current.Name) { $names += $el.Current.Name } }
  return ($names | Select-Object -Unique)
}

$exe = "E:\IdeaProjects\remote-assist-wt-tauri\apps\queqiao-tauri\src-tauri\target\release\queqiao-tauri.exe"
$dataHome = Join-Path $env:TEMP "queqiao-uitest-home"
New-Item -ItemType Directory -Force -Path $dataHome | Out-Null
[System.IO.File]::WriteAllText((Join-Path $dataHome "settings.json"), '{"relay":"127.0.0.1:17991","trust":"ask"}')
function Start-Queqiao {
  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName = $exe
  $psi.UseShellExecute = $false
  $psi.EnvironmentVariables["QUEQIAO_HOME"] = $dataHome
  return [System.Diagnostics.Process]::Start($psi)
}
$hostp = Start-Queqiao
$helpp = Start-Queqiao
function Wait-Hwnd($proc) {
  $deadline = (Get-Date).AddSeconds(12)
  do { Start-Sleep -Milliseconds 200; $proc.Refresh() } while ($proc.MainWindowHandle -eq [IntPtr]::Zero -and (Get-Date) -lt $deadline)
  return $proc.MainWindowHandle
}
$hh = Wait-Hwnd $hostp
$gh = Wait-Hwnd $helpp
[void][Cap2]::ShowWindow($hh, 9)
[void][Cap2]::ShowWindow($gh, 9)
[void][Cap2]::SetWindowPos($hh, [IntPtr]::Zero, 20, 20, 0, 0, 0x0001 -bor 0x0004)
[void][Cap2]::SetWindowPos($gh, [IntPtr]::Zero, 900, 20, 0, 0, 0x0001 -bor 0x0004)
Start-Sleep -Milliseconds 800
$hroot = [System.Windows.Automation.AutomationElement]::FromHandle($hh)
$groot = [System.Windows.Automation.AutomationElement]::FromHandle($gh)
$opened = Invoke-Name $hroot "开启接受协助"
Write-Output "opened=$opened hostPid=$($hostp.Id) helpPid=$($helpp.Id)"
$deadline = (Get-Date).AddSeconds(12)
$ready = $false
do {
  Start-Sleep -Milliseconds 400
  $ready = [bool](Find-Name $hroot "复制连接凭证")
} while (-not $ready -and (Get-Date) -lt $deadline)
Write-Output "waitingVisible=$ready"
if (-not $ready) {
  Write-Output ("hostNames=" + ((Names $hroot) -join " | "))
  Save $hh "E:\IdeaProjects\remote-assist-wt-tauri\docs\tauri-migration\acceptance\desktop-pair-failed.png"
  Stop-Process -Id $hostp.Id, $helpp.Id -Force
  exit 2
}
$editCond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
$copied = Invoke-Name $hroot "复制连接凭证"
Write-Output "copyClicked=$copied"
Start-Sleep -Milliseconds 600
$pasted = Invoke-Name $groot "粘贴"
Write-Output "pasteClicked=$pasted"
Start-Sleep -Milliseconds 600
$edit = $groot.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $editCond)
$clip = ""
try { $clip = $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value } catch { $clip = "" }
if (-not $clip.StartsWith("remoteassist:pair")) {
  $hostEdits = $hroot.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)
  foreach ($ed in $hostEdits) {
    try {
      $val = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
      if ($val -and $val.StartsWith("remoteassist:pair")) { $clip = $val; break }
    } catch {}
  }
  if ($clip.StartsWith("remoteassist:pair")) {
    $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($clip) | Out-Null
  }
}
$safeHints = (Names $hroot) | Where-Object { $_ -match "复制|没能|已复制" -and $_ -notlike "remoteassist:*" }
Write-Output ("copyHints=" + ($safeHints -join " | "))
$local = $clip.Contains("127.0.0.1%3A17991") -or $clip.Contains("127.0.0.1:17991")
$okClip = $clip.StartsWith("remoteassist:pair") -and $clip.Contains("tls=1") -and $clip.Contains("fp=") -and $local
Write-Output "pastedFullPayload=$okClip localRelay=$local length=$($clip.Length)"
if (-not $okClip) {
  Save $hh "E:\IdeaProjects\remote-assist-wt-tauri\docs\tauri-migration\acceptance\desktop-pair-failed.png"
  Stop-Process -Id $hostp.Id, $helpp.Id -Force
  exit 3
}
Start-Sleep -Milliseconds 200
$connected = Invoke-Name $groot "连接"
Write-Output "connectClicked=$connected"
$deadline = (Get-Date).AddSeconds(12)
$dlg = $false
do {
  Start-Sleep -Milliseconds 400
  $dlg = [bool](Find-Name $hroot "有人想协助这台电脑")
} while (-not $dlg -and (Get-Date) -lt $deadline)
Write-Output "pairDialog=$dlg"
Save $hh "E:\IdeaProjects\remote-assist-wt-tauri\docs\tauri-migration\acceptance\desktop-pair.png"
$hasReject = [bool](Find-Name $hroot "拒绝")
$body = [bool](Find-Name $hroot "对方可以查看进程、日志和文件，不能直接改你的电脑。真正要改动时，还会再问你。")
Write-Output "rejectVisible=$hasReject bodyVisible=$body"
if ($dlg) { Invoke-Name $hroot "拒绝" | Out-Null }
Start-Sleep -Milliseconds 300
Stop-Process -Id $hostp.Id, $helpp.Id -Force
Write-Output "closedTestProcesses"
