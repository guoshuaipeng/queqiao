$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient, System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class UiCopySession {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll", SetLastError=true)] public static extern int OpenClipboard(IntPtr h);
  [DllImport("user32.dll")] public static extern int CloseClipboard();
  [DllImport("user32.dll")] public static extern IntPtr GetOpenClipboardWindow();
  [DllImport("kernel32.dll")] public static extern uint GetLastError();
  public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
[void][UiCopySession]::SetProcessDPIAware()

$shot = "E:\IdeaProjects\remote-assist-wt-tauri\docs\tauri-migration\acceptance"
$log = Join-Path $env:TEMP "queqiao-copy-session-repro.txt"
$exe = "E:\IdeaProjects\remote-assist-wt-tauri\apps\queqiao-tauri\src-tauri\target\release\queqiao-tauri.exe"

function Save([IntPtr]$hwnd, [string]$out) {
  $rect = New-Object UiCopySession+RECT
  [void][UiCopySession]::GetWindowRect($hwnd, [ref]$rect)
  $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
  if ($w -lt 20 -or $h -lt 20) { return }
  $bmp = New-Object System.Drawing.Bitmap $w, $h
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc(); [void][UiCopySession]::PrintWindow($hwnd, $hdc, 2); $g.ReleaseHdc($hdc)
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
function Invoke-Button($root, $name) {
  $el = Find-Button $root $name
  if (-not $el) { return $false }
  $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
  return $true
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
function Clear-Secret($root) {
  $editCond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
  foreach ($ed in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)) {
    try {
      $pat = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
      if ($pat.Current.Value -and $pat.Current.Value.StartsWith("remoteassist:")) { $pat.SetValue("") | Out-Null }
    } catch {}
  }
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

function Send-Eval([string]$port, [string]$expression) {
  $deadline = (Get-Date).AddSeconds(6)
  $page = $null
  do {
    try {
      $pages = Invoke-RestMethod "http://127.0.0.1:$port/json" -TimeoutSec 2
      $page = @($pages) | Where-Object { $_.type -eq "page" } | Select-Object -First 1
    } catch {}
    if ($page) { break }
    Start-Sleep -Milliseconds 200
  } while ((Get-Date) -lt $deadline)
  if (-not $page) { return "no-cdp" }
  $ws = [System.Net.WebSockets.ClientWebSocket]::new()
  $ct = [Threading.CancellationToken]::None
  $ws.ConnectAsync([Uri]$page.webSocketDebuggerUrl, $ct).Wait()
  $exprJson = ($expression | ConvertTo-Json -Compress)
  $payload = '{"id":1,"method":"Runtime.evaluate","params":{"expression":' + $exprJson + ',"returnByValue":true}}'
  $bytes = [Text.Encoding]::UTF8.GetBytes($payload)
  $seg = [ArraySegment[byte]]::new($bytes)
  $ws.SendAsync($seg, [Net.WebSockets.WebSocketMessageType]::Text, $true, $ct).Wait()
  $buf = New-Object byte[] 4096
  $in = [ArraySegment[byte]]::new($buf)
  $got = $ws.ReceiveAsync($in, $ct).Result
  $ws.Dispose()
  return [Text.Encoding]::UTF8.GetString($buf, 0, $got.Count)
}
function Save-Clean($root, $hwnd, $out) {
  Clear-Secret $root
  Send-Eval "9338" "(() => { const b=document.getElementById('manual-copy'); if (b) { b.value = ''; b.blur(); } const h=document.getElementById('copy-hint'); return { cleared: !b || !b.value, hint: h ? h.textContent : '' }; })()" | Out-Null
  Start-Sleep -Milliseconds 250
  if (Has-Secret $root) { Write-Output "secret-not-saved $out"; return }
  Save $hwnd $out
}

# Minimal repro evidence (no credential)
$open = [UiCopySession]::OpenClipboard([IntPtr]::Zero)
$err = [UiCopySession]::GetLastError()
$holder = [UiCopySession]::GetOpenClipboardWindow()
if ($open -ne 0) { [void][UiCopySession]::CloseClipboard() }
$repro = @"
time=$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') Asia/Shanghai
OpenClipboard(NULL)= $open
GetLastError= $err
GetOpenClipboardWindow= $holder
note= error 5 means ACCESS_DENIED; app must not show copy success
"@
Set-Content -Path $log -Value $repro -Encoding UTF8
Copy-Item $log (Join-Path $shot "clipboard-error5-repro.txt") -Force
Write-Output $repro

$dataHome = Join-Path $env:TEMP "queqiao-copy-session"
$profile = Join-Path $env:TEMP "queqiao-copy-session-wv2"
New-Item -ItemType Directory -Force -Path $dataHome,$profile | Out-Null
[System.IO.File]::WriteAllText((Join-Path $dataHome "settings.json"), '{"relay":"127.0.0.1:17991","trust":"ask"}')

Get-Process queqiao-tauri -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 400

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $exe
$psi.UseShellExecute = $false
$psi.EnvironmentVariables["QUEQIAO_HOME"] = $dataHome
$psi.EnvironmentVariables["WEBVIEW2_USER_DATA_FOLDER"] = $profile
  $psi.EnvironmentVariables["WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS"] = "--remote-debugging-port=9338 --force-renderer-accessibility"
$proc = [System.Diagnostics.Process]::Start($psi)
$deadline = (Get-Date).AddSeconds(15)
do { Start-Sleep -Milliseconds 200; $proc.Refresh() } while ($proc.MainWindowHandle -eq [IntPtr]::Zero -and (Get-Date) -lt $deadline -and -not $proc.HasExited)
if ($proc.MainWindowHandle -eq [IntPtr]::Zero) { Write-Output "FAIL no window"; exit 1 }
[void][UiCopySession]::ShowWindow($proc.MainWindowHandle, 9)
[void][UiCopySession]::SetWindowPos($proc.MainWindowHandle, [IntPtr]::Zero, 40, 40, 860, 520, 0x0004)
Start-Sleep -Milliseconds 800
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)

Save $proc.MainWindowHandle (Join-Path $shot "desktop-copy-session-before.png")

if (-not (Wait-Button $root "开启接受协助" 15)) { Write-Output "FAIL no start button"; Stop-Process -Id $proc.Id -Force; exit 2 }
if (-not (Invoke-Button $root "开启接受协助")) { Write-Output "FAIL start click"; Stop-Process -Id $proc.Id -Force; exit 2 }
$waiting = Wait-Text $root "等待对方连接" 15
$copyBtn = Wait-Button $root "复制连接凭证" 3
$noConnected = -not (Has-Text $root "正在接受协助 · 已连接")
Write-Output "waiting=$waiting copyBtn=$copyBtn noConnected=$noConnected"
if (-not ($waiting -and $copyBtn -and $noConnected)) {
  Save $proc.MainWindowHandle (Join-Path $shot "desktop-copy-session-wait-fail.png")
  Stop-Process -Id $proc.Id -Force
  exit 3
}
Save-Clean $root $proc.MainWindowHandle (Join-Path $shot "desktop-waiting-after-open.png")

# Also narrow window acceptance for waiting state
[void][UiCopySession]::SetWindowPos($proc.MainWindowHandle, [IntPtr]::Zero, 40, 40, 660, 400, 0x0004)
Start-Sleep -Milliseconds 400
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
Save-Clean $root $proc.MainWindowHandle (Join-Path $shot "desktop-waiting-narrow.png")
[void][UiCopySession]::SetWindowPos($proc.MainWindowHandle, [IntPtr]::Zero, 40, 40, 860, 520, 0x0004)
Start-Sleep -Milliseconds 400
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)

Invoke-Button $root "复制连接凭证" | Out-Null
Start-Sleep -Milliseconds 900
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
$success = Has-Text $root "已复制，请完整发给对方。"
$failed = Has-Text $root "没能复制，请再点一次。"
$manual = Has-Secret $root
$focused = $false
$editCond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
foreach ($ed in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)) {
  try {
    $val = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
    if ($val -and $val.StartsWith("remoteassist:pair")) {
      $focused = $ed.Current.HasKeyboardFocus
    }
  } catch {}
}
Write-Output "copySuccessHint=$success copyFailHint=$failed manualBox=$manual manualFocused=$focused"

