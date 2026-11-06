from pathlib import Path
Path("tools/_r043_winshot/Program.cs").write_text("""using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

class Prog {
  [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lp);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr hdc, int f);
  delegate bool EnumWindowsProc(IntPtr h, IntPtr l);
  [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }

  static IntPtr FindBest(int pid) {
    IntPtr best = IntPtr.Zero; int bestArea = 0; int seen = 0;
    EnumWindows((h, l) => {
      uint p; GetWindowThreadProcessId(h, out p);
      if ((int)p != pid) return true;
      if (!IsWindowVisible(h)) return true;
      var sb = new StringBuilder(512);
      int n = GetWindowText(h, sb, 512);
      RECT r; if (!GetWindowRect(h, out r)) return true;
      int w = r.Right - r.Left, hh = r.Bottom - r.Top;
      int area = Math.Max(0, w) * Math.Max(0, hh);
      Console.WriteLine("cand hwnd=" + h.ToInt64() + " area=" + area + " titleLen=" + n + " title=" + sb);
      seen++;
      if (area < 200*100) return true;
      if (area > bestArea) { bestArea = area; best = h; }
      return true;
    }, IntPtr.Zero);
    Console.WriteLine("seen=" + seen + " bestArea=" + bestArea);
    return best;
  }

  static int Main(string[] a) {
    int pid = int.Parse(a[0]); string path = a[1]; int wait = a.Length > 2 ? int.Parse(a[2]) : 12000;
    Console.WriteLine("capture pid=" + pid + " path=" + path);
    var deadline = Environment.TickCount + wait;
    IntPtr hwnd = IntPtr.Zero;
    while (Environment.TickCount < deadline) {
      hwnd = FindBest(pid);
      if (hwnd != IntPtr.Zero) break;
      try {
        var proc = Process.GetProcessById(pid);
        proc.Refresh();
        if (proc.MainWindowHandle != IntPtr.Zero && IsWindowVisible(proc.MainWindowHandle))
          hwnd = proc.MainWindowHandle;
      } catch {}
      if (hwnd != IntPtr.Zero) break;
      Thread.Sleep(250);
    }
    if (hwnd == IntPtr.Zero) { Console.WriteLine("NO_HWND"); return 2; }
    ShowWindow(hwnd, 9); SetForegroundWindow(hwnd); Thread.Sleep(400);
    RECT wr; GetWindowRect(hwnd, out wr);
    int w = wr.Right - wr.Left, h = wr.Bottom - wr.Top;
    using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
    using (var g = Graphics.FromImage(bmp)) {
      var hdc = g.GetHdc(); bool ok = PrintWindow(hwnd, hdc, 2); g.ReleaseHdc(hdc);
      if (!ok) g.CopyFromScreen(wr.Left, wr.Top, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);
      bmp.Save(path, ImageFormat.Png);
    }
    Console.WriteLine("OK " + path + " " + w + "x" + h + " bytes=" + new System.IO.FileInfo(path).Length);
    return 0;
  }
}
""", encoding="utf-8", newline="\n")
print("wrote")
