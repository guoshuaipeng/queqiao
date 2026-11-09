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

$ran = Run-Lines $groot $hroot "打出四十行" 'for /l %i in (1,1,40) do @echo queqiao-line-%i'
if ($ran -ne "sent") { Fail 8 $ran }
$saw = $false
$deadline = (Get-Date).AddSeconds(12)
do { if (Row-Shown $groot "queqiao-line-40") { $saw = $true; break }; Start-Sleep -Milliseconds 250 } while ((Get-Date) -lt $deadline)
if (-not $saw) { Fail 9 "first-output-missing" }
$ran = Run-Lines $groot $hroot "再写一句" 'echo queqiao-now'
if ($ran -ne "sent") { Fail 10 $ran }
$now = $false
$deadline = (Get-Date).AddSeconds(12)
do { if (Row-Shown $groot "queqiao-now") { $now = $true; break }; Start-Sleep -Milliseconds 250 } while ((Get-Date) -lt $deadline)
if (-not $now) { Fail 11 "current-output-missing" }
Send-Eval "9335" "(() => { document.getElementById('summary').value = 'queqiao-task'; return 'set'; })()" | Out-Null
Start-Sleep -Milliseconds 300
$groot = [System.Windows.Automation.AutomationElement]::FromHandle($helpp.MainWindowHandle)
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 200
if (Has-Secret $groot) { Write-Output "before-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-twoline-before.png") }
$pastBtn = Find-ButtonContains $groot "之前的结果"
if (-not $pastBtn) { Fail 13 "no-past-button" }
$pastBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Start-Sleep -Milliseconds 700
$groot = [System.Windows.Automation.AutomationElement]::FromHandle($helpp.MainWindowHandle)
$win = $groot.Current.BoundingRectangle
$ops = Find-Button $groot "查看正在运行的程序"
$opsIn = Fully-In $ops $groot
$opsH = 0
if ($ops) { $opsH = [int]$ops.Current.BoundingRectangle.Height }
function Shown-Rects($root, $needle) {
  $list = @()
  foreach ($el in (All-Els $root)) {
    if ($el.Current.IsOffscreen) { continue }
    $text = $el.Current.Name
    if ($text) { $text = $text.Trim() }
    if ($text -ne $needle) { continue }
    $rect = $el.Current.BoundingRectangle
    if ($rect.Height -gt 12 -and $rect.Height -lt 30) { $list += $rect }
  }
  return $list
}
$pastBtnNow = Find-ButtonContains $groot "之前的结果"
$btnBottom = 0
if ($pastBtnNow) { $btnBottom = $pastBtnNow.Current.BoundingRectangle.Bottom }
$line40 = $false
$line40Bottom = 0
foreach ($line in (Shown-Rects $groot "queqiao-line-40")) {
  if ($line.Top -ge ($btnBottom - 4) -and $line.Bottom -le ($win.Bottom - 2) -and $btnBottom -gt 0) {
    $line40 = $true
    $line40Bottom = $line.Bottom
  }
}
$pastExit = $false
$exitBottom = 0
foreach ($ex in (Shown-Rects $groot "exit=0")) {
  $below = $ex.Top -ge ($line40Bottom - 6)
  $inside = $ex.Top -ge ($win.Top + 2) -and $ex.Bottom -le ($win.Bottom - 2)
  if ($line40 -and $below -and $inside) { $pastExit = $true; $exitBottom = [int]$ex.Bottom }
}
$end = Find-Button $groot "结束本次协助"
$endTop = 0
if ($end) { $endTop = [int]$end.Current.BoundingRectangle.Top }
$gap = $endTop - $exitBottom
$current = Row-Shown $groot "queqiao-now"
$task = Find-EditValue $groot "queqiao-task"
$taskIn = Fully-In $task $groot
Write-Output "opsIn=$opsIn opsH=$opsH line40=$line40 pastExit=$pastExit gap=$gap endTop=$endTop exitBottom=$exitBottom current=$current taskIn=$taskIn"
Clear-Secret $groot
Clear-Secret $hroot
Start-Sleep -Milliseconds 200
if (Has-Secret $groot) { Write-Output "after-not-saved" } else { Save $helpp.MainWindowHandle (Join-Path $shot "desktop-twoline-after.png") }
if (-not ($opsIn -and $opsH -ge 30 -and $line40 -and $pastExit -and $gap -ge 16 -and $current -and $taskIn)) { Fail 14 "two-lines-or-gap" }
Stop-Process -Id $hostp.Id,$helpp.Id -Force
$script:hostp = $null
$script:helpp = $null
Write-Output "two-ok"
