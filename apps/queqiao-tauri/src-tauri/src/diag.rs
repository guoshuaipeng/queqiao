//! 本机诊断日志：只记排障事实（版本、连接结果、重试、掉线、崩溃），
//! 不记密码、会话码、载荷或令牌。文件与协助记录同目录，界面上「打开协助记录目录」即可取。

use std::fs::{self, OpenOptions};
use std::io::Write;
use std::path::{Path, PathBuf};
use std::sync::Mutex;
use std::time::{SystemTime, UNIX_EPOCH};

use crate::settings;

const MAX_BYTES: u64 = 512 * 1024;
const KEEP: u32 = 3;

static WRITE: Mutex<()> = Mutex::new(());

pub fn path() -> PathBuf {
    settings::base_dir().join("diag.log")
}

/// 崩溃也要留下原因：release 是 panic=abort，没有这条记录就只剩「应用悄悄没了」。
pub fn init() {
    let previous = std::panic::take_hook();
    std::panic::set_hook(Box::new(move |info| {
        let place = info
            .location()
            .map(|l| format!("{}:{}", l.file(), l.line()))
            .unwrap_or_else(|| "unknown".into());
        // 崩溃线程可能正持有写锁，这里直接追加，不再等锁。
        append(&format!("panic {} {}", place, panic_text(info)));
        previous(info);
    }));
    note(
        "start",
        &format!("version={} os={}", env!("CARGO_PKG_VERSION"), std::env::consts::OS),
    );
}

pub fn note(event: &str, detail: &str) {
    let _guard = WRITE.lock().unwrap_or_else(|e| e.into_inner());
    let path = path();
    rotate_at(&path);
    append_at(&path, &format!("{event} {detail}"));
}

fn panic_text(info: &std::panic::PanicHookInfo<'_>) -> String {
    if let Some(s) = info.payload().downcast_ref::<&str>() {
        (*s).to_string()
    } else if let Some(s) = info.payload().downcast_ref::<String>() {
        s.clone()
    } else {
        "unknown".into()
    }
}

fn append(body: &str) {
    append_at(&path(), body);
}

fn append_at(path: &Path, body: &str) {
    if let Some(parent) = path.parent() {
        let _ = fs::create_dir_all(parent);
    }
    let Ok(mut file) = OpenOptions::new().create(true).append(true).open(path) else {
        return;
    };
    let detail: String = body.chars().take(400).collect();
    let _ = writeln!(file, "{} {detail}", stamp());
}

fn rotate_at(path: &Path) {
    let Ok(meta) = fs::metadata(path) else {
        return;
    };
    if meta.len() < MAX_BYTES {
        return;
    }
    for i in (1..KEEP).rev() {
        let _ = fs::rename(older(path, i), older(path, i + 1));
    }
    let _ = fs::rename(path, older(path, 1));
}

fn older(path: &Path, n: u32) -> PathBuf {
    path.with_file_name(format!("diag.{n}.log"))
}

fn stamp() -> String {
    let secs = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .map(|d| d.as_secs())
        .unwrap_or(0);
    stamp_at(secs)
}

/// 与界面审计一致用东八区；不引时间库，自己把天数换算成公历。
fn stamp_at(secs: u64) -> String {
    let local = secs + 8 * 3600;
    let (y, m, d) = civil((local / 86400) as i64);
    let rest = local % 86400;
    format!(
        "{y:04}-{m:02}-{d:02} {:02}:{:02}:{:02}",
        rest / 3600,
        (rest / 60) % 60,
        rest % 60
    )
}

fn civil(days: i64) -> (i64, u32, u32) {
    let z = days + 719468;
    let era = if z >= 0 { z } else { z - 146096 } / 146097;
    let doe = z - era * 146097;
    let yoe = (doe - doe / 1460 + doe / 36524 - doe / 146096) / 365;
    let y = yoe + era * 400;
    let doy = doe - (365 * yoe + yoe / 4 - yoe / 100);
    let mp = (5 * doy + 2) / 153;
    let d = (doy - (153 * mp + 2) / 5 + 1) as u32;
    let m = if mp < 10 { mp + 3 } else { mp - 9 } as u32;
    (if m <= 2 { y + 1 } else { y }, m, d)
}

#[cfg(test)]
mod tests {
    use super::{append_at, civil, rotate_at, stamp_at, MAX_BYTES};

    #[test]
    fn epoch_and_known_instant_render_as_local_time() {
        assert_eq!(stamp_at(0), "1970-01-01 08:00:00");
        assert_eq!(stamp_at(1_700_000_000), "2023-11-15 06:13:20");
        assert_eq!(civil(0), (1970, 1, 1));
        assert_eq!(civil(20454), (2026, 1, 1));
    }

    #[test]
    fn oversized_log_rotates_instead_of_growing() {
        let dir = std::env::temp_dir().join(format!("queqiao-diag-{}", std::process::id()));
        let _ = std::fs::remove_dir_all(&dir);
        std::fs::create_dir_all(&dir).unwrap();
        let log = dir.join("diag.log");
        let big = "x".repeat(MAX_BYTES as usize + 16);
        std::fs::write(&log, &big).unwrap();
        rotate_at(&log);
        append_at(&log, "rotate after oversize");
        let kept = std::fs::read_to_string(&log).unwrap();
        assert!(kept.contains("rotate after oversize"));
        assert!(kept.len() < big.len());
        assert_eq!(std::fs::read_to_string(dir.join("diag.1.log")).unwrap(), big);
        let _ = std::fs::remove_dir_all(&dir);
    }
}