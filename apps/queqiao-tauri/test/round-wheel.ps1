$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient, System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class WheelUi {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern uint SendInput(uint n, INPUT[] inputs, int cb);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, IntPtr cls, IntPtr name);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h, System.Text.StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
  [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT p);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
  [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool attach);
  [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, int data, System.UIntPtr extra);
  public struct RECT { public int Left, Top, Right, Bottom; }
  public struct POINT { public int X, Y; }
  [StructLayout(LayoutKind.Sequential)]
  public struct MOUSEINPUT {
    public int dx, dy;
    public uint mouseData, dwFlags, time;
    public IntPtr dwExtraInfo;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct INPUT {
    public uint type;
    public MOUSEINPUT mi;
  }
}
"@
[void][WheelUi]::SetProcessDPIAware()
function Save([IntPtr]$hwnd, [string]$out) {
  $rect = New-Object WheelUi+RECT
  [void][WheelUi]::GetWindowRect($hwnd, [ref]$rect)
  $bmp = New-Object System.Drawing.Bitmap ($rect.Right - $rect.Left), ($rect.Bottom - $rect.Top)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc(); [void][WheelUi]::PrintWindow($hwnd, $hdc, 2); $g.ReleaseHdc($hdc)
  $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  Write-Output "saved $out"
}
function Set-Client([IntPtr]$hwnd, [int]$w, [int]$h) {
  $wr = New-Object WheelUi+RECT
  $cr = New-Object WheelUi+RECT
  [void][WheelUi]::GetWindowRect($hwnd, [ref]$wr)
  [void][WheelUi]::GetClientRect($hwnd, [ref]$cr)
  $dx = ($wr.Right - $wr.Left) - ($cr.Right - $cr.Left)
  $dy = ($wr.Bottom - $wr.Top) - ($cr.Bottom - $cr.Top)
  [void][WheelUi]::SetWindowPos($hwnd, [IntPtr]::Zero, 40, 40, $w + $dx, $h + $dy, 0x0004)
}
function Client-Box([IntPtr]$hwnd) {
  $cr = New-Object WheelUi+RECT
  [void][WheelUi]::GetClientRect($hwnd, [ref]$cr)
  $origin = New-Object WheelUi+POINT
  [void][WheelUi]::ClientToScreen($hwnd, [ref]$origin)
  return @{ Left = $origin.X; Top = $origin.Y; Right = $origin.X + $cr.Right; Bottom = $origin.Y + $cr.Bottom; W = $cr.Right; H = $cr.Bottom }
}
function Button-Box($root) {
  $cond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty, "连接")
  $el = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
  if (-not $el) { return $null }
  $b = $el.Current.BoundingRectangle
  return @{ Top = [int]$b.Y; Bottom = [int]($b.Y + $b.Height); Left = [int]$b.X; Height = [int]$b.Height; El = $el }
}
function Focus-Window([IntPtr]$hwnd) {
  $forePid = 0
  $targetPid = 0
  $fore = [WheelUi]::GetForegroundWindow()
  $foreTid = [WheelUi]::GetWindowThreadProcessId($fore, [ref]$forePid)
  $targetTid = [WheelUi]::GetWindowThreadProcessId($hwnd, [ref]$targetPid)
  $mine = [WheelUi]::GetCurrentThreadId()
  [void][WheelUi]::AttachThreadInput($mine, $foreTid, $true)
  [void][WheelUi]::AttachThreadInput($mine, $targetTid, $true)
  [void][WheelUi]::ShowWindow($hwnd, 9)
  [void][WheelUi]::SetForegroundWindow($hwnd)
  [void][WheelUi]::AttachThreadInput($mine, $foreTid, $false)
  [void][WheelUi]::AttachThreadInput($mine, $targetTid, $false)
}
function Send-Wheel([IntPtr]$hwnd, [int]$x, [int]$y) {
  $script:render = [IntPtr]::Zero
  function Walk([IntPtr]$parent, [int]$depth) {
    if ($depth -gt 5) { return }
    $child = [IntPtr]::Zero
    while ($true) {
      $child = [WheelUi]::FindWindowEx($parent, $child, [IntPtr]::Zero, [IntPtr]::Zero)
      if ($child -eq [IntPtr]::Zero) { break }
      $sb = New-Object System.Text.StringBuilder 128
      [void][WheelUi]::GetClassName($child, $sb, 128)
      if ($sb.ToString() -eq "Chrome_RenderWidgetHostHWND") { $script:render = $child }
      Walk $child ($depth + 1)
    }
  }
  Walk $hwnd 0
  $target = $script:render
  if ($target -eq [IntPtr]::Zero) { $target = $hwnd }
  $delta = -120
  $wparam = [IntPtr]::new([int]($delta -shl 16))
  $move = [IntPtr]::new([int]((180 -shl 16) -bor 180))
  $wheel = [IntPtr]::new([int]((($y -band 0xFFFF) -shl 16) -bor ($x -band 0xFFFF)))
  [void][WheelUi]::PostMessage($target, 0x0200, [IntPtr]::Zero, $move)
  [void][WheelUi]::PostMessage($target, 0x020A, $wparam, $wheel)
  [void][WheelUi]::mouse_event(0x0800, 0, 0, $delta, [UIntPtr]::Zero)
  return "render=$target"
}
$exe = "E:\IdeaProjects\remote-assist-wt-tauri\apps\queqiao-tauri\src-tauri\target\release\queqiao-tauri.exe"
$dataHome = Join-Path $env:TEMP "queqiao-wheel-home"
New-Item -ItemType Directory -Force -Path $dataHome | Out-Null
[System.IO.File]::WriteAllText((Join-Path $dataHome "settings.json"), '{"relay":"127.0.0.1:17991","trust":"ask"}')
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $exe
$psi.UseShellExecute = $false
$psi.EnvironmentVariables["QUEQIAO_HOME"] = $dataHome
$psi.EnvironmentVariables["WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS"] = "--remote-debugging-port=9333"
$proc = [System.Diagnostics.Process]::Start($psi)
$deadline = (Get-Date).AddSeconds(12)
do { Start-Sleep -Milliseconds 200; $proc.Refresh() } while ($proc.MainWindowHandle -eq [IntPtr]::Zero -and (Get-Date) -lt $deadline)
[void][WheelUi]::ShowWindow($proc.MainWindowHandle, 9)
Set-Client $proc.MainWindowHandle 640 360
[void][WheelUi]::SetWindowPos($proc.MainWindowHandle, [IntPtr]::new(-1), 0, 0, 0, 0, 0x0001 -bor 0x0002)
Start-Sleep -Milliseconds 600
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
$client = Client-Box $proc.MainWindowHandle
$before = Button-Box $root
$shot = "E:\IdeaProjects\remote-assist-wt-tauri\docs\tauri-migration\acceptance"
if (-not $before) { Write-Output "no-connect"; Stop-Process -Id $proc.Id -Force; exit 1 }
Save $proc.MainWindowHandle (Join-Path $shot "desktop-narrow-before-wheel.png")
$hiddenBefore = $before.Bottom -gt $client.Bottom
Write-Output ("client=$($client.W)x$($client.H) beforeTop=$($before.Top) beforeBottom=$($before.Bottom) clientBottom=$($client.Bottom) hiddenBefore=$hiddenBefore")
[void][WheelUi]::SetForegroundWindow($proc.MainWindowHandle)
Focus-Window $proc.MainWindowHandle
$cx = [int](($client.Left + $client.Right) / 2)
$cy = [int](($client.Top + $client.Bottom) / 2)
[void][WheelUi]::SetCursorPos($cx, $cy)
$cursor = New-Object WheelUi+POINT
[void][WheelUi]::GetCursorPos([ref]$cursor)
$under = [WheelUi]::WindowFromPoint($cursor)
$fore = [WheelUi]::GetForegroundWindow()
Write-Output ("cursor=$($cursor.X),$($cursor.Y) under=$under fore=$fore ours=$($proc.MainWindowHandle)")
$underName = New-Object System.Text.StringBuilder 128
[void][WheelUi]::GetClassName($under, $underName, 128)
$ur = New-Object WheelUi+RECT
$or = New-Object WheelUi+RECT
[void][WheelUi]::GetWindowRect($under, [ref]$ur)
[void][WheelUi]::GetWindowRect($proc.MainWindowHandle, [ref]$or)
Write-Output ("underClass=$underName underRect=$($ur.Left),$($ur.Top)-$($ur.Right),$($ur.Bottom) ourRect=$($or.Left),$($or.Top)-$($or.Right),$($or.Bottom)")
function Send-CdpWheel {
  $deadline = (Get-Date).AddSeconds(8)
  $page = $null
  do {
    try {
      $pages = Invoke-RestMethod "http://127.0.0.1:9333/json" -TimeoutSec 2
      $page = @($pages) | Where-Object { $_.type -eq "page" } | Select-Object -First 1
    } catch {}
    if ($page) { break }
    Start-Sleep -Milliseconds 250
  } while ((Get-Date) -lt $deadline)
  if (-not $page) { return "no-cdp" }
  $ws = [System.Net.WebSockets.ClientWebSocket]::new()
  $ct = [Threading.CancellationToken]::None
  $ws.ConnectAsync([Uri]$page.webSocketDebuggerUrl, $ct).Wait()
  $payload = '{"id":1,"method":"Input.dispatchMouseEvent","params":{"type":"mouseWheel","x":220,"y":160,"deltaX":0,"deltaY":1200}}'
  $bytes = [Text.Encoding]::UTF8.GetBytes($payload)
  $seg = [ArraySegment[byte]]::new($bytes)
  $ws.SendAsync($seg, [Net.WebSockets.WebSocketMessageType]::Text, $true, $ct).Wait()
  Start-Sleep -Milliseconds 500
  $ws.Dispose()
  return "cdp-sent"
}
$sent = Send-CdpWheel
Start-Sleep -Milliseconds 300
$client = Client-Box $proc.MainWindowHandle
$after = Button-Box $root
if (-not $after) { Write-Output "lost-connect"; Stop-Process -Id $proc.Id -Force; exit 2 }
$visible = $after.Top -ge ($client.Top - 2) -and $after.Bottom -le ($client.Bottom + 2)
$moved = $after.Top -lt ($before.Top - 20)
Write-Output ("afterTop=$($after.Top) afterBottom=$($after.Bottom) visible=$visible moved=$moved sent=$sent")
Save $proc.MainWindowHandle (Join-Path $shot "desktop-narrow-after-wheel.png")
Stop-Process -Id $proc.Id -Force
if (-not ($hiddenBefore -and $visible -and $moved)) { exit 3 }
Write-Output "wheel-ok"
