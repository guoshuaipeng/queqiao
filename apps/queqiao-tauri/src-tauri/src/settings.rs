//! 本机设置。完全信任不能写入这个文件。

use std::fs;
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
    let Ok(text) = fs::read_to_string(path) else {
        return Settings::default();
    };
    let text = text.trim_start_matches('\u{feff}');
    let Ok(mut parsed) = serde_json::from_str::<Settings>(text) else {
        return Settings::default();
    };
    let trust = policy::trust_token(parsed.saved_trust());
    let relay_ok = parse_endpoint(&parsed.relay).is_some();
    if !relay_ok {
        parsed.relay = copy::DEFAULT_RELAY.to_string();
    }
    let dirty = parsed.trust != trust || text.contains("\"full\"");
    parsed.trust = trust.to_string();
    if dirty {
        let _ = save(path, &parsed);
    }
    parsed
}

pub fn save(path: &Path, settings: &Settings) -> Result<(), String> {
    if let Some(parent) = path.parent() {
        fs::create_dir_all(parent).map_err(|_| "没能保存设置。".to_string())?;
    }
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
    };
    if parse_endpoint(&clean.relay).is_none() {
        return Err("服务器地址不对。请填写 主机:端口。".into());
    }
    let body = serde_json::to_string_pretty(&clean).map_err(|_| "没能保存设置。".to_string())?;
    if body.contains("full") {
        return Err("没能保存设置。".into());
    }
    fs::write(path, body).map_err(|_| "没能保存设置。".to_string())
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
