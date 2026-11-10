//! 客户端版本检查与安装包更新。
//! 清单：`{SITE}/downloads/latest.json`（与官网安装包同目录）。

use std::fs::{self, File};
use std::io::{copy, Write};
use std::path::{Path, PathBuf};
use std::time::Duration;

use serde::Deserialize;

use crate::copy as text;

#[derive(Debug, Clone)]
pub struct Available {
    pub version: String,
    pub notes: String,
    pub url: String,
    pub name: String,
}

#[derive(Debug, Deserialize)]
struct Manifest {
    version: String,
    #[serde(default)]
    notes: String,
    #[serde(default)]
    platforms: std::collections::HashMap<String, Platform>,
}

#[derive(Debug, Deserialize)]
struct Platform {
    url: String,
    #[serde(default)]
    name: String,
}

pub fn current_version() -> &'static str {
    env!("CARGO_PKG_VERSION")
}

pub fn platform_key() -> Option<&'static str> {
    #[cfg(all(target_os = "windows", target_arch = "x86_64"))]
    {
        return Some("windows-x86_64");
    }
    #[cfg(all(target_os = "macos", target_arch = "aarch64"))]
    {
        return Some("darwin-aarch64");
    }
    #[cfg(all(target_os = "macos", target_arch = "x86_64"))]
    {
        return Some("darwin-x86_64");
    }
    #[cfg(all(target_os = "linux", target_arch = "x86_64"))]
    {
        return Some("linux-x86_64");
    }
    #[allow(unreachable_code)]
    None
}

pub fn manifest_url() -> String {
    let base = text::SITE.trim_end_matches('/');
    format!("{base}/downloads/latest.json")
}

/// 拉取清单；无新版本返回 `Ok(None)`。
pub fn check() -> Result<Option<Available>, String> {
    let key = platform_key().ok_or_else(|| "当前平台暂不支持自动更新。".to_string())?;
    let url = manifest_url();
    let agent = ureq::AgentBuilder::new()
        .timeout_connect(Duration::from_secs(8))
        .timeout_read(Duration::from_secs(12))
        .build();
    let body = agent
        .get(&url)
        .call()
        .map_err(|_| "没能检查更新，请检查网络后重试。".to_string())?
        .into_string()
        .map_err(|_| "没能读取更新信息。".to_string())?;
    let manifest: Manifest =
        serde_json::from_str(body.trim_start_matches('\u{feff}')).map_err(|_| {
            "更新信息不完整。请稍后再试，或到官网下载最新版。".to_string()
        })?;
    let remote = normalize_version(&manifest.version);
    if remote.is_empty() {
        return Err("更新信息里没有版本号。".into());
    }
    if !is_newer(&remote, current_version()) {
        return Ok(None);
    }
    let platform = manifest
        .platforms
        .get(key)
        .ok_or_else(|| "官网还没有这一版对应你系统的安装包。".to_string())?;
    let download = platform.url.trim();
    if download.is_empty() {
        return Err("更新地址为空。".into());
    }
    if !url_allowed(download) {
        return Err("更新地址不可信，已取消。".into());
    }
    let name = if platform.name.trim().is_empty() {
        file_name_from_url(download).unwrap_or_else(|| format!("Queqiao-{remote}"))
    } else {
        platform.name.trim().to_string()
    };
    Ok(Some(Available {
        version: remote,
        notes: manifest.notes.trim().to_string(),
        url: download.to_string(),
        name,
    }))
}

