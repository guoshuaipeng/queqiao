//! 启动环回被控端 + MCP 子进程，走 initialize / tools/list / tools/call。

use std::io::{Read, Write};
use std::net::TcpListener;
use std::path::PathBuf;
use std::process::{Command, Stdio};
use std::sync::Arc;
use std::thread;
use std::time::Duration;

use queqiao_protocol::credential::normalize_fp;
use queqiao_protocol::wire;
use rustls::pki_types::{CertificateDer, PrivateKeyDer, PrivatePkcs8KeyDer};
use rustls::{ServerConfig, ServerConnection, StreamOwned};
use serde_json::{json, Value};
use sha2::{Digest, Sha256};

fn fingerprint_of(der: &[u8]) -> String {
    Sha256::digest(der)
        .iter()
        .map(|b| format!("{b:02X}"))
        .collect()
}

fn identity() -> (Arc<ServerConfig>, String) {
    let key = rcgen::KeyPair::generate().unwrap();
    let mut params = rcgen::CertificateParams::new(vec!["RemoteAssist.Agent".into()]).unwrap();
    params
        .distinguished_name
        .push(rcgen::DnType::CommonName, "RemoteAssist.Agent");
    params.is_ca = rcgen::IsCa::NoCa;
    params.key_usages = vec![
        rcgen::KeyUsagePurpose::DigitalSignature,
        rcgen::KeyUsagePurpose::KeyEncipherment,
    ];
    params.extended_key_usages = vec![rcgen::ExtendedKeyUsagePurpose::ServerAuth];
    let cert = params.self_signed(&key).unwrap();
    let cert_der = cert.der().to_vec();
    let key_der = key.serialize_der();
    let fp = fingerprint_of(&cert_der);
    let provider = rustls::crypto::ring::default_provider();
    let _ = provider.install_default();
    let config = ServerConfig::builder_with_provider(Arc::new(
        rustls::crypto::ring::default_provider(),
    ))
    .with_protocol_versions(&[&rustls::version::TLS13, &rustls::version::TLS12])
    .unwrap()
    .with_no_client_auth()
    .with_single_cert(
        vec![CertificateDer::from(cert_der)],
        PrivateKeyDer::Pkcs8(PrivatePkcs8KeyDer::from(key_der)),
    )
    .unwrap();
    (Arc::new(config), fp)
}

fn host_loop(tcp: std::net::TcpStream, config: Arc<ServerConfig>) {
    wire::prepare(&tcp);
    let mut tls = StreamOwned::new(ServerConnection::new(config).unwrap(), tcp);
    let alive = || true;
    let frame = wire::read_frame(&mut tls, &alive).unwrap();
    wire::write_frame(
        &mut tls,
        "pair_result",
        &frame.id,
        None,
        json!({
            "ok": true,
            "token": "TOK",
            "permission": "Assist",
            "expiresAtUtc": "2099-01-01T00:00:00Z",
            "agentName": "h"
        }),
    )
    .unwrap();
    loop {
        let frame = match wire::read_frame(&mut tls, &alive) {
            Ok(f) => f,
            Err(_) => break,
        };
        if !frame.kind.eq_ignore_ascii_case("tool") {
            continue;
        }
        wire::write_frame(
            &mut tls,
            "tool_result",
            &frame.id,
            None,
            json!({"ok": true, "result": "stdio-smoke-ok"}),
        )
        .ok();
    }
}

fn write_msg(stdin: &mut impl Write, body: &str) {
    let b = body.as_bytes();
    write!(stdin, "Content-Length: {}\r\n\r\n", b.len()).unwrap();
    stdin.write_all(b).unwrap();
    stdin.flush().unwrap();
}

fn read_msg(stdout: &mut impl Read) -> Value {
    let mut headers = Vec::new();
    let mut line = Vec::new();
    loop {
        line.clear();
        loop {
            let mut b = [0u8; 1];
            stdout.read_exact(&mut b).unwrap();
            line.push(b[0]);
            if b[0] == b'\n' {
                break;
            }
        }
        while line.last().is_some_and(|c| *c == b'\n' || *c == b'\r') {
            line.pop();
        }
        if line.is_empty() {
            break;
        }
        headers.push(String::from_utf8_lossy(&line).into_owned());
    }
    let mut len = 0usize;
    for h in headers {
        let lower = h.to_ascii_lowercase();
        if let Some(rest) = lower.strip_prefix("content-length:") {
            len = rest.trim().parse().unwrap();
        }
    }
    let mut body = vec![0u8; len];
    stdout.read_exact(&mut body).unwrap();
    serde_json::from_slice(&body).unwrap()
}

fn mcp_exe() -> PathBuf {
    let mut p = PathBuf::from(env!("CARGO_MANIFEST_DIR"));
    p.push("../../target/debug/remote_assist_mcp.exe");
    if p.exists() {
        return p;
    }
    p = PathBuf::from(env!("CARGO_MANIFEST_DIR"));
    p.push("../../target/release/remote_assist_mcp.exe");
    p
}

