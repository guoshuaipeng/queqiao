//! 中继控制面 + 业务帧。确认通过之前不会执行写操作。

use std::io::{Read, Write};
use std::net::{TcpStream, ToSocketAddrs};
use std::sync::mpsc::{self, Receiver};
use std::time::Duration;

use serde_json::{json, Map, Value};

use crate::copy;
use crate::credential::{self, Via};
use crate::policy::{self, Decision, Tool, Trust};
use crate::tlsutil::{self, Identity};
use crate::wire;

#[derive(Debug)]
pub enum AskKind {
    Pair,
    Write { headline: String, detail: String },
}

#[derive(Debug)]
pub enum Ev {
    Waiting { code: String, payload: String },
    HostSession,
    HelperSession,
    Audit { text: String, kind: String },
    Status(String),
    Result(String),
    Fail(String),
    PeerEnded,
    Busy(bool),
}

pub trait Hooks: Send + Sync {
    fn alive(&self) -> bool;
    fn generation(&self) -> u64;
    fn trust(&self) -> Trust;
    fn bind_io(&self, stream: TcpStream);
    fn ask(&self, kind: AskKind) -> Decision;
    fn emit(&self, ev: Ev);
}

pub struct Job {
    pub tool: Tool,
    pub args: Map<String, Value>,
    pub summary: String,
    pub gen: u64,
}

#[derive(Debug)]
pub enum HostEnd {
    Stop,
    Fail,
    Relisten { code: String },
}

pub fn connect_endpoint(host: &str, port: u16) -> Result<TcpStream, ()> {
    let addr = format!("{host}:{port}");
    let mut last = std::io::Error::new(std::io::ErrorKind::NotFound, "no addr");
    for sock in addr.to_socket_addrs().map_err(|_| ())? {
        match TcpStream::connect_timeout(&sock, Duration::from_secs(8)) {
            Ok(stream) => {
                wire::prepare(&stream);
                return Ok(stream);
            }
            Err(err) => last = err,
        }
    }
    let _ = last;
    Err(())
}

pub fn run_host_once(
    mut tcp: TcpStream,
    relay: &str,
    password: &str,
    preferred_code: &str,
    identity: &Identity,
    hooks: &dyn Hooks,
) -> HostEnd {
    if let Ok(clone) = tcp.try_clone() {
        hooks.bind_io(clone);
    }
    if !hooks.alive() {
        return HostEnd::Stop;
    }
    let alive = || hooks.alive();
    let host_req = json!({
        "op": "guest_host",
        "password": password,
        "code": preferred_code,
    });
    if wire::write_json_line(&mut tcp, &host_req).is_err() {
        return end_io(hooks);
    }
    let reply = match wire::read_json_line(&mut tcp, &alive) {
        Ok(v) => v,
        Err(_) => return end_io(hooks),
    };
    if reply.get("ok") != Some(&Value::Bool(true)) || reply.get("op").and_then(|v| v.as_str()) != Some("guest_host")
    {
        return if hooks.alive() { HostEnd::Fail } else { HostEnd::Stop };
    }
    let Some(code) = reply.get("code").and_then(|v| v.as_str()).map(credential::normalize_code) else {
        return HostEnd::Fail;
    };
    if code.len() < 4 {
        return HostEnd::Fail;
    }
    let payload = credential::build_relay_payload(relay, &code, password, &identity.fingerprint);
    hooks.emit(Ev::Waiting {
        code: code.clone(),
        payload,
    });
    let paired = match wire::read_json_line(&mut tcp, &alive) {
        Ok(v) => v,
        Err(_) => return end_io(hooks),
    };
    if !hooks.alive() {
        return HostEnd::Stop;
    }
    if paired.get("ok") != Some(&Value::Bool(true)) || paired.get("op").and_then(|v| v.as_str()) != Some("paired") {
        return HostEnd::Relisten { code };
    }
    let config = match tlsutil::server_config(identity) {
        Ok(c) => c,
        Err(_) => return HostEnd::Fail,
    };
    let conn = match rustls::ServerConnection::new(config) {
        Ok(c) => c,
        Err(_) => return HostEnd::Fail,
    };
    let mut tls = rustls::StreamOwned::new(conn, tcp);
    match session_after_tls(&mut tls, &code, hooks) {
        Loop::Relisten => HostEnd::Relisten { code },
        Loop::Stop => HostEnd::Stop,
        Loop::PeerEnded => {
            hooks.emit(Ev::PeerEnded);
            HostEnd::Stop
        }
        Loop::Fail => HostEnd::Fail,
    }
}

