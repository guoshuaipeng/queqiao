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
function Invoke-ButtonContains($root, $needle) {
  foreach ($el in (All-Els $root)) {
    if ($el.Current.ControlType.ProgrammaticName -ne "ControlType.Button") { continue }
    $text = $el.Current.Name
    if ($text -and $text.Contains($needle)) {
      $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
      return $true
    }
  }
  return $false
}
function Select-Radio($root, $needle) {
  foreach ($el in (All-Els $root)) {
    if ($el.Current.ControlType.ProgrammaticName -ne "ControlType.RadioButton") { continue }
    $text = $el.Current.Name
    if ($text -and $text.Contains($needle)) {
      $el.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
      return $true
    }
  }
  return $false
}
function Shown-Text($root, $needle) {
  foreach ($el in (All-Els $root)) {
    if ($el.Current.IsOffscreen) { continue }
    $text = $el.Current.Name
    if ($text -and $text.Contains($needle)) { return $true }
  }
  return $false
}
$dataHome = Join-Path $env:TEMP "queqiao-again-home"
New-Item -ItemType Directory -Force -Path $dataHome | Out-Null
[System.IO.File]::WriteAllText((Join-Path $dataHome "settings.json"), '{"relay":"127.0.0.1:17991","trust":"ask"}')
$ran = Join-Path $env:TEMP "queqiao-again-ran.txt"
$held = Join-Path $env:TEMP "queqiao-again-held.txt"
Remove-Item -Force $ran, $held -ErrorAction SilentlyContinue
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
if (-not (Invoke-ButtonContains $hroot "设置")) { Write-Output "no-settings"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 8 }
Start-Sleep -Milliseconds 400
if (-not (Select-Radio $hroot "完全信任")) { Write-Output "no-radio"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 9 }
if (-not (Wait-Text $hroot "这次协助不再询问了吗？" 8)) { Write-Output "no-full-ask"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 10 }
if (-not (Invoke-Button $hroot "确定，这次不再询问")) { Write-Output "no-full-ok"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 11 }
Start-Sleep -Milliseconds 400
if (-not (Invoke-ButtonContains $hroot "返回")) { Write-Output "no-back"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 12 }
if (-not (Wait-Text $hroot "当前是「完全信任」" 8)) { Write-Output "no-full-banner"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 13 }
Clear-Secret $hroot
Start-Sleep -Milliseconds 200
if (Has-Secret $hroot) { Write-Output "banner-not-saved" } else { Save $hostp.MainWindowHandle (Join-Path $shot "desktop-again-full.png") }
if (-not (Invoke-Button $groot "提议一项改动")) { Write-Output "no-propose"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 14 }
Start-Sleep -Milliseconds 300
$ranCmd = "echo queqiao-again-ran& echo queqiao-again-ran>$ran"
if (-not (Fill-Propose $groot "写一句已执行" $ranCmd)) { Write-Output "no-fields"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 15 }
if (-not (Invoke-Button $groot "发给对方确认")) { Write-Output "no-send"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 16 }
$fullAsk = $false
$sawRan = $false
$deadline = (Get-Date).AddSeconds(12)
do {
  if (Has-Text $hroot "对方想在这台电脑执行操作") { $fullAsk = $true }
  if (Result-Has $groot "queqiao-again-ran") { $sawRan = $true; break }
  Start-Sleep -Milliseconds 250
} while ((Get-Date) -lt $deadline)
Start-Sleep -Milliseconds 400
$ranFile = Test-Path $ran
$executed = Has-Text $hroot "已执行"
$cleared = $false
$deadline = (Get-Date).AddSeconds(6)
do {
  $stuck = $false
  foreach ($v in (Edit-Values $groot)) {
    if ($v -and ($v.Contains("queqiao-again-ran") -or $v.Contains("写一句已执行"))) { $stuck = $true }
  }
  if (-not $stuck) { $cleared = $true; break }
  Start-Sleep -Milliseconds 200
} while ((Get-Date) -lt $deadline)
Write-Output "fullAsk=$fullAsk sawRan=$sawRan ranFile=$ranFile executed=$executed cleared=$cleared"
Clear-Secret $groot
Start-Sleep -Milliseconds 200
if (Has-Secret $groot) { Write-Output "cleared-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-cleared.png") }
if (-not (Invoke-Button $hroot "结束本次协助")) { Write-Output "no-end"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 17 }
if (-not (Wait-Button $hroot "结束协助" 6)) { Write-Output "no-end-confirm"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 18 }
if (-not (Invoke-Button $hroot "结束协助")) { Write-Output "no-end-click"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 19 }
$hostHome = Wait-Button $hroot "开启接受协助" 10
$helpHome = Wait-Button $groot "连接" 10
Write-Output "hostHome=$hostHome helpHome=$helpHome"
if (-not ($hostHome -and $helpHome)) { Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 20 }
Clear-Secret $hroot
Clear-Secret $groot
Start-Sleep -Milliseconds 200
if (Has-Secret $hroot) { Write-Output "home-not-saved" } else { Save $hostp.MainWindowHandle (Join-Path $shot "desktop-again-home.png") }
if (-not (Invoke-Button $hroot "开启接受协助")) { Write-Output "no-restart"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 21 }
if (-not (Wait-Button $hroot "复制连接凭证" 15)) { Write-Output "no-wait2"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 22 }
Invoke-Button $hroot "复制连接凭证" | Out-Null
Start-Sleep -Milliseconds 700
$payload2 = ""
foreach ($ed in $hroot.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)) {
  try {
    $val = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
    if ($val -and $val.StartsWith("remoteassist:pair") -and $val.Contains("127.0.0.1")) { $payload2 = $val; break }
  } catch {}
}
Write-Output "newPayload=$($payload2.Length -gt 40) different=$($payload2 -ne $payload)"
if ($payload2.Length -le 40 -or $payload2 -eq $payload) { Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 23 }
Put-Payload $groot $payload2
if (-not (Invoke-Button $groot "连接")) { Write-Output "no-connect2"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 24 }
if (-not (Wait-Text $hroot "有人想协助这台电脑" 15)) { Write-Output "no-ask2"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 25 }
$allowPair2 = Find-ButtonPrefix $hroot "允许协助"
if (-not $allowPair2) { Write-Output "no-pair2"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 26 }
$allowPair2.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
if (-not (Wait-Text $groot "正在协助对方 · 已连接" 12)) { Write-Output "no-session2"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 27 }
Start-Sleep -Milliseconds 800
$bannerBack = Has-Text $hroot "当前是「完全信任」"
Write-Output "bannerBack=$bannerBack"
Clear-Secret $hroot
Start-Sleep -Milliseconds 200
if (Has-Secret $hroot) { Write-Output "rejoin-not-saved" } else { Save $hostp.MainWindowHandle (Join-Path $shot "desktop-again-rejoined.png") }
if (-not (Invoke-Button $groot "提议一项改动")) { Write-Output "no-propose2"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 28 }
Start-Sleep -Milliseconds 300
$heldCmd = "echo queqiao-again-held& echo queqiao-again-held>$held"
if (-not (Fill-Propose $groot "再写一句" $heldCmd)) { Write-Output "no-fields2"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 29 }
if (-not (Invoke-Button $groot "发给对方确认")) { Write-Output "no-send2"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 30 }
if (-not (Wait-Text $hroot "对方想在这台电脑执行操作" 12)) { Write-Output "no-reask"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 31 }
Start-Sleep -Milliseconds 300
$showsCmd = Shown-Text $hroot "queqiao-again-held"
Write-Output "showsCmd=$showsCmd"
Clear-Secret $hroot
Start-Sleep -Milliseconds 200
if (Has-Secret $hroot) { Write-Output "ask-not-saved" } else { Save $hostp.MainWindowHandle (Join-Path $shot "desktop-again-ask.png") }
if (-not (Invoke-Button $hroot "拒绝")) { Write-Output "no-reject"; Stop-Process -Id $hostp.Id,$helpp.Id -Force; exit 32 }
$denied = Wait-Text $groot "已拒绝，未做任何改动" 8
Start-Sleep -Milliseconds 400
$heldFile = Test-Path $held
Write-Output "denied=$denied heldFile=$heldFile"
Stop-Process -Id $hostp.Id,$helpp.Id -Force
Remove-Item -Force $ran, $held -ErrorAction SilentlyContinue
if (-not ($sawRan -and $ranFile -and $executed -and -not $fullAsk -and $cleared -and $hostHome -and $helpHome -and -not $bannerBack -and $showsCmd -and $denied -and -not $heldFile)) { exit 33 }
Write-Output "again-ok"
