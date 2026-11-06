from pathlib import Path
Path("tools/_r043_winshot/Program.cs").write_text("""using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

class Prog {
  [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr hdc, int f);
  [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int n);
  [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }

  static int Main(string[] a) {
    // args: hwnd path
    long hwndVal = long.Parse(a[0]);
    IntPtr hwnd = new IntPtr(hwndVal);
    string path = a[1];
    ShowWindow(hwnd, 9); SetForegroundWindow(hwnd); Thread.Sleep(350);
    RECT wr; if (!GetWindowRect(hwnd, out wr)) { Console.WriteLine("bad rect"); return 2; }
    int w = wr.Right - wr.Left, h = wr.Bottom - wr.Top;
    if (w < 50 || h < 50) { Console.WriteLine("tiny"); return 3; }
    using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
    using (var g = Graphics.FromImage(bmp)) {
      var hdc = g.GetHdc(); bool ok = PrintWindow(hwnd, hdc, 2); g.ReleaseHdc(hdc);
      if (!ok) g.CopyFromScreen(wr.Left, wr.Top, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);
      bmp.Save(path, ImageFormat.Png);
    }
    Console.WriteLine("OK " + path + " " + w + "x" + h + " len=" + new System.IO.FileInfo(path).Length);
    return 0;
  }
}
""", encoding="utf-8", newline="\n")
print("ok")