enum Loop {
    Relisten,
    Stop,
    PeerEnded,
    Fail,
}

fn session_after_tls(tls: &mut dyn StreamRw, code: &str, hooks: &dyn Hooks) -> Loop {
    let alive = || hooks.alive();
    let mut session: Option<Session> = None;
    loop {
        if !hooks.alive() {
            return Loop::Stop;
        }
        let frame = match wire::read_frame(tls, &alive) {
            Ok(f) => f,
            Err(_) => {
                if session.is_some() {
                    return if hooks.alive() { Loop::PeerEnded } else { Loop::Stop };
                }
                return if hooks.alive() { Loop::Relisten } else { Loop::Stop };
            }
        };
        match frame.kind.to_ascii_lowercase().as_str() {
            "ping" => {
                let _ = wire::write_frame(tls, "pong", &frame.id, session.as_ref().map(|s| s.token.as_str()), json!({}));
            }
            "pair" if session.is_none() => match handle_pair(tls, &frame, code, hooks) {
                PairOut::Session(s) => {
                    session = Some(s);
                    hooks.emit(Ev::HostSession);
                }
                PairOut::Reject => return Loop::Relisten,
                PairOut::Stop => return Loop::Stop,
            },
            "pair" => {
                let _ = wire::write_frame(
                    tls,
                    "pair_result",
                    &frame.id,
                    None,
                    json!({"ok": false, "error": "已有会话"}),
                );
            }
            "tool" => {
                if !handle_tool(tls, &frame, session.as_ref(), hooks) {
                    return if hooks.alive() { Loop::PeerEnded } else { Loop::Stop };
                }
            }
            _ => {
                let _ = wire::write_frame(
                    tls,
                    "error",
                    &frame.id,
                    None,
                    json!({"message": "未知请求"}),
                );
            }
        }
    }
}

struct Session {
    token: String,
    exp: std::time::Instant,
}

enum PairOut {
    Session(Session),
    Reject,
    Stop,
}

fn handle_pair(tls: &mut dyn StreamRw, frame: &wire::Frame, code: &str, hooks: &dyn Hooks) -> PairOut {
    let got = frame
        .payload
        .get("sessionCode")
        .and_then(|v| v.as_str())
        .map(credential::normalize_code)
        .unwrap_or_default();
    if got != code {
        let _ = wire::write_frame(tls, "pair_result", &frame.id, None, json!({"ok": false, "error": "拒绝"}));
        hooks.emit(Ev::Audit {
            text: "已拒绝一次连接请求".into(),
            kind: "n".into(),
        });
        return PairOut::Reject;
    }
    // ownerLink 一律不能跳过本机确认。
    let _ = frame.payload.get("ownerLink");
    let decision = hooks.ask(AskKind::Pair);
    if !hooks.alive() || matches!(decision, Decision::Cancelled) {
        return PairOut::Stop;
    }
    if !matches!(decision, Decision::Allow) {
        let err = if policy::write_was_timeout(decision) {
            "配对确认超时，已自动拒绝"
        } else {
            "对方拒绝了配对"
        };
        let _ = wire::write_frame(tls, "pair_result", &frame.id, None, json!({"ok": false, "error": err}));
        hooks.emit(Ev::Audit {
            text: if policy::write_was_timeout(decision) {
                "配对超时，已自动拒绝".into()
            } else {
                "已拒绝这次协助".into()
            },
            kind: "n".into(),
        });
        return PairOut::Reject;
    }
    let token = wire::new_token();
    let name = std::env::var("COMPUTERNAME").unwrap_or_else(|_| "这台电脑".into());
    let exp = (std::time::SystemTime::now() + Duration::from_secs(2 * 60 * 60))
        .duration_since(std::time::UNIX_EPOCH)
        .map(|d| d.as_secs())
        .unwrap_or(0);
    let body = json!({
        "ok": true,
        "token": token,
        "permission": "Assist",
        "expiresAtUtc": exp,
        "agentName": name,
    });
    if wire::write_frame(tls, "pair_result", &frame.id, None, body).is_err() || !hooks.alive() {
        return PairOut::Stop;
    }
    hooks.emit(Ev::Audit {
        text: "已允许这次协助".into(),
        kind: "y".into(),
    });
    PairOut::Session(Session {
        token,
        exp: std::time::Instant::now() + Duration::from_secs(2 * 60 * 60),
    })
}

