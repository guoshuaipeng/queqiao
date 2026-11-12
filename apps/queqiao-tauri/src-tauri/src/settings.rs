//! 本机设置。完全信任不能写入这个文件。

use std::fs;
use std::io::Write;
use std::path::{Path, PathBuf};

use serde::{Deserialize, Serialize};

use crate::copy;
use crate::credential::parse_endpoint;
use crate::policy::{self, Trust};

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq, Eq)]
pub struct Settings {
    pub relay: String,
    pub trust: String,
    #[serde(default)]
    pub unattended_code: String,
    #[serde(default)]
    pub unattended_password: String,
    #[serde(default)]
    pub unattended_skip_write: bool,
    #[serde(default)]
    pub account_username: String,
    #[serde(default)]
    pub account_token: String,
    #[serde(default)]
    pub device_id: String,
    #[serde(default)]
    pub device_name: String,
    #[serde(default)]
    pub account_allow: bool,
    #[serde(default)]
    pub account_unattended: bool,
    #[serde(default)]
    pub account_skip_write: bool,
    #[serde(default)]
    pub account_autostart: bool,
    /// 发现新版本时自动下载并打开安装包（默认开）。
    #[serde(default = "default_true")]
    pub auto_update: bool,
}

fn default_true() -> bool {
    true
}

impl Default for Settings {
    fn default() -> Self {
        Self {
            relay: copy::DEFAULT_RELAY.to_string(),
            trust: "ask".to_string(),
            unattended_code: String::new(),
            unattended_password: String::new(),
            unattended_skip_write: false,
            account_username: String::new(),
            account_token: String::new(),
            device_id: String::new(),
            device_name: String::new(),
            account_allow: false,
            account_unattended: false,
            account_skip_write: false,
            account_autostart: false,
            auto_update: true,
        }
    }
}

impl Settings {
    pub fn saved_trust(&self) -> Trust {
        policy::parse_saved_trust(&self.trust)
    }
}

pub fn settings_file() -> PathBuf {
    base_dir().join("settings.json")
}

pub fn history_file() -> PathBuf {
    base_dir().join("history.json")
}

pub fn base_dir() -> PathBuf {
    if let Ok(dir) = std::env::var("QUEQIAO_HOME") {
        if !dir.trim().is_empty() {
            return PathBuf::from(dir);
        }
    }
    if let Ok(appdata) = std::env::var("APPDATA") {
        if !appdata.trim().is_empty() {
            return PathBuf::from(appdata).join("queqiao");
        }
    }
    PathBuf::from(".").join("queqiao-data")
}

pub fn load(path: &Path) -> Settings {
    let raw = fs::read_to_string(path).unwrap_or_default();
    // 主文件坏了（旧版本非原子写、磁盘满）先试备份，别把长久码密码静默丢成默认值。
    let Some(mut parsed) = parse(&raw).or_else(|| {
        fs::read_to_string(backup(path)).ok().and_then(|text| parse(&text))
    }) else {
        return Settings::default();
    };
    let trust = policy::trust_token(parsed.saved_trust());
    if parse_endpoint(&parsed.relay).is_none() {
        parsed.relay = copy::DEFAULT_RELAY.to_string();
    }
    let dirty = parsed.trust != trust || raw.contains("\"full\"");
    parsed.trust = trust.to_string();
    if dirty {
        let _ = save(path, &parsed);
    }
    parsed
}

fn parse(text: &str) -> Option<Settings> {
    serde_json::from_str::<Settings>(text.trim_start_matches('\u{feff}')).ok()
}

fn backup(path: &Path) -> PathBuf {
    let name = path.file_name().map(|n| n.to_string_lossy().to_string()).unwrap_or_default();
    path.with_file_name(format!("{name}.bak"))
}

/// 临时文件 + rename：进程被强杀或正好断电时，不会留下半截 JSON。
/// 上一份好内容留成 `.bak`，供 load 兜底。
pub fn write_atomic(path: &Path, body: &str) -> std::io::Result<()> {
    if let Some(parent) = path.parent() {
        fs::create_dir_all(parent)?;
    }
    if fs::metadata(path).map(|m| m.len() > 0).unwrap_or(false) {
        let _ = fs::copy(path, backup(path));
    }
    static SEQ: std::sync::atomic::AtomicU64 = std::sync::atomic::AtomicU64::new(0);
    let n = SEQ.fetch_add(1, std::sync::atomic::Ordering::Relaxed);
    let tmp = path.with_file_name(format!(
        "{}.{}-{n}.tmp",
        path.file_name().map(|v| v.to_string_lossy().to_string()).unwrap_or_default(),
        std::process::id()
    ));
    let write = || -> std::io::Result<()> {
        let mut file = fs::File::create(&tmp)?;
        file.write_all(body.as_bytes())?;
        file.sync_all()
    };
    if let Err(err) = write() {
        let _ = fs::remove_file(&tmp);
        return Err(err);
    }
    if let Err(err) = fs::rename(&tmp, path) {
        let _ = fs::remove_file(&tmp);
        return Err(err);
    }
    Ok(())
}

