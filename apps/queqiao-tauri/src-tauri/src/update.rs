//! 客户端版本检查与安装包更新。
//! 清单：`{SITE}/downloads/latest.json`（与官网安装包同目录）。

use std::fs::{self, File};
use std::io::{Read, Write};
use std::path::{Path, PathBuf};
use std::time::Duration;

use serde::Deserialize;
use sha2::{Digest, Sha256};

use crate::copy as text;

#[derive(Debug, Clone)]
pub struct Available {
    pub version: String,
    pub notes: String,
    pub url: String,
    pub name: String,
    pub sha256: String,
    pub size: u64,
}

#[derive(Debug, Deserialize)]
struct Manifest {
    /// 全局回退版本（旧清单 / 旧客户端）。新清单优先用各 platform.version。
    #[serde(default)]
    version: String,
    #[serde(default)]
    notes: String,
    #[serde(default)]
    platforms: std::collections::HashMap<String, Platform>,
}

#[derive(Debug, Deserialize)]
struct Platform {
    /// 本平台最新包版本；缺省则回退到清单顶层 version。
    #[serde(default)]
    version: String,
    url: String,
    #[serde(default)]
    name: String,
    /// 安装包 SHA256（十六进制）。缺失即拒绝自动更新。
    #[serde(default)]
    sha256: String,
    #[serde(default)]
    size: u64,
}

/// 解析本平台应比较的远端版本：platform.version > 顶层 version。
fn resolve_remote_version(manifest: &Manifest, platform: &Platform) -> Result<String, String> {
    let raw = if !platform.version.trim().is_empty() {
        platform.version.as_str()
    } else {
        manifest.version.as_str()
    };
    let remote = normalize_version(raw);
    if remote.is_empty() {
        return Err("更新信息里没有版本号。".into());
    }
    Ok(remote)
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
    let agent = ureq::AgentBuilder::new()
        .timeout_connect(Duration::from_secs(8))
        .timeout_read(Duration::from_secs(12))
        .build();
    let body = agent
        .get(&manifest_url())
        .call()
        .map_err(|_| "没能检查更新，请检查网络后重试。".to_string())?
        .into_string()
        .map_err(|_| "没能读取更新信息。".to_string())?;
    resolve(&body, key)
}

fn resolve(body: &str, key: &str) -> Result<Option<Available>, String> {
    let manifest: Manifest =
        serde_json::from_str(body.trim_start_matches('\u{feff}')).map_err(|_| {
            "更新信息不完整。请稍后再试，或到官网下载最新版。".to_string()
        })?;
    let platform = manifest
        .platforms
        .get(key)
        .ok_or_else(|| "官网还没有这一版对应你系统的安装包。".to_string())?;
    let remote = resolve_remote_version(&manifest, platform)?;
    if !is_newer(&remote, current_version()) {
        return Ok(None);
    }
    let download = platform.url.trim();
    if download.is_empty() {
        return Err("更新地址为空。".into());
    }
    if !url_allowed(download) {
        return Err("更新地址不可信，已取消。".into());
    }
    // 没有校验值就不自动装：宁可让用户去官网手动下，也不把来历不明的包直接跑起来。
    let sha256 = platform.sha256.trim().to_ascii_lowercase();
    if !is_sha256(&sha256) {
        return Err("更新包缺少校验信息，已取消自动更新。请到官网下载最新版。".into());
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
        sha256,
        size: platform.size,
    }))
}

fn is_sha256(value: &str) -> bool {
    value.len() == 64 && value.bytes().all(|b| b.is_ascii_hexdigit())
}

