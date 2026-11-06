$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient, System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Ui2 {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
function Save([IntPtr]$hwnd, [string]$out) {
  $rect = New-Object Ui2+RECT
  [void][Ui2]::GetWindowRect($hwnd, [ref]$rect)
  $bmp = New-Object System.Drawing.Bitmap ($rect.Right - $rect.Left), ($rect.Bottom - $rect.Top)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc(); [void][Ui2]::PrintWindow($hwnd, $hdc, 2); $g.ReleaseHdc($hdc)
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
$exe = "E:\IdeaProjects\remote-assist-wt-tauri\apps\queqiao-tauri\src-tauri\target\release\queqiao-tauri.exe"
$dir = "E:\IdeaProjects\remote-assist-wt-tauri\docs\tauri-migration\acceptance"
$shot = "E:\IdeaProjects\remote-assist-wt-tauri\docs\tauri-migration\acceptance"

function Start-WithRelay([string]$relay) {
  $dataHome = Join-Path $env:TEMP ("queqiao-" + ($relay -replace '[:\.]', '-'))
  New-Item -ItemType Directory -Force -Path $dataHome | Out-Null
  [System.IO.File]::WriteAllText((Join-Path $dataHome "settings.json"), "{`"relay`":`"$relay`",`"trust`":`"ask`"}")
  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName = $exe
  $psi.UseShellExecute = $false
  $psi.EnvironmentVariables["QUEQIAO_HOME"] = $dataHome
  $proc = [System.Diagnostics.Process]::Start($psi)
  $deadline = (Get-Date).AddSeconds(12)
  do { Start-Sleep -Milliseconds 200; $proc.Refresh() } while ($proc.MainWindowHandle -eq [IntPtr]::Zero -and (Get-Date) -lt $deadline)
  [void][Ui2]::ShowWindow($proc.MainWindowHandle, 9)
  [void][Ui2]::SetWindowPos($proc.MainWindowHandle, [IntPtr]::Zero, 40, 40, 0, 0, 0x0001 -bor 0x0004)
  Start-Sleep -Milliseconds 600
  return $proc
}

# 1. Closed port
$fail = Start-WithRelay "127.0.0.1:9"
$root = [System.Windows.Automation.AutomationElement]::FromHandle($fail.MainWindowHandle)
Save $fail.MainWindowHandle (Join-Path $shot "desktop-before-open.png")
if (-not (Invoke-Name $root "开启接受协助")) { Write-Output "FAIL no start button"; exit 1 }
$gotRetry = Wait-Name $root "重试开启" 15
$gotMsg = Wait-Name $root "没能开启接受协助，请检查网络后重试。" 2
$noWait = -not (Find-Name $root "等待对方连接")
$noCopy = -not (Find-Name $root "复制连接凭证")
$noSession = -not (Find-Name $root "正在接受协助 · 已连接")
Write-Output "retry=$gotRetry message=$gotMsg noWaiting=$noWait noCopy=$noCopy noSession=$noSession"
Save $fail.MainWindowHandle (Join-Path $shot "desktop-open-fail.png")
Stop-Process -Id $fail.Id -Force
if (-not ($gotRetry -and $gotMsg -and $noWait -and $noCopy -and $noSession)) { exit 2 }

# 2. Local relay must already be listening on 17991
$okp = Start-WithRelay "127.0.0.1:17991"
$root = [System.Windows.Automation.AutomationElement]::FromHandle($okp.MainWindowHandle)
if (-not (Invoke-Name $root "开启接受协助")) { Write-Output "FAIL no start"; Stop-Process -Id $okp.Id -Force; exit 3 }
$waiting = Wait-Name $root "复制连接凭证" 15
Write-Output "waitingForPeer=$waiting"
if (-not $waiting) { Save $okp.MainWindowHandle (Join-Path $shot "desktop-copy-failed.png"); Stop-Process -Id $okp.Id -Force; exit 4 }
Save $okp.MainWindowHandle (Join-Path $shot "desktop-before-copy.png")
Invoke-Name $root "复制连接凭证" | Out-Null
Start-Sleep -Milliseconds 700
$success = [bool](Find-Name $root "已复制，请完整发给对方。")
$failed = [bool](Find-Name $root "没能复制，请再点一次。")
Write-Output "copySuccessHint=$success copyFailHint=$failed"
$editCond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
$edits = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)
$focused = $false
$manual = $false
foreach ($ed in $edits) {
  try {
    $val = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
    if ($val -and $val.StartsWith("remoteassist:pair")) {
      $manual = $true
      $focused = $ed.Current.HasKeyboardFocus
    }
  } catch {}
}
Write-Output "manualBox=$manual manualFocused=$focused"
if ($success) {
  $pasted = Invoke-Name $root "粘贴"
  Start-Sleep -Milliseconds 400
  $round = $false
  $local = $false
  $len = 0
  $edits = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)
  foreach ($ed in $edits) {
    try {
      $val = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
      if ($val -and $val.StartsWith("remoteassist:pair")) {
        $round = $true
        $local = $val.Contains("127.0.0.1")
        $len = $val.Length
      }
    } catch {}
  }
  Write-Output "pasteClicked=$pasted roundtrip=$round localRelay=$local length=$len"
}
Save $okp.MainWindowHandle (Join-Path $shot "desktop-after-copy.png")
Stop-Process -Id $okp.Id -Force
if (-not $success) { exit 5 }
Write-Output "done"
