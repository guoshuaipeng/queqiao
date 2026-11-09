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
$now = Run-Lines $groot $hroot "再写一句" "echo queqiao-now"
if ($now -ne "sent") { Fail 10 $now }
$nowSeen = $false
$deadline = (Get-Date).AddSeconds(12)
do {
  foreach ($el in (All-Els $groot)) {
    try { if ($el.Current.Name -and $el.Current.Name.Trim() -eq "queqiao-now") { $nowSeen = $true; break } } catch {}
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
if (Has-Secret $groot) { Write-Output "before-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-pastold6-before.png") }
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
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "after-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-pastold6-after.png") }
if (-not ($old6Ok -and $old6Bar -and $old6Gap)) { Fail 41 "old6-lost" }
Stop-Process -Id $hostp.Id,$helpp.Id -Force
$script:hostp = $null
$script:helpp = $null
Write-Output "pastold6-ok"