#[test]
fn stdio_initialize_list_and_call() {
    let (config, fp) = identity();
    let listener = TcpListener::bind("127.0.0.1:0").unwrap();
    let port = listener.local_addr().unwrap().port();
    let cfg = config.clone();
    thread::spawn(move || {
        let (tcp, _) = listener.accept().unwrap();
        host_loop(tcp, cfg);
    });
    thread::sleep(Duration::from_millis(80));

    let payload = format!(
        "remoteassist:pair?v=2&via=lan&host=127.0.0.1&port={port}&code=ABC123&pwd=12345678&tls=1&fp={}",
        normalize_fp(&fp)
    );
    let exe = mcp_exe();
    assert!(exe.exists(), "build mcp first: {}", exe.display());

    let mut child = Command::new(&exe)
        .env("REMOTE_ASSIST_PAYLOAD", &payload)
        .stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .expect("spawn mcp");

    let mut stdin = child.stdin.take().unwrap();
    let mut stdout = child.stdout.take().unwrap();

    // give pair a moment
    thread::sleep(Duration::from_millis(200));

    write_msg(
        &mut stdin,
        &json!({
            "jsonrpc":"2.0",
            "id":1,
            "method":"initialize",
            "params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"t","version":"0"}}
        })
        .to_string(),
    );
    let init = read_msg(&mut stdout);
    assert_eq!(init["result"]["serverInfo"]["name"], "remote-assist");
    let instructions = init["result"]["instructions"].as_str().unwrap_or("");
    assert!(instructions.contains("鹊桥"), "{instructions}");
    assert!(instructions.contains("run_command"), "{instructions}");

    write_msg(
        &mut stdin,
        &json!({"jsonrpc":"2.0","id":2,"method":"tools/list"}).to_string(),
    );
    let list = read_msg(&mut stdout);
    let tools = list["result"]["tools"].as_array().unwrap();
    assert!(tools.iter().any(|t| t["name"] == "list_processes"));
    assert!(tools.iter().any(|t| t["name"] == "run_command"));
    let run = tools.iter().find(|t| t["name"] == "run_command").unwrap();
    let run_desc = run["description"].as_str().unwrap_or("");
    assert!(run_desc.contains("允许") || run_desc.contains("确认"), "{run_desc}");

    assert!(tools.iter().any(|t| t["name"] == "connect"));
    assert!(tools.iter().any(|t| t["name"] == "connection_status"));

    write_msg(
        &mut stdin,
        &json!({
            "jsonrpc":"2.0",
            "id":3,
            "method":"tools/call",
            "params":{"name":"list_processes","arguments":{}}
        })
        .to_string(),
    );
    let call = read_msg(&mut stdout);
    let text = call["result"]["content"][0]["text"].as_str().unwrap_or("");
    assert!(text.contains("stdio-smoke-ok"), "{call}");

    drop(stdin);
    let _ = child.wait();
}

#[test]
fn stdio_starts_disconnected_then_connect_tool() {
    let (config, fp) = identity();
    let listener = TcpListener::bind("127.0.0.1:0").unwrap();
    let port = listener.local_addr().unwrap().port();
    let cfg = config.clone();
    thread::spawn(move || {
        let (tcp, _) = listener.accept().unwrap();
        host_loop(tcp, cfg);
    });
    thread::sleep(Duration::from_millis(80));

    let payload = format!(
        "remoteassist:pair?v=2&via=lan&host=127.0.0.1&port={port}&code=ABC123&pwd=12345678&tls=1&fp={}",
        normalize_fp(&fp)
    );
    let exe = mcp_exe();
    assert!(exe.exists(), "build mcp first: {}", exe.display());

    // 无 PAYLOAD：应直接进入 stdio，不退出。
    let mut child = Command::new(&exe)
        .env_remove("REMOTE_ASSIST_PAYLOAD")
        .stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .expect("spawn mcp");

    let mut stdin = child.stdin.take().unwrap();
    let mut stdout = child.stdout.take().unwrap();

    write_msg(
        &mut stdin,
        &json!({
            "jsonrpc":"2.0",
            "id":1,
            "method":"initialize",
            "params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"t","version":"0"}}
        })
        .to_string(),
    );
    let init = read_msg(&mut stdout);
    assert_eq!(init["result"]["serverInfo"]["name"], "remote-assist");
    let instructions = init["result"]["instructions"].as_str().unwrap_or("");
    assert!(instructions.contains("connect"), "{instructions}");

    write_msg(
        &mut stdin,
        &json!({
            "jsonrpc":"2.0",
            "id":2,
            "method":"tools/call",
            "params":{"name":"connection_status","arguments":{}}
        })
        .to_string(),
    );
    let st = read_msg(&mut stdout);
    let st_text = st["result"]["content"][0]["text"].as_str().unwrap_or("");
    assert_eq!(st_text, "disconnected");

    write_msg(
        &mut stdin,
        &json!({
            "jsonrpc":"2.0",
            "id":3,
            "method":"tools/call",
            "params":{"name":"list_processes","arguments":{}}
        })
        .to_string(),
    );
    let before = read_msg(&mut stdout);
    assert_eq!(before["result"]["isError"], true);
    let before_text = before["result"]["content"][0]["text"].as_str().unwrap_or("");
    assert!(before_text.contains("connect"), "{before_text}");

    write_msg(
        &mut stdin,
        &json!({
            "jsonrpc":"2.0",
            "id":4,
            "method":"tools/call",
            "params":{"name":"connect","arguments":{"payload": payload}}
        })
        .to_string(),
    );
    let conn = read_msg(&mut stdout);
    assert_eq!(conn["result"]["isError"], false, "{conn}");

    write_msg(
        &mut stdin,
        &json!({
            "jsonrpc":"2.0",
            "id":5,
            "method":"tools/call",
            "params":{"name":"list_processes","arguments":{}}
        })
        .to_string(),
    );
    let call = read_msg(&mut stdout);
    let text = call["result"]["content"][0]["text"].as_str().unwrap_or("");
    assert!(text.contains("stdio-smoke-ok"), "{call}");

    drop(stdin);
    let _ = child.wait();
}