fn handle_tool(tls: &mut dyn StreamRw, frame: &wire::Frame, session: Option<&Session>, hooks: &dyn Hooks) -> bool {
    let Some(session) = session else {
        let _ = wire::write_frame(tls, "tool_result", &frame.id, None, json!({"ok": false, "error": "未授权或令牌过期，请重新配对"}));
        return true;
    };
    if frame.token != session.token || std::time::Instant::now() > session.exp {
        let _ = wire::write_frame(tls, "tool_result", &frame.id, None, json!({"ok": false, "error": "未授权或令牌过期，请重新配对"}));
        return true;
    }
    let tool_name = frame.payload.get("tool").and_then(|v| v.as_str()).unwrap_or("");
    let Some(tool) = Tool::parse(tool_name) else {
        let _ = wire::write_frame(tls, "tool_result", &frame.id, None, json!({"ok": false, "error": "未知工具"}));
        return true;
    };
    let args = frame
        .payload
        .get("arguments")
        .and_then(|v| v.as_object())
        .cloned()
        .unwrap_or_default();
    let command = args.get("command").and_then(|v| v.as_str()).unwrap_or("");
    let summary = args.get("summary").and_then(|v| v.as_str()).unwrap_or("");
    let needs = policy::needs_local_confirm(hooks.trust(), tool, command);
    let decision = if needs {
        let (headline, detail) = crate::tools::describe_write(summary, command);
        hooks.ask(AskKind::Write { headline, detail })
    } else if tool.is_read() {
        Decision::Allow
    } else {
        Decision::Allow
    };
    let alive = hooks.alive();
    if !policy::may_execute(needs, Some(decision), alive) {
        if !alive || matches!(decision, Decision::Cancelled) {
            return false;
        }
        let (err, kind_text) = if policy::write_was_timeout(decision) {
            (copy::TIMED_OUT, "已拒绝：确认超时，未做任何改动")
        } else {
            (copy::DENIED, "已拒绝，未做任何改动")
        };
        let _ = wire::write_frame(
            tls,
            "tool_result",
            &frame.id,
            Some(&session.token),
            json!({"ok": false, "error": err, "deniedByUser": true}),
        );
        hooks.emit(Ev::Audit {
            text: kind_text.into(),
            kind: "n".into(),
        });
        return true;
    }
    if !hooks.alive() {
        return false;
    }
    let label = match tool {
        Tool::ListProcesses => "查看正在运行的程序".to_string(),
        Tool::ListDir => format!("查看文件夹 {}", args.get("path").and_then(|v| v.as_str()).unwrap_or("")),
        Tool::ReadLogTail => format!("查看日志 {}", args.get("path").and_then(|v| v.as_str()).unwrap_or("")),
        Tool::ReadFile => format!("查看文件 {}", args.get("path").and_then(|v| v.as_str()).unwrap_or("")),
        Tool::RunCommand => {
            let (headline, _) = crate::tools::describe_write(summary, command);
            if needs {
                format!("已允许：{headline}")
            } else {
                format!("已执行：{headline}")
            }
        }
    };
    match crate::tools::execute(tool, &args) {
        Ok(result) => {
            hooks.emit(Ev::Audit {
                text: label,
                kind: if tool.is_read() { "info".into() } else { "y".into() },
            });
            let _ = wire::write_frame(
                tls,
                "tool_result",
                &frame.id,
                Some(&session.token),
                json!({"ok": true, "result": result}),
            );
            true
        }
        Err(err) => {
            hooks.emit(Ev::Audit {
                text: "这项操作没有完成".into(),
                kind: "n".into(),
            });
            let _ = wire::write_frame(
                tls,
                "tool_result",
                &frame.id,
                Some(&session.token),
                json!({"ok": false, "error": err}),
            );
            true
        }
    }
}