pub fn download_to_temp(update: &Available) -> Result<PathBuf, String> {
    if !url_allowed(&update.url) {
        return Err("更新地址不可信，已取消。".into());
    }
    let dir = std::env::temp_dir().join("queqiao-update");
    fs::create_dir_all(&dir).map_err(|_| "没能准备更新目录。".to_string())?;
    let safe_name = Path::new(&update.name)
        .file_name()
        .and_then(|s| s.to_str())
        .filter(|s| !s.is_empty() && !s.contains(".."))
        .unwrap_or("Queqiao-Update.bin");
    let dest = dir.join(safe_name);
    let agent = ureq::AgentBuilder::new()
        .timeout_connect(Duration::from_secs(10))
        .timeout_read(Duration::from_secs(300))
        .build();
    let response = agent
        .get(&update.url)
        .call()
        .map_err(|_| "没能下载更新包，请检查网络后重试。".to_string())?;
    let mut reader = response.into_reader();
    let mut file = File::create(&dest).map_err(|_| "没能写入更新包。".to_string())?;
    copy(&mut reader, &mut file).map_err(|_| "下载更新包时中断了。".to_string())?;
    file.flush().map_err(|_| "没能写完更新包。".to_string())?;
    #[cfg(unix)]
    {
        use std::os::unix::fs::PermissionsExt;
        let mut perms = fs::metadata(&dest)
            .map_err(|_| "没能设置更新包权限。".to_string())?
            .permissions();
        perms.set_mode(0o755);
        fs::set_permissions(&dest, perms).map_err(|_| "没能设置更新包权限。".to_string())?;
    }
    Ok(dest)
}

/// 启动安装包；调用方应随后退出应用。
pub fn launch_installer(path: &Path) -> Result<(), String> {
    #[cfg(windows)]
    {
        std::process::Command::new(path)
            .spawn()
            .map_err(|_| "没能打开安装程序。".to_string())?;
        return Ok(());
    }
    #[cfg(target_os = "macos")]
    {
        std::process::Command::new("open")
            .arg(path)
            .spawn()
            .map_err(|_| "没能打开安装包。".to_string())?;
        return Ok(());
    }
    #[cfg(target_os = "linux")]
    {
        // AppImage：直接运行新版本；其它格式交给桌面打开。
        let is_appimage = path
            .extension()
            .and_then(|e| e.to_str())
            .map(|e| e.eq_ignore_ascii_case("AppImage"))
            .unwrap_or(false);
        if is_appimage {
            std::process::Command::new(path)
                .spawn()
                .map_err(|_| "没能启动新版本。".to_string())?;
        } else {
            std::process::Command::new("xdg-open")
                .arg(path)
                .spawn()
                .map_err(|_| "没能打开安装包。".to_string())?;
        }
        return Ok(());
    }
    #[allow(unreachable_code)]
    Err("当前平台暂不支持自动安装。".into())
}

pub fn normalize_version(raw: &str) -> String {
    raw.trim().trim_start_matches('v').trim().to_string()
}

pub fn is_newer(remote: &str, local: &str) -> bool {
    match (parse_semver(remote), parse_semver(local)) {
        (Some(r), Some(l)) => r > l,
        _ => remote != local && !remote.is_empty(),
    }
}

fn parse_semver(v: &str) -> Option<(u64, u64, u64)> {
    let clean = normalize_version(v);
    let mut parts = clean.split('.');
    let major = parts.next()?.parse().ok()?;
    let minor = parts.next().unwrap_or("0").parse().unwrap_or(0);
    let patch = parts
        .next()
        .unwrap_or("0")
        .split(|c: char| !c.is_ascii_digit())
        .next()
        .unwrap_or("0")
        .parse()
        .unwrap_or(0);
    Some((major, minor, patch))
}

fn url_allowed(url: &str) -> bool {
    let site = text::SITE.trim_end_matches('/');
    let allowed = [
        site.to_string(),
        site.replace("http://", "https://"),
        site.replace("https://", "http://"),
    ];
    allowed.iter().any(|base| url.starts_with(base) || url.starts_with(&format!("{base}/")))
}

fn file_name_from_url(url: &str) -> Option<String> {
    let path = url.split('?').next()?;
    let name = path.rsplit('/').next()?;
    if name.is_empty() {
        None
    } else {
        Some(name.to_string())
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn newer_semver() {
        assert!(is_newer("0.2.0", "0.1.0"));
        assert!(is_newer("v0.1.1", "0.1.0"));
        assert!(!is_newer("0.1.0", "0.1.0"));
        assert!(!is_newer("0.1.0", "0.2.0"));
    }

    #[test]
    fn site_url_gate() {
        // SITE 编译期默认本机；只校验「必须以 SITE 为前缀」的逻辑形状。
        let site = text::SITE.trim_end_matches('/');
        assert!(url_allowed(&format!("{site}/downloads/a.exe")));
        assert!(!url_allowed("https://evil.example/a.exe"));
    }
}
