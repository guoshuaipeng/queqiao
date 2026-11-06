$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
$ref = [System.Drawing.Bitmap].Assembly.Location
Add-Type -ReferencedAssemblies $ref -TypeDefinition @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
public static class Cap51 {
  public delegate bool Cb(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(Cb cb, IntPtr l);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, int f);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L,T,R,B; }
  public static bool Save(int pid, string path) {
    IntPtr best = IntPtr.Zero; int bestArea = 0;
    EnumWindows((h, l) => {
      uint p; GetWindowThreadProcessId(h, out p);
      if ((int)p != pid || !IsWindowVisible(h)) return true;
      var sb = new StringBuilder(256);
      if (GetWindowText(h, sb, 256) <= 0) return true;
      RECT r; if (!GetWindowRect(h, out r)) return true;
      int area = Math.Max(0, r.R - r.L) * Math.Max(0, r.B - r.T);
      if (area > bestArea) { bestArea = area; best = h; }
      return true;
    }, IntPtr.Zero);
    if (best == IntPtr.Zero) { Console.WriteLine("NO_HWND"); return false; }
    ShowWindow(best, 9); SetForegroundWindow(best); Thread.Sleep(400);
    RECT wr; GetWindowRect(best, out wr);
    int w = wr.R - wr.L, hgt = wr.B - wr.T;
    using (var bmp = new Bitmap(w, hgt, PixelFormat.Format32bppArgb))
    using (var g = Graphics.FromImage(bmp)) {
      var hdc = g.GetHdc();
      bool ok = PrintWindow(best, hdc, 2);
      g.ReleaseHdc(hdc);
      if (!ok) g.CopyFromScreen(wr.L, wr.T, 0, 0, new Size(w, hgt), CopyPixelOperation.SourceCopy);
      bmp.Save(path, ImageFormat.Png);
    }
    Console.WriteLine("OK " + path + " " + w + "x" + hgt + " area=" + bestArea);
    return true;
  }
}
"@

$exe = "E:\IdeaProjects\remote-assist\artifacts\app-net48-fd\RemoteAssist.Net48.exe"
$out = "E:\IdeaProjects\remote-assist\docs\screenshots"
$dl = "C:\Users\user\Downloads\r043-shots"

function Shot($envVal, $name, $arg, $ms) {
  Get-Process RemoteAssist.Net48 -EA SilentlyContinue | Stop-Process -Force
  Start-Sleep -Milliseconds 400
  if ($envVal) { $env:REMOTE_ASSIST_R043_SHOT = $envVal } else { Remove-Item Env:\REMOTE_ASSIST_R043_SHOT -EA SilentlyContinue }
  $p = Start-Process -FilePath $exe -ArgumentList $arg -PassThru
  Start-Sleep -Milliseconds $ms
  [void][Cap51]::Save($p.Id, (Join-Path $out $name))
  Stop-Process -Id $p.Id -Force -EA SilentlyContinue
  Start-Sleep -Milliseconds 500
}

Shot "" "r043-net48-helper-main.png" "--control" 3500
Shot "advanced" "r043-net48-helper-advanced.png" "--control" 4200
Shot "pasted" "r043-net48-helper-pasted.png" "--control" 3500
Shot "fail" "r043-net48-helper-fail-human.png" "--control" 5000
Remove-Item Env:\REMOTE_ASSIST_R043_SHOT -EA SilentlyContinue
Shot "" "r043-net48-accept-first.png" "--accept" 5000
Copy-Item (Join-Path $out "r043-net48-accept-first.png") (Join-Path $out "r043-net48-accept-credential.png") -Force
Copy-Item (Join-Path $out "r043-net48-*.png") $dl -Force
Get-ChildItem $dl\r043-net48-*.png | ForEach-Object { $h=(Get-FileHash $_.FullName -Algorithm MD5).Hash; "$h  $($_.Name)  $($_.Length)" }