pub fn save(path: &Path, settings: &Settings) -> Result<(), String> {
    let clean = Settings {
        relay: settings.relay.trim().to_string(),
        trust: policy::trust_token(settings.saved_trust()).to_string(),
        unattended_code: settings.unattended_code.trim().to_string(),
        unattended_password: settings.unattended_password.trim().to_string(),
        unattended_skip_write: settings.unattended_skip_write,
        account_username: settings.account_username.trim().to_string(),
        account_token: settings.account_token.trim().to_string(),
        device_id: settings.device_id.trim().to_string(),
        device_name: settings.device_name.trim().to_string(),
        account_allow: settings.account_allow,
        account_unattended: settings.account_unattended,
        account_skip_write: settings.account_skip_write,
        account_autostart: settings.account_autostart,
        auto_update: settings.auto_update,
    };
    if parse_endpoint(&clean.relay).is_none() {
        return Err("服务器地址不对。请填写 主机:端口。".into());
    }
    let body = serde_json::to_string_pretty(&clean).map_err(|_| "没能保存设置。".to_string())?;
    if body.contains("full") {
        return Err("没能保存设置。".into());
    }
    write_atomic(path, &body).map_err(|_| "没能保存设置。".to_string())
}

pub fn set_unattended(path: &Path, code: &str, password: &str, skip: bool) -> Result<(), String> {
    let mut current = load(path);
    current.unattended_code = code.to_string();
    current.unattended_password = password.to_string();
    current.unattended_skip_write = skip;
    save(path, &current)
}

pub fn set_unattended_code(path: &Path, code: &str) -> Result<(), String> {
    let mut current = load(path);
    current.unattended_code = code.to_string();
    save(path, &current)
}

pub fn clear_unattended(path: &Path) -> Result<(), String> {
    let mut current = load(path);
    current.unattended_code.clear();
    current.unattended_password.clear();
    current.unattended_skip_write = false;
    save(path, &current)
}

#[cfg(test)]
mod tests {
    use super::*;

    fn temp_dir(tag: &str) -> PathBuf {
        let dir = std::env::temp_dir().join(format!("queqiao-{tag}-{}", std::process::id()));
        let _ = fs::remove_dir_all(&dir);
        fs::create_dir_all(&dir).unwrap();
        dir
    }

    #[test]
    fn half_written_settings_recover_from_backup() {
        let dir = temp_dir("bak");
        let path = dir.join("settings.json");
        let mut good = Settings {
            unattended_code: "ABCD1234".into(),
            unattended_password: "12345678".into(),
            ..Settings::default()
        };
        save(&path, &good).unwrap();
        good.device_name = "改过名字".into();
        save(&path, &good).unwrap();
        fs::write(&path, "{\"relay\":\"127.0.0.1:17891\",\"unattended_pa").unwrap();
        let loaded = load(&path);
        assert_eq!(loaded.unattended_password, "12345678");
        assert_eq!(loaded.unattended_code, "ABCD1234");
        let _ = fs::remove_dir_all(&dir);
    }

    #[test]
    fn atomic_write_keeps_backup_and_no_temp_files() {
        let dir = temp_dir("atomic");
        let path = dir.join("settings.json");
        save(&path, &Settings::default()).unwrap();
        let mut next = Settings::default();
        next.device_name = "第二份".into();
        save(&path, &next).unwrap();
        assert!(load(&path).device_name == "第二份");
        assert!(dir.join("settings.json.bak").exists());
        let leftovers = fs::read_dir(&dir)
            .unwrap()
            .flatten()
            .filter(|e| e.file_name().to_string_lossy().ends_with(".tmp"))
            .count();
        assert_eq!(leftovers, 0);
        let _ = fs::remove_dir_all(&dir);
    }

    #[test]
    fn full_trust_is_rewritten_to_ask() {
        let dir = std::env::temp_dir().join(format!("queqiao-set-{}", std::process::id()));
        let _ = fs::remove_dir_all(&dir);
        fs::create_dir_all(&dir).unwrap();
        let path = dir.join("settings.json");
        fs::write(&path, r#"{"relay":"127.0.0.1:17891","trust":"full"}"#).unwrap();
        let loaded = load(&path);
        assert_eq!(loaded.trust, "ask");
        let raw = fs::read_to_string(&path).unwrap();
        assert!(!raw.contains("full"));
        assert!(raw.contains("ask"));
        let _ = fs::remove_dir_all(&dir);
    }
}
