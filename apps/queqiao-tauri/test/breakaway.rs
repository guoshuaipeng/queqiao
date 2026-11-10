use std::ffi::c_void;
use std::ptr;

#[link(name = "kernel32")]
extern "system" {
    fn CreateProcessW(
        app: *const u16,
        cmd: *mut u16,
        pa: *mut c_void,
        ta: *mut c_void,
        inherit: i32,
        flags: u32,
        env: *mut c_void,
        dir: *const u16,
        si: *mut StartInfo,
        pi: *mut ProcInfo,
    ) -> i32;
    fn GetLastError() -> u32;
    fn WaitForSingleObject(h: *mut c_void, ms: u32) -> u32;
    fn CloseHandle(h: *mut c_void) -> i32;
    fn OpenProcess(access: u32, inherit: i32, pid: u32) -> *mut c_void;
    fn IsProcessInJob(proc: *mut c_void, job: *mut c_void, result: *mut i32) -> i32;
}

#[repr(C)]
struct StartInfo {
    cb: u32,
    reserved: *mut u16,
    desktop: *mut u16,
    title: *mut u16,
    x: u32,
    y: u32,
    w: u32,
    h: u32,
    xcount: u32,
    ycount: u32,
    fill: u32,
    flags: u32,
    show: u16,
    reserved2: u16,
    reserved3: *mut c_void,
    stdin: *mut c_void,
    stdout: *mut c_void,
    stderr: *mut c_void,
}

#[repr(C)]
struct ProcInfo {
    process: *mut c_void,
    thread: *mut c_void,
    pid: u32,
    tid: u32,
}

fn wide(s: &str) -> Vec<u16> {
    s.encode_utf16().chain(std::iter::once(0)).collect()
}

fn main() {
    let script = std::path::Path::new(file!()).with_file_name("clip-probe.ps1");
    let mut cmd = wide(&format!(
        r#"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File "{}""#,
        script.display()
    ));
    let mut si: StartInfo = unsafe { std::mem::zeroed() };
    si.cb = std::mem::size_of::<StartInfo>() as u32;
    let mut pi: ProcInfo = unsafe { std::mem::zeroed() };
    let ok = unsafe {
        CreateProcessW(
            ptr::null(),
            cmd.as_mut_ptr(),
            ptr::null_mut(),
            ptr::null_mut(),
            0,
            0x0100_0000,
            ptr::null_mut(),
            ptr::null(),
            &mut si,
            &mut pi,
        )
    };
    let err = unsafe { GetLastError() };
    println!("create={ok} err={err} pid={}", pi.pid);
    if ok == 0 {
        return;
    }
    let handle = unsafe { OpenProcess(0x1000, 0, pi.pid) };
    let mut in_job = 1i32;
    if !handle.is_null() {
        unsafe { IsProcessInJob(handle, ptr::null_mut(), &mut in_job) };
        unsafe { CloseHandle(handle) };
    }
    println!("childInJob={in_job}");
    unsafe { WaitForSingleObject(pi.process, 8000) };
    unsafe {
        CloseHandle(pi.process);
        CloseHandle(pi.thread);
    }
}