pub fn download_to_temp(update: &Available) -> Result<PathBuf, String> {
    if !url_allowed(&update.url) {
        return Err("更新地址不可信，已取消。".into());
    }
    if !is_sha256(&update.sha256) {
        return Err("更新包缺少校验信息，已取消自动更新。请到官网下载最新版。".into());
    }
    let dir = std::env::temp_dir().join("queqiao-update");
    fs::create_dir_all(&dir).map_err(|_| "没能准备更新目录。".to_string())?;
    let safe_name = Path::new(&update.name)
        .file_name()
        .and_then(|s| s.to_str())
        .filter(|s| !s.is_empty() && !s.contains(".."))
        .unwrap_or("Queqiao-Update.bin");
    let dest = dir.join(safe_name);
    // 先落到 .part：下载中断不会留下一个看起来完整的安装包。
    let part = dir.join(format!("{safe_name}.part"));
    let agent = ureq::AgentBuilder::new()
        .timeout_connect(Duration::from_secs(10))
        .timeout_read(Duration::from_secs(300))
        .build();
    let response = agent
        .get(&update.url)
        .call()
        .map_err(|_| "没能下载更新包，请检查网络后重试。".to_string())?;
    let mut reader = response.into_reader();
    let mut file = File::create(&part).map_err(|_| "没能写入更新包。".to_string())?;
    let mut hasher = Sha256::new();
    let mut buf = vec![0u8; 64 * 1024];
    let mut total = 0u64;
    let streamed = (|| -> Result<(), String> {
        loop {
            let n = reader
                .read(&mut buf)
                .map_err(|_| "下载更新包时中断了。".to_string())?;
            if n == 0 {
                break;
            }
            hasher.update(&buf[..n]);
            file.write_all(&buf[..n])
                .map_err(|_| "没能写入更新包。".to_string())?;
            total += n as u64;
        }
        file.flush().map_err(|_| "没能写完更新包。".to_string())?;
        file.sync_all().map_err(|_| "没能写完更新包。".to_string())
    })();
    drop(file);
    if let Err(err) = streamed {
        let _ = fs::remove_file(&part);
        return Err(err);
    }
    let digest = hex(&hasher.finalize());
    if let Err(err) = verify(update, total, &digest) {
        let _ = fs::remove_file(&part);
        crate::diag::note(
            "update verify failed",
            &format!("bytes={total} want_bytes={} sha256={digest}", update.size),
        );
        return Err(err);
    }
    fs::rename(&part, &dest).map_err(|_| "没能保存更新包。".to_string())?;
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

/// 大小或摘要对不上就不许启动安装包。
fn verify(update: &Available, total: u64, digest: &str) -> Result<(), String> {
    if update.size > 0 && total != update.size {
        return Err("更新包大小不对，已取消。请到官网重新下载。".into());
    }
    if !is_sha256(&update.sha256) || !digest.eq_ignore_ascii_case(update.sha256.trim()) {
        return Err("更新包校验没通过，已取消。请到官网重新下载。".into());
    }
    Ok(())
}

fn hex(bytes: &[u8]) -> String {
    bytes.iter().map(|b| format!("{b:02x}")).collect()
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

    const ABC_SHA256: &str = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    fn manifest(sha256: &str) -> String {
        let site = text::SITE.trim_end_matches('/');
        format!(
            r#"{{"version":"99.0.0","platforms":{{"windows-x86_64":{{"version":"99.0.0","url":"{site}/downloads/a.exe","name":"a.exe","sha256":"{sha256}","size":3}}}}}}"#
        )
    }

    fn available(sha256: &str) -> Available {
        Available {
            version: "99.0.0".into(),
            notes: String::new(),
            url: String::new(),
            name: "a.exe".into(),
            sha256: sha256.into(),
            size: 3,
        }
    }

    #[test]
    fn update_without_checksum_is_refused() {
        let err = resolve(&manifest(""), "windows-x86_64").unwrap_err();
        assert!(err.contains("校验"));
        assert!(resolve(&manifest("not-a-hash"), "windows-x86_64").is_err());
    }

    #[test]
    fn update_with_checksum_is_offered() {
        let want = "a".repeat(64);
        let avail = resolve(&manifest(&want), "windows-x86_64").unwrap().unwrap();
        assert_eq!(avail.sha256, want);
        assert_eq!(avail.size, 3);
    }

    #[test]
    fn checksum_and_size_both_gate_the_installer() {
        let avail = available(ABC_SHA256);
        assert!(verify(&avail, 3, &ABC_SHA256.to_uppercase()).is_ok());
        assert!(verify(&avail, 4, ABC_SHA256).is_err());
        assert!(verify(&avail, 3, &"0".repeat(64)).is_err());
        assert!(verify(&available(""), 3, ABC_SHA256).is_err());
    }

    #[test]
    fn sha256_of_bytes_matches_known_digest() {
        let mut hasher = Sha256::new();
        hasher.update(b"abc");
        assert_eq!(hex(&hasher.finalize()), ABC_SHA256);
    }

    #[test]
    fn newer_semver() {
        assert!(is_newer("0.2.0", "0.1.0"));
        assert!(is_newer("v0.1.1", "0.1.0"));
        assert!(!is_newer("0.1.0", "0.1.0"));
        assert!(!is_newer("0.1.0", "0.2.0"));
    }

    #[test]
    fn platform_version_overrides_top_level() {
        let manifest: Manifest = serde_json::from_str(
            r#"{
              "version": "0.9.0",
              "platforms": {
                "windows-x86_64": {
                  "version": "0.1.3",
                  "url": "http://127.0.0.1/downloads/a.exe",
                  "name": "a.exe"
                }
              }
            }"#,
        )
        .unwrap();
        let p = manifest.platforms.get("windows-x86_64").unwrap();
        assert_eq!(resolve_remote_version(&manifest, p).unwrap(), "0.1.3");
    }

    #[test]
    fn falls_back_to_top_level_version() {
        let manifest: Manifest = serde_json::from_str(
            r#"{
              "version": "0.1.2",
              "platforms": {
                "linux-x86_64": {
                  "url": "http://127.0.0.1/downloads/a.AppImage"
                }
              }
            }"#,
        )
        .unwrap();
        let p = manifest.platforms.get("linux-x86_64").unwrap();
        assert_eq!(resolve_remote_version(&manifest, p).unwrap(), "0.1.2");
    }

    #[test]
    fn site_url_gate() {
        // SITE 编译期默认本机；只校验「必须以 SITE 为前缀」的逻辑形状。
        let site = text::SITE.trim_end_matches('/');
        assert!(url_allowed(&format!("{site}/downloads/a.exe")));
        assert!(!url_allowed("https://evil.example/a.exe"));
    }
}
