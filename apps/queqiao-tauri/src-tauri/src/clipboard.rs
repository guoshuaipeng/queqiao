//! Windows 剪贴板。用本窗口句柄打开，避免空句柄被拒绝。

use std::ffi::c_void;
use std::thread;
use std::time::Duration;

use crate::copy;

pub fn set(owner: isize, value: &str) -> Result<(), String> {
    #[link(name = "user32")]
    extern "system" {
        fn OpenClipboard(hwnd: *mut c_void) -> i32;
        fn EmptyClipboard() -> i32;
        fn SetClipboardData(format: u32, mem: *mut c_void) -> *mut c_void;
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
    let owners = [0isize, owner];
    unsafe {
        for hwnd in owners {
            for _ in 0..8 {
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
                    CloseClipboard();
                    if set.is_null() {
                        return Err(copy::COPY_FAIL.into());
                    }
                    return Ok(());
                }
                let err = GetLastError();
                let _ = err;
                thread::sleep(Duration::from_millis(20));
            }
        }
    }
    Err(copy::COPY_FAIL.into())
}

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

#[cfg(test)]
mod tests {
    use super::set;

    #[test]
    fn set_without_window_does_not_panic() {
        let _ = set(0, "queqiao-probe-copy");
    }
}
