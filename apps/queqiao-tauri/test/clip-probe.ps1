Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class RawClip {
  [DllImport("user32.dll", SetLastError=true)] public static extern bool OpenClipboard(IntPtr h);
  [DllImport("user32.dll")] public static extern bool CloseClipboard();
  [DllImport("user32.dll")] public static extern IntPtr GetOpenClipboardWindow();
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr CreateWindowExW(uint ex, string cls, string name, uint style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);
  [DllImport("kernel32.dll")] public static extern uint GetLastError();
  [DllImport("kernel32.dll")] public static extern uint GetCurrentProcessId();
  [DllImport("kernel32.dll")] public static extern bool IsProcessInJob(IntPtr proc, IntPtr job, out bool inJob);
  [DllImport("kernel32.dll", SetLastError=true)] public static extern bool QueryInformationJobObject(IntPtr job, int cls, IntPtr info, uint len, out uint ret);
  [DllImport("user32.dll")] public static extern IntPtr GetProcessWindowStation();
  [DllImport("user32.dll")] public static extern IntPtr GetThreadDesktop(uint tid);
  [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
  [DllImport("user32.dll", CharSet=CharSet.Unicode, SetLastError=true)] public static extern bool GetUserObjectInformationW(IntPtr obj, int index, IntPtr buf, uint len, out uint need);
  public static string NameOf(IntPtr obj) {
    uint need;
    var buf = Marshal.AllocHGlobal(512);
    bool ok = GetUserObjectInformationW(obj, 2, buf, 512, out need);
    string name = ok ? Marshal.PtrToStringUni(buf) : ("err-" + Marshal.GetLastWin32Error());
    Marshal.FreeHGlobal(buf);
    return name;
  }
}
"@
$inJob = $false
[void][RawClip]::IsProcessInJob([IntPtr]::new(-1), [IntPtr]::Zero, [ref]$inJob)
$buf = [Runtime.InteropServices.Marshal]::AllocHGlobal(4)
$ret = 0
$q = [RawClip]::QueryInformationJobObject([IntPtr]::Zero, 4, $buf, 4, [ref]$ret)
$ui = if ($q) { [Runtime.InteropServices.Marshal]::ReadInt32($buf) } else { -1 }
[Runtime.InteropServices.Marshal]::FreeHGlobal($buf)
$hwnd = [RawClip]::CreateWindowExW(0, "Static", "queqiao-clip", 0, 0, 0, 0, 0, [IntPtr]::Zero, [IntPtr]::Zero, [IntPtr]::Zero, [IntPtr]::Zero)
$winErr = [RawClip]::GetLastError()
$ok = [RawClip]::OpenClipboard($hwnd)
$err = [RawClip]::GetLastError()
$holder = [RawClip]::GetOpenClipboardWindow()
if ($ok) { [void][RawClip]::CloseClipboard() }
$station = [RawClip]::NameOf([RawClip]::GetProcessWindowStation())
$desk = [RawClip]::NameOf([RawClip]::GetThreadDesktop([RawClip]::GetCurrentThreadId()))
$line = "pid=$([RawClip]::GetCurrentProcessId()) inJob=$inJob uiBits=$ui hwnd=$hwnd winErr=$winErr open=$ok err=$err holder=$holder station=$station desktop=$desk"
Set-Content -Path "$env:TEMP\queqiao-clip-probe.txt" -Value $line
Write-Output $line