pub fn run_helper(via: Via, jobs: &Receiver<Job>, hooks: &dyn Hooks) {
    hooks.emit(Ev::Status(copy::CONNECTING.into()));
    let (host, port, code, fp, relay_step) = match &via {
        Via::Relay(t) => (t.host.clone(), t.port, t.code.clone(), t.fingerprint.clone(), true),
        Via::Lan { host, port, code, fingerprint } => (host.clone(), *port, code.clone(), fingerprint.clone(), false),
    };
    let mut tcp = match connect_endpoint(&host, port) {
        Ok(s) => s,
        Err(()) => {
            hooks.emit(Ev::Fail(copy::CONNECT_FAIL.into()));
            return;
        }
    };
    if let Ok(clone) = tcp.try_clone() {
        hooks.bind_io(clone);
    }
    if relay_step {
        let password = match &via {
            Via::Relay(t) => t.password.clone(),
            Via::Lan { .. } => String::new(),
        };
        let req = json!({"op": "guest_join", "code": code, "password": password});
        if wire::write_json_line(&mut tcp, &req).is_err() {
            fail_or_cancel(hooks);
            return;
        }
        let alive = || hooks.alive();
        let reply = match wire::read_json_line(&mut tcp, &alive) {
            Ok(v) => v,
            Err(_) => {
                fail_or_cancel(hooks);
                return;
            }
        };
        if !hooks.alive() {
            return;
        }
        if reply.get("ok") != Some(&Value::Bool(true)) || reply.get("op").and_then(|v| v.as_str()) != Some("paired") {
            hooks.emit(Ev::Fail(copy::CONNECT_FAIL.into()));
            return;
        }
    }
    hooks.emit(Ev::Status(copy::WAIT_ALLOW.into()));
    let config = match tlsutil::client_config(&fp) {
        Ok(c) => c,
        Err(_) => {
            hooks.emit(Ev::Fail(copy::INCOMPLETE.into()));
            return;
        }
    };
    let conn = match rustls::ClientConnection::new(config, tlsutil::server_name()) {
        Ok(c) => c,
        Err(_) => {
            hooks.emit(Ev::Fail(copy::CONNECT_FAIL.into()));
            return;
        }
    };
    let mut tls = rustls::StreamOwned::new(conn, tcp);
    let id = wire::new_id();
    let pair_body = json!({
        "sessionCode": code,
        "requestedPermission": "Assist",
        "clientName": "鹊桥",
        "ownerLink": false,
    });
    if wire::write_frame(&mut tls, "pair", &id, None, pair_body).is_err() {
        fail_or_cancel(hooks);
        return;
    }
    let alive = || hooks.alive();
    let frame = match wire::read_frame(&mut tls, &alive) {
        Ok(f) => f,
        Err(_) => {
            fail_or_cancel(hooks);
            return;
        }
    };
    if !hooks.alive() {
        return;
    }
    let ok = frame.kind.eq_ignore_ascii_case("pair_result")
        && frame.payload.get("ok") == Some(&Value::Bool(true))
        && frame.payload.get("token").and_then(|v| v.as_str()).is_some();
    if !ok {
        let err = frame.payload.get("error").and_then(|v| v.as_str()).unwrap_or("");
        if err.contains("拒绝") || err.contains("超时") {
            hooks.emit(Ev::Fail(copy::PEER_DENIED.into()));
        } else {
            hooks.emit(Ev::Fail(copy::CONNECT_FAIL.into()));
        }
        return;
    }
    let token = frame.payload.get("token").and_then(|v| v.as_str()).unwrap_or("").to_string();
    if !hooks.alive() {
        return;
    }
    hooks.emit(Ev::HelperSession);
    helper_jobs(&mut tls, &token, jobs, hooks);
}

