Add-Type -AssemblyName System.Windows.Forms, System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class NativeShot {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint data, System.UIntPtr extra);
  public struct RECT { public int Left, Top, Right, Bottom; }
  public struct POINT { public int X; public int Y; }
}
"@
[void][NativeShot]::SetProcessDPIAware()
$exe = "E:\IdeaProjects\remote-assist-wt-tauri\apps\queqiao-tauri\src-tauri\target\release\queqiao-tauri.exe"
$existing = Get-CimInstance Win32_Process -Filter "Name='queqiao-tauri.exe'" | Where-Object { $_.ExecutablePath -ieq $exe } | Select-Object -First 1
if ($existing) {
  $p = Get-Process -Id $existing.ProcessId
  Write-Output "reuse pid=$($p.Id)"
} else {
  $p = Start-Process -FilePath $exe -PassThru
}
$deadline = (Get-Date).AddSeconds(10)
do {
  Start-Sleep -Milliseconds 200
  $p.Refresh()
} while ($p.MainWindowHandle -eq [IntPtr]::Zero -and (Get-Date) -lt $deadline -and -not $p.HasExited)
if ($p.HasExited) { Write-Output "EXITED $($p.ExitCode)"; exit 1 }
[void][NativeShot]::ShowWindow($p.MainWindowHandle, 9)
[void][NativeShot]::SetWindowPos($p.MainWindowHandle, [IntPtr]::Zero, 40, 40, 0, 0, 0x0001 -bor 0x0004)
[void][NativeShot]::SetForegroundWindow($p.MainWindowHandle)
Start-Sleep -Milliseconds 700
function Save-Window([IntPtr]$hwnd, [string]$out) {
  $rect = New-Object NativeShot+RECT
  [void][NativeShot]::GetWindowRect($hwnd, [ref]$rect)
  $w = [Math]::Max(1, $rect.Right - $rect.Left)
  $h = [Math]::Max(1, $rect.Bottom - $rect.Top)
  $bmp = New-Object System.Drawing.Bitmap $w, $h
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc()
  $ok = [NativeShot]::PrintWindow($hwnd, $hdc, 2)
  $g.ReleaseHdc($hdc)
  $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  Write-Output "saved $out print=$ok ${w}x${h}"
}
function Click-Client([IntPtr]$hwnd, [int]$x, [int]$y) {
  $pt = New-Object NativeShot+POINT
  $pt.X = $x; $pt.Y = $y
  [void][NativeShot]::ClientToScreen($hwnd, [ref]$pt)
  [void][NativeShot]::SetCursorPos($pt.X, $pt.Y)
  Start-Sleep -Milliseconds 80
  [NativeShot]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
  [NativeShot]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
  Write-Output "click client $x,$y screen $($pt.X),$($pt.Y)"
}
$dir = "E:\IdeaProjects\remote-assist-wt-tauri\docs\tauri-migration\acceptance"
Save-Window $p.MainWindowHandle (Join-Path $dir "desktop-idle.png")
Click-Client $p.MainWindowHandle 790 24
Start-Sleep -Milliseconds 500
Save-Window $p.MainWindowHandle (Join-Path $dir "desktop-settings.png")
Click-Client $p.MainWindowHandle 150 24
Start-Sleep -Milliseconds 500
Save-Window $p.MainWindowHandle (Join-Path $dir "desktop-home-again.png")
$cr = New-Object NativeShot+RECT
[void][NativeShot]::GetClientRect($p.MainWindowHandle, [ref]$cr)
$proc = Get-CimInstance Win32_Process -Filter "ProcessId=$($p.Id)"
$item = Get-Item $exe
Write-Output "pid=$($p.Id)"
Write-Output "title=$($p.MainWindowTitle)"
Write-Output "path=$($proc.ExecutablePath)"
Write-Output "exeTime=$($item.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))"
Write-Output "exeBytes=$($item.Length)"
Write-Output "client=$($cr.Right)x$($cr.Bottom)"
Write-Output "alive=$(-not $p.HasExited)"
