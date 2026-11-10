//! 完整 `remoteassist:pair` 与短码 `CODE-########` 解析。
//! 短码走 PAKE（`pake: true`），需另给中继地址；完整载荷须含 `fp=`。

use std::collections::BTreeMap;

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct RelayTarget {
    pub host: String,
    pub port: u16,
    pub code: String,
    pub password: String,
    pub fingerprint: String,
    /// 短码：密码不发给中继，指纹在 SPAKE2 握手后得到。
    pub pake: bool,
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum Via {
    Relay(RelayTarget),
    Lan {
        host: String,
        port: u16,
        code: String,
        fingerprint: String,
    },
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum CredClass {
    Ok(Via),
    Format,
    Incomplete,
}

pub fn classify(raw: &str) -> CredClass {
    let text = raw.trim();
    if text.is_empty() {
        return CredClass::Format;
    }
    if let Some((code, password)) = parse_short(text) {
        return CredClass::Ok(Via::Relay(RelayTarget {
            host: String::new(),
            port: 0,
            code,
            password,
            fingerprint: String::new(),
            pake: true,
        }));
    }
    if looks_two_line_legacy(text) {
        return CredClass::Incomplete;
    }
    if !text.to_ascii_lowercase().starts_with("remoteassist:pair") {
        return CredClass::Format;
    }
    let Some(q) = text.find('?') else {
        return CredClass::Format;
    };
    if q + 1 >= text.len() {
        return CredClass::Format;
    }
    let map = parse_query(&text[q + 1..]);
    let Some(code_raw) = map.get("code") else {
        return CredClass::Format;
    };
    let code = normalize_code(code_raw);
    if code.len() < 4 || code.len() > 16 {
        return CredClass::Format;
    }
    let password = map
        .get("pwd")
        .map(|p| normalize_secret(p))
        .filter(|p| p.len() >= 4);
    let tls_off = map.get("tls").is_some_and(|t| {
        let t = t.trim();
        t == "0" || t.eq_ignore_ascii_case("false") || t.eq_ignore_ascii_case("off")
    });
    let fp = map
        .get("fp")
        .map(|f| normalize_fp(f))
        .filter(|f| is_sha256_hex(f));
    if tls_off || fp.is_none() || password.is_none() {
        return CredClass::Incomplete;
    }
    let fingerprint = fp.unwrap();
    let password = password.unwrap();
    let via = map.get("via").map(|v| v.trim().to_ascii_lowercase());
    let relay_key = map.get("relay").map(|s| s.trim().to_string());
    if via.as_deref() == Some("relay") || relay_key.is_some() {
        let Some(rel) = relay_key else {
            return CredClass::Incomplete;
        };
        let Some((host, port)) = parse_endpoint(&rel) else {
            return CredClass::Format;
        };
        return CredClass::Ok(Via::Relay(RelayTarget {
            host,
            port,
            code,
            password,
            fingerprint,
            pake: false,
        }));
    }
    if via.as_deref() == Some("lan") || map.contains_key("host") {
        let Some(host) = map
            .get("host")
            .map(|h| h.trim().to_string())
            .filter(|h| !h.is_empty())
        else {
            return CredClass::Format;
        };
        let Some(port) = map
            .get("port")
            .and_then(|p| p.parse::<u16>().ok())
            .filter(|p| *p >= 1)
        else {
            return CredClass::Format;
        };
        return CredClass::Ok(Via::Lan {
            host,
            port,
            code,
            fingerprint,
        });
    }
    CredClass::Format
}

/// `CODE-########`（8 位数字密码）。
pub fn parse_short(raw: &str) -> Option<(String, String)> {
    if raw.to_ascii_lowercase().contains("remoteassist:pair") {
        return None;
    }
    let compact: String = raw.chars().filter(|c| !c.is_whitespace()).collect();
    let upper = compact.to_ascii_uppercase();
    let (code, pwd) = upper.split_once('-')?;
    let code_ok =
        (4..=16).contains(&code.len()) && code.chars().all(|c| c.is_ascii_alphanumeric());
    let pwd_ok = pwd.len() == 8 && pwd.chars().all(|c| c.is_ascii_digit());
    if code_ok && pwd_ok {
        Some((code.to_string(), pwd.to_string()))
    } else {
        None
    }
}

pub fn normalize_code(code: &str) -> String {
    code.trim()
        .chars()
        .filter(|c| !c.is_whitespace() && *c != '-')
        .collect::<String>()
        .to_ascii_uppercase()
}

pub fn normalize_secret(password: &str) -> String {
    password
        .trim()
        .chars()
        .filter(|c| !c.is_whitespace() && *c != '-')
        .collect::<String>()
        .to_ascii_uppercase()
}

pub fn normalize_fp(fp: &str) -> String {
    fp.chars()
        .filter(|c| !matches!(c, ':' | ' ' | '-'))
        .collect::<String>()
        .to_ascii_uppercase()
}

pub fn is_sha256_hex(fp: &str) -> bool {
    fp.len() == 64 && fp.chars().all(|c| c.is_ascii_hexdigit())
}

pub fn parse_endpoint(raw: &str) -> Option<(String, u16)> {
    let raw = raw.trim();
    if raw.is_empty() || raw.contains(' ') {
        return None;
    }
    let (host, port_s) = raw.rsplit_once(':')?;
    let host = host.trim().trim_matches(['[', ']']).to_string();
    if host.is_empty() || host.contains(':') {
        return None;
    }
    let port: u16 = port_s.parse().ok()?;
    if port == 0 {
        return None;
    }
    Some((host, port))
}

pub fn default_relay() -> &'static str {
    match option_env!("QUEQIAO_DEFAULT_RELAY") {
        Some(v) => v,
        None => "127.0.0.1:17891",
    }
}

fn looks_two_line_legacy(text: &str) -> bool {
    if text.to_ascii_lowercase().contains("remoteassist:pair") {
        return false;
    }
    let lines: Vec<&str> = text
        .lines()
        .map(str::trim)
        .filter(|l| !l.is_empty())
        .collect();
    if lines.len() == 2 {
        let has_code = lines
            .iter()
            .any(|l| l.contains("会话码") || l.to_ascii_lowercase().starts_with("code"));
        let has_pwd = lines
            .iter()
            .any(|l| l.contains("密码") || l.to_ascii_lowercase().contains("pwd"));
        return has_code && has_pwd;
    }
    false
}

fn parse_query(q: &str) -> BTreeMap<String, String> {
    let mut map = BTreeMap::new();
    for part in q.split('&') {
        let Some((k, v)) = part.split_once('=') else {
            continue;
        };
        if k.is_empty() {
            continue;
        }
        map.insert(url_decode(k), url_decode(v));
    }
    map
}

fn url_decode(s: &str) -> String {
    let bytes = s.as_bytes();
    let mut out = Vec::new();
    let mut i = 0;
    while i < bytes.len() {
        if bytes[i] == b'%' && i + 2 < bytes.len() {
            if let Ok(v) =
                u8::from_str_radix(std::str::from_utf8(&bytes[i + 1..i + 3]).unwrap_or(""), 16)
            {
                out.push(v);
                i += 3;
                continue;
            }
        }
        if bytes[i] == b'+' {
            out.push(b' ');
        } else {
            out.push(bytes[i]);
        }
        i += 1;
    }
    String::from_utf8_lossy(&out).into_owned()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn full_payload_ok() {
        let s = "remoteassist:pair?v=2&via=relay&relay=127.0.0.1:17891&code=AB12CD&pwd=12345678&tls=1&fp=".to_string()
            + &"AB".repeat(32);
        match classify(&s) {
            CredClass::Ok(Via::Relay(t)) => {
                assert_eq!(t.code, "AB12CD");
                assert_eq!(t.port, 17891);
                assert_eq!(t.fingerprint.len(), 64);
                assert!(!t.pake);
            }
            other => panic!("unexpected {other:?}"),
        }
    }

    #[test]
    fn short_code_pake() {
        match classify("8CEJTF-79570185") {
            CredClass::Ok(Via::Relay(t)) => {
                assert!(t.pake);
                assert_eq!(t.code, "8CEJTF");
                assert_eq!(t.password, "79570185");
                assert!(t.host.is_empty());
                assert!(t.fingerprint.is_empty());
            }
            other => panic!("unexpected {other:?}"),
        }
    }

    #[test]
    fn missing_fp_incomplete() {
        let no_fp =
            "remoteassist:pair?v=2&via=relay&relay=127.0.0.1:17891&code=AB12CD&pwd=12345678&tls=1";
        assert_eq!(classify(no_fp), CredClass::Incomplete);
    }
}
