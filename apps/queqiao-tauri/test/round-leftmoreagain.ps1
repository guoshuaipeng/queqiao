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
Set-NormalHeight $helpp.MainWindowHandle
Start-Sleep -Milliseconds 400
$wide = ("M" * 200) + "queqiao-wide-QUEEND"
$old = Run-Lines $groot $hroot "先写长行" "echo queqiao-old-1 & echo queqiao-old-2 & echo queqiao-old-3 & echo queqiao-old-4 & echo queqiao-old-5 & echo queqiao-old-6 & echo $wide"
if ($old -ne "sent") { Fail 8 $old }
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
$now = Run-Lines $groot $hroot "再写几行" "echo queqiao-now-1 & echo queqiao-now-2 & echo queqiao-now-3 & echo queqiao-now-4 & echo queqiao-now-5 & echo queqiao-now-6 & echo queqiao-now-7 & echo queqiao-now-8"
if ($now -ne "sent") { Fail 10 $now }
$nowSeen = $false
$deadline = (Get-Date).AddSeconds(12)
do {
  foreach ($el in (All-Els $groot)) {
    try { if ($el.Current.Name -and $el.Current.Name.Trim() -eq "queqiao-now-8") { $nowSeen = $true; break } } catch {}
  }
  if ($nowSeen) { break }
  Start-Sleep -Milliseconds 250
} while ((Get-Date) -lt $deadline)
if (-not $nowSeen) { Fail 11 "now-missing" }
Set-NormalHeight $helpp.MainWindowHandle
Start-Sleep -Milliseconds 500
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "closed-secret" }
$pastBtn = Find-ButtonContains $groot "之前的结果"
if (-not $pastBtn) { Fail 12 "no-past-button" }
$pastBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Start-Sleep -Milliseconds 700
$opened = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, h: Math.round(r.height), top: Math.round(r.top), bottom: Math.round(r.bottom), wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.trim() === "exit=0", el };
  });
  const visible = rows.filter((row) => row.seen);
  const wideRow = rows.find((row) => row.wide);
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
  const wideWhole = !!(wideRow && wideRow.whole && wideRow.seen);
  const wideAbove = !!(wideRow && wideRow.el.getBoundingClientRect().bottom <= br.bottom - barY + 1);
  return JSON.stringify({ hidden: past.hidden, barY: Math.round(barY), wideWhole, wideAbove, exitWhole, stuck, lowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), exit, visible: visible.map((row) => ({ t: row.t, whole: row.whole, h: row.h, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "opened=$opened"
$wideOk = $opened -match '\\"wideWhole\\":true' -and $opened -match '\\"wideAbove\\":true' -and $opened -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$exitOk = $opened -match '\\"exitWhole\\":true' -and $opened -match '\\"stuck\\":true' -and $opened -match '\\"lowest\\":true'
$lineOk = $opened -match '\\"allWhole\\":true' -and $opened -match '\\"hidden\\":false'
$pageOk = $opened -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $opened -match '\\"overflow\\":\\"hidden\\"' -and $opened -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
if (-not ($wideOk -and $exitOk -and $lineOk -and $pageOk)) { Fail 13 "past-wide-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "open-secret" }
$arm = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const past = document.getElementById("past");
  const keptTop = past.scrollTop;
  const keptLeft = past.scrollLeft;
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
  if (Math.abs(past.scrollTop - keptTop) > 1) past.scrollTop = keptTop;
  if (Math.abs(past.scrollLeft - keptLeft) > 1) past.scrollLeft = keptLeft;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight });
})()
'@
Write-Output "arm=$arm"
if ($arm -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 14 "page-cannot-scroll" }
if ($arm -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 15 "page-cannot-slide" }
if ($arm -notmatch '\\"x\\":(\d+)') { Fail 16 "no-x" }
$px = [int]$Matches[1]
if ($arm -notmatch '\\"y\\":(\d+)') { Fail 16 "no-y" }
$py = [int]$Matches[1]
if ($arm -notmatch '\\"pageY\\":(-?\d+)') { Fail 17 "no-page-y" }
$pageY0 = [int]$Matches[1]
if ($arm -notmatch '\\"pageX\\":(-?\d+)') { Fail 18 "no-page-x" }
$pageX0 = [int]$Matches[1]
$w1 = Send-WheelX "9335" $px $py 400
$w2 = Send-WheelX "9335" $px $py 400
$w3 = Send-WheelX "9335" $px $py 400
Write-Output "wheels=$w1,$w2,$w3"
Start-Sleep -Milliseconds 450
$slid = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.trim() === "exit=0", top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
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
  const left = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, left, formula, gap: formula - left, barY: Math.round(barY), wideWhole: !!(wide && wide.whole && wide.seen), end, exitWhole, stuck, lowest, barUnder: exitWhole && lowest && barY >= 10, exit });
})()
'@
Write-Output "slid=$slid"
$slidePageY = $slid -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageY0) -le 1
$slidePageX = $slid -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageX0) -le 1
$slideDoc = $slid -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$slideGap = $slid -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$slideEnd = $slid -match '\\"inside\\":true' -and $slid -match '\\"wideWhole\\":true'
$slideExit = $slid -match '\\"exitWhole\\":true' -and $slid -match '\\"stuck\\":true' -and $slid -match '\\"lowest\\":true' -and $slid -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$real = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.trim() === "exit=0", top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
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
  const left = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ left, formula, gap: formula - left, barY: Math.round(barY), wideWhole: !!(wide && wide.whole && wide.seen), end, exitWhole, stuck, lowest, barUnder: exitWhole && lowest && barY >= 10, exit, extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "real=$real"
$realGap = $real -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$realEnd = $real -match '\\"inside\\":true' -and $real -match '\\"wideWhole\\":true'
$realExit = $real -match '\\"exitWhole\\":true' -and $real -match '\\"stuck\\":true' -and $real -match '\\"lowest\\":true' -and $real -match '\\"barUnder\\":true'
$realPage = $real -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $real -match '\\"overflow\\":\\"hidden\\"' -and $real -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
if (-not ($slidePageY -and $slidePageX -and $slideDoc -and $slideGap -and $slideEnd -and $slideExit -and $realGap -and $realEnd -and $realExit -and $realPage)) { Fail 19 "past-end-lost" }
if ($real -notmatch '\\"left\\":(\d+)') { Fail 20 "no-end-left" }
$endLeft = [int]$Matches[1]
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "bottom-secret" }
$armU = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const past = document.getElementById("past");
  const keptTop = past.scrollTop;
  const keptLeft = past.scrollLeft;
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
  if (Math.abs(past.scrollTop - keptTop) > 1) past.scrollTop = keptTop;
  if (Math.abs(past.scrollLeft - keptLeft) > 1) past.scrollLeft = keptLeft;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastLeft: past.scrollLeft, pastMax: past.scrollHeight - past.clientHeight });
})()
'@
Write-Output "armU=$armU"
if ($armU -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 21 "page-cannot-scroll" }
if ($armU -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 22 "page-cannot-slide" }
if ($armU -notmatch '\\"x\\":(\d+)') { Fail 23 "no-up-x" }
$ux = [int]$Matches[1]
if ($armU -notmatch '\\"y\\":(\d+)') { Fail 23 "no-up-y" }
$uy = [int]$Matches[1]
if ($armU -notmatch '\\"pageY\\":(-?\d+)') { Fail 24 "no-up-page-y" }
$pageY1 = [int]$Matches[1]
if ($armU -notmatch '\\"pageX\\":(-?\d+)') { Fail 25 "no-up-page-x" }
$pageX1 = [int]$Matches[1]
if ($armU -notmatch '\\"pastTop\\":(-?\d+)') { Fail 26 "no-up-past-top" }
$pastTop0 = [int]$Matches[1]
if ($armU -notmatch '\\"pastLeft\\":(\d+)') { Fail 27 "no-up-past-left" }
$pastLeft0 = [int]$Matches[1]
if ($armU -notmatch '\\"pastMax\\":(-?\d+)' -or [int]$Matches[1] -lt 20) { Fail 29 "past-cannot-scroll" }
$pastMax0 = [int]$Matches[1]
if ([Math]::Abs($pastTop0 - $pastMax0) -gt 1) { Fail 30 "not-at-vertical-end" }
Send-Wheel "9335" $ux $uy -20 | Out-Null
Start-Sleep -Milliseconds 450
$up = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.trim() === "exit=0", top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
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
  const left = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, top: past.scrollTop, maxY: past.scrollHeight - past.clientHeight, left, formula, gap: formula - left, barY: Math.round(barY), wideWhole: !!(wide && wide.whole && wide.seen), end, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: visible.length > 0 && visible.every((row) => row.whole) && barY >= 10, visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })) });
})()
'@
Write-Output "up=$up"
$upPageY = $up -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageY1) -le 1
$upPageX = $up -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageX1) -le 1
$upDoc = $up -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$upTop = $false
if ($up -match '\\"top\\":(-?\d+)') { $upTopN = [int]$Matches[1]; $upTop = [Math]::Abs($upTopN - ($pastTop0 - 20)) -le 1 }
$upLeft = $up -match '\\"left\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeft0) -le 1
$upEnd = $up -match '\\"inside\\":true' -and $up -match '\\"wideWhole\\":true' -and $up -match '\\"allWhole\\":true' -and $up -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$kept = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.trim() === "exit=0", top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
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
  const left = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ top: past.scrollTop, left, formula, gap: formula - left, barY: Math.round(barY), wideWhole: !!(wide && wide.whole && wide.seen), end, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: visible.length > 0 && visible.every((row) => row.whole) && barY >= 10, visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "kept=$kept"
$keptGap = $kept -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeft = $kept -match '\\"left\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptEnd = $kept -match '\\"inside\\":true' -and $kept -match '\\"wideWhole\\":true' -and $kept -match '\\"allWhole\\":true' -and $kept -match '\\"barUnder\\":true'
$keptPage = $kept -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $kept -match '\\"overflow\\":\\"hidden\\"' -and $kept -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptTopOk = $false
if ($kept -match '\\"top\\":(-?\d+)') { $keptTopOk = [Math]::Abs([int]$Matches[1] - ($pastTop0 - 20)) -le 1 }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($upPageY -and $upPageX -and $upDoc -and $upTop -and $upLeft -and $upEnd -and $keptGap -and $keptLeft -and $keptEnd -and $keptTopOk -and $keptPage)) { Fail 28 "past-up-lost" }
$old6 = Send-Eval "9335" @'
(() => {
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { old: el.textContent.indexOf("queqiao-old-6") >= 0, whole, seen, el };
  });
  const row = rows.find((item) => item.old);
  let text = null;
  if (row && row.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(row.el.firstChild);
    const rect = range.getBoundingClientRect();
    const er = row.el.getBoundingClientRect();
    text = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(er.height), top: Math.round(er.top), bottom: Math.round(er.bottom) };
  }
  const stuck = !!(text && text.left >= text.boxLeft - 1 && text.left <= text.boxLeft + 40);
  const whole = !!(row && row.whole && row.seen);
  const left = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ barY: Math.round(barY), whole, stuck, clear: whole && stuck && barY >= 10, text, left, formula, gap: formula - left });
})()
'@
Write-Output "old6=$old6"
$old6Ok = $old6 -match '\\"whole\\":true' -and $old6 -match '\\"stuck\\":true' -and $old6 -match '\\"clear\\":true'
$old6Bar = $old6 -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$old6Gap = $old6 -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
if (-not ($old6Ok -and $old6Bar -and $old6Gap)) { Fail 41 "old6-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "old6-secret" }
$arm5 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const past = document.getElementById("past");
  const keptTop = past.scrollTop;
  const keptLeft = past.scrollLeft;
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
  if (Math.abs(past.scrollTop - keptTop) > 1) past.scrollTop = keptTop;
  if (Math.abs(past.scrollLeft - keptLeft) > 1) past.scrollLeft = keptLeft;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastLeft: past.scrollLeft });
})()
'@
Write-Output "arm5=$arm5"
if ($arm5 -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 42 "page-cannot-scroll" }
if ($arm5 -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 43 "page-cannot-slide" }
if ($arm5 -notmatch '\\"x\\":(\d+)') { Fail 44 "no-x5" }
$x5 = [int]$Matches[1]
if ($arm5 -notmatch '\\"y\\":(\d+)') { Fail 44 "no-y5" }
$y5 = [int]$Matches[1]
if ($arm5 -notmatch '\\"pageY\\":(-?\d+)') { Fail 45 "no-page-y5" }
$pageY5 = [int]$Matches[1]
if ($arm5 -notmatch '\\"pageX\\":(-?\d+)') { Fail 46 "no-page-x5" }
$pageX5 = [int]$Matches[1]
if ($arm5 -notmatch '\\"pastTop\\":(-?\d+)') { Fail 47 "no-past-top5" }
$top5 = [int]$Matches[1]
if ($arm5 -notmatch '\\"pastLeft\\":(\d+)') { Fail 48 "no-past-left5" }
$left5 = [int]$Matches[1]
Send-Wheel "9335" $x5 $y5 -20 | Out-Null
Start-Sleep -Milliseconds 450
$up5 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, old: el.textContent.indexOf("queqiao-old-5") >= 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const row = rows.find((item) => item.old);
  let text = null;
  if (row && row.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(row.el.firstChild);
    const rect = range.getBoundingClientRect();
    const er = row.el.getBoundingClientRect();
    text = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(er.height), top: Math.round(er.top), bottom: Math.round(er.bottom) };
  }
  const stuck = !!(text && text.left >= text.boxLeft - 1 && text.left <= text.boxLeft + 40);
  const whole = !!(row && row.whole && row.seen);
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, top: past.scrollTop, left: past.scrollLeft, barY: Math.round(barY), whole, stuck, allWhole: visible.length > 0 && visible.every((item) => item.whole), barUnder: visible.length > 0 && visible.every((item) => item.whole) && barY >= 10, text, visible: visible.map((item) => ({ t: item.t, whole: item.whole, top: item.top, bottom: item.bottom })) });
})()
'@
Write-Output "up5=$up5"
$p5y = $up5 -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageY5) -le 1
$p5x = $up5 -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageX5) -le 1
$p5doc = $up5 -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$t5 = $false
if ($up5 -match '\\"top\\":(-?\d+)') { $t5 = [Math]::Abs([int]$Matches[1] - ($top5 - 20)) -le 1 }
$l5 = $up5 -match '\\"left\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $left5) -le 1
$line5 = $up5 -match '\\"whole\\":true' -and $up5 -match '\\"stuck\\":true' -and $up5 -match '\\"allWhole\\":true' -and $up5 -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$real5 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, old: el.textContent.indexOf("queqiao-old-5") >= 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const row = rows.find((item) => item.old);
  let text = null;
  if (row && row.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(row.el.firstChild);
    const rect = range.getBoundingClientRect();
    const er = row.el.getBoundingClientRect();
    text = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(er.height), top: Math.round(er.top), bottom: Math.round(er.bottom) };
  }
  const stuck = !!(text && text.left >= text.boxLeft - 1 && text.left <= text.boxLeft + 40);
  const whole = !!(row && row.whole && row.seen);
  const left = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ top: past.scrollTop, left, formula, gap: formula - left, barY: Math.round(barY), whole, stuck, allWhole: visible.length > 0 && visible.every((item) => item.whole), barUnder: visible.length > 0 && visible.every((item) => item.whole) && barY >= 10, text, visible: visible.map((item) => ({ t: item.t, whole: item.whole, top: item.top, bottom: item.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "real5=$real5"
$real5Gap = $real5 -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$real5Left = $real5 -match '\\"left\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$real5Line = $real5 -match '\\"whole\\":true' -and $real5 -match '\\"stuck\\":true' -and $real5 -match '\\"allWhole\\":true' -and $real5 -match '\\"barUnder\\":true'
$real5Bar = $real5 -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$real5Page = $real5 -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $real5 -match '\\"overflow\\":\\"hidden\\"' -and $real5 -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$real5Top = $false
if ($real5 -match '\\"top\\":(-?\d+)') { $real5Top = [Math]::Abs([int]$Matches[1] - ($top5 - 20)) -le 1 }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($p5y -and $p5x -and $p5doc -and $t5 -and $l5 -and $line5 -and $real5Gap -and $real5Left -and $real5Line -and $real5Bar -and $real5Page -and $real5Top)) { Fail 49 "old5-lost" }
if ($real5 -notmatch '\\"top\\":(-?\d+)') { Fail 50 "no-real5-top" }
$held5 = [int]$Matches[1]
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "old5-secret" }
$armR = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const past = document.getElementById("past");
  const keptTop = past.scrollTop;
  const keptLeft = past.scrollLeft;
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
  if (Math.abs(past.scrollTop - keptTop) > 1) past.scrollTop = keptTop;
  if (Math.abs(past.scrollLeft - keptLeft) > 1) past.scrollLeft = keptLeft;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 30), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastLeft: past.scrollLeft });
})()
'@
Write-Output "armR=$armR"
if ($armR -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 51 "page-cannot-scroll" }
if ($armR -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 52 "page-cannot-slide" }
if ($armR -notmatch '\\"x\\":(\d+)') { Fail 53 "no-xr" }
$xr = [int]$Matches[1]
if ($armR -notmatch '\\"y\\":(\d+)') { Fail 53 "no-yr" }
$yr = [int]$Matches[1]
if ($armR -notmatch '\\"pageY\\":(-?\d+)') { Fail 54 "no-page-yr" }
$pageYr = [int]$Matches[1]
if ($armR -notmatch '\\"pageX\\":(-?\d+)') { Fail 55 "no-page-xr" }
$pageXr = [int]$Matches[1]
if ($armR -notmatch '\\"pastTop\\":(-?\d+)') { Fail 56 "no-past-topr" }
$topR = [int]$Matches[1]
if ($armR -notmatch '\\"pastLeft\\":(\d+)') { Fail 57 "no-past-leftr" }
$leftR = [int]$Matches[1]
Send-Wheel "9335" $xr $yr 20 | Out-Null
Start-Sleep -Milliseconds 450
$backR = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, old: el.textContent.indexOf("queqiao-old-6") >= 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const old = rows.find((row) => row.old);
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
  let text = null;
  if (old && old.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(old.el.firstChild);
    const rect = range.getBoundingClientRect();
    const er = old.el.getBoundingClientRect();
    text = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(er.height), top: Math.round(er.top), bottom: Math.round(er.bottom) };
  }
  const stuck = !!(text && text.left >= text.boxLeft - 1 && text.left <= text.boxLeft + 40);
  const oldWhole = !!(old && old.whole && old.seen);
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, top: past.scrollTop, left: past.scrollLeft, barY: Math.round(barY), wideWhole: !!(wide && wide.whole && wide.seen), end, oldWhole, stuck, allWhole: visible.length > 0 && visible.every((item) => item.whole), barUnder: visible.length > 0 && visible.every((item) => item.whole) && barY >= 10, text, visible: visible.map((item) => ({ t: item.t, whole: item.whole, top: item.top, bottom: item.bottom })) });
})()
'@
Write-Output "backR=$backR"
$ryOk = $backR -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYr) -le 1
$rxOk = $backR -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXr) -le 1
$rdOk = $backR -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$rtOk = $false
if ($backR -match '\\"top\\":(-?\d+)') { $rtOk = [Math]::Abs([int]$Matches[1] - ($topR + 20)) -le 1 }
$rlOk = $backR -match '\\"left\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $leftR) -le 1
$rend = $backR -match '\\"inside\\":true' -and $backR -match '\\"wideWhole\\":true' -and $backR -match '\\"oldWhole\\":true' -and $backR -match '\\"stuck\\":true' -and $backR -match '\\"allWhole\\":true' -and $backR -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$land = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, old: el.textContent.indexOf("queqiao-old-6") >= 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const old = rows.find((row) => row.old);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let text = null;
  if (old && old.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(old.el.firstChild);
    const rect = range.getBoundingClientRect();
    const er = old.el.getBoundingClientRect();
    text = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(er.height), top: Math.round(er.top), bottom: Math.round(er.bottom) };
  }
  const stuck = !!(text && text.left >= text.boxLeft - 1 && text.left <= text.boxLeft + 40);
  const oldWhole = !!(old && old.whole && old.seen);
  const left = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ top: past.scrollTop, left, formula, gap: formula - left, barY: Math.round(barY), wideWhole: !!(wide && wide.whole && wide.seen), end, oldWhole, stuck, allWhole: visible.length > 0 && visible.every((item) => item.whole), barUnder: visible.length > 0 && visible.every((item) => item.whole) && barY >= 10, text, visible: visible.map((item) => ({ t: item.t, whole: item.whole, top: item.top, bottom: item.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "land=$land"
$landTop = $false
if ($land -match '\\"top\\":(-?\d+)') { $landTop = [Math]::Abs([int]$Matches[1] - ($held5 + 20)) -le 1 }
$landGap = $land -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$landLeft = $land -match '\\"left\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$landEnd = $land -match '\\"inside\\":true' -and $land -match '\\"wideWhole\\":true' -and $land -match '\\"oldWhole\\":true' -and $land -match '\\"stuck\\":true' -and $land -match '\\"allWhole\\":true' -and $land -match '\\"barUnder\\":true'
$landPage = $land -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $land -match '\\"overflow\\":\\"hidden\\"' -and $land -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($ryOk -and $rxOk -and $rdOk -and $rtOk -and $rlOk -and $rend -and $landTop -and $landGap -and $landLeft -and $landEnd -and $landPage)) { Fail 58 "past-return-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "past-secret" }
$armN = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = box.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armN=$armN"
if ($armN -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 59 "page-cannot-scroll" }
if ($armN -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 60 "page-cannot-slide" }
if ($armN -notmatch '\\"x\\":(\d+)') { Fail 61 "no-xn" }
$xn = [int]$Matches[1]
if ($armN -notmatch '\\"y\\":(\d+)') { Fail 61 "no-yn" }
$yn = [int]$Matches[1]
if ($armN -notmatch '\\"pageY\\":(-?\d+)') { Fail 62 "no-page-yn" }
$pageYn = [int]$Matches[1]
if ($armN -notmatch '\\"pageX\\":(-?\d+)') { Fail 63 "no-page-xn" }
$pageXn = [int]$Matches[1]
if ($armN -notmatch '\\"pastTop\\":(-?\d+)') { Fail 64 "no-past-topn" }
$pastTopN = [int]$Matches[1]
if ($armN -notmatch '\\"pastLeft\\":(\d+)') { Fail 65 "no-past-leftn" }
$pastLeftN = [int]$Matches[1]
if ($armN -notmatch '\\"resultTop\\":(-?\d+)') { Fail 67 "no-result-top" }
$resultTopN = [int]$Matches[1]
if ($armN -notmatch '\\"resultMax\\":(-?\d+)' -or [int]$Matches[1] -lt 20) { Fail 68 "result-cannot-scroll" }
$resultMaxN = [int]$Matches[1]
if ([Math]::Abs($resultTopN - $resultMaxN) -gt 1) { Fail 69 "result-not-at-end" }
Send-Wheel "9335" $xn $yn -20 | Out-Null
Start-Sleep -Milliseconds 450
$spunN = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, old: el.textContent.indexOf("queqiao-old-6") >= 0, el };
  });
  const wide = rows.find((row) => row.wide);
  const old = rows.find((row) => row.old);
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
  let text = null;
  if (old && old.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(old.el.firstChild);
    const rect = range.getBoundingClientRect();
    text = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left) };
  }
  const stuck = !!(text && text.left >= text.boxLeft - 1 && text.left <= text.boxLeft + 40);
  const oldWhole = !!(old && old.whole && old.seen);
  const box = document.getElementById("result");
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastLeft: past.scrollLeft, resultTop: box.scrollTop, wideWhole: !!(wide && wide.whole && wide.seen), end, oldWhole, stuck });
})()
'@
Write-Output "spunN=$spunN"
$ny = $spunN -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYn) -le 1
$nx = $spunN -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXn) -le 1
$nd = $spunN -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$npt = $false
if ($spunN -match '\\"pastTop\\":(-?\d+)') { $npt = [Math]::Abs([int]$Matches[1] - $pastTopN) -le 1 }
$npl = $spunN -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftN) -le 1
$nend = $spunN -match '\\"inside\\":true' -and $spunN -match '\\"wideWhole\\":true' -and $spunN -match '\\"oldWhole\\":true' -and $spunN -match '\\"stuck\\":true'
$nrt = $false
if ($spunN -match '\\"resultTop\\":(-?\d+)') { $nrt = [Math]::Abs([int]$Matches[1] - ($resultTopN - 20)) -le 1 }
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$stay = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, old: el.textContent.indexOf("queqiao-old-6") >= 0, el };
  });
  const wide = rows.find((row) => row.wide);
  const old = rows.find((row) => row.old);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let text = null;
  if (old && old.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(old.el.firstChild);
    const rect = range.getBoundingClientRect();
    const er = old.el.getBoundingClientRect();
    text = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(er.height), top: Math.round(er.top), bottom: Math.round(er.bottom) };
  }
  const stuck = !!(text && text.left >= text.boxLeft - 1 && text.left <= text.boxLeft + 40);
  const oldWhole = !!(old && old.whole && old.seen);
  const left = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const box = document.getElementById("result");
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  return JSON.stringify({ pastTop: past.scrollTop, left, formula, gap: formula - left, barY: Math.round(barY), wideWhole: !!(wide && wide.whole && wide.seen), end, oldWhole, stuck, text, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "stay=$stay"
$stayTop = $false
if ($stay -match '\\"pastTop\\":(-?\d+)') { $stayTop = [Math]::Abs([int]$Matches[1] - $pastTopN) -le 1 }
$stayGap = $stay -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$stayLeft = $stay -match '\\"left\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$stayEnd = $stay -match '\\"inside\\":true' -and $stay -match '\\"wideWhole\\":true' -and $stay -match '\\"oldWhole\\":true' -and $stay -match '\\"stuck\\":true'
$stayPage = $stay -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $stay -match '\\"overflow\\":\\"hidden\\"' -and $stay -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$stayResult = $false
if ($stay -match '\\"resultTop\\":(-?\d+)') { $stayResult = [Math]::Abs([int]$Matches[1] - ($resultTopN - 20)) -le 1 }
$stayRows = $stay -match '\\"resultWhole\\":true'
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($ny -and $nx -and $nd -and $npt -and $npl -and $nend -and $nrt -and $stayTop -and $stayGap -and $stayLeft -and $stayEnd -and $stayPage -and $stayResult -and $stayRows)) { Fail 66 "now-wheel-moved" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "up-secret" }
$armB = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = box.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armB=$armB"
if ($armB -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 70 "page-cannot-scroll" }
if ($armB -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 71 "page-cannot-slide" }
if ($armB -notmatch '\\"x\\":(\d+)') { Fail 72 "no-xb" }
$xb = [int]$Matches[1]
if ($armB -notmatch '\\"y\\":(\d+)') { Fail 72 "no-yb" }
$yb = [int]$Matches[1]
if ($armB -notmatch '\\"pageY\\":(-?\d+)') { Fail 73 "no-page-yb" }
$pageYb = [int]$Matches[1]
if ($armB -notmatch '\\"pageX\\":(-?\d+)') { Fail 74 "no-page-xb" }
$pageXb = [int]$Matches[1]
if ($armB -notmatch '\\"pastTop\\":(-?\d+)') { Fail 75 "no-past-topb" }
$pastTopB = [int]$Matches[1]
if ($armB -notmatch '\\"pastLeft\\":(\d+)') { Fail 76 "no-past-leftb" }
$pastLeftB = [int]$Matches[1]
if ($armB -notmatch '\\"resultTop\\":(-?\d+)') { Fail 77 "no-result-topb" }
$resultTopB = [int]$Matches[1]
Send-Wheel "9335" $xb $yb 20 | Out-Null
Start-Sleep -Milliseconds 450
$downB = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, el };
  });
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
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), wideWhole: !!(wide && wide.whole && wide.seen), end, resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "downB=$downB"
$byOk = $downB -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYb) -le 1
$bxOk = $downB -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXb) -le 1
$bdOk = $downB -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$bpt = $false
if ($downB -match '\\"pastTop\\":(-?\d+)') { $bpt = [Math]::Abs([int]$Matches[1] - $pastTopB) -le 1 }
$bpl = $downB -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftB) -le 1
$brt = $false
if ($downB -match '\\"resultTop\\":(-?\d+)') { $brtN = [int]$Matches[1] } else { $brtN = -999 }
if ($downB -match '\\"resultMax\\":(-?\d+)') { $brt = [Math]::Abs($brtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($brtN - ($resultTopB + 20)) -le 1 }
$bend = $downB -match '\\"inside\\":true' -and $downB -match '\\"wideWhole\\":true' -and $downB -match '\\"resultWhole\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$backHome = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, el };
  });
  const wide = rows.find((row) => row.wide);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const left = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ pastTop: past.scrollTop, left, formula, gap: formula - left, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), end, extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "backHome=$backHome"