# Rapid double-click copy must still not show success on error 5
Invoke-Button $root "复制连接凭证" | Out-Null
Start-Sleep -Milliseconds 200
Invoke-Button $root "复制连接凭证" | Out-Null
Start-Sleep -Milliseconds 700
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
$success2 = Has-Text $root "已复制，请完整发给对方。"
$failed2 = Has-Text $root "没能复制，请再点一次。"
Write-Output "afterDouble success=$success2 fail=$failed2"

# Clear secret before saving after-copy shot
Save-Clean $root $proc.MainWindowHandle (Join-Path $shot "desktop-after-copy-fail.png")

# Switch views: open settings then back, copy again
if (Find-Button $root "设置") {
  Invoke-Button $root "设置" | Out-Null
  Start-Sleep -Milliseconds 400
  $root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
  if (Find-Button $root "返回") { Invoke-Button $root "返回" | Out-Null; Start-Sleep -Milliseconds 400 }
  $root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
}
if (Find-Button $root "复制连接凭证") {
  Invoke-Button $root "复制连接凭证" | Out-Null
  Start-Sleep -Milliseconds 700
  $root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
  $success3 = Has-Text $root "已复制，请完整发给对方。"
  $failed3 = Has-Text $root "没能复制，请再点一次。"
  Write-Output "afterSettings success=$success3 fail=$failed3"
}

$okWait = $waiting -and $copyBtn -and $noConnected
$okFailPath = (-not $success) -and (-not $success2) -and $failed
if ($err -eq 5 -and $success) {
  Write-Output "FAIL claimed success while OpenClipboard error 5"
  Stop-Process -Id $proc.Id -Force
  exit 5
}
Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
if (-not $okWait) { exit 3 }
if (-not $okFailPath -and $err -eq 5) { Write-Output "FAIL expected fail hint on error 5"; exit 5 }
if ($success) { Write-Output "copy succeeded with readback"; exit 0 }
Write-Output "done waiting-ok fail-path-ok err5=$($err -eq 5)"
exit 0