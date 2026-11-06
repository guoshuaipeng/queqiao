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
function Start-Queqiao([string]$dataDir, [string]$port, [string]$profile) {
  $exe = "E:\IdeaProjects\remote-assist-wt-tauri\apps\queqiao-tauri\src-tauri\target\release\queqiao-tauri.exe"
  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName = $exe
  $psi.UseShellExecute = $false
  $psi.EnvironmentVariables["QUEQIAO_HOME"] = $dataDir
  if ($profile) { $psi.EnvironmentVariables["WEBVIEW2_USER_DATA_FOLDER"] = $profile }
  if ($port) { $psi.EnvironmentVariables["WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS"] = "--remote-debugging-port=$port" }
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
function Find-AllowWrite($root) {
  foreach ($el in (All-Els $root)) {
    if ($el.Current.ControlType.ProgrammaticName -ne "ControlType.Button") { continue }
    $text = $el.Current.Name
    if ($text -and $text.StartsWith("允许") -and -not $text.StartsWith("允许协助")) { return $el }
  }
  return $null
}
function Shown-Text($root, $needle) {
  foreach ($el in (All-Els $root)) {
    if ($el.Current.IsOffscreen) { continue }
    $text = $el.Current.Name
    if ($text -and $text.Contains($needle)) { return $true }
  }
  return $false
}
function Text-Height($root, $needle) {
  $best = 9999
  foreach ($el in (All-Els $root)) {
    if ($el.Current.IsOffscreen) { continue }
    $text = $el.Current.Name
    if (-not $text -or -not $text.Contains($needle)) { continue }
    $h = $el.Current.BoundingRectangle.Height
    if ($h -gt 8 -and $h -lt $best) { $best = $h }
  }
  return $best
}
function Wait-Enabled($root, $name, $seconds) {
  $deadline = (Get-Date).AddSeconds($seconds)
  do {
    $el = Find-Button $root $name
    if ($el -and $el.Current.IsEnabled) { return $el }
    Start-Sleep -Milliseconds 200
  } while ((Get-Date) -lt $deadline)
  return $null
}
function Scroll-HistoryTop($root, $needle) {
  foreach ($el in (All-Els $root)) {
    $text = $el.Current.Name
    if (-not $text -or -not $text.Contains($needle)) { continue }
    try {
      $el.GetCurrentPattern([System.Windows.Automation.ScrollItemPattern]::Pattern).ScrollIntoView()
      return $true
    } catch {}
  }
  return $false
}
function Shown-Count($root, $needle) {
  $n = 0
  foreach ($el in (All-Els $root)) {
    if ($el.Current.IsOffscreen) { continue }
    $text = $el.Current.Name
    if ($text -and $text.Contains($needle)) { $n++ }
  }
  return $n
}
function Any-Count($root, $needle) {
  $n = 0
  foreach ($el in (All-Els $root)) {
    $text = $el.Current.Name
    if ($text -and $text.Contains($needle)) { $n++ }
  }
  return $n
}
function Row-Shown($root, $needle) {
  foreach ($el in (All-Els $root)) {
    if ($el.Current.IsOffscreen) { continue }
    $text = $el.Current.Name
    if ($text) { $text = $text.Trim() }
    if ($text -ne $needle) { continue }
    $h = $el.Current.BoundingRectangle.Height
    if ($h -gt 8 -and $h -lt 36) { return $true }
  }
  return $false
}

Add-Type @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public class PlaceMsg {
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr parent, EnumProc cb, IntPtr l);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  public static IntPtr render;
  public static bool Visit(IntPtr h, IntPtr l) {
    StringBuilder sb = new StringBuilder(128);
    GetClassName(h, sb, 128);
    if (sb.ToString() == "Chrome_RenderWidgetHostHWND") render = h;
    EnumChildWindows(h, Visit, IntPtr.Zero);
    return true;
  }
  public static IntPtr FindRender(IntPtr root) {
    render = IntPtr.Zero;
    EnumChildWindows(root, Visit, IntPtr.Zero);
    return render;
  }
}
"@
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class PlaceInput {
  [StructLayout(LayoutKind.Sequential)]
  public struct MOUSEINPUT {
    public int dx;
    public int dy;
    public uint mouseData;
    public uint dwFlags;
    public uint time;
    public System.IntPtr extra;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct INPUT {
    public uint type;
    public MOUSEINPUT mi;
  }
  [DllImport("user32.dll")] public static extern uint SendInput(uint n, INPUT[] inputs, int cb);
  [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
  public struct POINT { public int X; public int Y; }
  public static uint Wheel(int delta) {
    INPUT input = new INPUT();
    input.type = 0;
    input.mi.dwFlags = 0x0800;
    input.mi.mouseData = (uint)delta;
    INPUT[] arr = new INPUT[] { input };
    return SendInput(1, arr, Marshal.SizeOf(typeof(INPUT)));
  }
}
"@
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class PlaceWheel {
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, int data, UIntPtr extra);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
}
"@
function Scroll-AncestorTop($el) {
  $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
  $cur = $walker.GetParent($el)
  for ($i = 0; $i -lt 6 -and $cur; $i++) {
    try {
      $pat = $cur.GetCurrentPattern([System.Windows.Automation.ScrollPattern]::Pattern)
      $before = $pat.Current.VerticalScrollPercent
      $pat.SetScrollPercent(-1, 0)
      Start-Sleep -Milliseconds 200
      $after = $pat.Current.VerticalScrollPercent
      for ($n = 0; $n -lt 6; $n++) {
        $pat.Scroll([System.Windows.Automation.ScrollAmount]::NoAmount, [System.Windows.Automation.ScrollAmount]::LargeDecrement)
      }
      Start-Sleep -Milliseconds 200
      $scrolled = $pat.Current.VerticalScrollPercent
      return "i=$i before=$before set=$after scroll=$scrolled"
    } catch {}
    $cur = $walker.GetParent($cur)
  }
  return "no-pattern"
}
function Wheel-At($hwnd, $x, $y, $delta, $times) {
  [void][Ui9]::SetWindowPos($hwnd, [IntPtr]::new(-1), 0, 0, 0, 0, 0x0001 -bor 0x0002)
  [void][PlaceWheel]::SetForegroundWindow($hwnd)
  [void][PlaceWheel]::SetCursorPos($x, $y)
  Start-Sleep -Milliseconds 200
  $sent = 0
  for ($i = 0; $i -lt $times; $i++) {
    $sent += [PlaceInput]::Wheel($delta)
    Start-Sleep -Milliseconds 30
  }
  $cursor = New-Object PlaceInput+POINT
  [void][PlaceInput]::GetCursorPos([ref]$cursor)
  [void][Ui9]::SetWindowPos($hwnd, [IntPtr]::new(-2), 0, 0, 0, 0, 0x0001 -bor 0x0002)
  return "sent=$sent cursor=$($cursor.X),$($cursor.Y) target=$x,$y"
}
function Exact-Count($root, $needle) {
  $n = 0
  foreach ($el in (All-Els $root)) {
    $text = $el.Current.Name
    if ($text) { $text = $text.Trim() }
    if ($text -eq $needle) { $n++ }
  }
  return $n
}
function Fully-In($el, $root) {
  if (-not $el) { return $false }
  $r = $el.Current.BoundingRectangle
  $w = $root.Current.BoundingRectangle
  if ($r.Width -lt 20 -or $r.Height -lt 24) { return $false }
  return ($r.Top -ge ($w.Top + 2) -and $r.Bottom -le ($w.Bottom - 2) -and $r.Left -ge ($w.Left + 2) -and $r.Right -le ($w.Right - 2))
}
function Covered-By($cover, $target) {
  if (-not $cover -or -not $target) { return $false }
  $a = $cover.Current.BoundingRectangle
  $b = $target.Current.BoundingRectangle
  $x1 = [Math]::Max($a.Left, $b.Left)
  $y1 = [Math]::Max($a.Top, $b.Top)
  $x2 = [Math]::Min($a.Right, $b.Right)
  $y2 = [Math]::Min($a.Bottom, $b.Bottom)
  return (($x2 - $x1) -gt 8 -and ($y2 - $y1) -gt 8)
}
function Scroll-Exact($root, $needle) {
  foreach ($el in (All-Els $root)) {
    if ($el.Current.Name -ne $needle) { continue }
    try {
      $el.GetCurrentPattern([System.Windows.Automation.ScrollItemPattern]::Pattern).ScrollIntoView()
      return $true
    } catch {}
  }
  return $false
}
function Scroll-Button($root, $name) {
  $el = Find-Button $root $name
  if (-not $el) { return $false }
  try {
    $el.GetCurrentPattern([System.Windows.Automation.ScrollItemPattern]::Pattern).ScrollIntoView()
    return $true
  } catch { return $false }
}
function Send-Eval([string]$port, [string]$expression) {
  $deadline = (Get-Date).AddSeconds(8)
  $page = $null
  do {
    try {
      $pages = Invoke-RestMethod "http://127.0.0.1:$port/json" -TimeoutSec 2
      $page = @($pages) | Where-Object { $_.type -eq "page" } | Select-Object -First 1
    } catch {}
    if ($page) { break }
    Start-Sleep -Milliseconds 250
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
  $buf = New-Object byte[] 8192
  $in = [ArraySegment[byte]]::new($buf)
  $got = $ws.ReceiveAsync($in, $ct).Result
  $text = [Text.Encoding]::UTF8.GetString($buf, 0, $got.Count)
  $ws.Dispose()
  return $text
}
function Fail([int]$code, [string]$msg) {
  Write-Output $msg
  if ($script:hostp) { Stop-Process -Id $script:hostp.Id -Force -ErrorAction SilentlyContinue }
  if ($script:helpp) { Stop-Process -Id $script:helpp.Id -Force -ErrorAction SilentlyContinue }
  exit $code
}
function Find-ButtonContains($root, $needle) {
  foreach ($el in (All-Els $root)) {
    $text = $el.Current.Name
    if ($el.Current.ControlType.ProgrammaticName -eq "ControlType.Button" -and $text -and $text.Contains($needle)) { return $el }
  }
  return $null
}
function Find-EditValue($root, $value) {
  $editCond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
  foreach ($ed in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)) {
    try {
      $val = $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
      if ($val -eq $value) { return $ed }
    } catch {}
  }
  return $null
}
function Run-Lines($groot, $hroot, $summary, $command) {
  if (-not (Invoke-Button $groot "提议一项改动")) { return "no-propose" }
  Start-Sleep -Milliseconds 300
  if (-not (Fill-Propose $groot $summary $command)) { return "no-fields" }
  if (-not (Invoke-Button $groot "发给对方确认")) { return "no-send" }
  if (-not (Wait-Text $hroot "对方想在这台电脑执行操作" 12)) { return "no-ask-write" }
  $allowWrite = Find-AllowWrite $hroot
  if (-not $allowWrite) { return "no-allow" }
  $allowWrite.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
  return "sent"
}

$dataHome = Join-Path $env:TEMP "queqiao-past-home"
New-Item -ItemType Directory -Force -Path $dataHome | Out-Null
Remove-Item -Force (Join-Path $dataHome "history.json") -ErrorAction SilentlyContinue
[System.IO.File]::WriteAllText((Join-Path $dataHome "settings.json"), '{"relay":"127.0.0.1:17991","trust":"ask"}')
$hostProfile = Join-Path $env:TEMP "qq-profile-past-host"
$helpProfile = Join-Path $env:TEMP "qq-profile-past-help"
New-Item -ItemType Directory -Force -Path $hostProfile,$helpProfile | Out-Null
Get-Process queqiao-tauri -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 400
$script:hostp = Start-Queqiao $dataHome "9334" $hostProfile
Start-Sleep -Milliseconds 800
$script:helpp = Start-Queqiao $dataHome "9335" $helpProfile
if ($hostp.MainWindowHandle -eq [IntPtr]::Zero -or $helpp.MainWindowHandle -eq [IntPtr]::Zero) { Fail 1 "window-missing" }
[void][Ui9]::ShowWindow($hostp.MainWindowHandle, 9)
[void][Ui9]::ShowWindow($helpp.MainWindowHandle, 9)
[void][Ui9]::SetWindowPos($hostp.MainWindowHandle, [IntPtr]::Zero, 720, 40, 0, 0, 0x0001 -bor 0x0004)
[void][Ui9]::SetWindowPos($helpp.MainWindowHandle, [IntPtr]::Zero, 40, 40, 0, 0, 0x0001 -bor 0x0004)
Start-Sleep -Milliseconds 700
$hroot = [System.Windows.Automation.AutomationElement]::FromHandle($hostp.MainWindowHandle)
$groot = [System.Windows.Automation.AutomationElement]::FromHandle($helpp.MainWindowHandle)
$shot = "E:\IdeaProjects\remote-assist-wt-tauri\docs\tauri-migration\acceptance"
if (-not (Wait-Button $hroot "开启接受协助" 15)) { Fail 1 "no-start" }
if (-not (Invoke-Button $hroot "开启接受协助")) { Fail 1 "no-start-click" }
if (-not (Wait-Button $hroot "复制连接凭证" 15)) { Fail 2 "no-wait" }
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
if ($payload.Length -le 40) { Fail 3 "no-payload" }
Put-Payload $groot $payload
$connect = $null
$deadline = (Get-Date).AddSeconds(4)
do {
  $connect = Find-Button $groot "连接"
  if ($connect -and $connect.Current.IsEnabled) { break }
  Start-Sleep -Milliseconds 200
} while ((Get-Date) -lt $deadline)
if (-not $connect -or -not $connect.Current.IsEnabled) {
  $jsPayload = $payload.Replace('\', '\\').Replace('"', '\"')
  Send-Eval "9335" "(() => { const input = document.getElementById('cred'); input.value = `"$jsPayload`"; input.dispatchEvent(new Event('input', { bubbles: true })); return String(document.getElementById('btn-connect').disabled); })()" | Out-Null
  Start-Sleep -Milliseconds 300
  $connect = Find-Button $groot "连接"
}
if (-not $connect -or -not $connect.Current.IsEnabled) { Fail 4 "connect-disabled" }
$connect.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
if (-not (Wait-Text $hroot "有人想协助这台电脑" 15)) { Fail 5 "no-ask" }
$allowPair = Find-ButtonPrefix $hroot "允许协助"
if (-not $allowPair) { Fail 6 "no-pair-allow" }
$allowPair.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
if (-not (Wait-Text $groot "正在协助对方 · 已连接" 12)) { Fail 7 "no-session" }
function Send-Wheel([string]$port, [int]$x, [int]$y, [int]$delta) {
  $deadline = (Get-Date).AddSeconds(8)
  $page = $null
  do {
    try {
      $pages = Invoke-RestMethod "http://127.0.0.1:$port/json" -TimeoutSec 2
      $page = @($pages) | Where-Object { $_.type -eq "page" } | Select-Object -First 1
    } catch {}
    if ($page) { break }
    Start-Sleep -Milliseconds 250
  } while ((Get-Date) -lt $deadline)
  if (-not $page) { return "no-cdp" }
  $ws = [System.Net.WebSockets.ClientWebSocket]::new()
  $ct = [Threading.CancellationToken]::None
  $ws.ConnectAsync([Uri]$page.webSocketDebuggerUrl, $ct).Wait()
  $payload = '{"id":1,"method":"Input.dispatchMouseEvent","params":{"type":"mouseWheel","x":' + $x + ',"y":' + $y + ',"deltaX":0,"deltaY":' + $delta + '}}'
  $bytes = [Text.Encoding]::UTF8.GetBytes($payload)
  $seg = [ArraySegment[byte]]::new($bytes)
  $ws.SendAsync($seg, [Net.WebSockets.WebSocketMessageType]::Text, $true, $ct).Wait()
  Start-Sleep -Milliseconds 300
  $ws.Dispose()
  return "wheeled"
}
function Send-WheelX([string]$port, [int]$x, [int]$y, [int]$deltaX) {
  $deadline = (Get-Date).AddSeconds(8)
  $page = $null
  do {
    try {
      $pages = Invoke-RestMethod "http://127.0.0.1:$port/json" -TimeoutSec 2
      $page = @($pages) | Where-Object { $_.type -eq "page" } | Select-Object -First 1
    } catch {}
    if ($page) { break }
    Start-Sleep -Milliseconds 250
  } while ((Get-Date) -lt $deadline)
  if (-not $page) { return "no-cdp" }
  $ws = [System.Net.WebSockets.ClientWebSocket]::new()
  $ct = [Threading.CancellationToken]::None
  $ws.ConnectAsync([Uri]$page.webSocketDebuggerUrl, $ct).Wait()
  $payload = '{"id":1,"method":"Input.dispatchMouseEvent","params":{"type":"mouseWheel","x":' + $x + ',"y":' + $y + ',"deltaX":' + $deltaX + ',"deltaY":0}}'
  $bytes = [Text.Encoding]::UTF8.GetBytes($payload)
  $seg = [ArraySegment[byte]]::new($bytes)
  $ws.SendAsync($seg, [Net.WebSockets.WebSocketMessageType]::Text, $true, $ct).Wait()
  Start-Sleep -Milliseconds 150
  $ws.Dispose()
  return "wheeled"
}
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class MinhUi {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
}
"@
function Set-MinHeight([IntPtr]$hwnd) {
  Set-ClientHeight $hwnd 360
}
function Set-NormalHeight([IntPtr]$hwnd) {
  Set-ClientHeight $hwnd 480
}
function Set-ClientHeight([IntPtr]$hwnd, [int]$clientH) {
  $wr = New-Object MinhUi+RECT
  $cr = New-Object MinhUi+RECT
  [void][MinhUi]::GetWindowRect($hwnd, [ref]$wr)
  [void][MinhUi]::GetClientRect($hwnd, [ref]$cr)
  $dx = ($wr.Right - $wr.Left) - ($cr.Right - $cr.Left)
  $dy = ($wr.Bottom - $wr.Top) - ($cr.Bottom - $cr.Top)
  [void][MinhUi]::SetWindowPos($hwnd, [IntPtr]::Zero, 40, 40, 840 + $dx, $clientH + $dy, 0x0004)
}
$old = Run-Lines $groot $hroot "先写一句" "echo queqiao-old-line"
if ($old -ne "sent") { Fail 34 "old-not-sent" }
$wide = ("M" * 200) + "queqiao-wide-QUEEND"
$cmd = "echo queqiao-line-1 & echo queqiao-line-2 & echo queqiao-line-3 & echo queqiao-line-4 & echo queqiao-line-5 & echo $wide"
$ran = Run-Lines $groot $hroot "打出长结果" $cmd
if ($ran -ne "sent") { Fail 8 $ran }
$saw = $false
$deadline = (Get-Date).AddSeconds(12)
do {
  foreach ($el in (All-Els $groot)) {
    try { if ($el.Current.Name -and $el.Current.Name.Contains("QUEEND")) { $saw = $true; break } } catch {}
  }
  if ($saw) { break }
  Start-Sleep -Milliseconds 250
} while ((Get-Date) -lt $deadline)
if (-not $saw) { Fail 9 "long-output-missing" }
Set-MinHeight $helpp.MainWindowHandle
Start-Sleep -Milliseconds 700
$aim = Send-Eval "9335" @'
(() => {
  const box = document.getElementById("result");
  const br = box.getBoundingClientRect();
  const barY = Math.max(0, box.offsetHeight - box.clientHeight);
  const row = [...box.children].find((el) => el.textContent.indexOf("QUEEND") >= 0);
  let seen = false;
  if (row) {
    const r = row.getBoundingClientRect();
    seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
  }
  return JSON.stringify({ x: Math.round(br.left + 40), y: Math.round(br.top + 8), top: box.scrollTop, maxY: box.scrollHeight - box.clientHeight, seen });
})()
'@
Write-Output "aim=$aim"
if ($aim -notmatch '\\"x\\":(\d+)') { Fail 10 "no-result-x" }
$rx = [int]$Matches[1]
if ($aim -notmatch '\\"y\\":(\d+)') { Fail 10 "no-result-y" }
$ry = [int]$Matches[1]
if ($aim -notmatch '\\"seen\\":true') {
  Send-Wheel "9335" $rx $ry -20 | Out-Null
  Start-Sleep -Milliseconds 400
  $again = Send-Eval "9335" "(() => { const box = document.getElementById('result'); const br = box.getBoundingClientRect(); const barY = Math.max(0, box.offsetHeight - box.clientHeight); const row = [...box.children].find((el) => el.textContent.indexOf('QUEEND') >= 0); if (!row) return '0'; const r = row.getBoundingClientRect(); return (r.bottom > br.top + 2 && r.top < br.bottom - barY - 2) ? '1' : '0'; })()"
  Write-Output "again=$again"
  if ($again -notmatch '"value":"1"') {
    Send-Wheel "9335" $rx $ry -20 | Out-Null
    Start-Sleep -Milliseconds 400
  }
}
$w1 = Send-WheelX "9335" $rx $ry 400
$w2 = Send-WheelX "9335" $rx $ry 400
$w3 = Send-WheelX "9335" $rx $ry 400
Write-Output "wheels=$w1,$w2,$w3"
Start-Sleep -Milliseconds 500
$end0 = Send-Eval "9335" "(() => { const box = document.getElementById('result'); const formula = box.scrollWidth - box.clientWidth; return JSON.stringify({ top: box.scrollTop, maxY: box.scrollHeight - box.clientHeight, left: box.scrollLeft, formula }); })()"
Write-Output "end0=$end0"
if ($end0 -notmatch '\\"top\\":(\d+)') { Fail 11 "no-end-top" }
$topEnd = [int]$Matches[1]
if ($end0 -notmatch '\\"maxY\\":(\d+)' -or [Math]::Abs($topEnd - [int]$Matches[1]) -gt 1) { Fail 12 "not-at-bottom" }
if ($end0 -notmatch '\\"left\\":(\d+)') { Fail 13 "no-left" }
$leftEnd = [int]$Matches[1]
if ($end0 -notmatch '\\"formula\\":(\d+)' -or [Math]::Abs($leftEnd - [int]$Matches[1]) -gt 1) { Fail 14 "not-at-line-end" }
Send-Wheel "9335" $rx $ry -20 | Out-Null
Start-Sleep -Milliseconds 450
$up = Send-Eval "9335" @'
(() => {
  const box = document.getElementById("result");
  const br = box.getBoundingClientRect();
  const barY = Math.max(0, box.offsetHeight - box.clientHeight);
  const rows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const text = el.textContent.replace(/\s+/g, " ").trim().slice(0, 24);
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: text, whole, seen, h: Math.round(r.height), top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const visible = rows.filter((row) => row.seen);
  const left = box.scrollLeft;
  const formula = box.scrollWidth - box.clientWidth;
  return JSON.stringify({ top: box.scrollTop, maxY: box.scrollHeight - box.clientHeight, left, formula, gap: formula - left, barY: Math.round(barY), allWhole: visible.length > 0 && visible.every((row) => row.whole), visible });
})()
'@
Write-Output "up=$up"
$upTopOk = $false
if ($up -match '\\"top\\":(-?\d+)') { $upTop = [int]$Matches[1]; $upTopOk = [Math]::Abs($upTop - ($topEnd - 20)) -le 1 }
$upGapOk = $up -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$upLineOk = $up -match '\\"allWhole\\":true' -and $up -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "up-secret" }
Send-Wheel "9335" $rx $ry 20 | Out-Null
Start-Sleep -Milliseconds 450
$back = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const br = box.getBoundingClientRect();
  const barY = Math.max(0, box.offsetHeight - box.clientHeight);
  const rows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.trim() === "exit=0", el };
  });
  const visible = rows.filter((row) => row.seen);
  const exitRow = rows.find((row) => row.exit);
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    const er = exitRow.el.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(er.height), top: Math.round(er.top), bottom: Math.round(er.bottom) };
  }
  const stuck = !!(exit && exit.left >= exit.boxLeft - 1 && exit.left <= exit.boxLeft + 40);
  const exitWhole = !!(exitRow && exitRow.whole && exitRow.seen);
  const lowest = !!(exitRow && visible.every((row) => row.el.getBoundingClientRect().bottom <= exitRow.el.getBoundingClientRect().bottom + 1));
  const left = box.scrollLeft;
  const formula = box.scrollWidth - box.clientWidth;
  return JSON.stringify({ top: box.scrollTop, maxY: box.scrollHeight - box.clientHeight, left, formula, gap: formula - left, barY: Math.round(barY), exitWhole, stuck, lowest, barUnder: exitWhole && lowest && barY >= 10, exit, extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "back=$back"
$topOk = $false
if ($back -match '\\"top\\":(-?\d+)') { $top1 = [int]$Matches[1] } else { $top1 = -999 }
if ($back -match '\\"maxY\\":(-?\d+)') { $maxY1 = [int]$Matches[1]; $topOk = [Math]::Abs($top1 - $maxY1) -le 1 }
$gapOk = $back -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$exitOk = $back -match '\\"exitWhole\\":true' -and $back -match '\\"stuck\\":true' -and $back -match '\\"barUnder\\":true'
$pageOk = $back -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $back -match 'overflow\\":\\"hidden' -and $back -match '\\"pageTop\\":0'
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "min-secret" }
if (-not ($upTopOk -and $upGapOk -and $upLineOk -and $topOk -and $gapOk -and $exitOk -and $pageOk)) { Fail 19 "min-exit-lost" }
Set-NormalHeight $helpp.MainWindowHandle
Start-Sleep -Milliseconds 800
$grown = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const br = box.getBoundingClientRect();
  const barY = Math.max(0, box.offsetHeight - box.clientHeight);
  const rows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, h: Math.round(r.height), top: Math.round(r.top), bottom: Math.round(r.bottom), exit: el.textContent.trim() === "exit=0", el };
  });
  const visible = rows.filter((row) => row.seen);
  const exitRow = rows.find((row) => row.exit);
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    const er = exitRow.el.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(er.height), top: Math.round(er.top), bottom: Math.round(er.bottom) };
  }
  const stuck = !!(exit && exit.left >= exit.boxLeft - 1 && exit.left <= exit.boxLeft + 40);
  const exitWhole = !!(exitRow && exitRow.whole && exitRow.seen);
  const lowest = !!(exitRow && visible.every((row) => row.el.getBoundingClientRect().bottom <= exitRow.el.getBoundingClientRect().bottom + 1));
  const wide = rows.find((row) => row.el.textContent.indexOf("QUEEND") >= 0);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    const barX = Math.max(0, box.offsetWidth - box.clientWidth);
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const wideWhole = !!(wide && wide.whole && wide.seen);
  const left = box.scrollLeft;
  const formula = box.scrollWidth - box.clientWidth;
  box.scrollLeft = box.scrollWidth;
  const real = box.scrollLeft;
  if (Math.abs(real - left) > 1) box.scrollLeft = left;
  return JSON.stringify({ winH: window.innerHeight, top: box.scrollTop, maxY: box.scrollHeight - box.clientHeight, left, real, formula, gap: real - left, barY: Math.round(barY), allWhole: visible.length > 0 && visible.every((row) => row.whole), wideWhole, barUnder: wideWhole && barY >= 10, end, extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "grown=$grown"
$tall = $grown -match '\\"winH\\":(\d+)' -and [int]$Matches[1] -ge 450 -and [int]$Matches[1] -le 520
$gGap = $grown -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$gLine = $grown -match '\\"wideWhole\\":true' -and $grown -match '\\"allWhole\\":true' -and $grown -match '\\"barUnder\\":true'
$gEnd = $grown -match '\\"inside\\":true'
$gPage = $grown -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $grown -match 'overflow\\":\\"hidden' -and $grown -match '\\"pageTop\\":0'
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($tall -and $gGap -and $gLine -and $gEnd -and $gPage)) { Fail 20 "grow-queend-lost" }
if ($grown -notmatch '\\"top\\":(\d+)') { Fail 21 "no-grown-top" }
$grownTop = [int]$Matches[1]
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "bottom-secret" }
$arm = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const keptTop = box.scrollTop;
  const keptLeft = box.scrollLeft;
  page.style.overflowX = "auto";
  page.style.overflowY = "auto";
  page.style.paddingBottom = "800px";
  let spacer = document.getElementById("uitest-wide");
  if (!spacer) {
    spacer = document.createElement("div");
    spacer.id = "uitest-wide";
    spacer.style.cssText = "width:1800px;height:1px;flex:none";
    page.appendChild(spacer);
  }
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  if (Math.abs(box.scrollLeft - keptLeft) > 1) box.scrollLeft = keptLeft;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = box.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 30), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, top: box.scrollTop, left: box.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight });
})()
'@
Write-Output "arm=$arm"
if ($arm -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 22 "page-cannot-slide" }
if ($arm -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 23 "page-cannot-scroll" }
if ($arm -notmatch '\\"x\\":(\d+)') { Fail 24 "no-x2" }
$rx2 = [int]$Matches[1]
if ($arm -notmatch '\\"y\\":(\d+)') { Fail 24 "no-y2" }
$ry2 = [int]$Matches[1]
if ($arm -notmatch '\\"pageY\\":(-?\d+)') { Fail 25 "no-page-y" }
$pageY0 = [int]$Matches[1]
if ($arm -notmatch '\\"pageX\\":(-?\d+)') { Fail 26 "no-page-x" }
$pageX0 = [int]$Matches[1]
Send-Wheel "9335" $rx2 $ry2 -20 | Out-Null
Start-Sleep -Milliseconds 450
$up = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const br = box.getBoundingClientRect();
  const barY = Math.max(0, box.offsetHeight - box.clientHeight);
  const barX = Math.max(0, box.offsetWidth - box.clientWidth);
  const wide = [...box.children].find((el) => el.textContent.indexOf("QUEEND") >= 0);
  let end = null;
  let whole = false;
  let seen = false;
  if (wide) {
    const r = wide.getBoundingClientRect();
    whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    if (wide.firstChild && wide.firstChild.length) {
      const text = wide.firstChild;
      const start = text.textContent.lastIndexOf("QUEEND");
      const range = document.createRange();
      range.setStart(text, start);
      range.setEnd(text, start + "QUEEND".length);
      const rect = range.getBoundingClientRect();
      end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
    }
  }
  const left = box.scrollLeft;
  const formula = box.scrollWidth - box.clientWidth;
  return JSON.stringify({ top: box.scrollTop, left, formula, gap: formula - left, pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, whole, seen, end });
})()
'@
Write-Output "up=$up"
$pageYOk = $up -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageY0) -le 1
$pageXOk = $up -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageX0) -le 1
$docOk = $up -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$upTopOk = $false
if ($up -match '\\"top\\":(-?\d+)') { $upTop = [int]$Matches[1]; $upTopOk = [Math]::Abs($upTop - ($grownTop - 20)) -le 1 }
$endOk = $up -match '\\"inside\\":true' -and $up -match '\\"whole\\":true' -and $up -match '\\"seen\\":true'
if (-not ($pageYOk -and $pageXOk -and $docOk -and $upTopOk -and $endOk)) { Fail 27 "grow-page-up" }
if ($up -notmatch '\\"left\\":(\d+)') { Fail 28 "no-up-left" }
$upLeft = [int]$Matches[1]
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "up-secret" }
$arm2 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const keptTop = box.scrollTop;
  const keptLeft = box.scrollLeft;
  page.style.overflowX = "auto";
  page.style.overflowY = "auto";
  page.style.paddingBottom = "800px";
  let spacer = document.getElementById("uitest-wide");
  if (!spacer) {
    spacer = document.createElement("div");
    spacer.id = "uitest-wide";
    spacer.style.cssText = "width:1800px;height:1px;flex:none";
    page.appendChild(spacer);
  }
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  if (Math.abs(box.scrollLeft - keptLeft) > 1) box.scrollLeft = keptLeft;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = box.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 30), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight });
})()
'@
Write-Output "arm2=$arm2"
if ($arm2 -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 29 "page-cannot-scroll-2" }
if ($arm2 -notmatch '\\"x\\":(\d+)') { Fail 30 "no-x3" }
$rx3 = [int]$Matches[1]
if ($arm2 -notmatch '\\"y\\":(\d+)') { Fail 30 "no-y3" }
$ry3 = [int]$Matches[1]
if ($arm2 -notmatch '\\"pageY\\":(-?\d+)') { Fail 31 "no-page-y2" }
$pageY1 = [int]$Matches[1]
if ($arm2 -notmatch '\\"pageX\\":(-?\d+)') { Fail 32 "no-page-x2" }
$pageX1 = [int]$Matches[1]
Send-Wheel "9335" $rx3 $ry3 20 | Out-Null
Start-Sleep -Milliseconds 450
$down = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const br = box.getBoundingClientRect();
  const barY = Math.max(0, box.offsetHeight - box.clientHeight);
  const barX = Math.max(0, box.offsetWidth - box.clientWidth);
  const wide = [...box.children].find((el) => el.textContent.indexOf("QUEEND") >= 0);
  let end = null;
  let whole = false;
  let seen = false;
  if (wide) {
    const r = wide.getBoundingClientRect();
    whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    if (wide.firstChild && wide.firstChild.length) {
      const text = wide.firstChild;
      const start = text.textContent.lastIndexOf("QUEEND");
      const range = document.createRange();
      range.setStart(text, start);
      range.setEnd(text, start + "QUEEND".length);
      const rect = range.getBoundingClientRect();
      end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
    }
  }
  const left = box.scrollLeft;
  const formula = box.scrollWidth - box.clientWidth;
  return JSON.stringify({ top: box.scrollTop, maxY: box.scrollHeight - box.clientHeight, left, formula, gap: formula - left, pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, whole, seen, end });
})()
'@
Write-Output "down=$down"
$downPageY = $down -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageY1) -le 1
$downPageX = $down -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageX1) -le 1
$downDoc = $down -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$downTopOk = $false
if ($down -match '\\"top\\":(-?\d+)') { $downTop = [int]$Matches[1] } else { $downTop = -999 }
if ($down -match '\\"maxY\\":(-?\d+)') { $downMax = [int]$Matches[1]; $downTopOk = [Math]::Abs($downTop - $downMax) -le 1 -and [Math]::Abs($downTop - $grownTop) -le 1 }
$downLeftOk = $down -match '\\"left\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $upLeft) -le 1
$downEndOk = $down -match '\\"inside\\":true' -and $down -match '\\"whole\\":true' -and $down -match '\\"seen\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$real = Send-Eval "9335" @'
(() => {
  const box = document.getElementById("result");
  const br = box.getBoundingClientRect();
  const barY = Math.max(0, box.offsetHeight - box.clientHeight);
  const rows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { whole, seen, exit: el.textContent.trim() === "exit=0", el };
  });
  const visible = rows.filter((row) => row.seen);
  const exitRow = rows.find((row) => row.exit);
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    const er = exitRow.el.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(er.height), top: Math.round(er.top), bottom: Math.round(er.bottom) };
  }
  const stuck = !!(exit && exit.left >= exit.boxLeft - 1 && exit.left <= exit.boxLeft + 40);
  const exitWhole = !!(exitRow && exitRow.whole && exitRow.seen);
  const lowest = !!(exitRow && visible.every((row) => row.el.getBoundingClientRect().bottom <= exitRow.el.getBoundingClientRect().bottom + 1));
  const left = box.scrollLeft;
  const formula = box.scrollWidth - box.clientWidth;
  return JSON.stringify({ top: box.scrollTop, maxY: box.scrollHeight - box.clientHeight, left, formula, gap: formula - left, barY: Math.round(barY), exitWhole, stuck, lowest, barUnder: exitWhole && lowest && barY >= 10, exit });
})()
'@
Write-Output "real=$real"
$realTopOk = $false
if ($real -match '\\"top\\":(-?\d+)') { $realTop = [int]$Matches[1] } else { $realTop = -999 }
if ($real -match '\\"maxY\\":(-?\d+)') { $realMax = [int]$Matches[1]; $realTopOk = [Math]::Abs($realTop - $realMax) -le 1 }
$realGapOk = $real -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$realExitOk = $real -match '\\"exitWhole\\":true' -and $real -match '\\"stuck\\":true' -and $real -match '\\"barUnder\\":true'
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "pre-secret" }
if (-not ($downPageY -and $downPageX -and $downDoc -and $downTopOk -and $downLeftOk -and $downEndOk -and $realTopOk -and $realGapOk -and $realExitOk)) { Fail 33 "grow-exit-lost" }
$pastBtn = Find-ButtonContains $groot "之前的结果"
if (-not $pastBtn) { Fail 35 "no-past-button" }
$pastBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Start-Sleep -Milliseconds 700
$opened = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const result = document.getElementById("result");
  const past = document.getElementById("past");
  function lines(box) {
    const br = box.getBoundingClientRect();
    const barY = Math.max(0, box.offsetHeight - box.clientHeight);
    const rows = [...box.children].map((el) => {
      const r = el.getBoundingClientRect();
      const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
      const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
      return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, h: Math.round(r.height), top: Math.round(r.top), bottom: Math.round(r.bottom), exit: el.textContent.trim() === "exit=0", el };
    });
    const visible = rows.filter((row) => row.seen);
    const exitRow = rows.find((row) => row.exit);
    let exit = null;
    if (exitRow && exitRow.el.firstChild) {
      const range = document.createRange();
      range.selectNodeContents(exitRow.el.firstChild);
      const rect = range.getBoundingClientRect();
      const er = exitRow.el.getBoundingClientRect();
      exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(er.height), top: Math.round(er.top), bottom: Math.round(er.bottom) };
    }
    const stuck = !!(exit && exit.left >= exit.boxLeft - 1 && exit.left <= exit.boxLeft + 40);
    const exitWhole = !!(exitRow && exitRow.whole && exitRow.seen);
    const lowest = !!(exitRow && visible.length > 0 && visible.every((row) => row.el.getBoundingClientRect().bottom <= exitRow.el.getBoundingClientRect().bottom + 1));
    return { hidden: box.hidden, top: box.scrollTop, maxY: box.scrollHeight - box.clientHeight, barY: Math.round(barY), allWhole: visible.length > 0 && visible.every((row) => row.whole), exitWhole, stuck, lowest, exit, visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })) };
  }
  const cur = lines(result);
  const old = lines(past);
  const br = result.getBoundingClientRect();
  const barY = Math.max(0, result.offsetHeight - result.clientHeight);
  const barX = Math.max(0, result.offsetWidth - result.clientWidth);
  const wide = [...result.children].find((el) => el.textContent.indexOf("QUEEND") >= 0);
  let end = null;
  let wideWhole = false;
  if (wide) {
    const r = wide.getBoundingClientRect();
    wideWhole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1 && r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    if (wide.firstChild && wide.firstChild.length) {
      const text = wide.firstChild;
      const start = text.textContent.lastIndexOf("QUEEND");
      const range = document.createRange();
      range.setStart(text, start);
      range.setEnd(text, start + "QUEEND".length);
      const rect = range.getBoundingClientRect();
      end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
    }
  }
  const queendOk = !!(end && end.inside && wideWhole && barY >= 10);
  const pageOk = page.scrollHeight - page.clientHeight <= 4 && getComputedStyle(page).overflowY === "hidden" && page.scrollTop === 0;
  return JSON.stringify({ queendOk, pageOk, wideWhole, barY: Math.round(barY), end, extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "opened=$opened"
$queendOk = $opened -match '\\"queendOk\\":true'
$pageOk2 = $opened -match '\\"pageOk\\":true'
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "pre-secret" }
if (-not ($queendOk -and $pageOk2)) { Fail 36 "past-queend-lost" }
if ($opened -notmatch '\\"end\\":\{' -or $opened -notmatch '\\"top\\":(\d+)') { }
$arm = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const keptTop = box.scrollTop;
  const keptLeft = box.scrollLeft;
  page.style.overflowX = "auto";
  page.style.overflowY = "auto";
  page.style.paddingBottom = "800px";
  let spacer = document.getElementById("uitest-wide");
  if (!spacer) {
    spacer = document.createElement("div");
    spacer.id = "uitest-wide";
    spacer.style.cssText = "width:1800px;height:1px;flex:none";
    page.appendChild(spacer);
  }
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  if (Math.abs(box.scrollLeft - keptLeft) > 1) box.scrollLeft = keptLeft;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = box.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, top: box.scrollTop, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight });
})()
'@
Write-Output "arm=$arm"
if ($arm -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 37 "page-cannot-scroll" }
if ($arm -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 38 "page-cannot-slide" }
if ($arm -notmatch '\\"x\\":(\d+)') { Fail 39 "no-x" }
$rx2 = [int]$Matches[1]
if ($arm -notmatch '\\"y\\":(\d+)') { Fail 39 "no-y" }
$ry2 = [int]$Matches[1]
if ($arm -notmatch '\\"pageY\\":(-?\d+)') { Fail 40 "no-page-y" }
$pageY0 = [int]$Matches[1]
if ($arm -notmatch '\\"pageX\\":(-?\d+)') { Fail 41 "no-page-x" }
$pageX0 = [int]$Matches[1]
if ($arm -notmatch '\\"top\\":(\d+)') { Fail 42 "no-top" }
$top0 = [int]$Matches[1]
Send-Wheel "9335" $rx2 $ry2 -20 | Out-Null
Start-Sleep -Milliseconds 450
$up = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const br = box.getBoundingClientRect();
  const barY = Math.max(0, box.offsetHeight - box.clientHeight);
  const barX = Math.max(0, box.offsetWidth - box.clientWidth);
  const rows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, h: Math.round(r.height), top: Math.round(r.top), bottom: Math.round(r.bottom), wide: el.textContent.indexOf("QUEEND") >= 0, el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  return JSON.stringify({ top: box.scrollTop, left: box.scrollLeft, formula: box.scrollWidth - box.clientWidth, pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, barY: Math.round(barY), allWhole: visible.length > 0 && visible.every((row) => row.whole), wideWhole: !!(wide && wide.whole && wide.seen), end, visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })) });
})()
'@
Write-Output "up=$up"
$pageYOk = $up -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageY0) -le 1
$pageXOk = $up -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageX0) -le 1
$docOk = $up -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$upTopOk = $false
if ($up -match '\\"top\\":(-?\d+)') { $upTop = [int]$Matches[1]; $upTopOk = [Math]::Abs($upTop - ($top0 - 20)) -le 1 }
$lineOk = $up -match '\\"allWhole\\":true' -and $up -match '\\"wideWhole\\":true' -and $up -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$endOk = $up -match '\\"inside\\":true'
if (-not ($pageYOk -and $pageXOk -and $docOk -and $upTopOk -and $lineOk -and $endOk)) { Fail 43 "past-up-moved" }
if ($up -notmatch '\\"left\\":(\d+)') { Fail 44 "no-up-left" }
$upLeft = [int]$Matches[1]
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "up-secret" }
$arm2 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const keptTop = box.scrollTop;
  const keptLeft = box.scrollLeft;
  page.style.overflowX = "auto";
  page.style.overflowY = "auto";
  page.style.paddingBottom = "800px";
  let spacer = document.getElementById("uitest-wide");
  if (!spacer) {
    spacer = document.createElement("div");
    spacer.id = "uitest-wide";
    spacer.style.cssText = "width:1800px;height:1px;flex:none";
    page.appendChild(spacer);
  }
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  if (Math.abs(box.scrollLeft - keptLeft) > 1) box.scrollLeft = keptLeft;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = box.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight });
})()
'@
Write-Output "arm2=$arm2"
if ($arm2 -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 45 "page-cannot-scroll-2" }
if ($arm2 -notmatch '\\"x\\":(\d+)') { Fail 46 "no-x2" }
$rx3 = [int]$Matches[1]
if ($arm2 -notmatch '\\"y\\":(\d+)') { Fail 46 "no-y2" }
$ry3 = [int]$Matches[1]
if ($arm2 -notmatch '\\"pageY\\":(-?\d+)') { Fail 47 "no-page-y2" }
$pageY1 = [int]$Matches[1]
if ($arm2 -notmatch '\\"pageX\\":(-?\d+)') { Fail 48 "no-page-x2" }
$pageX1 = [int]$Matches[1]
Send-Wheel "9335" $rx3 $ry3 20 | Out-Null
Start-Sleep -Milliseconds 450
$down = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const br = box.getBoundingClientRect();
  const barY = Math.max(0, box.offsetHeight - box.clientHeight);
  const barX = Math.max(0, box.offsetWidth - box.clientWidth);
  const rows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom), wide: el.textContent.indexOf("QUEEND") >= 0, el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const left = box.scrollLeft;
  const formula = box.scrollWidth - box.clientWidth;
  return JSON.stringify({ top: box.scrollTop, maxY: box.scrollHeight - box.clientHeight, left, formula, pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, barY: Math.round(barY), allWhole: visible.length > 0 && visible.every((row) => row.whole), wideWhole: !!(wide && wide.whole && wide.seen), end, visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })) });
})()
'@
Write-Output "down=$down"
$downPageY = $down -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageY1) -le 1
$downPageX = $down -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageX1) -le 1
$downDoc = $down -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$downTopOk = $false
if ($down -match '\\"top\\":(-?\d+)') { $downTop = [int]$Matches[1] } else { $downTop = -999 }
if ($down -match '\\"maxY\\":(-?\d+)') { $downMax = [int]$Matches[1]; $downTopOk = [Math]::Abs($downTop - $downMax) -le 1 -and [Math]::Abs($downTop - $top0) -le 1 }
$downLeftOk = $down -match '\\"left\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $upLeft) -le 1
$downLineOk = $down -match '\\"allWhole\\":true' -and $down -match '\\"wideWhole\\":true' -and $down -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$downEndOk = $down -match '\\"inside\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
if (-not ($downPageY -and $downPageX -and $downDoc -and $downTopOk -and $downLeftOk -and $downLineOk -and $downEndOk)) { Fail 49 "past-down-moved" }
$real = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const br = box.getBoundingClientRect();
  const barY = Math.max(0, box.offsetHeight - box.clientHeight);
  const rows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { whole, seen, exit: el.textContent.trim() === "exit=0", el };
  });
  const visible = rows.filter((row) => row.seen);
  const exitRow = rows.find((row) => row.exit);
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    const er = exitRow.el.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(er.height), top: Math.round(er.top), bottom: Math.round(er.bottom) };
  }
  const stuck = !!(exit && exit.left >= exit.boxLeft - 1 && exit.left <= exit.boxLeft + 40);
  const exitWhole = !!(exitRow && exitRow.whole && exitRow.seen);
  const lowest = !!(exitRow && visible.every((row) => row.el.getBoundingClientRect().bottom <= exitRow.el.getBoundingClientRect().bottom + 1));
  const left = box.scrollLeft;
  const formula = box.scrollWidth - box.clientWidth;
  return JSON.stringify({ top: box.scrollTop, maxY: box.scrollHeight - box.clientHeight, left, formula, gap: formula - left, barY: Math.round(barY), exitWhole, stuck, lowest, barUnder: exitWhole && lowest && barY >= 10, exit, extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "real=$real"
$realTopOk = $false
if ($real -match '\\"top\\":(-?\d+)') { $realTop = [int]$Matches[1] } else { $realTop = -999 }
if ($real -match '\\"maxY\\":(-?\d+)') { $realMax = [int]$Matches[1]; $realTopOk = [Math]::Abs($realTop - $realMax) -le 1 }
$realGapOk = $real -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$realExitOk = $real -match '\\"exitWhole\\":true' -and $real -match '\\"stuck\\":true' -and $real -match '\\"lowest\\":true' -and $real -match '\\"barUnder\\":true'
$realPageOk = $real -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $real -match '\\"overflow\\":\\"hidden\\"' -and $real -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
if (-not ($realTopOk -and $realGapOk -and $realExitOk -and $realPageOk)) { Fail 50 "past-exit-lost" }
if ($real -notmatch '\\"left\\":(\d+)') { Fail 51 "no-result-left" }
$resultLeft = [int]$Matches[1]
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "before-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-growpwheel-before.png") }
$armP = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptTop = box.scrollTop;
  const keptLeft = box.scrollLeft;
  const keptPast = past.scrollTop;
  page.style.overflowX = "auto";
  page.style.overflowY = "auto";
  page.style.paddingBottom = "800px";
  let spacer = document.getElementById("uitest-wide");
  if (!spacer) {
    spacer = document.createElement("div");
    spacer.id = "uitest-wide";
    spacer.style.cssText = "width:1800px;height:1px;flex:none";
    page.appendChild(spacer);
  }
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  if (Math.abs(box.scrollLeft - keptLeft) > 1) box.scrollLeft = keptLeft;
  if (Math.abs(past.scrollTop - keptPast) > 1) past.scrollTop = keptPast;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 40), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, resultTop: box.scrollTop, resultLeft: box.scrollLeft, pastH: Math.round(r.height) });
})()
'@
Write-Output "armP=$armP"
if ($armP -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 52 "page-cannot-scroll" }
if ($armP -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 53 "page-cannot-slide" }
if ($armP -notmatch '\\"x\\":(\d+)') { Fail 54 "no-past-x" }
$px = [int]$Matches[1]
if ($armP -notmatch '\\"y\\":(\d+)') { Fail 54 "no-past-y" }
$py = [int]$Matches[1]
if ($armP -notmatch '\\"pageY\\":(-?\d+)') { Fail 55 "no-page-y" }
$pageY2 = [int]$Matches[1]
if ($armP -notmatch '\\"pageX\\":(-?\d+)') { Fail 56 "no-page-x" }
$pageX2 = [int]$Matches[1]
if ($armP -notmatch '\\"pastTop\\":(-?\d+)') { Fail 57 "no-past-top" }
$pastTop0 = [int]$Matches[1]
if ($armP -notmatch '\\"resultTop\\":(-?\d+)') { Fail 58 "no-result-top" }
$resultTop0 = [int]$Matches[1]
if ($armP -notmatch '\\"resultLeft\\":(\d+)') { Fail 59 "no-result-left-2" }
$resultLeft0 = [int]$Matches[1]
if ($armP -notmatch '\\"pastH\\":(\d+)' -or [int]$Matches[1] -lt 20) { Fail 60 "past-too-short" }
if ([Math]::Abs($resultTop0 - $realTop) -gt 1) { Fail 61 "result-moved-before-wheel" }
Send-Wheel "9335" $px $py -20 | Out-Null
Start-Sleep -Milliseconds 450
$spun = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom), h: Math.round(r.height) };
  });
  const visible = rows.filter((row) => row.seen);
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, resultTop: box.scrollTop, resultLeft: box.scrollLeft, resultMax: box.scrollHeight - box.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, allWhole: visible.length > 0 && visible.every((row) => row.whole), visible });
})()
'@
Write-Output "spun=$spun"
$spinPageY = $spun -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageY2) -le 1
$spinPageX = $spun -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageX2) -le 1
$spinDoc = $spun -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$spinResultTop = $false
if ($spun -match '\\"resultTop\\":(-?\d+)') { $spinResultTop = [Math]::Abs([int]$Matches[1] - $resultTop0) -le 1 }
$spinResultLeft = $spun -match '\\"resultLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $resultLeft0) -le 1
$spinPastTop = $false
if ($spun -match '\\"pastTop\\":(-?\d+)') { $spinPastTop = [Math]::Abs([int]$Matches[1] - $pastTop0) -le 1 }
$spinWhole = $spun -match '\\"allWhole\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$after = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom), h: Math.round(r.height) };
  });
  const visible = rows.filter((row) => row.seen);
  return JSON.stringify({ resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultLeft: box.scrollLeft, resultFormula: box.scrollWidth - box.clientWidth, pastTop: past.scrollTop, allWhole: visible.length > 0 && visible.every((row) => row.whole), visible, extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "after=$after"
$afterResult = $false
if ($after -match '\\"resultTop\\":(-?\d+)') { $afterTop = [int]$Matches[1] } else { $afterTop = -999 }
if ($after -match '\\"resultMax\\":(-?\d+)') { $afterResult = [Math]::Abs($afterTop - [int]$Matches[1]) -le 1 -and [Math]::Abs($afterTop - $realTop) -le 1 }
$afterLeft = $after -match '\\"resultLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $resultLeft) -le 1
$afterFormula = $false
if ($after -match '\\"resultFormula\\":(\d+)') { $afterFormula = [Math]::Abs([int]$Matches[1] - $resultLeft) -le 1 }
$afterWhole = $after -match '\\"allWhole\\":true'
$afterPage = $after -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $after -match '\\"overflow\\":\\"hidden\\"' -and $after -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "after-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-growpwheel-after.png") }
if (-not ($spinPageY -and $spinPageX -and $spinDoc -and $spinResultTop -and $spinResultLeft -and $spinPastTop -and $spinWhole -and $afterResult -and $afterLeft -and $afterFormula -and $afterWhole -and $afterPage)) { Fail 62 "past-wheel-moved" }
Stop-Process -Id $hostp.Id,$helpp.Id -Force
$script:hostp = $null
$script:helpp = $null
Write-Output "growpwheel-ok"