$backTop = $false
if ($backHome -match '\\"resultTop\\":(-?\d+)') { $backTopN = [int]$Matches[1] } else { $backTopN = -999 }
if ($backHome -match '\\"resultMax\\":(-?\d+)') { $backTop = [Math]::Abs($backTopN - [int]$Matches[1]) -le 1 -and [Math]::Abs($backTopN - $resultTopN) -le 1 }
$backGap = $backHome -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$backLeft = $backHome -match '\\"left\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$backEnd = $backHome -match '\\"inside\\":true' -and $backHome -match '\\"resultWhole\\":true'
$backPast = $false
if ($backHome -match '\\"pastTop\\":(-?\d+)') { $backPast = [Math]::Abs([int]$Matches[1] - $pastTopN) -le 1 }
$backPage = $backHome -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $backHome -match '\\"overflow\\":\\"hidden\\"' -and $backHome -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($byOk -and $bxOk -and $bdOk -and $bpt -and $bpl -and $brt -and $bend -and $backTop -and $backGap -and $backLeft -and $backEnd -and $backPast -and $backPage)) { Fail 78 "now-back-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "hold-secret" }
$armP = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armP=$armP"
if ($armP -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 79 "page-cannot-scroll" }
if ($armP -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 80 "page-cannot-slide" }
if ($armP -notmatch '\\"x\\":(\d+)') { Fail 81 "no-xp" }
$xp = [int]$Matches[1]
if ($armP -notmatch '\\"y\\":(\d+)') { Fail 81 "no-yp" }
$yp = [int]$Matches[1]
if ($armP -notmatch '\\"pageY\\":(-?\d+)') { Fail 82 "no-page-yp" }
$pageYp = [int]$Matches[1]
if ($armP -notmatch '\\"pageX\\":(-?\d+)') { Fail 83 "no-page-xp" }
$pageXp = [int]$Matches[1]
if ($armP -notmatch '\\"pastTop\\":(-?\d+)') { Fail 84 "no-past-topp" }
$pastTopP = [int]$Matches[1]
if ($armP -notmatch '\\"pastLeft\\":(\d+)') { Fail 85 "no-past-leftp" }
$pastLeftP = [int]$Matches[1]
if ($armP -notmatch '\\"resultTop\\":(-?\d+)') { Fail 86 "no-result-topp" }
$resultTopP = [int]$Matches[1]
Send-Wheel "9335" $xp $yp -20 | Out-Null
Start-Sleep -Milliseconds 450
$upP = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
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
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.seen !== false && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), wideWhole: !!(wide && wide.whole && wide.seen), end, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "upP=$upP"
$pyOk = $upP -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYp) -le 1
$pxOk = $upP -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXp) -le 1
$pdOk = $upP -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$ppt = $false
if ($upP -match '\\"pastTop\\":(-?\d+)') { $ppt = [Math]::Abs([int]$Matches[1] - ($pastTopP - 20)) -le 1 }
$ppl = $upP -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftP) -le 1
$prt = $false
if ($upP -match '\\"resultTop\\":(-?\d+)') { $prtN = [int]$Matches[1]; $prt = [Math]::Abs($prtN - $resultTopP) -le 1 }
$prows = $upP -match '\\"resultWhole\\":true' -and $upP -match '\\"allWhole\\":true'
$pend = $upP -match '\\"inside\\":true' -and $upP -match '\\"wideWhole\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptP = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
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
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const left = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ pastTop: past.scrollTop, left, formula, gap: formula - left, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptP=$keptP"
$keptPast = $false
if ($keptP -match '\\"pastTop\\":(-?\d+)') { $keptPast = [Math]::Abs([int]$Matches[1] - ($pastTopP - 20)) -le 1 }
$keptGap = $keptP -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeft = $keptP -match '\\"left\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResult = $false
if ($keptP -match '\\"resultTop\\":(-?\d+)') { $keptResultN = [int]$Matches[1] } else { $keptResultN = -999 }
if ($keptP -match '\\"resultMax\\":(-?\d+)') { $keptResult = [Math]::Abs($keptResultN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultN - $resultTopP) -le 1 }
$keptRows = $keptP -match '\\"resultWhole\\":true' -and $keptP -match '\\"allWhole\\":true' -and $keptP -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptEnd = $keptP -match '\\"inside\\":true' -and $keptP -match '\\"wideWhole\\":true'
$keptPage = $keptP -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptP -match '\\"overflow\\":\\"hidden\\"' -and $keptP -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($pyOk -and $pxOk -and $pdOk -and $ppt -and $ppl -and $prt -and $prows -and $keptPast -and $keptGap -and $keptLeft -and $keptResult -and $keptRows -and $keptPage)) { Fail 87 "past-hold-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "qend-secret"
$armQ = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armQ=$armQ"
if ($armQ -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 88 "page-cannot-scroll" }
if ($armQ -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 89 "page-cannot-slide" }
if ($armQ -notmatch '\\"x\\":(\d+)') { Fail 90 "no-xq" }
$xq = [int]$Matches[1]
if ($armQ -notmatch '\\"y\\":(\d+)') { Fail 90 "no-yq" }
$yq = [int]$Matches[1]
if ($armQ -notmatch '\\"pageY\\":(-?\d+)') { Fail 91 "no-page-yq" }
$pageYq = [int]$Matches[1]
if ($armQ -notmatch '\\"pageX\\":(-?\d+)') { Fail 92 "no-page-xq" }
$pageXq = [int]$Matches[1]
if ($armQ -notmatch '\\"pastTop\\":(-?\d+)') { Fail 93 "no-past-topq" }
$pastTopQ = [int]$Matches[1]
if ($armQ -notmatch '\\"pastLeft\\":(\d+)') { Fail 94 "no-past-leftq" }
$pastLeftQ = [int]$Matches[1]
if ($armQ -notmatch '\\"resultTop\\":(-?\d+)') { Fail 95 "no-result-topq" }
$resultTopQ = [int]$Matches[1]
Send-Wheel "9335" $xq $yq 20 | Out-Null
Start-Sleep -Milliseconds 450
$backQ = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
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
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), wideWhole: !!(wide && wide.whole && wide.seen), end, allWhole: visible.length > 0 && visible.every((row) => row.whole), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })) });
})()
'@
Write-Output "backQ=$backQ"
$qy = $backQ -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYq) -le 1
$qx = $backQ -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXq) -le 1
$qd = $backQ -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$qpt = $false
if ($backQ -match '\\"pastTop\\":(-?\d+)') { $qpt = [Math]::Abs([int]$Matches[1] - ($pastTopQ + 20)) -le 1 }
$qpl = $backQ -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftQ) -le 1
$qrt = $false
if ($backQ -match '\\"resultTop\\":(-?\d+)') { $qrtN = [int]$Matches[1] } else { $qrtN = -999 }
if ($backQ -match '\\"resultMax\\":(-?\d+)') { $qrt = [Math]::Abs($qrtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($qrtN - $resultTopQ) -le 1 }
$qrows = $backQ -match '\\"resultWhole\\":true' -and $backQ -match '\\"allWhole\\":true' -and $backQ -match '\\"wideWhole\\":true' -and $backQ -match '\\"inside\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$shown = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
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
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const left = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ pastTop: past.scrollTop, left, formula, gap: formula - left, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, allWhole: visible.length > 0 && visible.every((row) => row.whole), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "shown=$shown"
$shownPast = $false
if ($shown -match '\\"pastTop\\":(-?\d+)') { $shownPast = [Math]::Abs([int]$Matches[1] - ($pastTopQ + 20)) -le 1 }
$shownGap = $shown -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$shownLeft = $shown -match '\\"left\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$shownResult = $false
if ($shown -match '\\"resultTop\\":(-?\d+)') { $shownResultN = [int]$Matches[1] } else { $shownResultN = -999 }
if ($shown -match '\\"resultMax\\":(-?\d+)') { $shownResult = [Math]::Abs($shownResultN - [int]$Matches[1]) -le 1 -and [Math]::Abs($shownResultN - $resultTopQ) -le 1 }
$shownRows = $shown -match '\\"resultWhole\\":true' -and $shown -match '\\"allWhole\\":true' -and $shown -match '\\"wideWhole\\":true' -and $shown -match '\\"inside\\":true'
$shownPage = $shown -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $shown -match '\\"overflow\\":\\"hidden\\"' -and $shown -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
if (-not ($qy -and $qx -and $qd -and $qpt -and $qpl -and $qrt -and $qrows -and $shownPast -and $shownGap -and $shownLeft -and $shownResult -and $shownRows -and $shownPage)) { Fail 96 "past-qend-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "bottom-secret"
$armZ = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastLeft: past.scrollLeft, pastMax: past.scrollHeight - past.clientHeight, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armZ=$armZ"
if ($armZ -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 97 "page-cannot-scroll" }
if ($armZ -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 98 "page-cannot-slide" }
if ($armZ -notmatch '\\"x\\":(\d+)') { Fail 99 "no-xz" }
$xz = [int]$Matches[1]
if ($armZ -notmatch '\\"y\\":(\d+)') { Fail 99 "no-yz" }
$yz = [int]$Matches[1]
if ($armZ -notmatch '\\"pageY\\":(-?\d+)') { Fail 100 "no-page-yz" }
$pageYz = [int]$Matches[1]
if ($armZ -notmatch '\\"pageX\\":(-?\d+)') { Fail 101 "no-page-xz" }
$pageXz = [int]$Matches[1]
if ($armZ -notmatch '\\"pastTop\\":(-?\d+)') { Fail 102 "no-past-topz" }
$pastTopZ = [int]$Matches[1]
if ($armZ -notmatch '\\"pastLeft\\":(\d+)') { Fail 103 "no-past-leftz" }
$pastLeftZ = [int]$Matches[1]
if ($armZ -notmatch '\\"pastMax\\":(-?\d+)') { Fail 104 "no-past-maxz" }
$pastMaxZ = [int]$Matches[1]
if ($pastMaxZ - $pastTopZ -lt 10) { Fail 105 "past-already-bottom" }
if ($armZ -notmatch '\\"resultTop\\":(-?\d+)') { Fail 106 "no-result-topz" }
$resultTopZ = [int]$Matches[1]
Send-Wheel "9335" $xz $yz 20 | Out-Null
Start-Sleep -Milliseconds 450
$backZ = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
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
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const exitRow = rows.find((row) => row.exit);
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen, lowest: !!exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1) };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const barUnder = !!(exit && exit.seen && exit.lowest && barY >= 10);
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), wideWhole: !!(wide && wide.whole && wide.seen), end, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder, exit, visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })) });
})()
'@
Write-Output "backZ=$backZ"
$zy = $backZ -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYz) -le 1
$zx = $backZ -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXz) -le 1
$zd = $backZ -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$zpt = $false
if ($backZ -match '\\"pastTop\\":(-?\d+)') { $zptN = [int]$Matches[1] } else { $zptN = -999 }
if ($backZ -match '\\"pastMax\\":(-?\d+)') { $zpt = [Math]::Abs($zptN - [int]$Matches[1]) -le 1 -and $zptN -gt $pastTopZ + 10 }
$zpl = $backZ -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftZ) -le 1
$zrt = $false
if ($backZ -match '\\"resultTop\\":(-?\d+)') { $zrtN = [int]$Matches[1] } else { $zrtN = -999 }
if ($backZ -match '\\"resultMax\\":(-?\d+)') { $zrt = [Math]::Abs($zrtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($zrtN - $resultTopZ) -le 1 }
$zrows = $backZ -match '\\"resultWhole\\":true' -and $backZ -match '\\"allWhole\\":true' -and $backZ -match '\\"wideWhole\\":true' -and $backZ -match '\\"inside\\":true' -and $backZ -match '\\"stuck\\":true' -and $backZ -match '\\"lowest\\":true' -and $backZ -match '\\"barUnder\\":true' -and $backZ -match '\\"seen\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$floor = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
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
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const exitRow = rows.find((row) => row.exit);
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen, lowest: !!exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1) };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const left = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const barUnder = !!(exit && exit.seen && exit.lowest && barY >= 10);
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, left, formula, gap: formula - left, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder, exit, visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "floor=$floor"
$floorPast = $false
if ($floor -match '\\"pastTop\\":(-?\d+)') { $floorPastN = [int]$Matches[1] } else { $floorPastN = -999 }
if ($floor -match '\\"pastMax\\":(-?\d+)') { $floorPast = [Math]::Abs($floorPastN - [int]$Matches[1]) -le 1 -and $floorPastN -gt $pastTopZ + 10 }
$floorGap = $floor -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$floorLeft = $floor -match '\\"left\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$floorResult = $false
if ($floor -match '\\"resultTop\\":(-?\d+)') { $floorResultN = [int]$Matches[1] } else { $floorResultN = -999 }
if ($floor -match '\\"resultMax\\":(-?\d+)') { $floorResult = [Math]::Abs($floorResultN - [int]$Matches[1]) -le 1 -and [Math]::Abs($floorResultN - $resultTopZ) -le 1 }
$floorRows = $floor -match '\\"resultWhole\\":true' -and $floor -match '\\"allWhole\\":true' -and $floor -match '\\"wideWhole\\":true' -and $floor -match '\\"inside\\":true' -and $floor -match '\\"stuck\\":true' -and $floor -match '\\"lowest\\":true' -and $floor -match '\\"barUnder\\":true' -and $floor -match '\\"seen\\":true' -and $floor -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$floorPage = $floor -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $floor -match '\\"overflow\\":\\"hidden\\"' -and $floor -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($zy -and $zx -and $zd -and $zpt -and $zpl -and $zrt -and $zrows -and $floorPast -and $floorGap -and $floorLeft -and $floorResult -and $floorRows -and $floorPage)) { Fail 107 "past-bottom-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "stay-secret"
$armK = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = box.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armK=$armK"
if ($armK -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 108 "page-cannot-scroll" }
if ($armK -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 109 "page-cannot-slide" }
if ($armK -notmatch '\\"x\\":(\d+)') { Fail 110 "no-xk" }
$xk = [int]$Matches[1]
if ($armK -notmatch '\\"y\\":(\d+)') { Fail 110 "no-yk" }
$yk = [int]$Matches[1]
if ($armK -notmatch '\\"pageY\\":(-?\d+)') { Fail 111 "no-page-yk" }
$pageYk = [int]$Matches[1]
if ($armK -notmatch '\\"pageX\\":(-?\d+)') { Fail 112 "no-page-xk" }
$pageXk = [int]$Matches[1]
if ($armK -notmatch '\\"pastTop\\":(-?\d+)') { Fail 113 "no-past-topk" }
$pastTopK = [int]$Matches[1]
if ($armK -notmatch '\\"pastMax\\":(-?\d+)') { Fail 114 "no-past-maxk" }
$pastMaxK = [int]$Matches[1]
if ([Math]::Abs($pastTopK - $pastMaxK) -gt 1) { Fail 115 "past-not-at-end" }
if ($armK -notmatch '\\"pastLeft\\":(\d+)') { Fail 116 "no-past-leftk" }
$pastLeftK = [int]$Matches[1]
if ($armK -notmatch '\\"resultTop\\":(-?\d+)') { Fail 117 "no-result-topk" }
$resultTopK = [int]$Matches[1]
if ($armK -notmatch '\\"resultMax\\":(-?\d+)' -or [int]$Matches[1] -lt 20) { Fail 118 "result-cannot-scroll" }
$resultMaxK = [int]$Matches[1]
if ([Math]::Abs($resultTopK - $resultMaxK) -gt 1) { Fail 119 "result-not-at-end" }
Send-Wheel "9335" $xk $yk -20 | Out-Null
Start-Sleep -Milliseconds 450
$upK = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
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
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const exitRow = rows.find((row) => row.exit);
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen, lowest: !!exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1) };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const barUnder = !!(exit && exit.seen && exit.lowest && barY >= 10);
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder, exit, visible: visible.map((row) => ({ t: row.t, whole: row.whole })), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })) });
})()
'@
Write-Output "upK=$upK"
$ky = $upK -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYk) -le 1
$kx = $upK -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXk) -le 1
$kd = $upK -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$kpt = $false
if ($upK -match '\\"pastTop\\":(-?\d+)') { $kptN = [int]$Matches[1] } else { $kptN = -999 }
if ($upK -match '\\"pastMax\\":(-?\d+)') { $kpt = [Math]::Abs($kptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($kptN - $pastTopK) -le 1 }
$kpl = $upK -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftK) -le 1
$krt = $false
if ($upK -match '\\"resultTop\\":(-?\d+)') { $krtN = [int]$Matches[1] } else { $krtN = -999 }
if ($upK -match '\\"resultMax\\":(-?\d+)') { $krt = [Math]::Abs($krtN - ($resultTopK - 20)) -le 1 -and $krtN -lt [int]$Matches[1] - 10 }
$krows = $upK -match '\\"resultWhole\\":true' -and $upK -match '\\"resultNow7\\":true' -and $upK -match '\\"resultExitSeen\\":false' -and $upK -match '\\"allWhole\\":true' -and $upK -match '\\"wideWhole\\":true' -and $upK -match '\\"inside\\":true' -and $upK -match '\\"stuck\\":true' -and $upK -match '\\"lowest\\":true' -and $upK -match '\\"barUnder\\":true' -and $upK -match '\\"seen\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptS = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
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
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const exitRow = rows.find((row) => row.exit);
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen, lowest: !!exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1) };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const barUnder = !!(exit && exit.seen && exit.lowest && barY >= 10);
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder, exit, visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptS=$keptS"
$keptPastS = $false
if ($keptS -match '\\"pastTop\\":(-?\d+)') { $keptPastN = [int]$Matches[1] } else { $keptPastN = -999 }
if ($keptS -match '\\"pastMax\\":(-?\d+)') { $keptPastS = [Math]::Abs($keptPastN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastN - $pastTopK) -le 1 }
$keptGapS = $keptS -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftS = $keptS -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultS = $false
if ($keptS -match '\\"resultTop\\":(-?\d+)') { $keptResultN = [int]$Matches[1] } else { $keptResultN = -999 }
if ($keptS -match '\\"resultMax\\":(-?\d+)') { $keptResultS = [Math]::Abs($keptResultN - ($resultTopK - 20)) -le 1 -and $keptResultN -lt [int]$Matches[1] - 10 }
$keptRowsS = $keptS -match '\\"resultWhole\\":true' -and $keptS -match '\\"resultNow7\\":true' -and $keptS -match '\\"resultExitSeen\\":false' -and $keptS -match '\\"allWhole\\":true' -and $keptS -match '\\"wideWhole\\":true' -and $keptS -match '\\"inside\\":true' -and $keptS -match '\\"stuck\\":true' -and $keptS -match '\\"lowest\\":true' -and $keptS -match '\\"barUnder\\":true' -and $keptS -match '\\"seen\\":true' -and $keptS -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageS = $keptS -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptS -match '\\"overflow\\":\\"hidden\\"' -and $keptS -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($ky -and $kx -and $kd -and $kpt -and $kpl -and $krt -and $krows -and $keptPastS -and $keptGapS -and $keptLeftS -and $keptResultS -and $keptRowsS -and $keptPageS)) { Fail 120 "now-stay-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "back-secret"
$armF = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = box.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armF=$armF"
if ($armF -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 121 "page-cannot-scroll" }
if ($armF -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 122 "page-cannot-slide" }
if ($armF -notmatch '\\"x\\":(\d+)') { Fail 123 "no-xf" }
$xf = [int]$Matches[1]
if ($armF -notmatch '\\"y\\":(\d+)') { Fail 123 "no-yf" }
$yf = [int]$Matches[1]
if ($armF -notmatch '\\"pageY\\":(-?\d+)') { Fail 124 "no-page-yf" }
$pageYf = [int]$Matches[1]
if ($armF -notmatch '\\"pageX\\":(-?\d+)') { Fail 125 "no-page-xf" }
$pageXf = [int]$Matches[1]
if ($armF -notmatch '\\"pastTop\\":(-?\d+)') { Fail 126 "no-past-topf" }
$pastTopF = [int]$Matches[1]
if ($armF -notmatch '\\"pastMax\\":(-?\d+)') { Fail 127 "no-past-maxf" }
$pastMaxF = [int]$Matches[1]
if ([Math]::Abs($pastTopF - $pastMaxF) -gt 1) { Fail 128 "past-not-at-end" }
if ($armF -notmatch '\\"pastLeft\\":(\d+)') { Fail 129 "no-past-leftf" }
$pastLeftF = [int]$Matches[1]
if ($armF -notmatch '\\"resultTop\\":(-?\d+)') { Fail 130 "no-result-topf" }
$resultTopF = [int]$Matches[1]
if ($armF -notmatch '\\"resultMax\\":(-?\d+)' -or [int]$Matches[1] -lt 20) { Fail 131 "result-cannot-scroll" }
$resultMaxF = [int]$Matches[1]
if ($resultMaxF - $resultTopF -lt 10) { Fail 132 "result-already-bottom" }
Send-Wheel "9335" $xf $yf 20 | Out-Null
Start-Sleep -Milliseconds 450
$downF = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
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
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const exitRow = rows.find((row) => row.exit);
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen, lowest: !!exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1) };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const barUnder = !!(exit && exit.seen && exit.lowest && barY >= 10);
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder, exit, visible: visible.map((row) => ({ t: row.t, whole: row.whole })), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })) });
})()
'@
Write-Output "downF=$downF"
$fy = $downF -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYf) -le 1
$fx = $downF -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXf) -le 1
$fd = $downF -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$fpt = $false
if ($downF -match '\\"pastTop\\":(-?\d+)') { $fptN = [int]$Matches[1] } else { $fptN = -999 }
if ($downF -match '\\"pastMax\\":(-?\d+)') { $fpt = [Math]::Abs($fptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($fptN - $pastTopF) -le 1 }
$fpl = $downF -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftF) -le 1
$frt = $false
if ($downF -match '\\"resultTop\\":(-?\d+)') { $frtN = [int]$Matches[1] } else { $frtN = -999 }
if ($downF -match '\\"resultMax\\":(-?\d+)') { $frt = [Math]::Abs($frtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($frtN - ($resultTopF + 20)) -le 1 }
$frows = $downF -match '\\"resultWhole\\":true' -and $downF -match '\\"resultNow8\\":true' -and $downF -match '\\"resultNow7\\":false' -and $downF -match '\\"resultExitSeen\\":true' -and $downF -match '\\"allWhole\\":true' -and $downF -match '\\"wideWhole\\":true' -and $downF -match '\\"inside\\":true' -and $downF -match '\\"stuck\\":true' -and $downF -match '\\"lowest\\":true' -and $downF -match '\\"barUnder\\":true' -and $downF -match '\\"seen\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$landF = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
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
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const exitRow = rows.find((row) => row.exit);
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen, lowest: !!exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1) };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const barUnder = !!(exit && exit.seen && exit.lowest && barY >= 10);
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder, exit, visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "landF=$landF"
$landPast = $false
if ($landF -match '\\"pastTop\\":(-?\d+)') { $landPastN = [int]$Matches[1] } else { $landPastN = -999 }
if ($landF -match '\\"pastMax\\":(-?\d+)') { $landPast = [Math]::Abs($landPastN - [int]$Matches[1]) -le 1 -and [Math]::Abs($landPastN - $pastTopF) -le 1 }
$landGap = $landF -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$landLeft = $landF -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$landResult = $false
if ($landF -match '\\"resultTop\\":(-?\d+)') { $landResultN = [int]$Matches[1] } else { $landResultN = -999 }
if ($landF -match '\\"resultMax\\":(-?\d+)') { $landResult = [Math]::Abs($landResultN - [int]$Matches[1]) -le 1 -and [Math]::Abs($landResultN - ($resultTopF + 20)) -le 1 }
$landRows = $landF -match '\\"resultWhole\\":true' -and $landF -match '\\"resultNow8\\":true' -and $landF -match '\\"resultNow7\\":false' -and $landF -match '\\"resultExitSeen\\":true' -and $landF -match '\\"allWhole\\":true' -and $landF -match '\\"wideWhole\\":true' -and $landF -match '\\"inside\\":true' -and $landF -match '\\"stuck\\":true' -and $landF -match '\\"lowest\\":true' -and $landF -match '\\"barUnder\\":true' -and $landF -match '\\"seen\\":true' -and $landF -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$landPage = $landF -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $landF -match '\\"overflow\\":\\"hidden\\"' -and $landF -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($fy -and $fx -and $fd -and $fpt -and $fpl -and $frt -and $frows -and $landPast -and $landGap -and $landLeft -and $landResult -and $landRows -and $landPage)) { Fail 133 "res-back-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "six-secret"
$armG = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armG=$armG"
if ($armG -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 134 "page-cannot-scroll" }
if ($armG -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 135 "page-cannot-slide" }
if ($armG -notmatch '\\"x\\":(\d+)') { Fail 136 "no-xg" }
$xg = [int]$Matches[1]
if ($armG -notmatch '\\"y\\":(\d+)') { Fail 136 "no-yg" }
$yg = [int]$Matches[1]
if ($armG -notmatch '\\"pageY\\":(-?\d+)') { Fail 137 "no-page-yg" }
$pageYg = [int]$Matches[1]
if ($armG -notmatch '\\"pageX\\":(-?\d+)') { Fail 138 "no-page-xg" }
$pageXg = [int]$Matches[1]
if ($armG -notmatch '\\"pastTop\\":(-?\d+)') { Fail 139 "no-past-topg" }
$pastTopG = [int]$Matches[1]
if ($armG -notmatch '\\"pastMax\\":(-?\d+)') { Fail 140 "no-past-maxg" }
$pastMaxG = [int]$Matches[1]
if ([Math]::Abs($pastTopG - $pastMaxG) -gt 1 -or $pastMaxG -lt 20) { Fail 141 "past-not-at-end" }
if ($armG -notmatch '\\"pastLeft\\":(\d+)') { Fail 142 "no-past-leftg" }
$pastLeftG = [int]$Matches[1]
if ($armG -notmatch '\\"resultTop\\":(-?\d+)') { Fail 143 "no-result-topg" }
$resultTopG = [int]$Matches[1]
if ($armG -notmatch '\\"resultMax\\":(-?\d+)') { Fail 144 "no-result-maxg" }
$resultMaxG = [int]$Matches[1]
if ([Math]::Abs($resultTopG - $resultMaxG) -gt 1) { Fail 145 "result-not-at-end" }
Send-Wheel "9335" $xg $yg -20 | Out-Null
Start-Sleep -Milliseconds 450
$upG = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, old6: el.textContent.indexOf("queqiao-old-6") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
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
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const oldRow = rows.find((row) => row.old6);
  let old6 = null;
  if (oldRow && oldRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(oldRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    old6 = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!oldRow.whole, seen: !!oldRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const wideLowest = !!(wide && wide.seen && visible.every((row) => row.bottom <= wide.bottom + 1));
  const barUnder = !!(wideLowest && barY >= 10);
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), pastExitSeen: rows.some((row) => row.seen && row.exit), wideWhole: !!(wide && wide.whole && wide.seen), wideLowest, end, old6, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder, visible: visible.map((row) => ({ t: row.t, whole: row.whole })), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })) });
})()
'@
Write-Output "upG=$upG"
$gy = $upG -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYg) -le 1
$gx = $upG -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXg) -le 1
$gd = $upG -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$gpt = $false
if ($upG -match '\\"pastTop\\":(-?\d+)') { $gptN = [int]$Matches[1] } else { $gptN = -999 }
if ($upG -match '\\"pastMax\\":(-?\d+)') { $gpt = [Math]::Abs($gptN - ($pastTopG - 20)) -le 1 -and $gptN -lt [int]$Matches[1] - 10 }
$gpl = $upG -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftG) -le 1
$grt = $false
if ($upG -match '\\"resultTop\\":(-?\d+)') { $grtN = [int]$Matches[1] } else { $grtN = -999 }
if ($upG -match '\\"resultMax\\":(-?\d+)') { $grt = [Math]::Abs($grtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($grtN - $resultTopG) -le 1 }
$grows = $upG -match '\\"resultWhole\\":true' -and $upG -match '\\"resultNow8\\":true' -and $upG -match '\\"resultNow7\\":false' -and $upG -match '\\"resultExitSeen\\":true' -and $upG -match '\\"pastExitSeen\\":false' -and $upG -match '\\"allWhole\\":true' -and $upG -match '\\"wideWhole\\":true' -and $upG -match '\\"wideLowest\\":true' -and $upG -match '\\"inside\\":true' -and $upG -match '\\"stuck\\":true' -and $upG -match '\\"seen\\":true' -and $upG -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptG = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, old6: el.textContent.indexOf("queqiao-old-6") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
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
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const oldRow = rows.find((row) => row.old6);
  let old6 = null;
  if (oldRow && oldRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(oldRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    old6 = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!oldRow.whole, seen: !!oldRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const wideLowest = !!(wide && wide.seen && visible.every((row) => row.bottom <= wide.bottom + 1));
  const barUnder = !!(wideLowest && barY >= 10);
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), pastExitSeen: rows.some((row) => row.seen && row.exit), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), wideLowest, end, old6, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder, visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptG=$keptG"
$keptPastG = $false
if ($keptG -match '\\"pastTop\\":(-?\d+)') { $keptPastGN = [int]$Matches[1] } else { $keptPastGN = -999 }
if ($keptG -match '\\"pastMax\\":(-?\d+)') { $keptPastG = [Math]::Abs($keptPastGN - ($pastTopG - 20)) -le 1 -and $keptPastGN -lt [int]$Matches[1] - 10 }
$keptGapG = $keptG -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftG = $keptG -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultG = $false
if ($keptG -match '\\"resultTop\\":(-?\d+)') { $keptResultGN = [int]$Matches[1] } else { $keptResultGN = -999 }
if ($keptG -match '\\"resultMax\\":(-?\d+)') { $keptResultG = [Math]::Abs($keptResultGN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultGN - $resultTopG) -le 1 }
$keptRowsG = $keptG -match '\\"resultWhole\\":true' -and $keptG -match '\\"resultNow8\\":true' -and $keptG -match '\\"resultNow7\\":false' -and $keptG -match '\\"resultExitSeen\\":true' -and $keptG -match '\\"pastExitSeen\\":false' -and $keptG -match '\\"allWhole\\":true' -and $keptG -match '\\"wideWhole\\":true' -and $keptG -match '\\"wideLowest\\":true' -and $keptG -match '\\"inside\\":true' -and $keptG -match '\\"stuck\\":true' -and $keptG -match '\\"seen\\":true' -and $keptG -match '\\"barUnder\\":true' -and $keptG -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageG = $keptG -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptG -match '\\"overflow\\":\\"hidden\\"' -and $keptG -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($gy -and $gx -and $gd -and $gpt -and $gpl -and $grt -and $grows -and $keptPastG -and $keptGapG -and $keptLeftG -and $keptResultG -and $keptRowsG -and $keptPageG)) { Fail 146 "past-six-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "five-secret"
$armH = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armH=$armH"
if ($armH -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 147 "page-cannot-scroll" }
if ($armH -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 148 "page-cannot-slide" }
if ($armH -notmatch '\\"x\\":(\d+)') { Fail 149 "no-xh" }
$xh = [int]$Matches[1]
if ($armH -notmatch '\\"y\\":(\d+)') { Fail 149 "no-yh" }
$yh = [int]$Matches[1]
if ($armH -notmatch '\\"pageY\\":(-?\d+)') { Fail 150 "no-page-yh" }
$pageYh = [int]$Matches[1]
if ($armH -notmatch '\\"pageX\\":(-?\d+)') { Fail 151 "no-page-xh" }
$pageXh = [int]$Matches[1]
if ($armH -notmatch '\\"pastTop\\":(-?\d+)') { Fail 152 "no-past-toph" }
$pastTopH = [int]$Matches[1]
if ($armH -notmatch '\\"pastMax\\":(-?\d+)') { Fail 153 "no-past-maxh" }
$pastMaxH = [int]$Matches[1]
if ($pastTopH -lt 20 -or ($pastMaxH - $pastTopH) -lt 10) { Fail 154 "past-not-one-up" }
if ($armH -notmatch '\\"pastLeft\\":(\d+)') { Fail 155 "no-past-lefth" }
$pastLeftH = [int]$Matches[1]
if ($armH -notmatch '\\"resultTop\\":(-?\d+)') { Fail 156 "no-result-toph" }
$resultTopH = [int]$Matches[1]
if ($armH -notmatch '\\"resultMax\\":(-?\d+)') { Fail 157 "no-result-maxh" }
$resultMaxH = [int]$Matches[1]
if ([Math]::Abs($resultTopH - $resultMaxH) -gt 1) { Fail 158 "result-not-at-end" }
Send-Wheel "9335" $xh $yh -20 | Out-Null
Start-Sleep -Milliseconds 450
$upH = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const measure = (row) => {
    if (!row || !row.el.firstChild) return null;
    const range = document.createRange();
    range.selectNodeContents(row.el.firstChild);
    const rect = range.getBoundingClientRect();
    return { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!row.whole, seen: !!row.seen };
  };
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, old5: el.textContent.indexOf("queqiao-old-5") === 0, old6: el.textContent.indexOf("queqiao-old-6") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
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
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const old5Row = rows.find((row) => row.old5);
  const old6Row = rows.find((row) => row.old6);
  const old5 = measure(old5Row);
  const old6 = measure(old6Row);
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const old6Lowest = !!(old6Row && old6Row.seen && visible.every((row) => row.bottom <= old6Row.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideSeen: !!(wide && wide.seen), wideWhole: !!(wide && wide.whole && wide.seen), end, old5, old6, old5Seen: !!(old5 && old5.seen), old5Stuck: !!(old5 && old5.stuck), old5Whole: !!(old5 && old5.whole), old6Seen: !!(old6 && old6.seen), old6Stuck: !!(old6 && old6.stuck), old6Whole: !!(old6 && old6.whole), old6Lowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(old6Lowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole })), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })) });
})()
'@
Write-Output "upH=$upH"
$hy = $upH -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYh) -le 1
$hx = $upH -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXh) -le 1
$hd = $upH -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$hpt = $false
if ($upH -match '\\"pastTop\\":(-?\d+)') { $hptN = [int]$Matches[1] } else { $hptN = -999 }
if ($upH -match '\\"pastMax\\":(-?\d+)') { $hpt = [Math]::Abs($hptN - ($pastTopH - 20)) -le 1 -and $hptN -lt [int]$Matches[1] - 10 }
$hpl = $upH -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftH) -le 1
$hrt = $false
if ($upH -match '\\"resultTop\\":(-?\d+)') { $hrtN = [int]$Matches[1] } else { $hrtN = -999 }
if ($upH -match '\\"resultMax\\":(-?\d+)') { $hrt = [Math]::Abs($hrtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($hrtN - $resultTopH) -le 1 }
$hrows = $upH -match '\\"resultWhole\\":true' -and $upH -match '\\"resultNow8\\":true' -and $upH -match '\\"resultNow7\\":false' -and $upH -match '\\"resultExitSeen\\":true' -and $upH -match '\\"wideSeen\\":false' -and $upH -match '\\"inside\\":false' -and $upH -match '\\"old5Seen\\":true' -and $upH -match '\\"old5Stuck\\":true' -and $upH -match '\\"old5Whole\\":true' -and $upH -match '\\"old6Seen\\":true' -and $upH -match '\\"old6Stuck\\":true' -and $upH -match '\\"old6Whole\\":true' -and $upH -match '\\"old6Lowest\\":true' -and $upH -match '\\"allWhole\\":true' -and $upH -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptH = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const measure = (row) => {
    if (!row || !row.el.firstChild) return null;
    const range = document.createRange();
    range.selectNodeContents(row.el.firstChild);
    const rect = range.getBoundingClientRect();
    return { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!row.whole, seen: !!row.seen };
  };
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, old5: el.textContent.indexOf("queqiao-old-5") === 0, old6: el.textContent.indexOf("queqiao-old-6") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
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
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const old5Row = rows.find((row) => row.old5);
  const old6Row = rows.find((row) => row.old6);
  const old5 = measure(old5Row);
  const old6 = measure(old6Row);
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const old6Lowest = !!(old6Row && old6Row.seen && visible.every((row) => row.bottom <= old6Row.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideSeen: !!(wide && wide.seen), wideWhole: !!(wide && wide.whole && wide.seen), end, old5, old6, old5Seen: !!(old5 && old5.seen), old5Stuck: !!(old5 && old5.stuck), old5Whole: !!(old5 && old5.whole), old6Seen: !!(old6 && old6.seen), old6Stuck: !!(old6 && old6.stuck), old6Whole: !!(old6 && old6.whole), old6Lowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(old6Lowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptH=$keptH"
$keptPastH = $false
if ($keptH -match '\\"pastTop\\":(-?\d+)') { $keptPastHN = [int]$Matches[1] } else { $keptPastHN = -999 }
if ($keptH -match '\\"pastMax\\":(-?\d+)') { $keptPastH = [Math]::Abs($keptPastHN - ($pastTopH - 20)) -le 1 -and $keptPastHN -lt [int]$Matches[1] - 10 }
$keptGapH = $keptH -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftH = $keptH -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultH = $false
if ($keptH -match '\\"resultTop\\":(-?\d+)') { $keptResultHN = [int]$Matches[1] } else { $keptResultHN = -999 }
if ($keptH -match '\\"resultMax\\":(-?\d+)') { $keptResultH = [Math]::Abs($keptResultHN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultHN - $resultTopH) -le 1 }
$keptRowsH = $keptH -match '\\"resultWhole\\":true' -and $keptH -match '\\"resultNow8\\":true' -and $keptH -match '\\"resultNow7\\":false' -and $keptH -match '\\"resultExitSeen\\":true' -and $keptH -match '\\"wideSeen\\":false' -and $keptH -match '\\"inside\\":false' -and $keptH -match '\\"old5Seen\\":true' -and $keptH -match '\\"old5Stuck\\":true' -and $keptH -match '\\"old5Whole\\":true' -and $keptH -match '\\"old6Seen\\":true' -and $keptH -match '\\"old6Stuck\\":true' -and $keptH -match '\\"old6Whole\\":true' -and $keptH -match '\\"old6Lowest\\":true' -and $keptH -match '\\"allWhole\\":true' -and $keptH -match '\\"barUnder\\":true' -and $keptH -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageH = $keptH -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptH -match '\\"overflow\\":\\"hidden\\"' -and $keptH -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($hy -and $hx -and $hd -and $hpt -and $hpl -and $hrt -and $hrows -and $keptPastH -and $keptGapH -and $keptLeftH -and $keptResultH -and $keptRowsH -and $keptPageH)) { Fail 159 "past-five-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "back-secret"
$armJ = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armJ=$armJ"
if ($armJ -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 160 "page-cannot-scroll" }
if ($armJ -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 161 "page-cannot-slide" }
if ($armJ -notmatch '\\"x\\":(\d+)') { Fail 162 "no-xj" }
$xj = [int]$Matches[1]
if ($armJ -notmatch '\\"y\\":(\d+)') { Fail 162 "no-yj" }
$yj = [int]$Matches[1]
if ($armJ -notmatch '\\"pageY\\":(-?\d+)') { Fail 163 "no-page-yj" }
$pageYj = [int]$Matches[1]
if ($armJ -notmatch '\\"pageX\\":(-?\d+)') { Fail 164 "no-page-xj" }
$pageXj = [int]$Matches[1]
if ($armJ -notmatch '\\"pastTop\\":(-?\d+)') { Fail 165 "no-past-topj" }
$pastTopJ = [int]$Matches[1]
if ($armJ -notmatch '\\"pastMax\\":(-?\d+)') { Fail 166 "no-past-maxj" }
$pastMaxJ = [int]$Matches[1]
if ($pastTopJ -lt 20 -or ($pastMaxJ - $pastTopJ) -lt 20) { Fail 167 "past-not-ready" }
if ($armJ -notmatch '\\"pastLeft\\":(\d+)') { Fail 168 "no-past-leftj" }
$pastLeftJ = [int]$Matches[1]
if ($armJ -notmatch '\\"resultTop\\":(-?\d+)') { Fail 169 "no-result-topj" }
$resultTopJ = [int]$Matches[1]
if ($armJ -notmatch '\\"resultMax\\":(-?\d+)') { Fail 170 "no-result-maxj" }
$resultMaxJ = [int]$Matches[1]
if ([Math]::Abs($resultTopJ - $resultMaxJ) -gt 1) { Fail 171 "result-not-at-end" }
Send-Wheel "9335" $xj $yj 20 | Out-Null
Start-Sleep -Milliseconds 450
$downJ = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const measure = (row) => {
    if (!row || !row.el.firstChild) return null;
    const range = document.createRange();
    range.selectNodeContents(row.el.firstChild);
    const rect = range.getBoundingClientRect();
    return { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!row.whole, seen: !!row.seen };
  };
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, old5: el.textContent.indexOf("queqiao-old-5") === 0, old6: el.textContent.indexOf("queqiao-old-6") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
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
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const old6 = measure(rows.find((row) => row.old6));
  const wideRow = wide;
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const wideLowest = !!(wideRow && wideRow.seen && visible.every((row) => row.bottom <= wideRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideSeen: !!(wide && wide.seen), wideWhole: !!(wide && wide.whole && wide.seen), wideLowest, end, old6, old5Seen: rows.some((row) => row.seen && row.old5), old6Seen: !!(old6 && old6.seen), old6Stuck: !!(old6 && old6.stuck), old6Whole: !!(old6 && old6.whole), allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(wideLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole })), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })) });
})()
'@
Write-Output "downJ=$downJ"
$jy = $downJ -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYj) -le 1
$jx = $downJ -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXj) -le 1
$jd = $downJ -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$jpt = $false
if ($downJ -match '\\"pastTop\\":(-?\d+)') { $jptN = [int]$Matches[1] } else { $jptN = -999 }
if ($downJ -match '\\"pastMax\\":(-?\d+)') { $jpt = [Math]::Abs($jptN - ($pastTopJ + 20)) -le 1 -and $jptN -lt [int]$Matches[1] - 10 }
$jpl = $downJ -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftJ) -le 1
$jrt = $false
if ($downJ -match '\\"resultTop\\":(-?\d+)') { $jrtN = [int]$Matches[1] } else { $jrtN = -999 }
if ($downJ -match '\\"resultMax\\":(-?\d+)') { $jrt = [Math]::Abs($jrtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($jrtN - $resultTopJ) -le 1 }
$jrows = $downJ -match '\\"resultWhole\\":true' -and $downJ -match '\\"resultNow8\\":true' -and $downJ -match '\\"resultNow7\\":false' -and $downJ -match '\\"resultExitSeen\\":true' -and $downJ -match '\\"wideSeen\\":true' -and $downJ -match '\\"wideWhole\\":true' -and $downJ -match '\\"wideLowest\\":true' -and $downJ -match '\\"inside\\":true' -and $downJ -match '\\"old5Seen\\":false' -and $downJ -match '\\"old6Seen\\":true' -and $downJ -match '\\"old6Stuck\\":true' -and $downJ -match '\\"old6Whole\\":true' -and $downJ -match '\\"allWhole\\":true' -and $downJ -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptJ = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const measure = (row) => {
    if (!row || !row.el.firstChild) return null;
    const range = document.createRange();
    range.selectNodeContents(row.el.firstChild);
    const rect = range.getBoundingClientRect();
    return { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!row.whole, seen: !!row.seen };
  };
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, old5: el.textContent.indexOf("queqiao-old-5") === 0, old6: el.textContent.indexOf("queqiao-old-6") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
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
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  const old6 = measure(rows.find((row) => row.old6));
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const wideLowest = !!(wide && wide.seen && visible.every((row) => row.bottom <= wide.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideSeen: !!(wide && wide.seen), wideWhole: !!(wide && wide.whole && wide.seen), wideLowest, end, old6, old5Seen: rows.some((row) => row.seen && row.old5), old6Seen: !!(old6 && old6.seen), old6Stuck: !!(old6 && old6.stuck), old6Whole: !!(old6 && old6.whole), allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(wideLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptJ=$keptJ"
$keptPastJ = $false
if ($keptJ -match '\\"pastTop\\":(-?\d+)') { $keptPastJN = [int]$Matches[1] } else { $keptPastJN = -999 }
if ($keptJ -match '\\"pastMax\\":(-?\d+)') { $keptPastJ = [Math]::Abs($keptPastJN - ($pastTopJ + 20)) -le 1 -and $keptPastJN -lt [int]$Matches[1] - 10 }
$keptGapJ = $keptJ -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftJ = $keptJ -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultJ = $false
if ($keptJ -match '\\"resultTop\\":(-?\d+)') { $keptResultJN = [int]$Matches[1] } else { $keptResultJN = -999 }
if ($keptJ -match '\\"resultMax\\":(-?\d+)') { $keptResultJ = [Math]::Abs($keptResultJN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultJN - $resultTopJ) -le 1 }
$keptRowsJ = $keptJ -match '\\"resultWhole\\":true' -and $keptJ -match '\\"resultNow8\\":true' -and $keptJ -match '\\"resultNow7\\":false' -and $keptJ -match '\\"resultExitSeen\\":true' -and $keptJ -match '\\"wideSeen\\":true' -and $keptJ -match '\\"wideWhole\\":true' -and $keptJ -match '\\"wideLowest\\":true' -and $keptJ -match '\\"inside\\":true' -and $keptJ -match '\\"old5Seen\\":false' -and $keptJ -match '\\"old6Seen\\":true' -and $keptJ -match '\\"old6Stuck\\":true' -and $keptJ -match '\\"old6Whole\\":true' -and $keptJ -match '\\"allWhole\\":true' -and $keptJ -match '\\"barUnder\\":true' -and $keptJ -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageJ = $keptJ -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptJ -match '\\"overflow\\":\\"hidden\\"' -and $keptJ -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($jy -and $jx -and $jd -and $jpt -and $jpl -and $jrt -and $jrows -and $keptPastJ -and $keptGapJ -and $keptLeftJ -and $keptResultJ -and $keptRowsJ -and $keptPageJ)) { Fail 172 "six-back-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "exit-secret"
$armL = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armL=$armL"
if ($armL -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 173 "page-cannot-scroll" }
if ($armL -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 174 "page-cannot-slide" }
if ($armL -notmatch '\\"x\\":(\d+)') { Fail 175 "no-xl" }
$xl = [int]$Matches[1]
if ($armL -notmatch '\\"y\\":(\d+)') { Fail 175 "no-yl" }
$yl = [int]$Matches[1]
if ($armL -notmatch '\\"pageY\\":(-?\d+)') { Fail 176 "no-page-yl" }
$pageYl = [int]$Matches[1]
if ($armL -notmatch '\\"pageX\\":(-?\d+)') { Fail 177 "no-page-xl" }
$pageXl = [int]$Matches[1]
if ($armL -notmatch '\\"pastTop\\":(-?\d+)') { Fail 178 "no-past-topl" }
$pastTopL = [int]$Matches[1]
if ($armL -notmatch '\\"pastMax\\":(-?\d+)') { Fail 179 "no-past-maxl" }
$pastMaxL = [int]$Matches[1]
if (($pastMaxL - $pastTopL) -lt 10) { Fail 180 "past-already-bottom" }
if ($armL -notmatch '\\"pastLeft\\":(\d+)') { Fail 181 "no-past-leftl" }
$pastLeftL = [int]$Matches[1]
if ($armL -notmatch '\\"resultTop\\":(-?\d+)') { Fail 182 "no-result-topl" }
$resultTopL = [int]$Matches[1]
if ($armL -notmatch '\\"resultMax\\":(-?\d+)') { Fail 183 "no-result-maxl" }
$resultMaxL = [int]$Matches[1]
if ([Math]::Abs($resultTopL - $resultMaxL) -gt 1) { Fail 184 "result-not-at-end" }
Send-Wheel "9335" $xl $yl 20 | Out-Null
Start-Sleep -Milliseconds 450
$downL = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, old6: el.textContent.indexOf("queqiao-old-6") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideSeen: !!(wide && wide.seen), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, old6Seen: rows.some((row) => row.seen && row.old6), allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole })), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })) });
})()
'@
Write-Output "downL=$downL"
$ly = $downL -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYl) -le 1
$lx = $downL -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXl) -le 1
$ld = $downL -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$lpt = $false
if ($downL -match '\\"pastTop\\":(-?\d+)') { $lptN = [int]$Matches[1] } else { $lptN = -999 }
if ($downL -match '\\"pastMax\\":(-?\d+)') { $lpt = [Math]::Abs($lptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($lptN - ($pastTopL + 20)) -le 1 }
$lpl = $downL -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftL) -le 1
$lrt = $false
if ($downL -match '\\"resultTop\\":(-?\d+)') { $lrtN = [int]$Matches[1] } else { $lrtN = -999 }
if ($downL -match '\\"resultMax\\":(-?\d+)') { $lrt = [Math]::Abs($lrtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($lrtN - $resultTopL) -le 1 }
$lrows = $downL -match '\\"resultWhole\\":true' -and $downL -match '\\"resultNow8\\":true' -and $downL -match '\\"resultNow7\\":false' -and $downL -match '\\"resultExitSeen\\":true' -and $downL -match '\\"wideSeen\\":true' -and $downL -match '\\"wideWhole\\":true' -and $downL -match '\\"inside\\":true' -and $downL -match '\\"exitSeen\\":true' -and $downL -match '\\"exitStuck\\":true' -and $downL -match '\\"exitWhole\\":true' -and $downL -match '\\"exitLowest\\":true' -and $downL -match '\\"old6Seen\\":false' -and $downL -match '\\"allWhole\\":true' -and $downL -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptL = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, old6: el.textContent.indexOf("queqiao-old-6") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideSeen: !!(wide && wide.seen), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, old6Seen: rows.some((row) => row.seen && row.old6), allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptL=$keptL"
$keptPastL = $false
if ($keptL -match '\\"pastTop\\":(-?\d+)') { $keptPastLN = [int]$Matches[1] } else { $keptPastLN = -999 }
if ($keptL -match '\\"pastMax\\":(-?\d+)') { $keptPastL = [Math]::Abs($keptPastLN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastLN - ($pastTopL + 20)) -le 1 }
$keptGapL = $keptL -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftL = $keptL -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultL = $false
if ($keptL -match '\\"resultTop\\":(-?\d+)') { $keptResultLN = [int]$Matches[1] } else { $keptResultLN = -999 }
if ($keptL -match '\\"resultMax\\":(-?\d+)') { $keptResultL = [Math]::Abs($keptResultLN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultLN - $resultTopL) -le 1 }
$keptRowsL = $keptL -match '\\"resultWhole\\":true' -and $keptL -match '\\"resultNow8\\":true' -and $keptL -match '\\"resultNow7\\":false' -and $keptL -match '\\"resultExitSeen\\":true' -and $keptL -match '\\"wideSeen\\":true' -and $keptL -match '\\"wideWhole\\":true' -and $keptL -match '\\"inside\\":true' -and $keptL -match '\\"exitSeen\\":true' -and $keptL -match '\\"exitStuck\\":true' -and $keptL -match '\\"exitWhole\\":true' -and $keptL -match '\\"exitLowest\\":true' -and $keptL -match '\\"old6Seen\\":false' -and $keptL -match '\\"allWhole\\":true' -and $keptL -match '\\"barUnder\\":true' -and $keptL -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageL = $keptL -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptL -match '\\"overflow\\":\\"hidden\\"' -and $keptL -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($ly -and $lx -and $ld -and $lpt -and $lpl -and $lrt -and $lrows -and $keptPastL -and $keptGapL -and $keptLeftL -and $keptResultL -and $keptRowsL -and $keptPageL)) { Fail 185 "exit-back-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "drop-secret"
$armM = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armM=$armM"
if ($armM -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 186 "page-cannot-scroll" }
if ($armM -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 187 "page-cannot-slide" }
if ($armM -notmatch '\\"x\\":(\d+)') { Fail 188 "no-xm" }
$xm = [int]$Matches[1]
if ($armM -notmatch '\\"y\\":(\d+)') { Fail 188 "no-ym" }
$ym = [int]$Matches[1]
if ($armM -notmatch '\\"pageY\\":(-?\d+)') { Fail 189 "no-page-ym" }
$pageYm = [int]$Matches[1]
if ($armM -notmatch '\\"pageX\\":(-?\d+)') { Fail 190 "no-page-xm" }
$pageXm = [int]$Matches[1]
if ($armM -notmatch '\\"pastTop\\":(-?\d+)') { Fail 191 "no-past-topm" }
$pastTopM = [int]$Matches[1]
if ($armM -notmatch '\\"pastMax\\":(-?\d+)') { Fail 192 "no-past-maxm" }
$pastMaxM = [int]$Matches[1]
if ([Math]::Abs($pastTopM - $pastMaxM) -gt 1) { Fail 193 "past-not-at-end" }
if ($armM -notmatch '\\"pastLeft\\":(\d+)') { Fail 194 "no-past-leftm" }
$pastLeftM = [int]$Matches[1]
if ($armM -notmatch '\\"resultTop\\":(-?\d+)') { Fail 195 "no-result-topm" }
$resultTopM = [int]$Matches[1]
if ($armM -notmatch '\\"resultMax\\":(-?\d+)') { Fail 196 "no-result-maxm" }
$resultMaxM = [int]$Matches[1]
if ([Math]::Abs($resultTopM - $resultMaxM) -gt 1) { Fail 197 "result-not-at-end" }
Send-Wheel "9335" $xm $ym 20 | Out-Null
Start-Sleep -Milliseconds 450
$dropM = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideSeen: !!(wide && wide.seen), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole })), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })) });
})()
'@
Write-Output "dropM=$dropM"
$my = $dropM -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYm) -le 1
$mx = $dropM -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXm) -le 1
$md = $dropM -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$mpt = $false
if ($dropM -match '\\"pastTop\\":(-?\d+)') { $mptN = [int]$Matches[1] } else { $mptN = -999 }
if ($dropM -match '\\"pastMax\\":(-?\d+)') { $mpt = [Math]::Abs($mptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($mptN - $pastTopM) -le 1 }
$mpl = $dropM -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftM) -le 1
$mrt = $false
if ($dropM -match '\\"resultTop\\":(-?\d+)') { $mrtN = [int]$Matches[1] } else { $mrtN = -999 }
if ($dropM -match '\\"resultMax\\":(-?\d+)') { $mrt = [Math]::Abs($mrtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($mrtN - $resultTopM) -le 1 }
$mrows = $dropM -match '\\"resultWhole\\":true' -and $dropM -match '\\"resultNow8\\":true' -and $dropM -match '\\"resultNow7\\":false' -and $dropM -match '\\"resultExitSeen\\":true' -and $dropM -match '\\"wideSeen\\":true' -and $dropM -match '\\"wideWhole\\":true' -and $dropM -match '\\"inside\\":true' -and $dropM -match '\\"exitSeen\\":true' -and $dropM -match '\\"exitStuck\\":true' -and $dropM -match '\\"exitWhole\\":true' -and $dropM -match '\\"exitLowest\\":true' -and $dropM -match '\\"allWhole\\":true' -and $dropM -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptM = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideSeen: !!(wide && wide.seen), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptM=$keptM"
$keptPastM = $false
if ($keptM -match '\\"pastTop\\":(-?\d+)') { $keptPastMN = [int]$Matches[1] } else { $keptPastMN = -999 }
if ($keptM -match '\\"pastMax\\":(-?\d+)') { $keptPastM = [Math]::Abs($keptPastMN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastMN - $pastTopM) -le 1 }
$keptGapM = $keptM -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftM = $keptM -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultM = $false
if ($keptM -match '\\"resultTop\\":(-?\d+)') { $keptResultMN = [int]$Matches[1] } else { $keptResultMN = -999 }
if ($keptM -match '\\"resultMax\\":(-?\d+)') { $keptResultM = [Math]::Abs($keptResultMN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultMN - $resultTopM) -le 1 }
$keptRowsM = $keptM -match '\\"resultWhole\\":true' -and $keptM -match '\\"resultNow8\\":true' -and $keptM -match '\\"resultNow7\\":false' -and $keptM -match '\\"resultExitSeen\\":true' -and $keptM -match '\\"wideSeen\\":true' -and $keptM -match '\\"wideWhole\\":true' -and $keptM -match '\\"inside\\":true' -and $keptM -match '\\"exitSeen\\":true' -and $keptM -match '\\"exitStuck\\":true' -and $keptM -match '\\"exitWhole\\":true' -and $keptM -match '\\"exitLowest\\":true' -and $keptM -match '\\"allWhole\\":true' -and $keptM -match '\\"barUnder\\":true' -and $keptM -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageM = $keptM -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptM -match '\\"overflow\\":\\"hidden\\"' -and $keptM -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($my -and $mx -and $md -and $mpt -and $mpl -and $mrt -and $mrows -and $keptPastM -and $keptGapM -and $keptLeftM -and $keptResultM -and $keptRowsM -and $keptPageM)) { Fail 198 "past-drop-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "res-secret"
$armE = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = box.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armE=$armE"
if ($armE -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 199 "page-cannot-scroll" }
if ($armE -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 200 "page-cannot-slide" }
if ($armE -notmatch '\\"x\\":(\d+)') { Fail 201 "no-xe" }
$xe = [int]$Matches[1]
if ($armE -notmatch '\\"y\\":(\d+)') { Fail 201 "no-ye" }
$ye = [int]$Matches[1]
if ($armE -notmatch '\\"pageY\\":(-?\d+)') { Fail 202 "no-page-ye" }
$pageYe = [int]$Matches[1]
if ($armE -notmatch '\\"pageX\\":(-?\d+)') { Fail 203 "no-page-xe" }
$pageXe = [int]$Matches[1]
if ($armE -notmatch '\\"pastTop\\":(-?\d+)') { Fail 204 "no-past-tope" }
$pastTopE = [int]$Matches[1]
if ($armE -notmatch '\\"pastMax\\":(-?\d+)') { Fail 205 "no-past-maxe" }
$pastMaxE = [int]$Matches[1]
if ([Math]::Abs($pastTopE - $pastMaxE) -gt 1) { Fail 206 "past-not-at-end" }
if ($armE -notmatch '\\"pastLeft\\":(\d+)') { Fail 207 "no-past-lefte" }
$pastLeftE = [int]$Matches[1]
if ($armE -notmatch '\\"resultTop\\":(-?\d+)') { Fail 208 "no-result-tope" }
$resultTopE = [int]$Matches[1]
if ($armE -notmatch '\\"resultMax\\":(-?\d+)') { Fail 209 "no-result-maxe" }
$resultMaxE = [int]$Matches[1]
if ([Math]::Abs($resultTopE - $resultMaxE) -gt 1) { Fail 210 "result-not-at-end" }
Send-Wheel "9335" $xe $ye 20 | Out-Null
Start-Sleep -Milliseconds 450
$dropE = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideSeen: !!(wide && wide.seen), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole })), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })) });
})()
'@
Write-Output "dropE=$dropE"
$ey = $dropE -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYe) -le 1
$ex = $dropE -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXe) -le 1
$ed = $dropE -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$ept = $false
if ($dropE -match '\\"pastTop\\":(-?\d+)') { $eptN = [int]$Matches[1] } else { $eptN = -999 }
if ($dropE -match '\\"pastMax\\":(-?\d+)') { $ept = [Math]::Abs($eptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($eptN - $pastTopE) -le 1 }
$epl = $dropE -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftE) -le 1
$ert = $false
if ($dropE -match '\\"resultTop\\":(-?\d+)') { $ertN = [int]$Matches[1] } else { $ertN = -999 }
if ($dropE -match '\\"resultMax\\":(-?\d+)') { $ert = [Math]::Abs($ertN - [int]$Matches[1]) -le 1 -and [Math]::Abs($ertN - $resultTopE) -le 1 }
$erows = $dropE -match '\\"resultWhole\\":true' -and $dropE -match '\\"resultNow8\\":true' -and $dropE -match '\\"resultNow7\\":false' -and $dropE -match '\\"resultExitSeen\\":true' -and $dropE -match '\\"wideSeen\\":true' -and $dropE -match '\\"wideWhole\\":true' -and $dropE -match '\\"inside\\":true' -and $dropE -match '\\"exitSeen\\":true' -and $dropE -match '\\"exitStuck\\":true' -and $dropE -match '\\"exitWhole\\":true' -and $dropE -match '\\"exitLowest\\":true' -and $dropE -match '\\"allWhole\\":true' -and $dropE -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptE = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideSeen: !!(wide && wide.seen), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptE=$keptE"
$keptPastE = $false
if ($keptE -match '\\"pastTop\\":(-?\d+)') { $keptPastEN = [int]$Matches[1] } else { $keptPastEN = -999 }
if ($keptE -match '\\"pastMax\\":(-?\d+)') { $keptPastE = [Math]::Abs($keptPastEN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastEN - $pastTopE) -le 1 }
$keptGapE = $keptE -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftE = $keptE -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultE = $false
if ($keptE -match '\\"resultTop\\":(-?\d+)') { $keptResultEN = [int]$Matches[1] } else { $keptResultEN = -999 }
if ($keptE -match '\\"resultMax\\":(-?\d+)') { $keptResultE = [Math]::Abs($keptResultEN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultEN - $resultTopE) -le 1 }
$keptRowsE = $keptE -match '\\"resultWhole\\":true' -and $keptE -match '\\"resultNow8\\":true' -and $keptE -match '\\"resultNow7\\":false' -and $keptE -match '\\"resultExitSeen\\":true' -and $keptE -match '\\"wideSeen\\":true' -and $keptE -match '\\"wideWhole\\":true' -and $keptE -match '\\"inside\\":true' -and $keptE -match '\\"exitSeen\\":true' -and $keptE -match '\\"exitStuck\\":true' -and $keptE -match '\\"exitWhole\\":true' -and $keptE -match '\\"exitLowest\\":true' -and $keptE -match '\\"allWhole\\":true' -and $keptE -match '\\"barUnder\\":true' -and $keptE -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageE = $keptE -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptE -match '\\"overflow\\":\\"hidden\\"' -and $keptE -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($ey -and $ex -and $ed -and $ept -and $epl -and $ert -and $erows -and $keptPastE -and $keptGapE -and $keptLeftE -and $keptResultE -and $keptRowsE -and $keptPageE)) { Fail 211 "res-drop-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "more-secret"
$armT = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, formula, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armT=$armT"
if ($armT -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 212 "page-cannot-scroll" }
if ($armT -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 213 "page-cannot-slide" }
if ($armT -notmatch '\\"x\\":(\d+)') { Fail 214 "no-xt" }
$xt = [int]$Matches[1]
if ($armT -notmatch '\\"y\\":(\d+)') { Fail 214 "no-yt" }
$yt = [int]$Matches[1]
if ($armT -notmatch '\\"pageY\\":(-?\d+)') { Fail 215 "no-page-yt" }
$pageYt = [int]$Matches[1]
if ($armT -notmatch '\\"pageX\\":(-?\d+)') { Fail 216 "no-page-xt" }
$pageXt = [int]$Matches[1]
if ($armT -notmatch '\\"pastTop\\":(-?\d+)') { Fail 217 "no-past-topt" }
$pastTopT = [int]$Matches[1]
if ($armT -notmatch '\\"pastMax\\":(-?\d+)') { Fail 218 "no-past-maxt" }
$pastMaxT = [int]$Matches[1]
if ([Math]::Abs($pastTopT - $pastMaxT) -gt 1) { Fail 219 "past-not-at-end" }
if ($armT -notmatch '\\"pastLeft\\":(\d+)') { Fail 220 "no-past-leftt" }
$pastLeftT = [int]$Matches[1]
if ($armT -notmatch '\\"formula\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 221 "past-cannot-slide" }
if ($armT -notmatch '\\"resultTop\\":(-?\d+)') { Fail 222 "no-result-topt" }
$resultTopT = [int]$Matches[1]
if ($armT -notmatch '\\"resultMax\\":(-?\d+)') { Fail 223 "no-result-maxt" }
$resultMaxT = [int]$Matches[1]
if ([Math]::Abs($resultTopT - $resultMaxT) -gt 1) { Fail 224 "result-not-at-end" }
Send-WheelX "9335" $xt $yt 400 | Out-Null
Start-Sleep -Milliseconds 450
$slideT = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideSeen: !!(wide && wide.seen), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "slideT=$slideT"
$ty = $slideT -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYt) -le 1
$tx = $slideT -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXt) -le 1
$td = $slideT -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$tpt = $false
if ($slideT -match '\\"pastTop\\":(-?\d+)') { $tptN = [int]$Matches[1] } else { $tptN = -999 }
if ($slideT -match '\\"pastMax\\":(-?\d+)') { $tpt = [Math]::Abs($tptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($tptN - $pastTopT) -le 1 }
$tgap = $slideT -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$trt = $false
if ($slideT -match '\\"resultTop\\":(-?\d+)') { $trtN = [int]$Matches[1] } else { $trtN = -999 }
if ($slideT -match '\\"resultMax\\":(-?\d+)') { $trt = [Math]::Abs($trtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($trtN - $resultTopT) -le 1 }
$trows = $slideT -match '\\"resultWhole\\":true' -and $slideT -match '\\"resultNow8\\":true' -and $slideT -match '\\"resultNow7\\":false' -and $slideT -match '\\"resultExitSeen\\":true' -and $slideT -match '\\"wideSeen\\":true' -and $slideT -match '\\"wideWhole\\":true' -and $slideT -match '\\"inside\\":true' -and $slideT -match '\\"exitSeen\\":true' -and $slideT -match '\\"exitStuck\\":true' -and $slideT -match '\\"exitWhole\\":true' -and $slideT -match '\\"exitLowest\\":true' -and $slideT -match '\\"allWhole\\":true' -and $slideT -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptT = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideSeen: !!(wide && wide.seen), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptT=$keptT"
$keptPastT = $false
if ($keptT -match '\\"pastTop\\":(-?\d+)') { $keptPastTN = [int]$Matches[1] } else { $keptPastTN = -999 }
if ($keptT -match '\\"pastMax\\":(-?\d+)') { $keptPastT = [Math]::Abs($keptPastTN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastTN - $pastTopT) -le 1 }
$keptGapT = $keptT -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftT = $keptT -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultT = $false
if ($keptT -match '\\"resultTop\\":(-?\d+)') { $keptResultTN = [int]$Matches[1] } else { $keptResultTN = -999 }
if ($keptT -match '\\"resultMax\\":(-?\d+)') { $keptResultT = [Math]::Abs($keptResultTN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultTN - $resultTopT) -le 1 }
$keptRowsT = $keptT -match '\\"resultWhole\\":true' -and $keptT -match '\\"resultNow8\\":true' -and $keptT -match '\\"resultNow7\\":false' -and $keptT -match '\\"resultExitSeen\\":true' -and $keptT -match '\\"wideSeen\\":true' -and $keptT -match '\\"wideWhole\\":true' -and $keptT -match '\\"inside\\":true' -and $keptT -match '\\"exitSeen\\":true' -and $keptT -match '\\"exitStuck\\":true' -and $keptT -match '\\"exitWhole\\":true' -and $keptT -match '\\"exitLowest\\":true' -and $keptT -match '\\"allWhole\\":true' -and $keptT -match '\\"barUnder\\":true' -and $keptT -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageT = $keptT -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptT -match '\\"overflow\\":\\"hidden\\"' -and $keptT -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($ty -and $tx -and $td -and $tpt -and $tgap -and $trt -and $trows -and $keptPastT -and $keptGapT -and $keptLeftT -and $keptResultT -and $keptRowsT -and $keptPageT)) { Fail 225 "tail-more-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "left-secret"
$armV = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, formula, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armV=$armV"
if ($armV -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 226 "page-cannot-scroll" }
if ($armV -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 227 "page-cannot-slide" }
if ($armV -notmatch '\\"x\\":(\d+)') { Fail 228 "no-xv" }
$xv = [int]$Matches[1]
if ($armV -notmatch '\\"y\\":(\d+)') { Fail 228 "no-yv" }
$yv = [int]$Matches[1]
if ($armV -notmatch '\\"pageY\\":(-?\d+)') { Fail 229 "no-page-yv" }
$pageYv = [int]$Matches[1]
if ($armV -notmatch '\\"pageX\\":(-?\d+)') { Fail 230 "no-page-xv" }
$pageXv = [int]$Matches[1]
if ($armV -notmatch '\\"pastTop\\":(-?\d+)') { Fail 231 "no-past-topv" }
$pastTopV = [int]$Matches[1]
if ($armV -notmatch '\\"pastMax\\":(-?\d+)') { Fail 232 "no-past-maxv" }
$pastMaxV = [int]$Matches[1]
if ([Math]::Abs($pastTopV - $pastMaxV) -gt 1) { Fail 233 "past-not-at-end" }
if ($armV -notmatch '\\"pastLeft\\":(\d+)') { Fail 234 "no-past-leftv" }
$pastLeftV = [int]$Matches[1]
if ($armV -notmatch '\\"formula\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 235 "past-cannot-slide" }
if ($armV -notmatch '\\"resultTop\\":(-?\d+)') { Fail 236 "no-result-topv" }
$resultTopV = [int]$Matches[1]
if ($armV -notmatch '\\"resultMax\\":(-?\d+)') { Fail 237 "no-result-maxv" }
$resultMaxV = [int]$Matches[1]
if ([Math]::Abs($resultTopV - $resultMaxV) -gt 1) { Fail 238 "result-not-at-end" }
Send-WheelX "9335" $xv $yv (-200) | Out-Null
Start-Sleep -Milliseconds 450
$slideV = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "slideV=$slideV"
$vy = $slideV -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYv) -le 1
$vx = $slideV -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXv) -le 1
$vd = $slideV -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$vpt = $false
if ($slideV -match '\\"pastTop\\":(-?\d+)') { $vptN = [int]$Matches[1] } else { $vptN = -999 }
if ($slideV -match '\\"pastMax\\":(-?\d+)') { $vpt = [Math]::Abs($vptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($vptN - $pastTopV) -le 1 }
$vleft = $false
if ($slideV -match '\\"pastLeft\\":(\d+)') { $vleftN = [int]$Matches[1] } else { $vleftN = 99999 }
$vgap = $false
if ($slideV -match '\\"gap\\":(-?\d+)') { $vgap = [int]$Matches[1] -ge 40 -and $vleftN -le ($pastLeftV - 40) }
$vrt = $false
if ($slideV -match '\\"resultTop\\":(-?\d+)') { $vrtN = [int]$Matches[1] } else { $vrtN = -999 }
if ($slideV -match '\\"resultMax\\":(-?\d+)') { $vrt = [Math]::Abs($vrtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($vrtN - $resultTopV) -le 1 }
$vrows = $slideV -match '\\"resultWhole\\":true' -and $slideV -match '\\"resultNow8\\":true' -and $slideV -match '\\"resultNow7\\":false' -and $slideV -match '\\"resultExitSeen\\":true' -and $slideV -match '\\"wideWhole\\":true' -and $slideV -match '\\"inside\\":false' -and $slideV -match '\\"exitSeen\\":true' -and $slideV -match '\\"exitStuck\\":true' -and $slideV -match '\\"exitWhole\\":true' -and $slideV -match '\\"exitLowest\\":true' -and $slideV -match '\\"allWhole\\":true' -and $slideV -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptV = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptV=$keptV"
$keptPastV = $false
if ($keptV -match '\\"pastTop\\":(-?\d+)') { $keptPastVN = [int]$Matches[1] } else { $keptPastVN = -999 }
if ($keptV -match '\\"pastMax\\":(-?\d+)') { $keptPastV = [Math]::Abs($keptPastVN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastVN - $pastTopV) -le 1 }
$keptLeftV = $false
if ($keptV -match '\\"pastLeft\\":(\d+)') { $keptLeftVN = [int]$Matches[1] } else { $keptLeftVN = 99999 }
$keptGapV = $false
if ($keptV -match '\\"gap\\":(-?\d+)') { $keptGapV = [int]$Matches[1] -ge 40 -and $keptLeftVN -le ($endLeft - 40) }
$keptResultV = $false
if ($keptV -match '\\"resultTop\\":(-?\d+)') { $keptResultVN = [int]$Matches[1] } else { $keptResultVN = -999 }
if ($keptV -match '\\"resultMax\\":(-?\d+)') { $keptResultV = [Math]::Abs($keptResultVN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultVN - $resultTopV) -le 1 }
$keptRowsV = $keptV -match '\\"resultWhole\\":true' -and $keptV -match '\\"resultNow8\\":true' -and $keptV -match '\\"resultNow7\\":false' -and $keptV -match '\\"resultExitSeen\\":true' -and $keptV -match '\\"wideWhole\\":true' -and $keptV -match '\\"inside\\":false' -and $keptV -match '\\"exitSeen\\":true' -and $keptV -match '\\"exitStuck\\":true' -and $keptV -match '\\"exitWhole\\":true' -and $keptV -match '\\"exitLowest\\":true' -and $keptV -match '\\"allWhole\\":true' -and $keptV -match '\\"barUnder\\":true' -and $keptV -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageV = $keptV -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptV -match '\\"overflow\\":\\"hidden\\"' -and $keptV -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($vy -and $vx -and $vd -and $vpt -and $vgap -and $vrt -and $vrows -and $keptPastV -and $keptGapV -and $keptResultV -and $keptRowsV -and $keptPageV)) { Fail 239 "tail-left-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "back-secret"
$armW = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, formula, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armW=$armW"
if ($armW -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 240 "page-cannot-scroll" }
if ($armW -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 241 "page-cannot-slide" }
if ($armW -notmatch '\\"x\\":(\d+)') { Fail 242 "no-xw" }
$xw = [int]$Matches[1]
if ($armW -notmatch '\\"y\\":(\d+)') { Fail 242 "no-yw" }
$yw = [int]$Matches[1]
if ($armW -notmatch '\\"pageY\\":(-?\d+)') { Fail 243 "no-page-yw" }
$pageYw = [int]$Matches[1]
if ($armW -notmatch '\\"pageX\\":(-?\d+)') { Fail 244 "no-page-xw" }
$pageXw = [int]$Matches[1]
if ($armW -notmatch '\\"pastTop\\":(-?\d+)') { Fail 245 "no-past-topw" }
$pastTopW = [int]$Matches[1]
if ($armW -notmatch '\\"pastMax\\":(-?\d+)') { Fail 246 "no-past-maxw" }
$pastMaxW = [int]$Matches[1]
if ([Math]::Abs($pastTopW - $pastMaxW) -gt 1) { Fail 247 "past-not-at-end" }
if ($armW -notmatch '\\"pastLeft\\":(\d+)') { Fail 248 "no-past-leftw" }
$pastLeftW = [int]$Matches[1]
if ($armW -notmatch '\\"formula\\":(\d+)') { Fail 249 "no-formulaw" }
$formulaW = [int]$Matches[1]
if (($formulaW - $pastLeftW) -lt 40) { Fail 250 "already-at-tail" }
if ($armW -notmatch '\\"resultTop\\":(-?\d+)') { Fail 251 "no-result-topw" }
$resultTopW = [int]$Matches[1]
if ($armW -notmatch '\\"resultMax\\":(-?\d+)') { Fail 252 "no-result-maxw" }
$resultMaxW = [int]$Matches[1]
if ([Math]::Abs($resultTopW - $resultMaxW) -gt 1) { Fail 253 "result-not-at-end" }
Send-WheelX "9335" $xw $yw 400 | Out-Null
Start-Sleep -Milliseconds 450
$slideW = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "slideW=$slideW"
$wy = $slideW -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYw) -le 1
$wx = $slideW -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXw) -le 1
$wd = $slideW -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$wpt = $false
if ($slideW -match '\\"pastTop\\":(-?\d+)') { $wptN = [int]$Matches[1] } else { $wptN = -999 }
if ($slideW -match '\\"pastMax\\":(-?\d+)') { $wpt = [Math]::Abs($wptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($wptN - $pastTopW) -le 1 }
$wgap = $slideW -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$wrt = $false
if ($slideW -match '\\"resultTop\\":(-?\d+)') { $wrtN = [int]$Matches[1] } else { $wrtN = -999 }
if ($slideW -match '\\"resultMax\\":(-?\d+)') { $wrt = [Math]::Abs($wrtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($wrtN - $resultTopW) -le 1 }
$wrows = $slideW -match '\\"resultWhole\\":true' -and $slideW -match '\\"resultNow8\\":true' -and $slideW -match '\\"resultNow7\\":false' -and $slideW -match '\\"resultExitSeen\\":true' -and $slideW -match '\\"wideWhole\\":true' -and $slideW -match '\\"inside\\":true' -and $slideW -match '\\"exitSeen\\":true' -and $slideW -match '\\"exitStuck\\":true' -and $slideW -match '\\"exitWhole\\":true' -and $slideW -match '\\"exitLowest\\":true' -and $slideW -match '\\"allWhole\\":true' -and $slideW -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptW = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptW=$keptW"
$keptPastW = $false
if ($keptW -match '\\"pastTop\\":(-?\d+)') { $keptPastWN = [int]$Matches[1] } else { $keptPastWN = -999 }
if ($keptW -match '\\"pastMax\\":(-?\d+)') { $keptPastW = [Math]::Abs($keptPastWN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastWN - $pastTopW) -le 1 }
$keptGapW = $keptW -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftW = $keptW -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultW = $false
if ($keptW -match '\\"resultTop\\":(-?\d+)') { $keptResultWN = [int]$Matches[1] } else { $keptResultWN = -999 }
if ($keptW -match '\\"resultMax\\":(-?\d+)') { $keptResultW = [Math]::Abs($keptResultWN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultWN - $resultTopW) -le 1 }
$keptRowsW = $keptW -match '\\"resultWhole\\":true' -and $keptW -match '\\"resultNow8\\":true' -and $keptW -match '\\"resultNow7\\":false' -and $keptW -match '\\"resultExitSeen\\":true' -and $keptW -match '\\"wideWhole\\":true' -and $keptW -match '\\"inside\\":true' -and $keptW -match '\\"exitSeen\\":true' -and $keptW -match '\\"exitStuck\\":true' -and $keptW -match '\\"exitWhole\\":true' -and $keptW -match '\\"exitLowest\\":true' -and $keptW -match '\\"allWhole\\":true' -and $keptW -match '\\"barUnder\\":true' -and $keptW -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageW = $keptW -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptW -match '\\"overflow\\":\\"hidden\\"' -and $keptW -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($wy -and $wx -and $wd -and $wpt -and $wgap -and $wrt -and $wrows -and $keptPastW -and $keptGapW -and $keptLeftW -and $keptResultW -and $keptRowsW -and $keptPageW)) { Fail 254 "tail-back-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "up-secret"
$armX = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armX=$armX"
if ($armX -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 255 "page-cannot-scroll" }
if ($armX -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 256 "page-cannot-slide" }
if ($armX -notmatch '\\"x\\":(\d+)') { Fail 257 "no-xx" }
$xx = [int]$Matches[1]
if ($armX -notmatch '\\"y\\":(\d+)') { Fail 257 "no-yx" }
$yx = [int]$Matches[1]
if ($armX -notmatch '\\"pageY\\":(-?\d+)') { Fail 258 "no-page-yx" }
$pageYx = [int]$Matches[1]
if ($armX -notmatch '\\"pageX\\":(-?\d+)') { Fail 259 "no-page-xx" }
$pageXx = [int]$Matches[1]
if ($armX -notmatch '\\"pastTop\\":(-?\d+)') { Fail 260 "no-past-topx" }
$pastTopX = [int]$Matches[1]
if ($armX -notmatch '\\"pastMax\\":(-?\d+)') { Fail 261 "no-past-maxx" }
$pastMaxX = [int]$Matches[1]
if ([Math]::Abs($pastTopX - $pastMaxX) -gt 1 -or $pastMaxX -lt 20) { Fail 262 "past-not-at-end" }
if ($armX -notmatch '\\"pastLeft\\":(\d+)') { Fail 263 "no-past-leftx" }
$pastLeftX = [int]$Matches[1]
if ($armX -notmatch '\\"resultTop\\":(-?\d+)') { Fail 264 "no-result-topx" }
$resultTopX = [int]$Matches[1]
if ($armX -notmatch '\\"resultMax\\":(-?\d+)') { Fail 265 "no-result-maxx" }
$resultMaxX = [int]$Matches[1]
if ([Math]::Abs($resultTopX - $resultMaxX) -gt 1) { Fail 266 "result-not-at-end" }
Send-Wheel "9335" $xx $yx -20 | Out-Null
Start-Sleep -Milliseconds 450
$upX = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, old6: el.textContent.indexOf("queqiao-old-6") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const old6Row = rows.find((row) => row.old6);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let old6 = null;
  if (old6Row && old6Row.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(old6Row.el.firstChild);
    const rect = range.getBoundingClientRect();
    old6 = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!old6Row.whole, seen: !!old6Row.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const wideLowest = !!(wide && wide.seen && visible.every((row) => row.bottom <= wide.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), pastExitSeen: rows.some((row) => row.seen && row.exit), wideWhole: !!(wide && wide.whole && wide.seen), wideLowest, end, old6, old6Seen: !!(old6 && old6.seen), old6Stuck: !!(old6 && old6.stuck), old6Whole: !!(old6 && old6.whole), allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(wideLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "upX=$upX"
$xy = $upX -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYx) -le 1
$xxOk = $upX -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXx) -le 1
$xd = $upX -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$xpt = $false
if ($upX -match '\\"pastTop\\":(-?\d+)') { $xptN = [int]$Matches[1] } else { $xptN = -999 }
if ($upX -match '\\"pastMax\\":(-?\d+)') { $xpt = [Math]::Abs($xptN - ($pastTopX - 20)) -le 1 -and $xptN -lt [int]$Matches[1] - 10 }
$xpl = $upX -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftX) -le 1
$xrt = $false
if ($upX -match '\\"resultTop\\":(-?\d+)') { $xrtN = [int]$Matches[1] } else { $xrtN = -999 }
if ($upX -match '\\"resultMax\\":(-?\d+)') { $xrt = [Math]::Abs($xrtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($xrtN - $resultTopX) -le 1 }
$xrows = $upX -match '\\"resultWhole\\":true' -and $upX -match '\\"resultNow8\\":true' -and $upX -match '\\"resultNow7\\":false' -and $upX -match '\\"resultExitSeen\\":true' -and $upX -match '\\"pastExitSeen\\":false' -and $upX -match '\\"wideWhole\\":true' -and $upX -match '\\"wideLowest\\":true' -and $upX -match '\\"inside\\":true' -and $upX -match '\\"old6Seen\\":true' -and $upX -match '\\"old6Stuck\\":true' -and $upX -match '\\"old6Whole\\":true' -and $upX -match '\\"allWhole\\":true' -and $upX -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptX = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, old6: el.textContent.indexOf("queqiao-old-6") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const old6Row = rows.find((row) => row.old6);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let old6 = null;
  if (old6Row && old6Row.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(old6Row.el.firstChild);
    const rect = range.getBoundingClientRect();
    old6 = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!old6Row.whole, seen: !!old6Row.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const wideLowest = !!(wide && wide.seen && visible.every((row) => row.bottom <= wide.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), pastExitSeen: rows.some((row) => row.seen && row.exit), wideWhole: !!(wide && wide.whole && wide.seen), wideLowest, end, old6, old6Seen: !!(old6 && old6.seen), old6Stuck: !!(old6 && old6.stuck), old6Whole: !!(old6 && old6.whole), allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(wideLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptX=$keptX"
$keptPastX = $false
if ($keptX -match '\\"pastTop\\":(-?\d+)') { $keptPastXN = [int]$Matches[1] } else { $keptPastXN = -999 }
if ($keptX -match '\\"pastMax\\":(-?\d+)') { $keptPastX = [Math]::Abs($keptPastXN - ($pastTopX - 20)) -le 1 -and $keptPastXN -lt [int]$Matches[1] - 10 }
$keptGapX = $keptX -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftX = $keptX -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultX = $false
if ($keptX -match '\\"resultTop\\":(-?\d+)') { $keptResultXN = [int]$Matches[1] } else { $keptResultXN = -999 }
if ($keptX -match '\\"resultMax\\":(-?\d+)') { $keptResultX = [Math]::Abs($keptResultXN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultXN - $resultTopX) -le 1 }
$keptRowsX = $keptX -match '\\"resultWhole\\":true' -and $keptX -match '\\"resultNow8\\":true' -and $keptX -match '\\"resultNow7\\":false' -and $keptX -match '\\"resultExitSeen\\":true' -and $keptX -match '\\"pastExitSeen\\":false' -and $keptX -match '\\"wideWhole\\":true' -and $keptX -match '\\"wideLowest\\":true' -and $keptX -match '\\"inside\\":true' -and $keptX -match '\\"old6Seen\\":true' -and $keptX -match '\\"old6Stuck\\":true' -and $keptX -match '\\"old6Whole\\":true' -and $keptX -match '\\"allWhole\\":true' -and $keptX -match '\\"barUnder\\":true' -and $keptX -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageX = $keptX -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptX -match '\\"overflow\\":\\"hidden\\"' -and $keptX -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($xy -and $xxOk -and $xd -and $xpt -and $xpl -and $xrt -and $xrows -and $keptPastX -and $keptGapX -and $keptLeftX -and $keptResultX -and $keptRowsX -and $keptPageX)) { Fail 267 "tail-up-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "down-secret"
$armZ2 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armZ2=$armZ2"
if ($armZ2 -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 268 "page-cannot-scroll" }
if ($armZ2 -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 269 "page-cannot-slide" }
if ($armZ2 -notmatch '\\"x\\":(\d+)') { Fail 270 "no-xz2" }
$xz2 = [int]$Matches[1]
if ($armZ2 -notmatch '\\"y\\":(\d+)') { Fail 270 "no-yz2" }
$yz2 = [int]$Matches[1]
if ($armZ2 -notmatch '\\"pageY\\":(-?\d+)') { Fail 271 "no-page-yz2" }
$pageYz2 = [int]$Matches[1]
if ($armZ2 -notmatch '\\"pageX\\":(-?\d+)') { Fail 272 "no-page-xz2" }
$pageXz2 = [int]$Matches[1]
if ($armZ2 -notmatch '\\"pastTop\\":(-?\d+)') { Fail 273 "no-past-topz2" }
$pastTopZ2 = [int]$Matches[1]
if ($armZ2 -notmatch '\\"pastMax\\":(-?\d+)') { Fail 274 "no-past-maxz2" }
$pastMaxZ2 = [int]$Matches[1]
if (($pastMaxZ2 - $pastTopZ2) -lt 10) { Fail 275 "past-already-bottom" }
if ($armZ2 -notmatch '\\"pastLeft\\":(\d+)') { Fail 276 "no-past-leftz2" }
$pastLeftZ2 = [int]$Matches[1]
if ($armZ2 -notmatch '\\"resultTop\\":(-?\d+)') { Fail 277 "no-result-topz2" }
$resultTopZ2 = [int]$Matches[1]
if ($armZ2 -notmatch '\\"resultMax\\":(-?\d+)') { Fail 278 "no-result-maxz2" }
$resultMaxZ2 = [int]$Matches[1]
if ([Math]::Abs($resultTopZ2 - $resultMaxZ2) -gt 1) { Fail 279 "result-not-at-end" }
Send-Wheel "9335" $xz2 $yz2 20 | Out-Null
Start-Sleep -Milliseconds 450
$downZ2 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, old6: el.textContent.indexOf("queqiao-old-6") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, old6Seen: rows.some((row) => row.seen && row.old6), allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "downZ2=$downZ2"
$z2y = $downZ2 -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYz2) -le 1
$z2x = $downZ2 -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXz2) -le 1
$z2d = $downZ2 -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$z2pt = $false
if ($downZ2 -match '\\"pastTop\\":(-?\d+)') { $z2ptN = [int]$Matches[1] } else { $z2ptN = -999 }
if ($downZ2 -match '\\"pastMax\\":(-?\d+)') { $z2pt = [Math]::Abs($z2ptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($z2ptN - ($pastTopZ2 + 20)) -le 1 }
$z2pl = $downZ2 -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftZ2) -le 1
$z2rt = $false
if ($downZ2 -match '\\"resultTop\\":(-?\d+)') { $z2rtN = [int]$Matches[1] } else { $z2rtN = -999 }
if ($downZ2 -match '\\"resultMax\\":(-?\d+)') { $z2rt = [Math]::Abs($z2rtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($z2rtN - $resultTopZ2) -le 1 }
$z2rows = $downZ2 -match '\\"resultWhole\\":true' -and $downZ2 -match '\\"resultNow8\\":true' -and $downZ2 -match '\\"resultNow7\\":false' -and $downZ2 -match '\\"resultExitSeen\\":true' -and $downZ2 -match '\\"wideWhole\\":true' -and $downZ2 -match '\\"inside\\":true' -and $downZ2 -match '\\"exitSeen\\":true' -and $downZ2 -match '\\"exitStuck\\":true' -and $downZ2 -match '\\"exitWhole\\":true' -and $downZ2 -match '\\"exitLowest\\":true' -and $downZ2 -match '\\"old6Seen\\":false' -and $downZ2 -match '\\"allWhole\\":true' -and $downZ2 -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptZ2 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, old6: el.textContent.indexOf("queqiao-old-6") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, old6Seen: rows.some((row) => row.seen && row.old6), allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptZ2=$keptZ2"
$keptPastZ2 = $false
if ($keptZ2 -match '\\"pastTop\\":(-?\d+)') { $keptPastZ2N = [int]$Matches[1] } else { $keptPastZ2N = -999 }
if ($keptZ2 -match '\\"pastMax\\":(-?\d+)') { $keptPastZ2 = [Math]::Abs($keptPastZ2N - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastZ2N - ($pastTopZ2 + 20)) -le 1 }
$keptGapZ2 = $keptZ2 -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftZ2 = $keptZ2 -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultZ2 = $false
if ($keptZ2 -match '\\"resultTop\\":(-?\d+)') { $keptResultZ2N = [int]$Matches[1] } else { $keptResultZ2N = -999 }
if ($keptZ2 -match '\\"resultMax\\":(-?\d+)') { $keptResultZ2 = [Math]::Abs($keptResultZ2N - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultZ2N - $resultTopZ2) -le 1 }
$keptRowsZ2 = $keptZ2 -match '\\"resultWhole\\":true' -and $keptZ2 -match '\\"resultNow8\\":true' -and $keptZ2 -match '\\"resultNow7\\":false' -and $keptZ2 -match '\\"resultExitSeen\\":true' -and $keptZ2 -match '\\"wideWhole\\":true' -and $keptZ2 -match '\\"inside\\":true' -and $keptZ2 -match '\\"exitSeen\\":true' -and $keptZ2 -match '\\"exitStuck\\":true' -and $keptZ2 -match '\\"exitWhole\\":true' -and $keptZ2 -match '\\"exitLowest\\":true' -and $keptZ2 -match '\\"old6Seen\\":false' -and $keptZ2 -match '\\"allWhole\\":true' -and $keptZ2 -match '\\"barUnder\\":true' -and $keptZ2 -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageZ2 = $keptZ2 -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptZ2 -match '\\"overflow\\":\\"hidden\\"' -and $keptZ2 -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($z2y -and $z2x -and $z2d -and $z2pt -and $z2pl -and $z2rt -and $z2rows -and $keptPastZ2 -and $keptGapZ2 -and $keptLeftZ2 -and $keptResultZ2 -and $keptRowsZ2 -and $keptPageZ2)) { Fail 280 "tail-down-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "up-secret"
$armC = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = box.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armC=$armC"
if ($armC -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 281 "page-cannot-scroll" }
if ($armC -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 282 "page-cannot-slide" }
if ($armC -notmatch '\\"x\\":(\d+)') { Fail 283 "no-xc" }
$xc = [int]$Matches[1]
if ($armC -notmatch '\\"y\\":(\d+)') { Fail 283 "no-yc" }
$yc = [int]$Matches[1]
if ($armC -notmatch '\\"pageY\\":(-?\d+)') { Fail 284 "no-page-yc" }
$pageYc = [int]$Matches[1]
if ($armC -notmatch '\\"pageX\\":(-?\d+)') { Fail 285 "no-page-xc" }
$pageXc = [int]$Matches[1]
if ($armC -notmatch '\\"pastTop\\":(-?\d+)') { Fail 286 "no-past-topc" }
$pastTopC = [int]$Matches[1]
if ($armC -notmatch '\\"pastMax\\":(-?\d+)') { Fail 287 "no-past-maxc" }
$pastMaxC = [int]$Matches[1]
if ([Math]::Abs($pastTopC - $pastMaxC) -gt 1) { Fail 288 "past-not-at-end" }
if ($armC -notmatch '\\"pastLeft\\":(\d+)') { Fail 289 "no-past-leftc" }
$pastLeftC = [int]$Matches[1]
if ($armC -notmatch '\\"resultTop\\":(-?\d+)') { Fail 290 "no-result-topc" }
$resultTopC = [int]$Matches[1]
if ($armC -notmatch '\\"resultMax\\":(-?\d+)') { Fail 291 "no-result-maxc" }
$resultMaxC = [int]$Matches[1]
if ([Math]::Abs($resultTopC - $resultMaxC) -gt 1 -or $resultMaxC -lt 20) { Fail 292 "result-not-at-end" }
Send-Wheel "9335" $xc $yc -20 | Out-Null
Start-Sleep -Milliseconds 450
$upC = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "upC=$upC"
$cy = $upC -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYc) -le 1
$cx = $upC -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXc) -le 1
$cd = $upC -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$cpt = $false
if ($upC -match '\\"pastTop\\":(-?\d+)') { $cptN = [int]$Matches[1] } else { $cptN = -999 }
if ($upC -match '\\"pastMax\\":(-?\d+)') { $cpt = [Math]::Abs($cptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($cptN - $pastTopC) -le 1 }
$cpl = $upC -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftC) -le 1
$crt = $false
if ($upC -match '\\"resultTop\\":(-?\d+)') { $crtN = [int]$Matches[1] } else { $crtN = -999 }
if ($upC -match '\\"resultMax\\":(-?\d+)') { $crt = [Math]::Abs($crtN - ($resultTopC - 20)) -le 1 -and $crtN -lt [int]$Matches[1] - 10 }
$crows = $upC -match '\\"resultWhole\\":true' -and $upC -match '\\"resultNow8\\":true' -and $upC -match '\\"resultNow7\\":true' -and $upC -match '\\"resultExitSeen\\":false' -and $upC -match '\\"wideWhole\\":true' -and $upC -match '\\"inside\\":true' -and $upC -match '\\"exitSeen\\":true' -and $upC -match '\\"exitStuck\\":true' -and $upC -match '\\"exitWhole\\":true' -and $upC -match '\\"exitLowest\\":true' -and $upC -match '\\"allWhole\\":true' -and $upC -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptC = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptC=$keptC"
$keptPastC = $false
if ($keptC -match '\\"pastTop\\":(-?\d+)') { $keptPastCN = [int]$Matches[1] } else { $keptPastCN = -999 }
if ($keptC -match '\\"pastMax\\":(-?\d+)') { $keptPastC = [Math]::Abs($keptPastCN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastCN - $pastTopC) -le 1 }
$keptGapC = $keptC -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftC = $keptC -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultC = $false
if ($keptC -match '\\"resultTop\\":(-?\d+)') { $keptResultCN = [int]$Matches[1] } else { $keptResultCN = -999 }
if ($keptC -match '\\"resultMax\\":(-?\d+)') { $keptResultC = [Math]::Abs($keptResultCN - ($resultTopC - 20)) -le 1 -and $keptResultCN -lt [int]$Matches[1] - 10 }
$keptRowsC = $keptC -match '\\"resultWhole\\":true' -and $keptC -match '\\"resultNow8\\":true' -and $keptC -match '\\"resultNow7\\":true' -and $keptC -match '\\"resultExitSeen\\":false' -and $keptC -match '\\"wideWhole\\":true' -and $keptC -match '\\"inside\\":true' -and $keptC -match '\\"exitSeen\\":true' -and $keptC -match '\\"exitStuck\\":true' -and $keptC -match '\\"exitWhole\\":true' -and $keptC -match '\\"exitLowest\\":true' -and $keptC -match '\\"allWhole\\":true' -and $keptC -match '\\"barUnder\\":true' -and $keptC -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageC = $keptC -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptC -match '\\"overflow\\":\\"hidden\\"' -and $keptC -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (-not ($cy -and $cx -and $cd -and $cpt -and $cpl -and $crt -and $crows -and $keptPastC -and $keptGapC -and $keptLeftC -and $keptResultC -and $keptRowsC -and $keptPageC)) { Fail 293 "res-up-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "down-secret"
$armD2 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = box.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armD2=$armD2"
if ($armD2 -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 294 "page-cannot-scroll" }
if ($armD2 -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 295 "page-cannot-slide" }
if ($armD2 -notmatch '\\"x\\":(\d+)') { Fail 296 "no-xd2" }
$xd2 = [int]$Matches[1]
if ($armD2 -notmatch '\\"y\\":(\d+)') { Fail 296 "no-yd2" }
$yd2 = [int]$Matches[1]
if ($armD2 -notmatch '\\"pageY\\":(-?\d+)') { Fail 297 "no-page-yd2" }
$pageYd2 = [int]$Matches[1]
if ($armD2 -notmatch '\\"pageX\\":(-?\d+)') { Fail 298 "no-page-xd2" }
$pageXd2 = [int]$Matches[1]
if ($armD2 -notmatch '\\"pastTop\\":(-?\d+)') { Fail 299 "no-past-topd2" }
$pastTopD2 = [int]$Matches[1]
if ($armD2 -notmatch '\\"pastMax\\":(-?\d+)') { Fail 300 "no-past-maxd2" }
$pastMaxD2 = [int]$Matches[1]
if ([Math]::Abs($pastTopD2 - $pastMaxD2) -gt 1) { Fail 301 "past-not-at-end" }
if ($armD2 -notmatch '\\"pastLeft\\":(\d+)') { Fail 302 "no-past-leftd2" }
$pastLeftD2 = [int]$Matches[1]
if ($armD2 -notmatch '\\"resultTop\\":(-?\d+)') { Fail 303 "no-result-topd2" }
$resultTopD2 = [int]$Matches[1]
if ($armD2 -notmatch '\\"resultMax\\":(-?\d+)') { Fail 304 "no-result-maxd2" }
$resultMaxD2 = [int]$Matches[1]
if (($resultMaxD2 - $resultTopD2) -lt 10) { Fail 305 "result-already-bottom" }
Send-Wheel "9335" $xd2 $yd2 20 | Out-Null
Start-Sleep -Milliseconds 450
$downD2 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "downD2=$downD2"
$d2y = $downD2 -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYd2) -le 1
$d2x = $downD2 -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXd2) -le 1
$d2d = $downD2 -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$d2pt = $false
if ($downD2 -match '\\"pastTop\\":(-?\d+)') { $d2ptN = [int]$Matches[1] } else { $d2ptN = -999 }
if ($downD2 -match '\\"pastMax\\":(-?\d+)') { $d2pt = [Math]::Abs($d2ptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($d2ptN - $pastTopD2) -le 1 }
$d2pl = $downD2 -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftD2) -le 1
$d2rt = $false
if ($downD2 -match '\\"resultTop\\":(-?\d+)') { $d2rtN = [int]$Matches[1] } else { $d2rtN = -999 }
if ($downD2 -match '\\"resultMax\\":(-?\d+)') { $d2rt = [Math]::Abs($d2rtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($d2rtN - ($resultTopD2 + 20)) -le 1 }
$d2rows = $downD2 -match '\\"resultWhole\\":true' -and $downD2 -match '\\"resultNow8\\":true' -and $downD2 -match '\\"resultNow7\\":false' -and $downD2 -match '\\"resultExitSeen\\":true' -and $downD2 -match '\\"wideWhole\\":true' -and $downD2 -match '\\"inside\\":true' -and $downD2 -match '\\"exitSeen\\":true' -and $downD2 -match '\\"exitStuck\\":true' -and $downD2 -match '\\"exitWhole\\":true' -and $downD2 -match '\\"exitLowest\\":true' -and $downD2 -match '\\"allWhole\\":true' -and $downD2 -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptD2 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptD2=$keptD2"
$keptPastD2 = $false
if ($keptD2 -match '\\"pastTop\\":(-?\d+)') { $keptPastD2N = [int]$Matches[1] } else { $keptPastD2N = -999 }
if ($keptD2 -match '\\"pastMax\\":(-?\d+)') { $keptPastD2 = [Math]::Abs($keptPastD2N - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastD2N - $pastTopD2) -le 1 }
$keptGapD2 = $keptD2 -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftD2 = $keptD2 -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultD2 = $false
if ($keptD2 -match '\\"resultTop\\":(-?\d+)') { $keptResultD2N = [int]$Matches[1] } else { $keptResultD2N = -999 }
if ($keptD2 -match '\\"resultMax\\":(-?\d+)') { $keptResultD2 = [Math]::Abs($keptResultD2N - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultD2N - ($resultTopD2 + 20)) -le 1 }
$keptRowsD2 = $keptD2 -match '\\"resultWhole\\":true' -and $keptD2 -match '\\"resultNow8\\":true' -and $keptD2 -match '\\"resultNow7\\":false' -and $keptD2 -match '\\"resultExitSeen\\":true' -and $keptD2 -match '\\"wideWhole\\":true' -and $keptD2 -match '\\"inside\\":true' -and $keptD2 -match '\\"exitSeen\\":true' -and $keptD2 -match '\\"exitStuck\\":true' -and $keptD2 -match '\\"exitWhole\\":true' -and $keptD2 -match '\\"exitLowest\\":true' -and $keptD2 -match '\\"allWhole\\":true' -and $keptD2 -match '\\"barUnder\\":true' -and $keptD2 -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageD2 = $keptD2 -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptD2 -match '\\"overflow\\":\\"hidden\\"' -and $keptD2 -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "down-after-secret"
if (-not ($d2y -and $d2x -and $d2d -and $d2pt -and $d2pl -and $d2rt -and $d2rows -and $keptPastD2 -and $keptGapD2 -and $keptLeftD2 -and $keptResultD2 -and $keptRowsD2 -and $keptPageD2)) { Fail 306 "res-down-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "more-secret"
$armF = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = box.getBoundingClientRect();
  return JSON.stringify({ x: Math.round(r.left + 80), y: Math.round(r.top + 8), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armF=$armF"
if ($armF -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 307 "page-cannot-scroll" }
if ($armF -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 308 "page-cannot-slide" }
if ($armF -notmatch '\\"x\\":(\d+)') { Fail 309 "no-xf" }
$xf2 = [int]$Matches[1]
if ($armF -notmatch '\\"y\\":(\d+)') { Fail 309 "no-yf" }
$yf2 = [int]$Matches[1]
if ($armF -notmatch '\\"pageY\\":(-?\d+)') { Fail 310 "no-page-yf" }
$pageYf = [int]$Matches[1]
if ($armF -notmatch '\\"pageX\\":(-?\d+)') { Fail 311 "no-page-xf" }
$pageXf = [int]$Matches[1]
if ($armF -notmatch '\\"pastTop\\":(-?\d+)') { Fail 312 "no-past-topf" }
$pastTopF = [int]$Matches[1]
if ($armF -notmatch '\\"pastMax\\":(-?\d+)') { Fail 313 "no-past-maxf" }
$pastMaxF = [int]$Matches[1]
if ([Math]::Abs($pastTopF - $pastMaxF) -gt 1) { Fail 314 "past-not-at-end" }
if ($armF -notmatch '\\"pastLeft\\":(\d+)') { Fail 315 "no-past-leftf" }
$pastLeftF = [int]$Matches[1]
if ($armF -notmatch '\\"resultTop\\":(-?\d+)') { Fail 316 "no-result-topf" }
$resultTopF = [int]$Matches[1]
if ($armF -notmatch '\\"resultMax\\":(-?\d+)') { Fail 317 "no-result-maxf" }
$resultMaxF = [int]$Matches[1]
if ([Math]::Abs($resultTopF - $resultMaxF) -gt 1) { Fail 318 "result-not-at-end" }
Send-Wheel "9335" $xf2 $yf2 20 | Out-Null
Start-Sleep -Milliseconds 450
$moreF = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "moreF=$moreF"
$fy = $moreF -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYf) -le 1
$fx = $moreF -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXf) -le 1
$fd = $moreF -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$fpt = $false
if ($moreF -match '\\"pastTop\\":(-?\d+)') { $fptN = [int]$Matches[1] } else { $fptN = -999 }
if ($moreF -match '\\"pastMax\\":(-?\d+)') { $fpt = [Math]::Abs($fptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($fptN - $pastTopF) -le 1 }
$fpl = $moreF -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftF) -le 1
$frt = $false
if ($moreF -match '\\"resultTop\\":(-?\d+)') { $frtN = [int]$Matches[1] } else { $frtN = -999 }
if ($moreF -match '\\"resultMax\\":(-?\d+)') { $frt = [Math]::Abs($frtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($frtN - $resultTopF) -le 1 }
$frows = $moreF -match '\\"resultWhole\\":true' -and $moreF -match '\\"resultNow8\\":true' -and $moreF -match '\\"resultNow7\\":false' -and $moreF -match '\\"resultExitSeen\\":true' -and $moreF -match '\\"wideWhole\\":true' -and $moreF -match '\\"inside\\":true' -and $moreF -match '\\"exitSeen\\":true' -and $moreF -match '\\"exitStuck\\":true' -and $moreF -match '\\"exitWhole\\":true' -and $moreF -match '\\"exitLowest\\":true' -and $moreF -match '\\"allWhole\\":true' -and $moreF -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptF = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptF=$keptF"
$keptPastF = $false
if ($keptF -match '\\"pastTop\\":(-?\d+)') { $keptPastFN = [int]$Matches[1] } else { $keptPastFN = -999 }
if ($keptF -match '\\"pastMax\\":(-?\d+)') { $keptPastF = [Math]::Abs($keptPastFN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastFN - $pastTopF) -le 1 }
$keptGapF = $keptF -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftF = $keptF -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultF = $false
if ($keptF -match '\\"resultTop\\":(-?\d+)') { $keptResultFN = [int]$Matches[1] } else { $keptResultFN = -999 }
if ($keptF -match '\\"resultMax\\":(-?\d+)') { $keptResultF = [Math]::Abs($keptResultFN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultFN - $resultTopF) -le 1 }
$keptRowsF = $keptF -match '\\"resultWhole\\":true' -and $keptF -match '\\"resultNow8\\":true' -and $keptF -match '\\"resultNow7\\":false' -and $keptF -match '\\"resultExitSeen\\":true' -and $keptF -match '\\"wideWhole\\":true' -and $keptF -match '\\"inside\\":true' -and $keptF -match '\\"exitSeen\\":true' -and $keptF -match '\\"exitStuck\\":true' -and $keptF -match '\\"exitWhole\\":true' -and $keptF -match '\\"exitLowest\\":true' -and $keptF -match '\\"allWhole\\":true' -and $keptF -match '\\"barUnder\\":true' -and $keptF -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageF = $keptF -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptF -match '\\"overflow\\":\\"hidden\\"' -and $keptF -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "more-after-secret"
if (-not ($fy -and $fx -and $fd -and $fpt -and $fpl -and $frt -and $frows -and $keptPastF -and $keptGapF -and $keptLeftF -and $keptResultF -and $keptRowsF -and $keptPageF)) { Fail 319 "res-more-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "past-secret"
$armG = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  const rb = box.getBoundingClientRect();
  const x = Math.round(r.left + 80);
  const y = Math.round(r.top + 8);
  const hit = document.elementFromPoint(x, y);
  const hitPast = !!(hit && hit.closest && hit.closest("#past"));
  return JSON.stringify({ x, y, hitPast, resultBottom: Math.round(rb.bottom), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armG=$armG"
if ($armG -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 320 "page-cannot-scroll" }
if ($armG -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 321 "page-cannot-slide" }
if ($armG -notmatch '\\"hitPast\\":true') { Fail 322 "wheel-missed-past" }
if ($armG -notmatch '\\"x\\":(\d+)') { Fail 323 "no-xg" }
$xg2 = [int]$Matches[1]
if ($armG -notmatch '\\"y\\":(\d+)') { Fail 323 "no-yg" }
$yg2 = [int]$Matches[1]
if ($armG -notmatch '\\"resultBottom\\":(-?\d+)') { Fail 324 "no-result-bottom" }
if ($yg2 -le [int]$Matches[1]) { Fail 325 "aim-still-on-result" }
if ($armG -notmatch '\\"pageY\\":(-?\d+)') { Fail 326 "no-page-yg" }
$pageYg = [int]$Matches[1]
if ($armG -notmatch '\\"pageX\\":(-?\d+)') { Fail 327 "no-page-xg" }
$pageXg = [int]$Matches[1]
if ($armG -notmatch '\\"pastTop\\":(-?\d+)') { Fail 328 "no-past-topg" }
$pastTopG = [int]$Matches[1]
if ($armG -notmatch '\\"pastMax\\":(-?\d+)') { Fail 329 "no-past-maxg" }
$pastMaxG = [int]$Matches[1]
if ([Math]::Abs($pastTopG - $pastMaxG) -gt 1) { Fail 330 "past-not-at-end" }
if ($armG -notmatch '\\"pastLeft\\":(\d+)') { Fail 331 "no-past-leftg" }
$pastLeftG = [int]$Matches[1]
if ($armG -notmatch '\\"resultTop\\":(-?\d+)') { Fail 332 "no-result-topg" }
$resultTopG = [int]$Matches[1]
if ($armG -notmatch '\\"resultMax\\":(-?\d+)') { Fail 333 "no-result-maxg" }
$resultMaxG = [int]$Matches[1]
if ([Math]::Abs($resultTopG - $resultMaxG) -gt 1) { Fail 334 "result-not-at-end" }
Send-Wheel "9335" $xg2 $yg2 20 | Out-Null
Start-Sleep -Milliseconds 450
$moreG = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "moreG=$moreG"
$gy = $moreG -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYg) -le 1
$gx = $moreG -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXg) -le 1
$gd = $moreG -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$gpt = $false
if ($moreG -match '\\"pastTop\\":(-?\d+)') { $gptN = [int]$Matches[1] } else { $gptN = -999 }
if ($moreG -match '\\"pastMax\\":(-?\d+)') { $gpt = [Math]::Abs($gptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($gptN - $pastTopG) -le 1 }
$gpl = $moreG -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftG) -le 1
$grt = $false
if ($moreG -match '\\"resultTop\\":(-?\d+)') { $grtN = [int]$Matches[1] } else { $grtN = -999 }
if ($moreG -match '\\"resultMax\\":(-?\d+)') { $grt = [Math]::Abs($grtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($grtN - $resultTopG) -le 1 }
$grows = $moreG -match '\\"resultWhole\\":true' -and $moreG -match '\\"resultNow8\\":true' -and $moreG -match '\\"resultNow7\\":false' -and $moreG -match '\\"resultExitSeen\\":true' -and $moreG -match '\\"wideWhole\\":true' -and $moreG -match '\\"inside\\":true' -and $moreG -match '\\"exitSeen\\":true' -and $moreG -match '\\"exitStuck\\":true' -and $moreG -match '\\"exitWhole\\":true' -and $moreG -match '\\"exitLowest\\":true' -and $moreG -match '\\"allWhole\\":true' -and $moreG -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptG = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptG=$keptG"
$keptPastG = $false
if ($keptG -match '\\"pastTop\\":(-?\d+)') { $keptPastGN = [int]$Matches[1] } else { $keptPastGN = -999 }
if ($keptG -match '\\"pastMax\\":(-?\d+)') { $keptPastG = [Math]::Abs($keptPastGN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastGN - $pastTopG) -le 1 }
$keptGapG = $keptG -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftG = $keptG -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultG = $false
if ($keptG -match '\\"resultTop\\":(-?\d+)') { $keptResultGN = [int]$Matches[1] } else { $keptResultGN = -999 }
if ($keptG -match '\\"resultMax\\":(-?\d+)') { $keptResultG = [Math]::Abs($keptResultGN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultGN - $resultTopG) -le 1 }
$keptRowsG = $keptG -match '\\"resultWhole\\":true' -and $keptG -match '\\"resultNow8\\":true' -and $keptG -match '\\"resultNow7\\":false' -and $keptG -match '\\"resultExitSeen\\":true' -and $keptG -match '\\"wideWhole\\":true' -and $keptG -match '\\"inside\\":true' -and $keptG -match '\\"exitSeen\\":true' -and $keptG -match '\\"exitStuck\\":true' -and $keptG -match '\\"exitWhole\\":true' -and $keptG -match '\\"exitLowest\\":true' -and $keptG -match '\\"allWhole\\":true' -and $keptG -match '\\"barUnder\\":true' -and $keptG -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageG = $keptG -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptG -match '\\"overflow\\":\\"hidden\\"' -and $keptG -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "past-after-secret"
if (-not ($gy -and $gx -and $gd -and $gpt -and $gpl -and $grt -and $grows -and $keptPastG -and $keptGapG -and $keptLeftG -and $keptResultG -and $keptRowsG -and $keptPageG)) { Fail 335 "past-more-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "again-secret"
$armH = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  const rb = box.getBoundingClientRect();
  const x = Math.round(r.left + 80);
  const y = Math.round(r.top + 8);
  const hit = document.elementFromPoint(x, y);
  const hitPast = !!(hit && hit.closest && hit.closest("#past"));
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ x, y, hitPast, resultBottom: Math.round(rb.bottom), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, formula, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armH=$armH"
if ($armH -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 336 "page-cannot-scroll" }
if ($armH -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 337 "page-cannot-slide" }
if ($armH -notmatch '\\"hitPast\\":true') { Fail 338 "wheel-missed-past" }
if ($armH -notmatch '\\"x\\":(\d+)') { Fail 339 "no-xh" }
$xh2 = [int]$Matches[1]
if ($armH -notmatch '\\"y\\":(\d+)') { Fail 339 "no-yh" }
$yh2 = [int]$Matches[1]
if ($armH -notmatch '\\"resultBottom\\":(-?\d+)') { Fail 340 "no-result-bottom" }
if ($yh2 -le [int]$Matches[1]) { Fail 341 "aim-still-on-result" }
if ($armH -notmatch '\\"pageY\\":(-?\d+)') { Fail 342 "no-page-yh" }
$pageYh = [int]$Matches[1]
if ($armH -notmatch '\\"pageX\\":(-?\d+)') { Fail 343 "no-page-xh" }
$pageXh = [int]$Matches[1]
if ($armH -notmatch '\\"pastTop\\":(-?\d+)') { Fail 344 "no-past-toph" }
$pastTopH = [int]$Matches[1]
if ($armH -notmatch '\\"pastMax\\":(-?\d+)') { Fail 345 "no-past-maxh" }
$pastMaxH = [int]$Matches[1]
if ([Math]::Abs($pastTopH - $pastMaxH) -gt 1) { Fail 346 "past-not-at-end" }
if ($armH -notmatch '\\"pastLeft\\":(\d+)') { Fail 347 "no-past-lefth" }
$pastLeftH = [int]$Matches[1]
if ($armH -notmatch '\\"formula\\":(\d+)') { Fail 348 "no-formula-h" }
$formulaH = [int]$Matches[1]
if (($formulaH - $pastLeftH) -lt 10) { Fail 349 "box-not-narrowed" }
if ($armH -notmatch '\\"resultTop\\":(-?\d+)') { Fail 350 "no-result-toph" }
$resultTopH = [int]$Matches[1]
if ($armH -notmatch '\\"resultMax\\":(-?\d+)') { Fail 351 "no-result-maxh" }
$resultMaxH = [int]$Matches[1]
if ([Math]::Abs($resultTopH - $resultMaxH) -gt 1) { Fail 352 "result-not-at-end" }
Send-WheelX "9335" $xh2 $yh2 400 | Out-Null
Start-Sleep -Milliseconds 450
$slideH = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "slideH=$slideH"
$hy = $slideH -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYh) -le 1
$hx = $slideH -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXh) -le 1
$hd = $slideH -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$hpt = $false
if ($slideH -match '\\"pastTop\\":(-?\d+)') { $hptN = [int]$Matches[1] } else { $hptN = -999 }
if ($slideH -match '\\"pastMax\\":(-?\d+)') { $hpt = [Math]::Abs($hptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($hptN - $pastTopH) -le 1 }
$hgap = $false
if ($slideH -match '\\"pastLeft\\":(\d+)') { $hLeftN = [int]$Matches[1] } else { $hLeftN = -999 }
if ($slideH -match '\\"gap\\":(-?\d+)') { $hgap = [Math]::Abs([int]$Matches[1]) -le 1 -and ($hLeftN - $pastLeftH) -ge 10 }
$hrt = $false
if ($slideH -match '\\"resultTop\\":(-?\d+)') { $hrtN = [int]$Matches[1] } else { $hrtN = -999 }
if ($slideH -match '\\"resultMax\\":(-?\d+)') { $hrt = [Math]::Abs($hrtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($hrtN - $resultTopH) -le 1 }
$hrows = $slideH -match '\\"resultWhole\\":true' -and $slideH -match '\\"resultNow8\\":true' -and $slideH -match '\\"resultNow7\\":false' -and $slideH -match '\\"resultExitSeen\\":true' -and $slideH -match '\\"wideWhole\\":true' -and $slideH -match '\\"inside\\":true' -and $slideH -match '\\"exitSeen\\":true' -and $slideH -match '\\"exitStuck\\":true' -and $slideH -match '\\"exitWhole\\":true' -and $slideH -match '\\"exitLowest\\":true' -and $slideH -match '\\"allWhole\\":true' -and $slideH -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptH = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptH=$keptH"
$keptPastH = $false
if ($keptH -match '\\"pastTop\\":(-?\d+)') { $keptPastHN = [int]$Matches[1] } else { $keptPastHN = -999 }
if ($keptH -match '\\"pastMax\\":(-?\d+)') { $keptPastH = [Math]::Abs($keptPastHN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastHN - $pastTopH) -le 1 }
$keptGapH = $keptH -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftH = $keptH -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultH = $false
if ($keptH -match '\\"resultTop\\":(-?\d+)') { $keptResultHN = [int]$Matches[1] } else { $keptResultHN = -999 }
if ($keptH -match '\\"resultMax\\":(-?\d+)') { $keptResultH = [Math]::Abs($keptResultHN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultHN - $resultTopH) -le 1 }
$keptRowsH = $keptH -match '\\"resultWhole\\":true' -and $keptH -match '\\"resultNow8\\":true' -and $keptH -match '\\"resultNow7\\":false' -and $keptH -match '\\"resultExitSeen\\":true' -and $keptH -match '\\"wideWhole\\":true' -and $keptH -match '\\"inside\\":true' -and $keptH -match '\\"exitSeen\\":true' -and $keptH -match '\\"exitStuck\\":true' -and $keptH -match '\\"exitWhole\\":true' -and $keptH -match '\\"exitLowest\\":true' -and $keptH -match '\\"allWhole\\":true' -and $keptH -match '\\"barUnder\\":true' -and $keptH -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageH = $keptH -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptH -match '\\"overflow\\":\\"hidden\\"' -and $keptH -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "again-after-secret"
if (-not ($hy -and $hx -and $hd -and $hpt -and $hgap -and $hrt -and $hrows -and $keptPastH -and $keptGapH -and $keptLeftH -and $keptResultH -and $keptRowsH -and $keptPageH)) { Fail 353 "tail-again-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "left-secret"
$armJ = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  const rb = box.getBoundingClientRect();
  const x = Math.round(r.left + 80);
  const y = Math.round(r.top + 8);
  const hit = document.elementFromPoint(x, y);
  const hitPast = !!(hit && hit.closest && hit.closest("#past"));
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ x, y, hitPast, resultBottom: Math.round(rb.bottom), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, formula, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armJ=$armJ"
if ($armJ -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 354 "page-cannot-scroll" }
if ($armJ -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 355 "page-cannot-slide" }
if ($armJ -notmatch '\\"hitPast\\":true') { Fail 356 "wheel-missed-past" }
if ($armJ -notmatch '\\"x\\":(\d+)') { Fail 357 "no-xj" }
$xj2 = [int]$Matches[1]
if ($armJ -notmatch '\\"y\\":(\d+)') { Fail 357 "no-yj" }
$yj2 = [int]$Matches[1]
if ($armJ -notmatch '\\"resultBottom\\":(-?\d+)') { Fail 358 "no-result-bottom" }
if ($yj2 -le [int]$Matches[1]) { Fail 359 "aim-still-on-result" }
if ($armJ -notmatch '\\"pageY\\":(-?\d+)') { Fail 360 "no-page-yj" }
$pageYj = [int]$Matches[1]
if ($armJ -notmatch '\\"pageX\\":(-?\d+)') { Fail 361 "no-page-xj" }
$pageXj = [int]$Matches[1]
if ($armJ -notmatch '\\"pastTop\\":(-?\d+)') { Fail 362 "no-past-topj" }
$pastTopJ = [int]$Matches[1]
if ($armJ -notmatch '\\"pastMax\\":(-?\d+)') { Fail 363 "no-past-maxj" }
$pastMaxJ = [int]$Matches[1]
if ([Math]::Abs($pastTopJ - $pastMaxJ) -gt 1) { Fail 364 "past-not-at-end" }
if ($armJ -notmatch '\\"pastLeft\\":(\d+)') { Fail 365 "no-past-leftj" }
$pastLeftJ = [int]$Matches[1]
if ($armJ -notmatch '\\"formula\\":(\d+)') { Fail 366 "no-formula-j" }
$formulaJ = [int]$Matches[1]
if (($formulaJ - $pastLeftJ) -lt 10) { Fail 367 "box-not-narrowed" }
if ($armJ -notmatch '\\"resultTop\\":(-?\d+)') { Fail 368 "no-result-topj" }
$resultTopJ = [int]$Matches[1]
if ($armJ -notmatch '\\"resultMax\\":(-?\d+)') { Fail 369 "no-result-maxj" }
$resultMaxJ = [int]$Matches[1]
if ([Math]::Abs($resultTopJ - $resultMaxJ) -gt 1) { Fail 370 "result-not-at-end" }
Send-WheelX "9335" $xj2 $yj2 (-200) | Out-Null
Start-Sleep -Milliseconds 450
$slideJ = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "slideJ=$slideJ"
$jy = $slideJ -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYj) -le 1
$jx = $slideJ -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXj) -le 1
$jd = $slideJ -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$jpt = $false
if ($slideJ -match '\\"pastTop\\":(-?\d+)') { $jptN = [int]$Matches[1] } else { $jptN = -999 }
if ($slideJ -match '\\"pastMax\\":(-?\d+)') { $jpt = [Math]::Abs($jptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($jptN - $pastTopJ) -le 1 }
$jleft = $false
if ($slideJ -match '\\"pastLeft\\":(\d+)') { $jleftN = [int]$Matches[1] } else { $jleftN = 99999 }
$jgap = $false
if ($slideJ -match '\\"gap\\":(-?\d+)') { $jgap = [int]$Matches[1] -ge 40 -and $jleftN -le ($pastLeftJ - 40) }
$jrt = $false
if ($slideJ -match '\\"resultTop\\":(-?\d+)') { $jrtN = [int]$Matches[1] } else { $jrtN = -999 }
if ($slideJ -match '\\"resultMax\\":(-?\d+)') { $jrt = [Math]::Abs($jrtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($jrtN - $resultTopJ) -le 1 }
$jrows = $slideJ -match '\\"resultWhole\\":true' -and $slideJ -match '\\"resultNow8\\":true' -and $slideJ -match '\\"resultNow7\\":false' -and $slideJ -match '\\"resultExitSeen\\":true' -and $slideJ -match '\\"wideWhole\\":true' -and $slideJ -match '\\"inside\\":false' -and $slideJ -match '\\"exitSeen\\":true' -and $slideJ -match '\\"exitStuck\\":true' -and $slideJ -match '\\"exitWhole\\":true' -and $slideJ -match '\\"exitLowest\\":true' -and $slideJ -match '\\"allWhole\\":true' -and $slideJ -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptJ = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptJ=$keptJ"
$keptPastJ = $false
if ($keptJ -match '\\"pastTop\\":(-?\d+)') { $keptPastJN = [int]$Matches[1] } else { $keptPastJN = -999 }
if ($keptJ -match '\\"pastMax\\":(-?\d+)') { $keptPastJ = [Math]::Abs($keptPastJN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastJN - $pastTopJ) -le 1 }
$keptLeftJ = $false
if ($keptJ -match '\\"pastLeft\\":(\d+)') { $keptLeftJN = [int]$Matches[1] } else { $keptLeftJN = 99999 }
$keptGapJ = $false
if ($keptJ -match '\\"gap\\":(-?\d+)') { $keptGapJ = [int]$Matches[1] -ge 40 -and $keptLeftJN -le ($endLeft - 40) }
$keptResultJ = $false
if ($keptJ -match '\\"resultTop\\":(-?\d+)') { $keptResultJN = [int]$Matches[1] } else { $keptResultJN = -999 }
if ($keptJ -match '\\"resultMax\\":(-?\d+)') { $keptResultJ = [Math]::Abs($keptResultJN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultJN - $resultTopJ) -le 1 }
$keptRowsJ = $keptJ -match '\\"resultWhole\\":true' -and $keptJ -match '\\"resultNow8\\":true' -and $keptJ -match '\\"resultNow7\\":false' -and $keptJ -match '\\"resultExitSeen\\":true' -and $keptJ -match '\\"wideWhole\\":true' -and $keptJ -match '\\"inside\\":false' -and $keptJ -match '\\"exitSeen\\":true' -and $keptJ -match '\\"exitStuck\\":true' -and $keptJ -match '\\"exitWhole\\":true' -and $keptJ -match '\\"exitLowest\\":true' -and $keptJ -match '\\"allWhole\\":true' -and $keptJ -match '\\"barUnder\\":true' -and $keptJ -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageJ = $keptJ -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptJ -match '\\"overflow\\":\\"hidden\\"' -and $keptJ -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "left-after-secret"
if (-not ($jy -and $jx -and $jd -and $jpt -and $jgap -and $jrt -and $jrows -and $keptPastJ -and $keptGapJ -and $keptResultJ -and $keptRowsJ -and $keptPageJ)) { Fail 371 "left-again-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "back-secret"
$armK = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  const rb = box.getBoundingClientRect();
  const x = Math.round(r.left + 80);
  const y = Math.round(r.top + 8);
  const hit = document.elementFromPoint(x, y);
  const hitPast = !!(hit && hit.closest && hit.closest("#past"));
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ x, y, hitPast, resultBottom: Math.round(rb.bottom), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, formula, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armK=$armK"
if ($armK -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 372 "page-cannot-scroll" }
if ($armK -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 373 "page-cannot-slide" }
if ($armK -notmatch '\\"hitPast\\":true') { Fail 374 "wheel-missed-past" }
if ($armK -notmatch '\\"x\\":(\d+)') { Fail 375 "no-xk" }
$xk2 = [int]$Matches[1]
if ($armK -notmatch '\\"y\\":(\d+)') { Fail 375 "no-yk" }
$yk2 = [int]$Matches[1]
if ($armK -notmatch '\\"resultBottom\\":(-?\d+)') { Fail 376 "no-result-bottom" }
if ($yk2 -le [int]$Matches[1]) { Fail 377 "aim-still-on-result" }
if ($armK -notmatch '\\"pageY\\":(-?\d+)') { Fail 378 "no-page-yk" }
$pageYk = [int]$Matches[1]
if ($armK -notmatch '\\"pageX\\":(-?\d+)') { Fail 379 "no-page-xk" }
$pageXk = [int]$Matches[1]
if ($armK -notmatch '\\"pastTop\\":(-?\d+)') { Fail 380 "no-past-topk" }
$pastTopK = [int]$Matches[1]
if ($armK -notmatch '\\"pastMax\\":(-?\d+)') { Fail 381 "no-past-maxk" }
$pastMaxK = [int]$Matches[1]
if ([Math]::Abs($pastTopK - $pastMaxK) -gt 1) { Fail 382 "past-not-at-end" }
if ($armK -notmatch '\\"pastLeft\\":(\d+)') { Fail 383 "no-past-leftk" }
$pastLeftK = [int]$Matches[1]
if ($armK -notmatch '\\"formula\\":(\d+)') { Fail 384 "no-formula-k" }
$formulaK = [int]$Matches[1]
if (($formulaK - $pastLeftK) -lt 40) { Fail 385 "already-at-tail" }
if ($armK -notmatch '\\"resultTop\\":(-?\d+)') { Fail 386 "no-result-topk" }
$resultTopK = [int]$Matches[1]
if ($armK -notmatch '\\"resultMax\\":(-?\d+)') { Fail 387 "no-result-maxk" }
$resultMaxK = [int]$Matches[1]
if ([Math]::Abs($resultTopK - $resultMaxK) -gt 1) { Fail 388 "result-not-at-end" }
Send-WheelX "9335" $xk2 $yk2 400 | Out-Null
Start-Sleep -Milliseconds 450
$slideK = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "slideK=$slideK"
$ky = $slideK -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYk) -le 1
$kx = $slideK -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXk) -le 1
$kd = $slideK -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$kpt = $false
if ($slideK -match '\\"pastTop\\":(-?\d+)') { $kptN = [int]$Matches[1] } else { $kptN = -999 }
if ($slideK -match '\\"pastMax\\":(-?\d+)') { $kpt = [Math]::Abs($kptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($kptN - $pastTopK) -le 1 }
$kleft = $false
if ($slideK -match '\\"pastLeft\\":(\d+)') { $kleftN = [int]$Matches[1] } else { $kleftN = -999 }
$kgap = $false
if ($slideK -match '\\"gap\\":(-?\d+)') { $kgap = [Math]::Abs([int]$Matches[1]) -le 1 -and ($kleftN - $pastLeftK) -ge 40 }
$krt = $false
if ($slideK -match '\\"resultTop\\":(-?\d+)') { $krtN = [int]$Matches[1] } else { $krtN = -999 }
if ($slideK -match '\\"resultMax\\":(-?\d+)') { $krt = [Math]::Abs($krtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($krtN - $resultTopK) -le 1 }
$krows = $slideK -match '\\"resultWhole\\":true' -and $slideK -match '\\"resultNow8\\":true' -and $slideK -match '\\"resultNow7\\":false' -and $slideK -match '\\"resultExitSeen\\":true' -and $slideK -match '\\"wideWhole\\":true' -and $slideK -match '\\"inside\\":true' -and $slideK -match '\\"exitSeen\\":true' -and $slideK -match '\\"exitStuck\\":true' -and $slideK -match '\\"exitWhole\\":true' -and $slideK -match '\\"exitLowest\\":true' -and $slideK -match '\\"allWhole\\":true' -and $slideK -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptK = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, exitSeen: !!(exit && exit.seen), exitStuck: !!(exit && exit.stuck), exitWhole: !!(exit && exit.whole), exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptK=$keptK"
$keptPastK = $false
if ($keptK -match '\\"pastTop\\":(-?\d+)') { $keptPastKN = [int]$Matches[1] } else { $keptPastKN = -999 }
if ($keptK -match '\\"pastMax\\":(-?\d+)') { $keptPastK = [Math]::Abs($keptPastKN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastKN - $pastTopK) -le 1 }
$keptGapK = $keptK -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftK = $keptK -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultK = $false
if ($keptK -match '\\"resultTop\\":(-?\d+)') { $keptResultKN = [int]$Matches[1] } else { $keptResultKN = -999 }
if ($keptK -match '\\"resultMax\\":(-?\d+)') { $keptResultK = [Math]::Abs($keptResultKN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultKN - $resultTopK) -le 1 }
$keptRowsK = $keptK -match '\\"resultWhole\\":true' -and $keptK -match '\\"resultNow8\\":true' -and $keptK -match '\\"resultNow7\\":false' -and $keptK -match '\\"resultExitSeen\\":true' -and $keptK -match '\\"wideWhole\\":true' -and $keptK -match '\\"inside\\":true' -and $keptK -match '\\"exitSeen\\":true' -and $keptK -match '\\"exitStuck\\":true' -and $keptK -match '\\"exitWhole\\":true' -and $keptK -match '\\"exitLowest\\":true' -and $keptK -match '\\"allWhole\\":true' -and $keptK -match '\\"barUnder\\":true' -and $keptK -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageK = $keptK -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptK -match '\\"overflow\\":\\"hidden\\"' -and $keptK -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "back-after-secret"
if (-not ($ky -and $kx -and $kd -and $kpt -and $kgap -and $krt -and $krows -and $keptPastK -and $keptGapK -and $keptLeftK -and $keptResultK -and $keptRowsK -and $keptPageK)) { Fail 389 "right-back-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "up-secret"
$armQ = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  const rb = box.getBoundingClientRect();
  const x = Math.round(r.left + 80);
  const y = Math.round(r.top + 8);
  const hit = document.elementFromPoint(x, y);
  const hitPast = !!(hit && hit.closest && hit.closest("#past"));
  return JSON.stringify({ x, y, hitPast, resultBottom: Math.round(rb.bottom), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armQ=$armQ"
if ($armQ -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 390 "page-cannot-scroll" }
if ($armQ -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 391 "page-cannot-slide" }
if ($armQ -notmatch '\\"hitPast\\":true') { Fail 392 "wheel-missed-past" }
if ($armQ -notmatch '\\"x\\":(\d+)') { Fail 393 "no-xq" }
$xq2 = [int]$Matches[1]
if ($armQ -notmatch '\\"y\\":(\d+)') { Fail 393 "no-yq" }
$yq2 = [int]$Matches[1]
if ($armQ -notmatch '\\"resultBottom\\":(-?\d+)') { Fail 394 "no-result-bottom" }
if ($yq2 -le [int]$Matches[1]) { Fail 395 "aim-still-on-result" }
if ($armQ -notmatch '\\"pageY\\":(-?\d+)') { Fail 396 "no-page-yq" }
$pageYq = [int]$Matches[1]
if ($armQ -notmatch '\\"pageX\\":(-?\d+)') { Fail 397 "no-page-xq" }
$pageXq = [int]$Matches[1]
if ($armQ -notmatch '\\"pastTop\\":(-?\d+)') { Fail 398 "no-past-topq" }
$pastTopQ = [int]$Matches[1]
if ($armQ -notmatch '\\"pastMax\\":(-?\d+)') { Fail 399 "no-past-maxq" }
$pastMaxQ = [int]$Matches[1]
if ([Math]::Abs($pastTopQ - $pastMaxQ) -gt 1 -or $pastMaxQ -lt 20) { Fail 400 "past-not-at-end" }
if ($armQ -notmatch '\\"pastLeft\\":(\d+)') { Fail 401 "no-past-leftq" }
$pastLeftQ = [int]$Matches[1]
if ([Math]::Abs($pastLeftQ - $endLeft) -gt 1) { Fail 402 "past-not-at-tail" }
if ($armQ -notmatch '\\"resultTop\\":(-?\d+)') { Fail 403 "no-result-topq" }
$resultTopQ = [int]$Matches[1]
if ($armQ -notmatch '\\"resultMax\\":(-?\d+)') { Fail 404 "no-result-maxq" }
$resultMaxQ = [int]$Matches[1]
if ([Math]::Abs($resultTopQ - $resultMaxQ) -gt 1) { Fail 405 "result-not-at-end" }
Send-Wheel "9335" $xq2 $yq2 -20 | Out-Null
Start-Sleep -Milliseconds 450
$upQ = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, old6: el.textContent.indexOf("queqiao-old-6") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const old6Row = rows.find((row) => row.old6);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let old6 = null;
  if (old6Row && old6Row.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(old6Row.el.firstChild);
    const rect = range.getBoundingClientRect();
    old6 = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!old6Row.whole, seen: !!old6Row.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const wideLowest = !!(wide && wide.seen && visible.every((row) => row.bottom <= wide.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), pastExitSeen: rows.some((row) => row.seen && row.exit), wideWhole: !!(wide && wide.whole && wide.seen), wideLowest, end, old6, old6Seen: !!(old6 && old6.seen), old6Stuck: !!(old6 && old6.stuck), old6Whole: !!(old6 && old6.whole), allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(wideLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "upQ=$upQ"
$qy = $upQ -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYq) -le 1
$qx = $upQ -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXq) -le 1
$qd = $upQ -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$qpt = $false
if ($upQ -match '\\"pastTop\\":(-?\d+)') { $qptN = [int]$Matches[1] } else { $qptN = -999 }
if ($upQ -match '\\"pastMax\\":(-?\d+)') { $qpt = [Math]::Abs($qptN - ($pastTopQ - 20)) -le 1 -and $qptN -lt [int]$Matches[1] - 10 }
$qpl = $upQ -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftQ) -le 1
$qrt = $false
if ($upQ -match '\\"resultTop\\":(-?\d+)') { $qrtN = [int]$Matches[1] } else { $qrtN = -999 }
if ($upQ -match '\\"resultMax\\":(-?\d+)') { $qrt = [Math]::Abs($qrtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($qrtN - $resultTopQ) -le 1 }
$qrows = $upQ -match '\\"resultWhole\\":true' -and $upQ -match '\\"resultNow8\\":true' -and $upQ -match '\\"resultNow7\\":false' -and $upQ -match '\\"resultExitSeen\\":true' -and $upQ -match '\\"pastExitSeen\\":false' -and $upQ -match '\\"wideWhole\\":true' -and $upQ -match '\\"wideLowest\\":true' -and $upQ -match '\\"inside\\":true' -and $upQ -match '\\"old6Seen\\":true' -and $upQ -match '\\"old6Stuck\\":true' -and $upQ -match '\\"old6Whole\\":true' -and $upQ -match '\\"allWhole\\":true' -and $upQ -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptQ = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, old6: el.textContent.indexOf("queqiao-old-6") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const old6Row = rows.find((row) => row.old6);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let old6 = null;
  if (old6Row && old6Row.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(old6Row.el.firstChild);
    const rect = range.getBoundingClientRect();
    old6 = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!old6Row.whole, seen: !!old6Row.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const wideLowest = !!(wide && wide.seen && visible.every((row) => row.bottom <= wide.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), pastExitSeen: rows.some((row) => row.seen && row.exit), wideWhole: !!(wide && wide.whole && wide.seen), wideLowest, end, old6, old6Seen: !!(old6 && old6.seen), old6Stuck: !!(old6 && old6.stuck), old6Whole: !!(old6 && old6.whole), allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(wideLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptQ=$keptQ"
$keptPastQ = $false
if ($keptQ -match '\\"pastTop\\":(-?\d+)') { $keptPastQN = [int]$Matches[1] } else { $keptPastQN = -999 }
if ($keptQ -match '\\"pastMax\\":(-?\d+)') { $keptPastQ = [Math]::Abs($keptPastQN - ($pastTopQ - 20)) -le 1 -and $keptPastQN -lt [int]$Matches[1] - 10 }
$keptGapQ = $keptQ -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftQ = $keptQ -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultQ = $false
if ($keptQ -match '\\"resultTop\\":(-?\d+)') { $keptResultQN = [int]$Matches[1] } else { $keptResultQN = -999 }
if ($keptQ -match '\\"resultMax\\":(-?\d+)') { $keptResultQ = [Math]::Abs($keptResultQN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultQN - $resultTopQ) -le 1 }
$keptRowsQ = $keptQ -match '\\"resultWhole\\":true' -and $keptQ -match '\\"resultNow8\\":true' -and $keptQ -match '\\"resultNow7\\":false' -and $keptQ -match '\\"resultExitSeen\\":true' -and $keptQ -match '\\"pastExitSeen\\":false' -and $keptQ -match '\\"wideWhole\\":true' -and $keptQ -match '\\"wideLowest\\":true' -and $keptQ -match '\\"inside\\":true' -and $keptQ -match '\\"old6Seen\\":true' -and $keptQ -match '\\"old6Stuck\\":true' -and $keptQ -match '\\"old6Whole\\":true' -and $keptQ -match '\\"allWhole\\":true' -and $keptQ -match '\\"barUnder\\":true' -and $keptQ -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageQ = $keptQ -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptQ -match '\\"overflow\\":\\"hidden\\"' -and $keptQ -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "up-after-secret"
if (-not ($qy -and $qx -and $qd -and $qpt -and $qpl -and $qrt -and $qrows -and $keptPastQ -and $keptGapQ -and $keptLeftQ -and $keptResultQ -and $keptRowsQ -and $keptPageQ)) { Fail 406 "up-again-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "down-secret"
$armR = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  const rb = box.getBoundingClientRect();
  const x = Math.round(r.left + 80);
  const y = Math.round(r.top + 8);
  const hit = document.elementFromPoint(x, y);
  const hitPast = !!(hit && hit.closest && hit.closest("#past"));
  return JSON.stringify({ x, y, hitPast, resultBottom: Math.round(rb.bottom), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armR=$armR"
if ($armR -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 407 "page-cannot-scroll" }
if ($armR -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 408 "page-cannot-slide" }
if ($armR -notmatch '\\"hitPast\\":true') { Fail 409 "wheel-missed-past" }
if ($armR -notmatch '\\"x\\":(\d+)') { Fail 410 "no-xr" }
$xr2 = [int]$Matches[1]
if ($armR -notmatch '\\"y\\":(\d+)') { Fail 410 "no-yr" }
$yr2 = [int]$Matches[1]
if ($armR -notmatch '\\"resultBottom\\":(-?\d+)') { Fail 411 "no-result-bottom" }
if ($yr2 -le [int]$Matches[1]) { Fail 412 "aim-still-on-result" }
if ($armR -notmatch '\\"pageY\\":(-?\d+)') { Fail 413 "no-page-yr" }
$pageYr = [int]$Matches[1]
if ($armR -notmatch '\\"pageX\\":(-?\d+)') { Fail 414 "no-page-xr" }
$pageXr = [int]$Matches[1]
if ($armR -notmatch '\\"pastTop\\":(-?\d+)') { Fail 415 "no-past-topr" }
$pastTopR = [int]$Matches[1]
if ($armR -notmatch '\\"pastMax\\":(-?\d+)') { Fail 416 "no-past-maxr" }
$pastMaxR = [int]$Matches[1]
if (($pastMaxR - $pastTopR) -lt 10) { Fail 417 "past-already-bottom" }
if ($armR -notmatch '\\"pastLeft\\":(\d+)') { Fail 418 "no-past-leftr" }
$pastLeftR = [int]$Matches[1]
if ([Math]::Abs($pastLeftR - $endLeft) -gt 1) { Fail 419 "past-not-at-tail" }
if ($armR -notmatch '\\"resultTop\\":(-?\d+)') { Fail 420 "no-result-topr" }
$resultTopR = [int]$Matches[1]
if ($armR -notmatch '\\"resultMax\\":(-?\d+)') { Fail 421 "no-result-maxr" }
$resultMaxR = [int]$Matches[1]
if ([Math]::Abs($resultTopR - $resultMaxR) -gt 1) { Fail 422 "result-not-at-end" }
Send-Wheel "9335" $xr2 $yr2 20 | Out-Null
Start-Sleep -Milliseconds 450
$downR = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, old6: el.textContent.indexOf("queqiao-old-6") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, pastExitSeen: !!(exit && exit.seen), pastExitStuck: !!(exit && exit.stuck), pastExitWhole: !!(exit && exit.whole), pastExitLowest: exitLowest, old6Seen: rows.some((row) => row.seen && row.old6), allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "downR=$downR"
$ry = $downR -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYr) -le 1
$rx = $downR -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXr) -le 1
$rd = $downR -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$rpt = $false
if ($downR -match '\\"pastTop\\":(-?\d+)') { $rptN = [int]$Matches[1] } else { $rptN = -999 }
if ($downR -match '\\"pastMax\\":(-?\d+)') { $rpt = [Math]::Abs($rptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($rptN - ($pastTopR + 20)) -le 1 }
$rpl = $downR -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftR) -le 1
$rrt = $false
if ($downR -match '\\"resultTop\\":(-?\d+)') { $rrtN = [int]$Matches[1] } else { $rrtN = -999 }
if ($downR -match '\\"resultMax\\":(-?\d+)') { $rrt = [Math]::Abs($rrtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($rrtN - $resultTopR) -le 1 }
$rrows = $downR -match '\\"resultWhole\\":true' -and $downR -match '\\"resultNow8\\":true' -and $downR -match '\\"resultNow7\\":false' -and $downR -match '\\"resultExitSeen\\":true' -and $downR -match '\\"wideWhole\\":true' -and $downR -match '\\"inside\\":true' -and $downR -match '\\"pastExitSeen\\":true' -and $downR -match '\\"pastExitStuck\\":true' -and $downR -match '\\"pastExitWhole\\":true' -and $downR -match '\\"pastExitLowest\\":true' -and $downR -match '\\"old6Seen\\":false' -and $downR -match '\\"allWhole\\":true' -and $downR -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptR = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, old6: el.textContent.indexOf("queqiao-old-6") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, pastExitSeen: !!(exit && exit.seen), pastExitStuck: !!(exit && exit.stuck), pastExitWhole: !!(exit && exit.whole), pastExitLowest: exitLowest, old6Seen: rows.some((row) => row.seen && row.old6), allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptR=$keptR"
$keptPastR = $false
if ($keptR -match '\\"pastTop\\":(-?\d+)') { $keptPastRN = [int]$Matches[1] } else { $keptPastRN = -999 }
if ($keptR -match '\\"pastMax\\":(-?\d+)') { $keptPastR = [Math]::Abs($keptPastRN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastRN - ($pastTopR + 20)) -le 1 }
$keptGapR = $keptR -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftR = $keptR -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultR = $false
if ($keptR -match '\\"resultTop\\":(-?\d+)') { $keptResultRN = [int]$Matches[1] } else { $keptResultRN = -999 }
if ($keptR -match '\\"resultMax\\":(-?\d+)') { $keptResultR = [Math]::Abs($keptResultRN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultRN - $resultTopR) -le 1 }
$keptRowsR = $keptR -match '\\"resultWhole\\":true' -and $keptR -match '\\"resultNow8\\":true' -and $keptR -match '\\"resultNow7\\":false' -and $keptR -match '\\"resultExitSeen\\":true' -and $keptR -match '\\"wideWhole\\":true' -and $keptR -match '\\"inside\\":true' -and $keptR -match '\\"pastExitSeen\\":true' -and $keptR -match '\\"pastExitStuck\\":true' -and $keptR -match '\\"pastExitWhole\\":true' -and $keptR -match '\\"pastExitLowest\\":true' -and $keptR -match '\\"old6Seen\\":false' -and $keptR -match '\\"allWhole\\":true' -and $keptR -match '\\"barUnder\\":true' -and $keptR -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageR = $keptR -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptR -match '\\"overflow\\":\\"hidden\\"' -and $keptR -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "down-after-secret"
if (-not ($ry -and $rx -and $rd -and $rpt -and $rpl -and $rrt -and $rrows -and $keptPastR -and $keptGapR -and $keptLeftR -and $keptResultR -and $keptRowsR -and $keptPageR)) { Fail 423 "down-again-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "up-secret"
$armS = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = box.getBoundingClientRect();
  const rp = past.getBoundingClientRect();
  const x = Math.round(r.left + 80);
  const y = Math.round(r.top + 8);
  const hit = document.elementFromPoint(x, y);
  const hitResult = !!(hit && hit.closest && hit.closest("#result"));
  return JSON.stringify({ x, y, hitResult, pastTopEdge: Math.round(rp.top), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armS=$armS"
if ($armS -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 424 "page-cannot-scroll" }
if ($armS -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 425 "page-cannot-slide" }
if ($armS -notmatch '\\"hitResult\\":true') { Fail 426 "wheel-missed-result" }
if ($armS -notmatch '\\"x\\":(\d+)') { Fail 427 "no-xs" }
$xs2 = [int]$Matches[1]
if ($armS -notmatch '\\"y\\":(\d+)') { Fail 427 "no-ys" }
$ys2 = [int]$Matches[1]
if ($armS -notmatch '\\"pastTopEdge\\":(-?\d+)') { Fail 428 "no-past-edge" }
if ($ys2 -ge [int]$Matches[1]) { Fail 429 "aim-still-on-past" }
if ($armS -notmatch '\\"pageY\\":(-?\d+)') { Fail 430 "no-page-ys" }
$pageYs = [int]$Matches[1]
if ($armS -notmatch '\\"pageX\\":(-?\d+)') { Fail 431 "no-page-xs" }
$pageXs = [int]$Matches[1]
if ($armS -notmatch '\\"pastTop\\":(-?\d+)') { Fail 432 "no-past-tops" }
$pastTopS = [int]$Matches[1]
if ($armS -notmatch '\\"pastMax\\":(-?\d+)') { Fail 433 "no-past-maxs" }
$pastMaxS = [int]$Matches[1]
if ([Math]::Abs($pastTopS - $pastMaxS) -gt 1) { Fail 434 "past-not-at-end" }
if ($armS -notmatch '\\"pastLeft\\":(\d+)') { Fail 435 "no-past-lefts" }
$pastLeftS = [int]$Matches[1]
if ([Math]::Abs($pastLeftS - $endLeft) -gt 1) { Fail 436 "past-not-at-tail" }
if ($armS -notmatch '\\"resultTop\\":(-?\d+)') { Fail 437 "no-result-tops" }
$resultTopS = [int]$Matches[1]
if ($armS -notmatch '\\"resultMax\\":(-?\d+)') { Fail 438 "no-result-maxs" }
$resultMaxS = [int]$Matches[1]
if ([Math]::Abs($resultTopS - $resultMaxS) -gt 1) { Fail 439 "result-not-at-end" }
Send-Wheel "9335" $xs2 $ys2 -20 | Out-Null
Start-Sleep -Milliseconds 450
$upS = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, pastExitSeen: !!(exit && exit.seen), pastExitStuck: !!(exit && exit.stuck), pastExitWhole: !!(exit && exit.whole), pastExitLowest: exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "upS=$upS"
$sy = $upS -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYs) -le 1
$sx = $upS -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXs) -le 1
$sd = $upS -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$spt = $false
if ($upS -match '\\"pastTop\\":(-?\d+)') { $sptN = [int]$Matches[1] } else { $sptN = -999 }
if ($upS -match '\\"pastMax\\":(-?\d+)') { $spt = [Math]::Abs($sptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($sptN - $pastTopS) -le 1 }
$spl = $upS -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftS) -le 1
$srt = $false
if ($upS -match '\\"resultTop\\":(-?\d+)') { $srtN = [int]$Matches[1] } else { $srtN = -999 }
if ($upS -match '\\"resultMax\\":(-?\d+)') { $srt = [Math]::Abs($srtN - ($resultTopS - 20)) -le 1 -and $srtN -lt [int]$Matches[1] - 10 }
$srows = $upS -match '\\"resultWhole\\":true' -and $upS -match '\\"resultNow8\\":true' -and $upS -match '\\"resultNow7\\":true' -and $upS -match '\\"resultExitSeen\\":false' -and $upS -match '\\"wideWhole\\":true' -and $upS -match '\\"inside\\":true' -and $upS -match '\\"pastExitSeen\\":true' -and $upS -match '\\"pastExitStuck\\":true' -and $upS -match '\\"pastExitWhole\\":true' -and $upS -match '\\"pastExitLowest\\":true' -and $upS -match '\\"allWhole\\":true' -and $upS -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptS = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, pastExitSeen: !!(exit && exit.seen), pastExitStuck: !!(exit && exit.stuck), pastExitWhole: !!(exit && exit.whole), pastExitLowest: exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptS=$keptS"
$keptPastS = $false
if ($keptS -match '\\"pastTop\\":(-?\d+)') { $keptPastSN = [int]$Matches[1] } else { $keptPastSN = -999 }
if ($keptS -match '\\"pastMax\\":(-?\d+)') { $keptPastS = [Math]::Abs($keptPastSN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastSN - $pastTopS) -le 1 }
$keptGapS = $keptS -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftS = $keptS -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultS = $false
if ($keptS -match '\\"resultTop\\":(-?\d+)') { $keptResultSN = [int]$Matches[1] } else { $keptResultSN = -999 }
if ($keptS -match '\\"resultMax\\":(-?\d+)') { $keptResultS = [Math]::Abs($keptResultSN - ($resultTopS - 20)) -le 1 -and $keptResultSN -lt [int]$Matches[1] - 10 }
$keptRowsS = $keptS -match '\\"resultWhole\\":true' -and $keptS -match '\\"resultNow8\\":true' -and $keptS -match '\\"resultNow7\\":true' -and $keptS -match '\\"resultExitSeen\\":false' -and $keptS -match '\\"wideWhole\\":true' -and $keptS -match '\\"inside\\":true' -and $keptS -match '\\"pastExitSeen\\":true' -and $keptS -match '\\"pastExitStuck\\":true' -and $keptS -match '\\"pastExitWhole\\":true' -and $keptS -match '\\"pastExitLowest\\":true' -and $keptS -match '\\"allWhole\\":true' -and $keptS -match '\\"barUnder\\":true' -and $keptS -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageS = $keptS -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptS -match '\\"overflow\\":\\"hidden\\"' -and $keptS -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "up-after-secret"
if (-not ($sy -and $sx -and $sd -and $spt -and $spl -and $srt -and $srows -and $keptPastS -and $keptGapS -and $keptLeftS -and $keptResultS -and $keptRowsS -and $keptPageS)) { Fail 440 "now-up-again-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "back-secret"
$armU = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = box.getBoundingClientRect();
  const rp = past.getBoundingClientRect();
  const x = Math.round(r.left + 80);
  const y = Math.round(r.top + 8);
  const hit = document.elementFromPoint(x, y);
  const hitResult = !!(hit && hit.closest && hit.closest("#result"));
  return JSON.stringify({ x, y, hitResult, pastTopEdge: Math.round(rp.top), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armU=$armU"
if ($armU -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 441 "page-cannot-scroll" }
if ($armU -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 442 "page-cannot-slide" }
if ($armU -notmatch '\\"hitResult\\":true') { Fail 443 "wheel-missed-result" }
if ($armU -notmatch '\\"x\\":(\d+)') { Fail 444 "no-xu" }
$xu2 = [int]$Matches[1]
if ($armU -notmatch '\\"y\\":(\d+)') { Fail 444 "no-yu" }
$yu2 = [int]$Matches[1]
if ($armU -notmatch '\\"pastTopEdge\\":(-?\d+)') { Fail 445 "no-past-edge" }
if ($yu2 -ge [int]$Matches[1]) { Fail 446 "aim-still-on-past" }
if ($armU -notmatch '\\"pageY\\":(-?\d+)') { Fail 447 "no-page-yu" }
$pageYu = [int]$Matches[1]
if ($armU -notmatch '\\"pageX\\":(-?\d+)') { Fail 448 "no-page-xu" }
$pageXu = [int]$Matches[1]
if ($armU -notmatch '\\"pastTop\\":(-?\d+)') { Fail 449 "no-past-topu" }
$pastTopU = [int]$Matches[1]
if ($armU -notmatch '\\"pastMax\\":(-?\d+)') { Fail 450 "no-past-maxu" }
$pastMaxU = [int]$Matches[1]
if ([Math]::Abs($pastTopU - $pastMaxU) -gt 1) { Fail 451 "past-not-at-end" }
if ($armU -notmatch '\\"pastLeft\\":(\d+)') { Fail 452 "no-past-leftu" }
$pastLeftU = [int]$Matches[1]
if ([Math]::Abs($pastLeftU - $endLeft) -gt 1) { Fail 453 "past-not-at-tail" }
if ($armU -notmatch '\\"resultTop\\":(-?\d+)') { Fail 454 "no-result-topu" }
$resultTopU = [int]$Matches[1]
if ($armU -notmatch '\\"resultMax\\":(-?\d+)') { Fail 455 "no-result-maxu" }
$resultMaxU = [int]$Matches[1]
if (($resultMaxU - $resultTopU) -lt 10) { Fail 456 "result-already-bottom" }
Send-Wheel "9335" $xu2 $yu2 20 | Out-Null
Start-Sleep -Milliseconds 450
$downU = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, pastExitSeen: !!(exit && exit.seen), pastExitStuck: !!(exit && exit.stuck), pastExitWhole: !!(exit && exit.whole), pastExitLowest: exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "downU=$downU"
$uy = $downU -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYu) -le 1
$ux = $downU -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXu) -le 1
$ud = $downU -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$upt = $false
if ($downU -match '\\"pastTop\\":(-?\d+)') { $uptN = [int]$Matches[1] } else { $uptN = -999 }
if ($downU -match '\\"pastMax\\":(-?\d+)') { $upt = [Math]::Abs($uptN - [int]$Matches[1]) -le 1 -and [Math]::Abs($uptN - $pastTopU) -le 1 }
$upl = $downU -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftU) -le 1
$urt = $false
if ($downU -match '\\"resultTop\\":(-?\d+)') { $urtN = [int]$Matches[1] } else { $urtN = -999 }
if ($downU -match '\\"resultMax\\":(-?\d+)') { $urt = [Math]::Abs($urtN - [int]$Matches[1]) -le 1 -and [Math]::Abs($urtN - ($resultTopU + 20)) -le 1 }
$urows = $downU -match '\\"resultWhole\\":true' -and $downU -match '\\"resultNow8\\":true' -and $downU -match '\\"resultNow7\\":false' -and $downU -match '\\"resultExitSeen\\":true' -and $downU -match '\\"wideWhole\\":true' -and $downU -match '\\"inside\\":true' -and $downU -match '\\"pastExitSeen\\":true' -and $downU -match '\\"pastExitStuck\\":true' -and $downU -match '\\"pastExitWhole\\":true' -and $downU -match '\\"pastExitLowest\\":true' -and $downU -match '\\"allWhole\\":true' -and $downU -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptU = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, pastExitSeen: !!(exit && exit.seen), pastExitStuck: !!(exit && exit.stuck), pastExitWhole: !!(exit && exit.whole), pastExitLowest: exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptU=$keptU"
$keptPastU = $false
if ($keptU -match '\\"pastTop\\":(-?\d+)') { $keptPastUN = [int]$Matches[1] } else { $keptPastUN = -999 }
if ($keptU -match '\\"pastMax\\":(-?\d+)') { $keptPastU = [Math]::Abs($keptPastUN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastUN - $pastTopU) -le 1 }
$keptGapU = $keptU -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftU = $keptU -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultU = $false
if ($keptU -match '\\"resultTop\\":(-?\d+)') { $keptResultUN = [int]$Matches[1] } else { $keptResultUN = -999 }
if ($keptU -match '\\"resultMax\\":(-?\d+)') { $keptResultU = [Math]::Abs($keptResultUN - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultUN - ($resultTopU + 20)) -le 1 }
$keptRowsU = $keptU -match '\\"resultWhole\\":true' -and $keptU -match '\\"resultNow8\\":true' -and $keptU -match '\\"resultNow7\\":false' -and $keptU -match '\\"resultExitSeen\\":true' -and $keptU -match '\\"wideWhole\\":true' -and $keptU -match '\\"inside\\":true' -and $keptU -match '\\"pastExitSeen\\":true' -and $keptU -match '\\"pastExitStuck\\":true' -and $keptU -match '\\"pastExitWhole\\":true' -and $keptU -match '\\"pastExitLowest\\":true' -and $keptU -match '\\"allWhole\\":true' -and $keptU -match '\\"barUnder\\":true' -and $keptU -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageU = $keptU -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptU -match '\\"overflow\\":\\"hidden\\"' -and $keptU -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "back-after-secret"
if (-not ($uy -and $ux -and $ud -and $upt -and $upl -and $urt -and $urows -and $keptPastU -and $keptGapU -and $keptLeftU -and $keptResultU -and $keptRowsU -and $keptPageU)) { Fail 457 "res-down-again-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "more-secret"
$armW2 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = box.getBoundingClientRect();
  const rp = past.getBoundingClientRect();
  const x = Math.round(r.left + 80);
  const y = Math.round(r.top + 8);
  const hit = document.elementFromPoint(x, y);
  const hitResult = !!(hit && hit.closest && hit.closest("#result"));
  return JSON.stringify({ x, y, hitResult, pastTopEdge: Math.round(rp.top), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armW2=$armW2"
if ($armW2 -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 458 "page-cannot-scroll" }
if ($armW2 -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 459 "page-cannot-slide" }
if ($armW2 -notmatch '\\"hitResult\\":true') { Fail 460 "wheel-missed-result" }
if ($armW2 -notmatch '\\"x\\":(\d+)') { Fail 461 "no-xw" }
$xw2 = [int]$Matches[1]
if ($armW2 -notmatch '\\"y\\":(\d+)') { Fail 461 "no-yw" }
$yw2 = [int]$Matches[1]
if ($armW2 -notmatch '\\"pastTopEdge\\":(-?\d+)') { Fail 462 "no-past-edge" }
if ($yw2 -ge [int]$Matches[1]) { Fail 463 "aim-still-on-past" }
if ($armW2 -notmatch '\\"pageY\\":(-?\d+)') { Fail 464 "no-page-yw" }
$pageYw2 = [int]$Matches[1]
if ($armW2 -notmatch '\\"pageX\\":(-?\d+)') { Fail 465 "no-page-xw" }
$pageXw2 = [int]$Matches[1]
if ($armW2 -notmatch '\\"pastTop\\":(-?\d+)') { Fail 466 "no-past-topw" }
$pastTopW2 = [int]$Matches[1]
if ($armW2 -notmatch '\\"pastMax\\":(-?\d+)') { Fail 467 "no-past-maxw" }
$pastMaxW2 = [int]$Matches[1]
if ([Math]::Abs($pastTopW2 - $pastMaxW2) -gt 1) { Fail 468 "past-not-at-end" }
if ($armW2 -notmatch '\\"pastLeft\\":(\d+)') { Fail 469 "no-past-leftw" }
$pastLeftW2 = [int]$Matches[1]
if ([Math]::Abs($pastLeftW2 - $endLeft) -gt 1) { Fail 470 "past-not-at-tail" }
if ($armW2 -notmatch '\\"resultTop\\":(-?\d+)') { Fail 471 "no-result-topw" }
$resultTopW2 = [int]$Matches[1]
if ($armW2 -notmatch '\\"resultMax\\":(-?\d+)') { Fail 472 "no-result-maxw" }
$resultMaxW2 = [int]$Matches[1]
if ([Math]::Abs($resultTopW2 - $resultMaxW2) -gt 1) { Fail 473 "result-not-at-end" }
Send-Wheel "9335" $xw2 $yw2 20 | Out-Null
Start-Sleep -Milliseconds 450
$moreW2 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, pastExitSeen: !!(exit && exit.seen), pastExitStuck: !!(exit && exit.stuck), pastExitWhole: !!(exit && exit.whole), pastExitLowest: exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "moreW2=$moreW2"
$wy2 = $moreW2 -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYw2) -le 1
$wx2 = $moreW2 -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXw2) -le 1
$wd2 = $moreW2 -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$wpt2 = $false
if ($moreW2 -match '\\"pastTop\\":(-?\d+)') { $wpt2N = [int]$Matches[1] } else { $wpt2N = -999 }
if ($moreW2 -match '\\"pastMax\\":(-?\d+)') { $wpt2 = [Math]::Abs($wpt2N - [int]$Matches[1]) -le 1 -and [Math]::Abs($wpt2N - $pastTopW2) -le 1 }
$wpl2 = $moreW2 -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftW2) -le 1
$wrt2 = $false
if ($moreW2 -match '\\"resultTop\\":(-?\d+)') { $wrt2N = [int]$Matches[1] } else { $wrt2N = -999 }
if ($moreW2 -match '\\"resultMax\\":(-?\d+)') { $wrt2 = [Math]::Abs($wrt2N - [int]$Matches[1]) -le 1 -and [Math]::Abs($wrt2N - $resultTopW2) -le 1 }
$wrows2 = $moreW2 -match '\\"resultWhole\\":true' -and $moreW2 -match '\\"resultNow8\\":true' -and $moreW2 -match '\\"resultNow7\\":false' -and $moreW2 -match '\\"resultExitSeen\\":true' -and $moreW2 -match '\\"wideWhole\\":true' -and $moreW2 -match '\\"inside\\":true' -and $moreW2 -match '\\"pastExitSeen\\":true' -and $moreW2 -match '\\"pastExitStuck\\":true' -and $moreW2 -match '\\"pastExitWhole\\":true' -and $moreW2 -match '\\"pastExitLowest\\":true' -and $moreW2 -match '\\"allWhole\\":true' -and $moreW2 -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptW2 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, pastExitSeen: !!(exit && exit.seen), pastExitStuck: !!(exit && exit.stuck), pastExitWhole: !!(exit && exit.whole), pastExitLowest: exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptW2=$keptW2"
$keptPastW2 = $false
if ($keptW2 -match '\\"pastTop\\":(-?\d+)') { $keptPastW2N = [int]$Matches[1] } else { $keptPastW2N = -999 }
if ($keptW2 -match '\\"pastMax\\":(-?\d+)') { $keptPastW2 = [Math]::Abs($keptPastW2N - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastW2N - $pastTopW2) -le 1 }
$keptGapW2 = $keptW2 -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftW2 = $keptW2 -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultW2 = $false
if ($keptW2 -match '\\"resultTop\\":(-?\d+)') { $keptResultW2N = [int]$Matches[1] } else { $keptResultW2N = -999 }
if ($keptW2 -match '\\"resultMax\\":(-?\d+)') { $keptResultW2 = [Math]::Abs($keptResultW2N - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultW2N - $resultTopW2) -le 1 }
$keptRowsW2 = $keptW2 -match '\\"resultWhole\\":true' -and $keptW2 -match '\\"resultNow8\\":true' -and $keptW2 -match '\\"resultNow7\\":false' -and $keptW2 -match '\\"resultExitSeen\\":true' -and $keptW2 -match '\\"wideWhole\\":true' -and $keptW2 -match '\\"inside\\":true' -and $keptW2 -match '\\"pastExitSeen\\":true' -and $keptW2 -match '\\"pastExitStuck\\":true' -and $keptW2 -match '\\"pastExitWhole\\":true' -and $keptW2 -match '\\"pastExitLowest\\":true' -and $keptW2 -match '\\"allWhole\\":true' -and $keptW2 -match '\\"barUnder\\":true' -and $keptW2 -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageW2 = $keptW2 -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptW2 -match '\\"overflow\\":\\"hidden\\"' -and $keptW2 -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "more-after-secret"
if (-not ($wy2 -and $wx2 -and $wd2 -and $wpt2 -and $wpl2 -and $wrt2 -and $wrows2 -and $keptPastW2 -and $keptGapW2 -and $keptLeftW2 -and $keptResultW2 -and $keptRowsW2 -and $keptPageW2)) { Fail 474 "res-more-again-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "past-secret"
$armZ3 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  const rb = box.getBoundingClientRect();
  const x = Math.round(r.left + 80);
  const y = Math.round(r.top + 8);
  const hit = document.elementFromPoint(x, y);
  const hitPast = !!(hit && hit.closest && hit.closest("#past"));
  return JSON.stringify({ x, y, hitPast, resultBottom: Math.round(rb.bottom), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armZ3=$armZ3"
if ($armZ3 -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 475 "page-cannot-scroll" }
if ($armZ3 -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 476 "page-cannot-slide" }
if ($armZ3 -notmatch '\\"hitPast\\":true') { Fail 477 "wheel-missed-past" }
if ($armZ3 -notmatch '\\"x\\":(\d+)') { Fail 478 "no-xz" }
$xz3 = [int]$Matches[1]
if ($armZ3 -notmatch '\\"y\\":(\d+)') { Fail 478 "no-yz" }
$yz3 = [int]$Matches[1]
if ($armZ3 -notmatch '\\"resultBottom\\":(-?\d+)') { Fail 479 "no-result-bottom" }
if ($yz3 -le [int]$Matches[1]) { Fail 480 "aim-still-on-result" }
if ($armZ3 -notmatch '\\"pageY\\":(-?\d+)') { Fail 481 "no-page-yz" }
$pageYz3 = [int]$Matches[1]
if ($armZ3 -notmatch '\\"pageX\\":(-?\d+)') { Fail 482 "no-page-xz" }
$pageXz3 = [int]$Matches[1]
if ($armZ3 -notmatch '\\"pastTop\\":(-?\d+)') { Fail 483 "no-past-topz" }
$pastTopZ3 = [int]$Matches[1]
if ($armZ3 -notmatch '\\"pastMax\\":(-?\d+)') { Fail 484 "no-past-maxz" }
$pastMaxZ3 = [int]$Matches[1]
if ([Math]::Abs($pastTopZ3 - $pastMaxZ3) -gt 1) { Fail 485 "past-not-at-end" }
if ($armZ3 -notmatch '\\"pastLeft\\":(\d+)') { Fail 486 "no-past-leftz" }
$pastLeftZ3 = [int]$Matches[1]
if ([Math]::Abs($pastLeftZ3 - $endLeft) -gt 1) { Fail 487 "past-not-at-tail" }
if ($armZ3 -notmatch '\\"resultTop\\":(-?\d+)') { Fail 488 "no-result-topz" }
$resultTopZ3 = [int]$Matches[1]
if ($armZ3 -notmatch '\\"resultMax\\":(-?\d+)') { Fail 489 "no-result-maxz" }
$resultMaxZ3 = [int]$Matches[1]
if ([Math]::Abs($resultTopZ3 - $resultMaxZ3) -gt 1) { Fail 490 "result-not-at-end" }
Send-Wheel "9335" $xz3 $yz3 20 | Out-Null
Start-Sleep -Milliseconds 450
$moreZ3 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, pastExitSeen: !!(exit && exit.seen), pastExitStuck: !!(exit && exit.stuck), pastExitWhole: !!(exit && exit.whole), pastExitLowest: exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "moreZ3=$moreZ3"
$zy3 = $moreZ3 -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYz3) -le 1
$zx3 = $moreZ3 -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXz3) -le 1
$zd3 = $moreZ3 -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$zpt3 = $false
if ($moreZ3 -match '\\"pastTop\\":(-?\d+)') { $zpt3N = [int]$Matches[1] } else { $zpt3N = -999 }
if ($moreZ3 -match '\\"pastMax\\":(-?\d+)') { $zpt3 = [Math]::Abs($zpt3N - [int]$Matches[1]) -le 1 -and [Math]::Abs($zpt3N - $pastTopZ3) -le 1 }
$zpl3 = $moreZ3 -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $pastLeftZ3) -le 1
$zrt3 = $false
if ($moreZ3 -match '\\"resultTop\\":(-?\d+)') { $zrt3N = [int]$Matches[1] } else { $zrt3N = -999 }
if ($moreZ3 -match '\\"resultMax\\":(-?\d+)') { $zrt3 = [Math]::Abs($zrt3N - [int]$Matches[1]) -le 1 -and [Math]::Abs($zrt3N - $resultTopZ3) -le 1 }
$zrows3 = $moreZ3 -match '\\"resultWhole\\":true' -and $moreZ3 -match '\\"resultNow8\\":true' -and $moreZ3 -match '\\"resultNow7\\":false' -and $moreZ3 -match '\\"resultExitSeen\\":true' -and $moreZ3 -match '\\"wideWhole\\":true' -and $moreZ3 -match '\\"inside\\":true' -and $moreZ3 -match '\\"pastExitSeen\\":true' -and $moreZ3 -match '\\"pastExitStuck\\":true' -and $moreZ3 -match '\\"pastExitWhole\\":true' -and $moreZ3 -match '\\"pastExitLowest\\":true' -and $moreZ3 -match '\\"allWhole\\":true' -and $moreZ3 -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptZ3 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, pastExitSeen: !!(exit && exit.seen), pastExitStuck: !!(exit && exit.stuck), pastExitWhole: !!(exit && exit.whole), pastExitLowest: exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptZ3=$keptZ3"
$keptPastZ3 = $false
if ($keptZ3 -match '\\"pastTop\\":(-?\d+)') { $keptPastZ3N = [int]$Matches[1] } else { $keptPastZ3N = -999 }
if ($keptZ3 -match '\\"pastMax\\":(-?\d+)') { $keptPastZ3 = [Math]::Abs($keptPastZ3N - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastZ3N - $pastTopZ3) -le 1 }
$keptGapZ3 = $keptZ3 -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftZ3 = $keptZ3 -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultZ3 = $false
if ($keptZ3 -match '\\"resultTop\\":(-?\d+)') { $keptResultZ3N = [int]$Matches[1] } else { $keptResultZ3N = -999 }
if ($keptZ3 -match '\\"resultMax\\":(-?\d+)') { $keptResultZ3 = [Math]::Abs($keptResultZ3N - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultZ3N - $resultTopZ3) -le 1 }
$keptRowsZ3 = $keptZ3 -match '\\"resultWhole\\":true' -and $keptZ3 -match '\\"resultNow8\\":true' -and $keptZ3 -match '\\"resultNow7\\":false' -and $keptZ3 -match '\\"resultExitSeen\\":true' -and $keptZ3 -match '\\"wideWhole\\":true' -and $keptZ3 -match '\\"inside\\":true' -and $keptZ3 -match '\\"pastExitSeen\\":true' -and $keptZ3 -match '\\"pastExitStuck\\":true' -and $keptZ3 -match '\\"pastExitWhole\\":true' -and $keptZ3 -match '\\"pastExitLowest\\":true' -and $keptZ3 -match '\\"allWhole\\":true' -and $keptZ3 -match '\\"barUnder\\":true' -and $keptZ3 -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageZ3 = $keptZ3 -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptZ3 -match '\\"overflow\\":\\"hidden\\"' -and $keptZ3 -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "past-after-secret"
if (-not ($zy3 -and $zx3 -and $zd3 -and $zpt3 -and $zpl3 -and $zrt3 -and $zrows3 -and $keptPastZ3 -and $keptGapZ3 -and $keptLeftZ3 -and $keptResultZ3 -and $keptRowsZ3 -and $keptPageZ3)) { Fail 491 "past-more-again-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "right-secret"
$armB4 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  const rb = box.getBoundingClientRect();
  const x = Math.round(r.left + 80);
  const y = Math.round(r.top + 8);
  const hit = document.elementFromPoint(x, y);
  const hitPast = !!(hit && hit.closest && hit.closest("#past"));
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ x, y, hitPast, resultBottom: Math.round(rb.bottom), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, formula, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armB4=$armB4"
if ($armB4 -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 492 "page-cannot-scroll" }
if ($armB4 -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 493 "page-cannot-slide" }
if ($armB4 -notmatch '\\"hitPast\\":true') { Fail 494 "wheel-missed-past" }
if ($armB4 -notmatch '\\"x\\":(\d+)') { Fail 495 "no-xb" }
$xb4 = [int]$Matches[1]
if ($armB4 -notmatch '\\"y\\":(\d+)') { Fail 495 "no-yb" }
$yb4 = [int]$Matches[1]
if ($armB4 -notmatch '\\"resultBottom\\":(-?\d+)') { Fail 496 "no-result-bottom" }
if ($yb4 -le [int]$Matches[1]) { Fail 497 "aim-still-on-result" }
if ($armB4 -notmatch '\\"pageY\\":(-?\d+)') { Fail 498 "no-page-yb" }
$pageYb4 = [int]$Matches[1]
if ($armB4 -notmatch '\\"pageX\\":(-?\d+)') { Fail 499 "no-page-xb" }
$pageXb4 = [int]$Matches[1]
if ($armB4 -notmatch '\\"pastTop\\":(-?\d+)') { Fail 500 "no-past-topb" }
$pastTopB4 = [int]$Matches[1]
if ($armB4 -notmatch '\\"pastMax\\":(-?\d+)') { Fail 501 "no-past-maxb" }
$pastMaxB4 = [int]$Matches[1]
if ([Math]::Abs($pastTopB4 - $pastMaxB4) -gt 1) { Fail 502 "past-not-at-end" }
if ($armB4 -notmatch '\\"pastLeft\\":(\d+)') { Fail 503 "no-past-leftb" }
$pastLeftB4 = [int]$Matches[1]
if ([Math]::Abs($pastLeftB4 - $endLeft) -gt 1) { Fail 504 "past-not-at-tail" }
if ($armB4 -notmatch '\\"formula\\":(\d+)') { Fail 505 "no-formula-b" }
$formulaB4 = [int]$Matches[1]
if (($formulaB4 - $pastLeftB4) -lt 10) { Fail 506 "box-not-narrowed" }
if ($armB4 -notmatch '\\"resultTop\\":(-?\d+)') { Fail 507 "no-result-topb" }
$resultTopB4 = [int]$Matches[1]
if ($armB4 -notmatch '\\"resultMax\\":(-?\d+)') { Fail 508 "no-result-maxb" }
$resultMaxB4 = [int]$Matches[1]
if ([Math]::Abs($resultTopB4 - $resultMaxB4) -gt 1) { Fail 509 "result-not-at-end" }
Send-WheelX "9335" $xb4 $yb4 400 | Out-Null
Start-Sleep -Milliseconds 450
$slideB4 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, pastExitSeen: !!(exit && exit.seen), pastExitStuck: !!(exit && exit.stuck), pastExitWhole: !!(exit && exit.whole), pastExitLowest: exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "slideB4=$slideB4"
$by4 = $slideB4 -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYb4) -le 1
$bx4 = $slideB4 -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXb4) -le 1
$bd4 = $slideB4 -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$bpt4 = $false
if ($slideB4 -match '\\"pastTop\\":(-?\d+)') { $bpt4N = [int]$Matches[1] } else { $bpt4N = -999 }
if ($slideB4 -match '\\"pastMax\\":(-?\d+)') { $bpt4 = [Math]::Abs($bpt4N - [int]$Matches[1]) -le 1 -and [Math]::Abs($bpt4N - $pastTopB4) -le 1 }
$bleft4 = $false
if ($slideB4 -match '\\"pastLeft\\":(\d+)') { $bleft4N = [int]$Matches[1] } else { $bleft4N = -999 }
$bgap4 = $false
if ($slideB4 -match '\\"gap\\":(-?\d+)') { $bgap4 = [Math]::Abs([int]$Matches[1]) -le 1 -and ($bleft4N - $pastLeftB4) -ge 10 }
$brt4 = $false
if ($slideB4 -match '\\"resultTop\\":(-?\d+)') { $brt4N = [int]$Matches[1] } else { $brt4N = -999 }
if ($slideB4 -match '\\"resultMax\\":(-?\d+)') { $brt4 = [Math]::Abs($brt4N - [int]$Matches[1]) -le 1 -and [Math]::Abs($brt4N - $resultTopB4) -le 1 }
$brows4 = $slideB4 -match '\\"resultWhole\\":true' -and $slideB4 -match '\\"resultNow8\\":true' -and $slideB4 -match '\\"resultNow7\\":false' -and $slideB4 -match '\\"resultExitSeen\\":true' -and $slideB4 -match '\\"wideWhole\\":true' -and $slideB4 -match '\\"inside\\":true' -and $slideB4 -match '\\"pastExitSeen\\":true' -and $slideB4 -match '\\"pastExitStuck\\":true' -and $slideB4 -match '\\"pastExitWhole\\":true' -and $slideB4 -match '\\"pastExitLowest\\":true' -and $slideB4 -match '\\"allWhole\\":true' -and $slideB4 -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptB4 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, pastExitSeen: !!(exit && exit.seen), pastExitStuck: !!(exit && exit.stuck), pastExitWhole: !!(exit && exit.whole), pastExitLowest: exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptB4=$keptB4"
$keptPastB4 = $false
if ($keptB4 -match '\\"pastTop\\":(-?\d+)') { $keptPastB4N = [int]$Matches[1] } else { $keptPastB4N = -999 }
if ($keptB4 -match '\\"pastMax\\":(-?\d+)') { $keptPastB4 = [Math]::Abs($keptPastB4N - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastB4N - $pastTopB4) -le 1 }
$keptGapB4 = $keptB4 -match '\\"gap\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$keptLeftB4 = $keptB4 -match '\\"pastLeft\\":(\d+)' -and [Math]::Abs([int]$Matches[1] - $endLeft) -le 1
$keptResultB4 = $false
if ($keptB4 -match '\\"resultTop\\":(-?\d+)') { $keptResultB4N = [int]$Matches[1] } else { $keptResultB4N = -999 }
if ($keptB4 -match '\\"resultMax\\":(-?\d+)') { $keptResultB4 = [Math]::Abs($keptResultB4N - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultB4N - $resultTopB4) -le 1 }
$keptRowsB4 = $keptB4 -match '\\"resultWhole\\":true' -and $keptB4 -match '\\"resultNow8\\":true' -and $keptB4 -match '\\"resultNow7\\":false' -and $keptB4 -match '\\"resultExitSeen\\":true' -and $keptB4 -match '\\"wideWhole\\":true' -and $keptB4 -match '\\"inside\\":true' -and $keptB4 -match '\\"pastExitSeen\\":true' -and $keptB4 -match '\\"pastExitStuck\\":true' -and $keptB4 -match '\\"pastExitWhole\\":true' -and $keptB4 -match '\\"pastExitLowest\\":true' -and $keptB4 -match '\\"allWhole\\":true' -and $keptB4 -match '\\"barUnder\\":true' -and $keptB4 -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageB4 = $keptB4 -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptB4 -match '\\"overflow\\":\\"hidden\\"' -and $keptB4 -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
Write-Output "right-after-secret"
if (-not ($by4 -and $bx4 -and $bd4 -and $bpt4 -and $bgap4 -and $brt4 -and $brows4 -and $keptPastB4 -and $keptGapB4 -and $keptLeftB4 -and $keptResultB4 -and $keptRowsB4 -and $keptPageB4)) { Fail 510 "right-more-again-lost" }
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "before-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-leftmoreagain-before.png") }
$armC5 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const keptPastTop = past.scrollTop;
  const keptPastLeft = past.scrollLeft;
  const keptTop = box.scrollTop;
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
  if (Math.abs(past.scrollTop - keptPastTop) > 1) past.scrollTop = keptPastTop;
  if (Math.abs(past.scrollLeft - keptPastLeft) > 1) past.scrollLeft = keptPastLeft;
  if (Math.abs(box.scrollTop - keptTop) > 1) box.scrollTop = keptTop;
  page.scrollTop = 40;
  page.scrollLeft = 40;
  const r = past.getBoundingClientRect();
  const rb = box.getBoundingClientRect();
  const x = Math.round(r.left + 80);
  const y = Math.round(r.top + 8);
  const hit = document.elementFromPoint(x, y);
  const hitPast = !!(hit && hit.closest && hit.closest("#past"));
  const formula = past.scrollWidth - past.clientWidth;
  return JSON.stringify({ x, y, hitPast, resultBottom: Math.round(rb.bottom), pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, roomX: page.scrollWidth - page.clientWidth, roomY: page.scrollHeight - page.clientHeight, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft: past.scrollLeft, formula, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight });
})()
'@
Write-Output "armC5=$armC5"
if ($armC5 -notmatch '\\"roomY\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 511 "page-cannot-scroll" }
if ($armC5 -notmatch '\\"roomX\\":(\d+)' -or [int]$Matches[1] -lt 40) { Fail 512 "page-cannot-slide" }
if ($armC5 -notmatch '\\"hitPast\\":true') { Fail 513 "wheel-missed-past" }
if ($armC5 -notmatch '\\"x\\":(\d+)') { Fail 514 "no-xc" }
$xc5 = [int]$Matches[1]
if ($armC5 -notmatch '\\"y\\":(\d+)') { Fail 514 "no-yc" }
$yc5 = [int]$Matches[1]
if ($armC5 -notmatch '\\"resultBottom\\":(-?\d+)') { Fail 515 "no-result-bottom" }
if ($yc5 -le [int]$Matches[1]) { Fail 516 "aim-still-on-result" }
if ($armC5 -notmatch '\\"pageY\\":(-?\d+)') { Fail 517 "no-page-yc" }
$pageYc5 = [int]$Matches[1]
if ($armC5 -notmatch '\\"pageX\\":(-?\d+)') { Fail 518 "no-page-xc" }
$pageXc5 = [int]$Matches[1]
if ($armC5 -notmatch '\\"pastTop\\":(-?\d+)') { Fail 519 "no-past-topc" }
$pastTopC5 = [int]$Matches[1]
if ($armC5 -notmatch '\\"pastMax\\":(-?\d+)') { Fail 520 "no-past-maxc" }
$pastMaxC5 = [int]$Matches[1]
if ([Math]::Abs($pastTopC5 - $pastMaxC5) -gt 1) { Fail 521 "past-not-at-end" }
if ($armC5 -notmatch '\\"pastLeft\\":(\d+)') { Fail 522 "no-past-leftc" }
$pastLeftC5 = [int]$Matches[1]
if ([Math]::Abs($pastLeftC5 - $endLeft) -gt 1) { Fail 523 "past-not-at-tail" }
if ($armC5 -notmatch '\\"formula\\":(\d+)') { Fail 524 "no-formula-c" }
$formulaC5 = [int]$Matches[1]
if (($formulaC5 - $pastLeftC5) -lt 10) { Fail 525 "box-not-narrowed" }
if ($armC5 -notmatch '\\"resultTop\\":(-?\d+)') { Fail 526 "no-result-topc" }
$resultTopC5 = [int]$Matches[1]
if ($armC5 -notmatch '\\"resultMax\\":(-?\d+)') { Fail 527 "no-result-maxc" }
$resultMaxC5 = [int]$Matches[1]
if ([Math]::Abs($resultTopC5 - $resultMaxC5) -gt 1) { Fail 528 "result-not-at-end" }
Send-WheelX "9335" $xc5 $yc5 (-200) | Out-Null
Start-Sleep -Milliseconds 450
$slideC5 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pageY: page.scrollTop, pageX: page.scrollLeft, docX: document.documentElement.scrollLeft, pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), wideWhole: !!(wide && wide.whole && wide.seen), end, pastExitSeen: !!(exit && exit.seen), pastExitStuck: !!(exit && exit.stuck), pastExitWhole: !!(exit && exit.whole), pastExitLowest: exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barUnder: !!(exitLowest && barY >= 10), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole })) });
})()
'@
Write-Output "slideC5=$slideC5"
$cy5 = $slideC5 -match '\\"pageY\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageYc5) -le 1
$cx5 = $slideC5 -match '\\"pageX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1] - $pageXc5) -le 1
$cd5 = $slideC5 -match '\\"docX\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
$cpt5 = $false
if ($slideC5 -match '\\"pastTop\\":(-?\d+)') { $cpt5N = [int]$Matches[1] } else { $cpt5N = -999 }
if ($slideC5 -match '\\"pastMax\\":(-?\d+)') { $cpt5 = [Math]::Abs($cpt5N - [int]$Matches[1]) -le 1 -and [Math]::Abs($cpt5N - $pastTopC5) -le 1 }
$cleft5 = $false
if ($slideC5 -match '\\"pastLeft\\":(\d+)') { $cleft5N = [int]$Matches[1] } else { $cleft5N = 99999 }
$cgap5 = $false
if ($slideC5 -match '\\"gap\\":(-?\d+)') { $cgap5 = [int]$Matches[1] -ge 40 -and $cleft5N -le ($pastLeftC5 - 40) }
$crt5 = $false
if ($slideC5 -match '\\"resultTop\\":(-?\d+)') { $crt5N = [int]$Matches[1] } else { $crt5N = -999 }
if ($slideC5 -match '\\"resultMax\\":(-?\d+)') { $crt5 = [Math]::Abs($crt5N - [int]$Matches[1]) -le 1 -and [Math]::Abs($crt5N - $resultTopC5) -le 1 }
$crows5 = $slideC5 -match '\\"resultWhole\\":true' -and $slideC5 -match '\\"resultNow8\\":true' -and $slideC5 -match '\\"resultNow7\\":false' -and $slideC5 -match '\\"resultExitSeen\\":true' -and $slideC5 -match '\\"wideWhole\\":true' -and $slideC5 -match '\\"inside\\":false' -and $slideC5 -match '\\"pastExitSeen\\":true' -and $slideC5 -match '\\"pastExitStuck\\":true' -and $slideC5 -match '\\"pastExitWhole\\":true' -and $slideC5 -match '\\"pastExitLowest\\":true' -and $slideC5 -match '\\"allWhole\\":true' -and $slideC5 -match '\\"barUnder\\":true'
Send-Eval "9335" "(() => { const page = document.getElementById('helper-scroll'); const spacer = document.getElementById('uitest-wide'); if (spacer) spacer.remove(); page.style.paddingBottom = '0px'; page.style.overflowX = 'hidden'; const extra = page.scrollHeight - page.clientHeight; page.style.overflowY = extra <= 4 ? 'hidden' : 'auto'; page.scrollTop = 0; page.scrollLeft = 0; return String(extra); })()" | Out-Null
Start-Sleep -Milliseconds 200
$keptC5 = Send-Eval "9335" @'
(() => {
  const page = document.getElementById("helper-scroll");
  const box = document.getElementById("result");
  const past = document.getElementById("past");
  const br = past.getBoundingClientRect();
  const barY = Math.max(0, past.offsetHeight - past.clientHeight);
  const barX = Math.max(0, past.offsetWidth - past.clientWidth);
  const rows = [...past.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= br.top - 1 && r.bottom <= br.bottom - barY + 1;
    const seen = r.bottom > br.top + 2 && r.top < br.bottom - barY - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, wide: el.textContent.indexOf("QUEEND") >= 0, exit: el.textContent.indexOf("exit=0") === 0, top: Math.round(r.top), bottom: Math.round(r.bottom), el };
  });
  const visible = rows.filter((row) => row.seen);
  const wide = rows.find((row) => row.wide);
  const exitRow = rows.find((row) => row.exit);
  let end = null;
  if (wide && wide.el.firstChild && wide.el.firstChild.length) {
    const text = wide.el.firstChild;
    const start = text.textContent.lastIndexOf("QUEEND");
    const range = document.createRange();
    range.setStart(text, start);
    range.setEnd(text, start + "QUEEND".length);
    const rect = range.getBoundingClientRect();
    end = { left: Math.round(rect.left), right: Math.round(rect.right), top: Math.round(rect.top), bottom: Math.round(rect.bottom), inside: rect.width > 4 && rect.top < br.bottom - barY - 2 && rect.bottom > br.top + 2 && rect.left >= br.left + 4 && rect.right <= br.right - barX - 1 };
  }
  let exit = null;
  if (exitRow && exitRow.el.firstChild) {
    const range = document.createRange();
    range.selectNodeContents(exitRow.el.firstChild);
    const rect = range.getBoundingClientRect();
    exit = { left: Math.round(rect.left), right: Math.round(rect.right), boxLeft: Math.round(br.left), h: Math.round(rect.height), top: Math.round(rect.top), bottom: Math.round(rect.bottom), stuck: rect.left >= br.left - 1 && rect.left <= br.left + 40, whole: !!exitRow.whole, seen: !!exitRow.seen };
  }
  const brb = box.getBoundingClientRect();
  const barYb = Math.max(0, box.offsetHeight - box.clientHeight);
  const resultRows = [...box.children].map((el) => {
    const r = el.getBoundingClientRect();
    const whole = r.height >= 16 && r.top >= brb.top - 1 && r.bottom <= brb.bottom - barYb + 1;
    const seen = r.bottom > brb.top + 2 && r.top < brb.bottom - barYb - 2;
    return { t: el.textContent.replace(/\s+/g, " ").trim().slice(0, 24), whole, seen, top: Math.round(r.top), bottom: Math.round(r.bottom) };
  });
  const resultSeen = resultRows.filter((row) => row.seen);
  const pastLeft = past.scrollLeft;
  const formula = past.scrollWidth - past.clientWidth;
  const exitLowest = !!(exitRow && exitRow.seen && visible.every((row) => row.bottom <= exitRow.bottom + 1));
  return JSON.stringify({ pastTop: past.scrollTop, pastMax: past.scrollHeight - past.clientHeight, pastLeft, formula, gap: formula - pastLeft, resultTop: box.scrollTop, resultMax: box.scrollHeight - box.clientHeight, resultWhole: resultSeen.length > 0 && resultSeen.every((row) => row.whole), resultNow8: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-8") === 0), resultNow7: resultRows.some((row) => row.seen && row.t.indexOf("queqiao-now-7") === 0), resultExitSeen: resultRows.some((row) => row.seen && row.t.indexOf("exit=0") === 0), resultSeen: resultSeen.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), wideWhole: !!(wide && wide.whole && wide.seen), end, exit, pastExitSeen: !!(exit && exit.seen), pastExitStuck: !!(exit && exit.stuck), pastExitWhole: !!(exit && exit.whole), pastExitLowest: exitLowest, allWhole: visible.length > 0 && visible.every((row) => row.whole), barY: Math.round(barY), barUnder: !!(exitLowest && barY >= 10), visible: visible.map((row) => ({ t: row.t, whole: row.whole, top: row.top, bottom: row.bottom })), extra: page.scrollHeight - page.clientHeight, overflow: getComputedStyle(page).overflowY, pageTop: page.scrollTop });
})()
'@
Write-Output "keptC5=$keptC5"
$keptPastC5 = $false
if ($keptC5 -match '\\"pastTop\\":(-?\d+)') { $keptPastC5N = [int]$Matches[1] } else { $keptPastC5N = -999 }
if ($keptC5 -match '\\"pastMax\\":(-?\d+)') { $keptPastC5 = [Math]::Abs($keptPastC5N - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptPastC5N - $pastTopC5) -le 1 }
$keptLeftC5 = $false
if ($keptC5 -match '\\"pastLeft\\":(\d+)') { $keptLeftC5N = [int]$Matches[1] } else { $keptLeftC5N = 99999 }
$keptGapC5 = $false
if ($keptC5 -match '\\"gap\\":(-?\d+)') { $keptGapC5 = [int]$Matches[1] -ge 40 -and $keptLeftC5N -le ($endLeft - 40) }
$keptResultC5 = $false
if ($keptC5 -match '\\"resultTop\\":(-?\d+)') { $keptResultC5N = [int]$Matches[1] } else { $keptResultC5N = -999 }
if ($keptC5 -match '\\"resultMax\\":(-?\d+)') { $keptResultC5 = [Math]::Abs($keptResultC5N - [int]$Matches[1]) -le 1 -and [Math]::Abs($keptResultC5N - $resultTopC5) -le 1 }
$keptRowsC5 = $keptC5 -match '\\"resultWhole\\":true' -and $keptC5 -match '\\"resultNow8\\":true' -and $keptC5 -match '\\"resultNow7\\":false' -and $keptC5 -match '\\"resultExitSeen\\":true' -and $keptC5 -match '\\"wideWhole\\":true' -and $keptC5 -match '\\"inside\\":false' -and $keptC5 -match '\\"pastExitSeen\\":true' -and $keptC5 -match '\\"pastExitStuck\\":true' -and $keptC5 -match '\\"pastExitWhole\\":true' -and $keptC5 -match '\\"pastExitLowest\\":true' -and $keptC5 -match '\\"allWhole\\":true' -and $keptC5 -match '\\"barUnder\\":true' -and $keptC5 -match '\\"barY\\":(\d+)' -and [int]$Matches[1] -ge 10
$keptPageC5 = $keptC5 -match '\\"extra\\":(-?\d+)' -and [int]$Matches[1] -le 4 -and $keptC5 -match '\\"overflow\\":\\"hidden\\"' -and $keptC5 -match '\\"pageTop\\":(-?\d+)' -and [Math]::Abs([int]$Matches[1]) -le 1
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "after-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-leftmoreagain-after.png") }
if (-not ($cy5 -and $cx5 -and $cd5 -and $cpt5 -and $cgap5 -and $crt5 -and $crows5 -and $keptPastC5 -and $keptGapC5 -and $keptResultC5 -and $keptRowsC5 -and $keptPageC5)) { Fail 529 "left-more-again-lost" }
Stop-Process -Id $hostp.Id,$helpp.Id -Force
$script:hostp = $null
$script:helpp = $null
Write-Output "leftmoreagain-ok"
