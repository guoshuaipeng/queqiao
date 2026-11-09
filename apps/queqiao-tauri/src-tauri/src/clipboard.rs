//! Windows 剪贴板。用本窗口句柄打开，避免空句柄被拒绝。
//! 成功写入必须在关闭前读回；OpenClipboard 失败不得当成已写入。

#[cfg(windows)]
use std::ffi::c_void;
#[cfg(windows)]
use std::thread;
#[cfg(windows)]
use std::time::Duration;

use crate::copy;

#[cfg(windows)]
pub fn set(owner: isize, value: &str) -> Result<(), String> {
    #[link(name = "user32")]
    extern "system" {
        fn OpenClipboard(hwnd: *mut c_void) -> i32;
        fn EmptyClipboard() -> i32;
        fn SetClipboardData(format: u32, mem: *mut c_void) -> *mut c_void;
        fn GetClipboardData(format: u32) -> *mut c_void;
        fn CloseClipboard() -> i32;
    }
    #[link(name = "kernel32")]
    extern "system" {
        fn GlobalAlloc(flags: u32, bytes: usize) -> *mut c_void;
        fn GlobalLock(mem: *mut c_void) -> *mut c_void;
        fn GlobalUnlock(mem: *mut c_void) -> i32;
        fn GlobalFree(mem: *mut c_void) -> *mut c_void;
        fn GetLastError() -> u32;
    }
    const GMEM_MOVEABLE: u32 = 0x0002;
    const CF_UNICODETEXT: u32 = 13;
    let wide: Vec<u16> = value.encode_utf16().chain(std::iter::once(0)).collect();
    let owners = [owner, 0isize];
    let mut last_err = 0u32;
    unsafe {
        for hwnd in owners {
            for _ in 0..12 {
                if OpenClipboard(hwnd as *mut c_void) != 0 {
                    EmptyClipboard();
                    let mem = GlobalAlloc(GMEM_MOVEABLE, wide.len() * 2);
                    if mem.is_null() {
                        CloseClipboard();
                        return Err(copy::COPY_FAIL.into());
                    }
                    let locked = GlobalLock(mem);
                    if locked.is_null() {
                        GlobalFree(mem);
                        CloseClipboard();
                        return Err(copy::COPY_FAIL.into());
                    }
                    std::ptr::copy_nonoverlapping(wide.as_ptr(), locked as *mut u16, wide.len());
                    GlobalUnlock(mem);
                    let set = SetClipboardData(CF_UNICODETEXT, mem);
                    if set.is_null() {
                        GlobalFree(mem);
                        CloseClipboard();
                        return Err(copy::COPY_FAIL.into());
                    }
                    let mut matched = false;
                    let check = GetClipboardData(CF_UNICODETEXT);
                    if !check.is_null() {
                        let locked = GlobalLock(check);
                        if !locked.is_null() {
                            matched = true;
                            for (i, ch) in wide.iter().enumerate() {
                                if *((locked as *const u16).add(i)) != *ch {
                                    matched = false;
                                    break;
                                }
                            }
                            GlobalUnlock(check);
                        }
                    }
                    CloseClipboard();
                    if matched {
                        return Ok(());
                    }
                    return Err(copy::COPY_FAIL.into());
                }
                last_err = GetLastError();
                let _ = last_err;
                thread::sleep(Duration::from_millis(25));
            }
        }
    }
    let _ = last_err;
    Err(copy::COPY_FAIL.into())
}

/// 探测 OpenClipboard 是否可用。不写入、不读凭证。返回 (opened, last_error)。
#[cfg(windows)]
#[allow(dead_code)]
pub fn probe_open(owner: isize) -> (bool, u32) {
    #[link(name = "user32")]
    extern "system" {
        fn OpenClipboard(hwnd: *mut c_void) -> i32;
        fn CloseClipboard() -> i32;
    }
    #[link(name = "kernel32")]
    extern "system" {
        fn GetLastError() -> u32;
    }
    unsafe {
        for hwnd in [owner, 0isize] {
            if OpenClipboard(hwnd as *mut c_void) != 0 {
                CloseClipboard();
                return (true, 0);
            }
            let err = GetLastError();
            if err != 0 {
                return (false, err);
            }
        }
    }
    (false, 5)
}