fn helper_jobs(tls: &mut dyn StreamRw, token: &str, jobs: &Receiver<Job>, hooks: &dyn Hooks) {
    let alive = || hooks.alive();
    loop {
        if !hooks.alive() {
            return;
        }
        let job = match jobs.recv_timeout(Duration::from_millis(200)) {
            Ok(j) => j,
            Err(mpsc::RecvTimeoutError::Timeout) => continue,
            Err(mpsc::RecvTimeoutError::Disconnected) => return,
        };
        if job.gen != hooks.generation() || !hooks.alive() {
            hooks.emit(Ev::Busy(false));
            continue;
        }
        hooks.emit(Ev::Busy(true));
        if !job.tool.is_read() {
            hooks.emit(Ev::Status(copy::WAIT_CONFIRM.into()));
        }
        let mut args = job.args.clone();
        if !job.summary.is_empty() {
            args.insert("summary".into(), Value::String(job.summary));
        }
        let id = wire::new_id();
        if wire::write_frame(
            tls,
            "tool",
            &id,
            Some(token),
            json!({"tool": job.tool.as_str(), "arguments": args}),
        )
        .is_err()
        {
            hooks.emit(Ev::Busy(false));
            if hooks.alive() && job.gen == hooks.generation() {
                hooks.emit(Ev::PeerEnded);
            }
            return;
        }
        let frame = match wire::read_frame(tls, &alive) {
            Ok(f) => f,
            Err(_) => {
                hooks.emit(Ev::Busy(false));
                if hooks.alive() && job.gen == hooks.generation() {
                    hooks.emit(Ev::PeerEnded);
                }
                return;
            }
        };
        if job.gen != hooks.generation() || !hooks.alive() {
            hooks.emit(Ev::Busy(false));
            return;
        }
        hooks.emit(Ev::Busy(false));
        let ok = frame.payload.get("ok") == Some(&Value::Bool(true));
        if ok {
            let result = frame.payload.get("result").and_then(|v| v.as_str()).unwrap_or("").to_string();
            hooks.emit(Ev::Result(result));
            hooks.emit(Ev::Status(copy::DONE.into()));
        } else {
            let err = frame.payload.get("error").and_then(|v| v.as_str()).unwrap_or("");
            if err.contains("超时") {
                hooks.emit(Ev::Status(copy::HELPER_TIMEOUT.into()));
                hooks.emit(Ev::Result(copy::HELPER_TIMEOUT.into()));
            } else if err.contains("拒绝") || frame.payload.get("deniedByUser") == Some(&Value::Bool(true)) {
                hooks.emit(Ev::Status(copy::DENIED.into()));
                hooks.emit(Ev::Result(copy::DENIED.into()));
            } else if err.is_empty() {
                hooks.emit(Ev::Result(copy::CONNECT_FAIL.into()));
            } else {
                hooks.emit(Ev::Result(err.to_string()));
            }
        }
    }
}

fn end_io(hooks: &dyn Hooks) -> HostEnd {
    if hooks.alive() {
        HostEnd::Fail
    } else {
        HostEnd::Stop
    }
}

fn fail_or_cancel(hooks: &dyn Hooks) {
    if hooks.alive() {
        hooks.emit(Ev::Fail(copy::CONNECT_FAIL.into()));
    }
}

