//! 完整 `remoteassist:pair` 解析。缺指纹或短码在联网前阻断。不记录载荷。

use crate::copy;

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct RelayTarget {
    pub host: String,
    pub port: u16,
    pub code: String,
    pub password: String,
    pub fingerprint: String,
    /// 短码。密码不发给中继，指纹在握手之后才得知。
    pub pake: bool,
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum Via {
    Relay(RelayTarget),
    /// 既有局域网载荷。协助端可直连；本客户端接受协助只走中继。
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
        let Some(host) = map.get("host").map(|h| h.trim().to_string()).filter(|h| !h.is_empty())
        else {
            return CredClass::Format;
        };
        let Some(port) = map.get("port").and_then(|p| p.parse::<u16>().ok()).filter(|p| *p >= 1)
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

pub fn user_message(class: &CredClass) -> Option<&'static str> {
    match class {
        CredClass::Ok(_) => None,
        CredClass::Format => Some(copy::FORMAT_BAD),
        CredClass::Incomplete => Some(copy::INCOMPLETE),
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

pub fn display_code(code: &str) -> String {
    let chars: Vec<char> = code.chars().collect();
    if chars.is_empty() {
        return String::new();
    }
    let mut out = String::new();
    for (i, c) in chars.iter().enumerate() {
        if i > 0 && i % 3 == 0 {
            out.push(' ');
        }
        out.push(*c);
    }
    out
}

pub fn short_credential(code: &str, password: &str) -> String {
    format!("{code}-{password}")
}

pub fn build_relay_payload(relay: &str, code: &str, password: &str, fp: &str) -> String {
    format!(
        "remoteassist:pair?v=2&via=relay&relay={}&code={}&pwd={}&tls=1&fp={}",
        url_encode(relay),
        url_encode(code),
        url_encode(password),
        url_encode(fp)
    )
}

fn parse_short(text: &str) -> Option<(String, String)> {
    if text.to_ascii_lowercase().contains("remoteassist:pair") || text.contains('?') {
        return None;
    }
    let compact: String = text.chars().filter(|c| !c.is_whitespace()).collect();
    let upper = compact.to_ascii_uppercase();
    let (code, pwd) = upper.split_once('-')?;
    let code_ok = (4..=16).contains(&code.len()) && code.chars().all(|c| c.is_ascii_alphanumeric());
    let pwd_ok = (8..=8).contains(&pwd.len()) && pwd.chars().all(|c| c.is_ascii_digit());
    if code_ok && pwd_ok {
        Some((code.to_string(), pwd.to_string()))
    } else {
        None
    }
}

fn looks_two_line_legacy(text: &str) -> bool {
    let lines: Vec<&str> = text.lines().map(str::trim).filter(|l| !l.is_empty()).collect();
    if lines.len() == 2 {
        let has_code = lines.iter().any(|l| l.contains("会话码") || l.to_ascii_lowercase().starts_with("code"));
        let has_pwd = lines.iter().any(|l| l.contains("密码") || l.to_ascii_lowercase().contains("pwd"));
        if has_code && has_pwd {
            return true;
        }
    }
    false
}

fn parse_query(q: &str) -> std::collections::BTreeMap<String, String> {
    let mut map = std::collections::BTreeMap::new();
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

fn url_encode(s: &str) -> String {
    let mut out = String::new();
    for b in s.bytes() {
        match b {
            b'A'..=b'Z' | b'a'..=b'z' | b'0'..=b'9' | b'-' | b'_' | b'.' | b'~' => out.push(b as char),
            _ => out.push_str(&format!("%{b:02X}")),
        }
    }
    out
}

fn url_decode(s: &str) -> String {
    let bytes = s.as_bytes();
    let mut out = Vec::new();
    let mut i = 0;
    while i < bytes.len() {
        if bytes[i] == b'%' && i + 2 < bytes.len() {
            if let Ok(v) = u8::from_str_radix(std::str::from_utf8(&bytes[i + 1..i + 3]).unwrap_or(""), 16) {
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

    fn sample() -> String {
        build_relay_payload("127.0.0.1:17891", "AB12CD", "12345678", &"AB".repeat(32))
    }

    #[test]
    fn full_payload_ok() {
        match classify(&sample()) {
            CredClass::Ok(Via::Relay(t)) => {
                assert_eq!(t.code, "AB12CD");
                assert_eq!(t.password, "12345678");
                assert_eq!(t.port, 17891);
                assert_eq!(t.fingerprint.len(), 64);
            }
            other => panic!("unexpected {other:?}"),
        }
    }

    #[test]
    fn short_code_is_pake_without_fingerprint() {
        match classify("AB12CD-12345678") {
            CredClass::Ok(Via::Relay(t)) => {
                assert!(t.pake);
                assert_eq!(t.code, "AB12CD");
                assert_eq!(t.password, "12345678");
                assert!(t.fingerprint.is_empty());
                assert!(t.host.is_empty());
            }
            other => panic!("unexpected {other:?}"),
        }
        assert_eq!(
            classify("会话码：AB12CD\n验证密码：12345678"),
            CredClass::Incomplete
        );
    }

    #[test]
    fn full_payload_is_not_pake() {
        match classify(&sample()) {
            CredClass::Ok(Via::Relay(t)) => assert!(!t.pake),
            other => panic!("unexpected {other:?}"),
        }
    }

    #[test]
    fn missing_fp_or_tls_off_incomplete() {
        let mut s = sample();
        s = s.replace("&fp=", "&x=");
        // rebuild without fp
        let no_fp = "remoteassist:pair?v=2&via=relay&relay=127.0.0.1:17891&code=AB12CD&pwd=12345678&tls=1";
        assert_eq!(classify(no_fp), CredClass::Incomplete);
        let tls0 = sample().replace("tls=1", "tls=0");
        assert_eq!(classify(&tls0), CredClass::Incomplete);
        let _ = s;
    }

    #[test]
    fn garbage_is_format() {
        assert_eq!(classify("hello"), CredClass::Format);
        assert_eq!(classify("remoteassist:pair"), CredClass::Format);
    }

    #[test]
    fn display_groups_by_three() {
        assert_eq!(display_code("482913075"), "482 913 075");
        assert_eq!(display_code("AB12CD"), "AB1 2CD");
    }
}