#[cfg(windows)]
pub fn get(owner: isize) -> Result<String, String> {
    #[link(name = "user32")]
    extern "system" {
        fn OpenClipboard(hwnd: *mut c_void) -> i32;
        fn GetClipboardData(format: u32) -> *mut c_void;
        fn CloseClipboard() -> i32;
    }
    #[link(name = "kernel32")]
    extern "system" {
        fn GlobalLock(mem: *mut c_void) -> *mut c_void;
        fn GlobalUnlock(mem: *mut c_void) -> i32;
    }
    const CF_UNICODETEXT: u32 = 13;
    unsafe {
        for _ in 0..15 {
            if OpenClipboard(owner as *mut c_void) != 0 {
                let mem = GetClipboardData(CF_UNICODETEXT);
                if mem.is_null() {
                    CloseClipboard();
                    return Err("剪贴板里没有文本。".into());
                }
                let locked = GlobalLock(mem);
                if locked.is_null() {
                    CloseClipboard();
                    return Err("没能读取剪贴板。".into());
                }
                let mut len = 0usize;
                while len < 1_000_000 && *((locked as *const u16).add(len)) != 0 {
                    len += 1;
                }
                let slice = std::slice::from_raw_parts(locked as *const u16, len);
                let text = String::from_utf16_lossy(slice);
                GlobalUnlock(mem);
                CloseClipboard();
                return Ok(text);
            }
            thread::sleep(Duration::from_millis(20));
        }
    }
    Err("没能读取剪贴板。".into())
}

#[cfg(not(windows))]
pub fn set(_owner: isize, value: &str) -> Result<(), String> {
    if feed("wl-copy", &[], value).is_ok() || feed("xclip", &["-selection", "clipboard"], value).is_ok() {
        Ok(())
    } else {
        Err(copy::COPY_FAIL.into())
    }
}

#[cfg(not(windows))]
pub fn get(_owner: isize) -> Result<String, String> {
    if let Ok(text) = read_out("wl-paste", &["-n"]) {
        return Ok(text);
    }
    if let Ok(text) = read_out("xclip", &["-selection", "clipboard", "-o"]) {
        return Ok(text);
    }
    Err("没能读取剪贴板。".into())
}

#[cfg(not(windows))]
pub fn probe_open(_owner: isize) -> (bool, u32) {
    (false, 5)
}

#[cfg(not(windows))]
fn feed(cmd: &str, args: &[&str], value: &str) -> Result<(), ()> {
    use std::io::Write;
    use std::process::{Command, Stdio};
    let mut child = Command::new(cmd)
        .args(args)
        .stdin(Stdio::piped())
        .stdout(Stdio::null())
        .stderr(Stdio::null())
        .spawn()
        .map_err(|_| ())?;
    let mut stdin = child.stdin.take().ok_or(())?;
    stdin.write_all(value.as_bytes()).map_err(|_| ())?;
    drop(stdin);
    let status = child.wait().map_err(|_| ())?;
    if status.success() { Ok(()) } else { Err(()) }
}

#[cfg(not(windows))]
fn read_out(cmd: &str, args: &[&str]) -> Result<String, ()> {
    use std::process::{Command, Stdio};
    let out = Command::new(cmd)
        .args(args)
        .stdin(Stdio::null())
        .stdout(Stdio::piped())
        .stderr(Stdio::null())
        .output()
        .map_err(|_| ())?;
    if !out.status.success() {
        return Err(());
    }
    String::from_utf8(out.stdout).map_err(|_| ())
}

#[cfg(test)]
mod tests {
    use super::{get, probe_open, set};
    use crate::copy;

    #[test]
    fn set_without_window_does_not_panic() {
        let _ = set(0, "queqiao-probe-copy");
    }

    #[test]
    fn set_roundtrips_when_clipboard_opens_otherwise_reports_failure() {
        match set(0, "queqiao-probe-copy") {
            Ok(()) => {
                let got = get(0).expect("a successful write can be read back");
                assert_eq!(got, "queqiao-probe-copy");
            }
            Err(err) => assert_eq!(err, copy::COPY_FAIL),
        }
    }

    #[test]
    fn probe_open_never_claims_success_when_access_denied() {
        let (opened, err) = probe_open(0);
        if !opened && err == 5 {
            assert!(set(0, "queqiao-probe-copy").is_err());
        }
    }
}