trait StreamRw: Read + Write {}
impl<T: Read + Write> StreamRw for T {}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::{Read, Write};
    use std::net::TcpListener;
    use std::sync::{Arc, Mutex};
    use std::thread;
    use std::time::Duration;

    struct TestHooks {
        alive: std::sync::atomic::AtomicBool,
        gen: std::sync::atomic::AtomicU64,
        trust: Mutex<Trust>,
        decisions: Mutex<Vec<Decision>>,
        events: Mutex<Vec<String>>,
        connected: std::sync::atomic::AtomicBool,
        asks: std::sync::atomic::AtomicUsize,
    }

    impl TestHooks {
        fn new(decisions: Vec<Decision>) -> Self {
            Self {
                alive: std::sync::atomic::AtomicBool::new(true),
                gen: std::sync::atomic::AtomicU64::new(1),
                trust: Mutex::new(Trust::Ask),
                decisions: Mutex::new(decisions),
                events: Mutex::new(Vec::new()),
                connected: std::sync::atomic::AtomicBool::new(false),
                asks: std::sync::atomic::AtomicUsize::new(0),
            }
        }
    }

    impl Hooks for TestHooks {
        fn alive(&self) -> bool {
            self.alive.load(std::sync::atomic::Ordering::SeqCst)
        }
        fn generation(&self) -> u64 {
            self.gen.load(std::sync::atomic::Ordering::SeqCst)
        }
        fn trust(&self) -> Trust {
            *self.trust.lock().unwrap()
        }
        fn bind_io(&self, _stream: TcpStream) {}
        fn ask(&self, _kind: AskKind) -> Decision {
            self.asks.fetch_add(1, std::sync::atomic::Ordering::SeqCst);
            self.decisions.lock().unwrap().pop().unwrap_or(Decision::Deny)
        }
        fn emit(&self, ev: Ev) {
            let label = match ev {
                Ev::Waiting { .. } => "waiting".into(),
                Ev::HostSession => {
                    self.connected.store(true, std::sync::atomic::Ordering::SeqCst);
                    "host-session".into()
                }
                Ev::HelperSession => {
                    self.connected.store(true, std::sync::atomic::Ordering::SeqCst);
                    "helper-session".into()
                }
                Ev::Fail(s) => format!("fail:{s}"),
                Ev::Result(s) => format!("result:{s}"),
                Ev::PeerEnded => "peer-ended".into(),
                Ev::Status(s) => format!("status:{s}"),
                Ev::Audit { text, .. } => format!("audit:{text}"),
                Ev::Busy(_) => "busy".into(),
            };
            self.events.lock().unwrap().push(label);
        }
    }

    fn splice(mut a: TcpStream, mut b: TcpStream) {
        let mut a_read = a.try_clone().unwrap();
        let mut b_read = b.try_clone().unwrap();
        thread::spawn(move || {
            let _ = std::io::copy(&mut a_read, &mut b);
        });
        thread::spawn(move || {
            let _ = std::io::copy(&mut b_read, &mut a);
        });
    }

    fn stub_relay() -> u16 {
        let listener = TcpListener::bind("127.0.0.1:0").unwrap();
        let port = listener.local_addr().unwrap().port();
        thread::spawn(move || {
            let (mut host, _) = listener.accept().unwrap();
            let mut buf = Vec::new();
            read_line(&mut host, &mut buf);
            let v: Value = serde_json::from_slice(&buf).unwrap();
            let code = {
                let c = v.get("code").and_then(|c| c.as_str()).unwrap_or("");
                if c.is_empty() { "AB12CD".to_string() } else { c.to_string() }
            };
            let line = format!("{}\n", json!({"ok": true, "op": "guest_host", "code": code, "expires_at": 0}));
            host.write_all(line.as_bytes()).unwrap();
            let (mut helper, _) = listener.accept().unwrap();
            buf.clear();
            read_line(&mut helper, &mut buf);
            let paired = format!("{}\n", json!({"ok": true, "op": "paired", "mode": "guest", "code": code}));
            helper.write_all(paired.as_bytes()).unwrap();
            host.write_all(paired.as_bytes()).unwrap();
            splice(host, helper);
        });
        port
    }

    fn read_line(stream: &mut TcpStream, buf: &mut Vec<u8>) {
        buf.clear();
        let mut byte = [0u8; 1];
        loop {
            stream.read_exact(&mut byte).unwrap();
            if byte[0] == b'\n' {
                break;
            }
            if byte[0] != b'\r' {
                buf.push(byte[0]);
            }
        }
    }

    #[test]
    fn denied_write_does_not_create_file() {
        let port = stub_relay();
        let dir = std::env::temp_dir().join(format!("queqiao-deny-{}", std::process::id()));
        let _ = std::fs::remove_dir_all(&dir);
        std::fs::create_dir_all(&dir).unwrap();
        let marker = dir.join("should-not-exist.txt");
        let marker_s = marker.to_string_lossy().replace('\\', "\\\\");
        let host_hooks = Arc::new(TestHooks::new(vec![Decision::Deny, Decision::Allow]));
        let helper_hooks = Arc::new(TestHooks::new(vec![]));
        let identity = tlsutil::create_identity().unwrap();
        let fp = identity.fingerprint.clone();
        let password = "12345678".to_string();
        let relay = format!("127.0.0.1:{port}");
        let host_hooks2 = host_hooks.clone();
        let identity_host = identity;
        let relay_h = relay.clone();
        let pwd = password.clone();
        let host_thread = thread::spawn(move || {
            let tcp = connect_endpoint("127.0.0.1", port).unwrap();
            run_host_once(tcp, &relay_h, &pwd, "AB12CD", &identity_host, host_hooks2.as_ref())
        });
        let start = std::time::Instant::now();
        while !host_hooks.events.lock().unwrap().iter().any(|e| e == "waiting") {
            if start.elapsed() > Duration::from_secs(8) {
                panic!("host did not reach waiting");
            }
            thread::sleep(Duration::from_millis(20));
        }
        let (tx, rx) = mpsc::channel();
        let helper_hooks2 = helper_hooks.clone();
        let helper_thread = thread::spawn(move || {
            let via = Via::Relay(credential::RelayTarget {
                host: "127.0.0.1".into(),
                port,
                code: "AB12CD".into(),
                password,
                fingerprint: fp,
            });
            run_helper(via, &rx, helper_hooks2.as_ref());
        });
        let start = std::time::Instant::now();
        while !helper_hooks.connected.load(std::sync::atomic::Ordering::SeqCst) {
            if start.elapsed() > Duration::from_secs(8) {
                panic!("helper did not connect: {:?}", helper_hooks.events.lock().unwrap());
            }
            thread::sleep(Duration::from_millis(20));
        }
        let mut args = Map::new();
        args.insert(
            "command".into(),
            Value::String(format!("echo hi > \"{}\"", marker.display())),
        );
        let _ = marker_s;
        tx.send(Job {
            tool: Tool::RunCommand,
            args,
            summary: "写入一个测试文件".into(),
            gen: 1,
        })
        .unwrap();
        let start = std::time::Instant::now();
        while !helper_hooks.events.lock().unwrap().iter().any(|e| e.starts_with("result:")) {
            if start.elapsed() > Duration::from_secs(8) {
                panic!("no result {:?}", helper_hooks.events.lock().unwrap());
            }
            thread::sleep(Duration::from_millis(20));
        }
        assert!(!marker.exists(), "denied write must not touch the disk");
        let results = helper_hooks.events.lock().unwrap().clone();
        assert!(results.iter().any(|e| e.contains("已拒绝") || e.contains(copy::DENIED)));
        assert!(host_hooks.asks.load(std::sync::atomic::Ordering::SeqCst) >= 2);
        host_hooks.alive.store(false, std::sync::atomic::Ordering::SeqCst);
        helper_hooks.alive.store(false, std::sync::atomic::Ordering::SeqCst);
        drop(tx);
        let _ = host_thread.join();
        let _ = helper_thread.join();
        let _ = std::fs::remove_dir_all(&dir);
    }

    #[test]
    fn wrong_fingerprint_does_not_enter_session() {
        let port = stub_relay();
        let host_hooks = Arc::new(TestHooks::new(vec![Decision::Allow]));
        let helper_hooks = Arc::new(TestHooks::new(vec![]));
        let identity = tlsutil::create_identity().unwrap();
        let bad = "AA".repeat(32);
        assert_ne!(bad, identity.fingerprint);
        let host_hooks2 = host_hooks.clone();
        let host_thread = thread::spawn(move || {
            let tcp = connect_endpoint("127.0.0.1", port).unwrap();
            run_host_once(tcp, &format!("127.0.0.1:{port}"), "12345678", "AB12CD", &identity, host_hooks2.as_ref())
        });
        thread::sleep(Duration::from_millis(150));
        let helper_hooks2 = helper_hooks.clone();
        let helper_thread = thread::spawn(move || {
            let via = Via::Relay(credential::RelayTarget {
                host: "127.0.0.1".into(),
                port,
                code: "AB12CD".into(),
                password: "12345678".into(),
                fingerprint: bad,
            });
            let (_tx, rx) = mpsc::channel();
            run_helper(via, &rx, helper_hooks2.as_ref());
        });
        let _ = helper_thread.join();
        assert!(!helper_hooks.connected.load(std::sync::atomic::Ordering::SeqCst));
        assert!(helper_hooks.events.lock().unwrap().iter().any(|e| e.starts_with("fail:")));
        host_hooks.alive.store(false, std::sync::atomic::Ordering::SeqCst);
        let _ = host_thread.join();
    }
}
