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
if (Has-Secret $groot) { Write-Output "before-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-pastqend-before.png") }
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
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 150
if (Has-Secret $groot) { Write-Output "after-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-pastqend-after.png") }
if (-not ($qy -and $qx -and $qd -and $qpt -and $qpl -and $qrt -and $qrows -and $shownPast -and $shownGap -and $shownLeft -and $shownResult -and $shownRows -and $shownPage)) { Fail 96 "past-qend-lost" }
Stop-Process -Id $hostp.Id,$helpp.Id -Force
$script:hostp = $null
$script:helpp = $null
Write-Output "pastqend-ok"
