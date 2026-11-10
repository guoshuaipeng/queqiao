//! 环回：最小被控端（自动允许配对 / 可拒绝写）↔ AssistSession。

use std::net::TcpListener;
use std::sync::Arc;
use std::thread;
use std::time::Duration;

use queqiao_protocol::credential::normalize_fp;
use queqiao_protocol::session::AssistSession;
use queqiao_protocol::tools::ToolKind;
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
    let key = rcgen::KeyPair::generate().expect("key");
    let mut params =
        rcgen::CertificateParams::new(vec!["RemoteAssist.Agent".into()]).expect("params");
    params
        .distinguished_name
        .push(rcgen::DnType::CommonName, "RemoteAssist.Agent");
    params.is_ca = rcgen::IsCa::NoCa;
    params.key_usages = vec![
        rcgen::KeyUsagePurpose::DigitalSignature,
        rcgen::KeyUsagePurpose::KeyEncipherment,
    ];
    params.extended_key_usages = vec![rcgen::ExtendedKeyUsagePurpose::ServerAuth];
    let cert = params.self_signed(&key).expect("cert");
    let cert_der = cert.der().to_vec();
    let key_der = key.serialize_der();
    let fp = fingerprint_of(&cert_der);

    let provider = rustls::crypto::ring::default_provider();
    let _ = provider.install_default();
    let config = ServerConfig::builder_with_provider(Arc::new(
        rustls::crypto::ring::default_provider(),
    ))
    .with_protocol_versions(&[&rustls::version::TLS13, &rustls::version::TLS12])
    .expect("versions")
    .with_no_client_auth()
    .with_single_cert(
        vec![CertificateDer::from(cert_der)],
        PrivateKeyDer::Pkcs8(PrivatePkcs8KeyDer::from(key_der)),
    )
    .expect("server config");
    (Arc::new(config), fp)
}

fn handle_client(tcp: std::net::TcpStream, config: Arc<ServerConfig>, deny_write: bool) {
    wire::prepare(&tcp);
    let conn = ServerConnection::new(config).expect("server conn");
    let mut tls = StreamOwned::new(conn, tcp);
    let alive = || true;

    let frame = wire::read_frame(&mut tls, &alive).expect("pair frame");
    assert!(frame.kind.eq_ignore_ascii_case("pair"));
    let token = "TESTTOKEN00112233445566778899AABBCCDDEEFF";
    wire::write_frame(
        &mut tls,
        "pair_result",
        &frame.id,
        None,
        json!({
            "ok": true,
            "token": token,
            "permission": "Assist",
            "expiresAtUtc": "2099-01-01T00:00:00Z",
            "agentName": "loopback"
        }),
    )
    .expect("pair_result");

    loop {
        let frame = match wire::read_frame(&mut tls, &alive) {
            Ok(f) => f,
            Err(_) => break,
        };
        if !frame.kind.eq_ignore_ascii_case("tool") {
            continue;
        }
        let tool = frame
            .payload
            .get("tool")
            .and_then(|v| v.as_str())
            .unwrap_or("");
        if tool == "RunCommand" && deny_write {
            wire::write_frame(
                &mut tls,
                "tool_result",
                &frame.id,
                None,
                json!({
                    "ok": false,
                    "error": "已拒绝，未做任何改动",
                    "deniedByUser": true
                }),
            )
            .ok();
            continue;
        }
        let result = match tool {
            "ListProcesses" => "loopback.exe|1|1 MB",
            "RunCommand" => "ok",
            _ => "ok",
        };
        wire::write_frame(
            &mut tls,
            "tool_result",
            &frame.id,
            None,
            json!({ "ok": true, "result": result }),
        )
        .ok();
    }
}

#[test]
fn list_processes_and_denied_run_command() {
    let (config, fp) = identity();
    let listener = TcpListener::bind("127.0.0.1:0").expect("bind");
    let port = listener.local_addr().unwrap().port();
    let cfg = config.clone();
    thread::spawn(move || {
        let (tcp, _) = listener.accept().expect("accept");
        handle_client(tcp, cfg, true);
    });
    thread::sleep(Duration::from_millis(80));

    let payload = format!(
        "remoteassist:pair?v=2&via=lan&host=127.0.0.1&port={port}&code=ABC123&pwd=12345678&tls=1&fp={}",
        normalize_fp(&fp)
    );
    let mut session = AssistSession::connect(&payload, "test").expect("connect");
    let mut args = std::collections::BTreeMap::new();
    let list = session
        .invoke(ToolKind::ListProcesses, args.clone())
        .expect("list");
    assert!(list.ok, "{}", list.text);
    assert!(list.text.contains("loopback"));

    args.insert("command".into(), Value::String("echo hi".into()));
    let run = session.invoke(ToolKind::RunCommand, args).expect("run");
    assert!(!run.ok);
    assert!(run.denied_by_user);
    assert!(run.text.contains("denied by local user"));
}
