//! 中继控制面 + 业务帧。确认通过之前不会执行写操作。

use std::io::{Read, Write};
use std::net::{TcpStream, ToSocketAddrs};
use std::sync::mpsc::{self, Receiver};
use std::thread;
use std::time::{Duration, Instant};

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
    Dropped,
    Busy(bool),
    ClearPropose,
}

pub trait Hooks: Send + Sync {
    fn alive(&self) -> bool;
    fn generation(&self) -> u64;
    fn trust(&self) -> Trust;
    fn bind_io(&self, stream: TcpStream);
    fn peer_gone(&self) -> bool {
        false
    }
    fn auto_allow_pair(&self) -> bool {
        false
    }
    fn auto_pair_note(&self) -> &'static str {
        "已按无人值守允许这次协助"
    }
    fn can_take_session(&self) -> bool {
        true
    }
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
    PakeFail,
    Locked,
    Relisten { code: String },
}

pub fn connect_endpoint(host: &str, port: u16, alive: &dyn Fn() -> bool) -> Result<TcpStream, ()> {
    if !alive() {
        return Err(());
    }
    let addr = format!("{host}:{port}");
    let (tx, rx) = mpsc::channel();
    thread::spawn(move || {
        let found = (|| -> Option<TcpStream> {
            let targets = addr.to_socket_addrs().ok()?;
            for sock in targets {
                if let Ok(stream) = TcpStream::connect_timeout(&sock, Duration::from_secs(8)) {
                    return Some(stream);
                }
            }
            None
        })();
        let _ = tx.send(found);
    });
    let start = Instant::now();
    loop {
        if !alive() {
            return Err(());
        }
        match rx.try_recv() {
            Ok(Some(stream)) => {
                wire::prepare(&stream);
                return Ok(stream);
            }
            Ok(None) => return Err(()),
            Err(mpsc::TryRecvError::Empty) => {
                if start.elapsed() > Duration::from_secs(12) {
                    return Err(());
                }
                thread::sleep(Duration::from_millis(50));
            }
            Err(mpsc::TryRecvError::Disconnected) => return Err(()),
        }
    }
}

pub fn run_host_once(
    mut tcp: TcpStream,
    relay: &str,
    password: &str,
    preferred_code: &str,
    identity: &Identity,
    hooks: &dyn Hooks,
    pake: bool,
) -> HostEnd {
    if let Ok(clone) = tcp.try_clone() {
        hooks.bind_io(clone);
    }
    if !hooks.alive() {
        return HostEnd::Stop;
    }
    let alive = || hooks.alive();
    let host_req = if pake {
        json!({
            "op": "guest_host",
            "auth": "pake",
            "code": preferred_code,
        })
    } else {
        json!({
            "op": "guest_host",
            "password": password,
            "code": preferred_code,
        })
    };
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
    let payload = if pake {
        credential::short_credential(&code, password)
    } else {
        credential::build_relay_payload(relay, &code, password, &identity.fingerprint)
    };
    hooks.emit(Ev::Waiting {
        code: code.clone(),
        payload,
    });
    let paired = match wire::read_json_line(&mut tcp, &alive) {
        Ok(v) => v,
        Err(_) => {
            return if hooks.alive() {
                HostEnd::Relisten { code }
            } else {
                HostEnd::Stop
            };
        }
    };
    if !hooks.alive() {
        return HostEnd::Stop;
    }
    if paired.get("ok") != Some(&Value::Bool(true)) || paired.get("op").and_then(|v| v.as_str()) != Some("paired") {
        let err = paired.get("error").and_then(|v| v.as_str()).unwrap_or("");
        if err.contains("too many") {
            return HostEnd::Locked;
        }
        return HostEnd::Relisten { code };
    }
    if pake && crate::pake::confirm_agent(&mut tcp, password, &code, &identity.fingerprint, &alive).is_err() {
        return if hooks.alive() {
            HostEnd::PakeFail
        } else {
            HostEnd::Stop
        };
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
        Loop::Dropped => {
            hooks.emit(Ev::Dropped);
            HostEnd::Relisten { code }
        }
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
    Dropped,
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
                    return if hooks.alive() { Loop::Dropped } else { Loop::Stop };
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
                    return if hooks.alive() { Loop::Dropped } else { Loop::Stop };
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
    // ownerLink 一律不能跳过本机确认。长久码在开启时已经确认过。
    let _ = frame.payload.get("ownerLink");
    let auto = hooks.auto_allow_pair();
    let decision = if auto { Decision::Allow } else { hooks.ask(AskKind::Pair) };
    if !hooks.alive() || matches!(decision, Decision::Cancelled) {
        return PairOut::Stop;
    }
    if !matches!(decision, Decision::Allow) {
        if hooks.peer_gone() {
            return PairOut::Reject;
        }
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
        text: if auto {
            hooks.auto_pair_note().into()
        } else {
            "已允许这次协助".into()
        },
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
        Tool::ListServices => "查看服务状态".to_string(),
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
    let (host, port, code, mut fp, relay_step, pake) = match &via {
        Via::Relay(t) => (
            t.host.clone(),
            t.port,
            t.code.clone(),
            t.fingerprint.clone(),
            true,
            t.pake,
        ),
        Via::Lan { host, port, code, fingerprint } => {
            (host.clone(), *port, code.clone(), fingerprint.clone(), false, false)
        }
    };
    let mut tcp = match connect_endpoint(&host, port, &|| hooks.alive()) {
        Ok(s) => s,
        Err(()) => {
            if hooks.alive() {
                hooks.emit(Ev::Fail(copy::CONNECT_FAIL.into()));
            }
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
        let req = if pake {
            json!({"op": "guest_join", "auth": "pake", "code": code})
        } else {
            json!({"op": "guest_join", "code": code, "password": password})
        };
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
            let err = reply.get("error").and_then(|v| v.as_str()).unwrap_or("");
            let msg = if err.contains("too many") {
                copy::TOO_MANY
            } else if join_means_not_waiting(err) {
                copy::NOT_WAITING
            } else {
                copy::CONNECT_FAIL
            };
            hooks.emit(Ev::Fail(msg.into()));
            return;
        }
    }
    if pake {
        let password = match &via {
            Via::Relay(t) => t.password.clone(),
            Via::Lan { .. } => String::new(),
        };
        let alive = || hooks.alive();
        match crate::pake::confirm_helper(&mut tcp, &password, &code, &alive) {
            Ok(got) => fp = got,
            Err(_) => {
                if hooks.alive() {
                    hooks.emit(Ev::Fail(copy::CONNECT_FAIL.into()));
                }
                return;
            }
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
            Err(mpsc::RecvTimeoutError::Timeout) => {
                if hooks.peer_gone() {
                    if hooks.alive() {
                        hooks.emit(Ev::Dropped);
                    }
                    return;
                }
                continue;
            }
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
                hooks.emit(Ev::Dropped);
            }
            return;
        }
        let frame = match wire::read_frame(tls, &alive) {
            Ok(f) => f,
            Err(_) => {
                hooks.emit(Ev::Busy(false));
                if hooks.alive() && job.gen == hooks.generation() {
                    hooks.emit(Ev::Dropped);
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
            if !job.tool.is_read() {
                hooks.emit(Ev::ClearPropose);
            }
        } else {
            let err = frame.payload.get("error").and_then(|v| v.as_str()).unwrap_or("");
            if err.contains("超时") {
                hooks.emit(Ev::Status(copy::TIMED_OUT.into()));
                hooks.emit(Ev::Result(copy::TIMED_OUT.into()));
                hooks.emit(Ev::ClearPropose);
            } else if err.contains("拒绝") || frame.payload.get("deniedByUser") == Some(&Value::Bool(true)) {
                hooks.emit(Ev::Status(copy::DENIED.into()));
                hooks.emit(Ev::Result(copy::DENIED.into()));
                hooks.emit(Ev::ClearPropose);
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

fn join_means_not_waiting(err: &str) -> bool {
    let err = err.to_ascii_lowercase();
    err.contains("peer not online")
        || err.contains("invalid code")
        || err.contains("password")
        || err.contains("helper already connected")
}

fn fail_or_cancel(hooks: &dyn Hooks) {
    if hooks.alive() {
        hooks.emit(Ev::Fail(copy::CONNECT_FAIL.into()));
    }
}

trait StreamRw: Read + Write {}
impl<T: Read + Write> StreamRw for T {}

pub struct ListedDevice {
    pub id: String,
    pub name: String,
    pub online: bool,
    pub unattended: bool,
    pub fp: String,
}

fn relay_tcp(relay: &str, alive: &dyn Fn() -> bool) -> Result<TcpStream, ()> {
    let Some((host, port)) = credential::parse_endpoint(relay) else {
        return Err(());
    };
    connect_endpoint(&host, port, alive)
}

fn exchange(tcp: &mut TcpStream, req: Value, alive: &dyn Fn() -> bool) -> Result<Value, ()> {
    if wire::write_json_line(tcp, &req).is_err() {
        return Err(());
    }
    wire::read_json_line(tcp, alive).map_err(|_| ())
}

fn net_err() -> String {
    "没能连上，请检查网络后重试。".into()
}

pub fn account_signup(relay: &str, username: &str, password: &str) -> Result<String, String> {
    let alive = || true;
    let mut tcp = relay_tcp(relay, &alive).map_err(|_| net_err())?;
    let created = exchange(
        &mut tcp,
        json!({"op": "signup", "username": username, "password": password}),
        &alive,
    )
    .map_err(|_| net_err())?;
    if created.get("ok") != Some(&Value::Bool(true)) {
        let err = created.get("error").and_then(|v| v.as_str()).unwrap_or("");
        return Err(crate::account::explain(err));
    }
    let logged = exchange(
        &mut tcp,
        json!({"op": "login", "username": username, "password": password}),
        &alive,
    )
    .map_err(|_| net_err())?;
    token_of(logged)
}

pub fn account_login(relay: &str, username: &str, password: &str) -> Result<String, String> {
    let alive = || true;
    let mut tcp = relay_tcp(relay, &alive).map_err(|_| net_err())?;
    let logged = exchange(
        &mut tcp,
        json!({"op": "login", "username": username, "password": password}),
        &alive,
    )
    .map_err(|_| net_err())?;
    token_of(logged)
}

fn token_of(reply: Value) -> Result<String, String> {
    if reply.get("ok") != Some(&Value::Bool(true)) {
        let err = reply.get("error").and_then(|v| v.as_str()).unwrap_or("");
        return Err(crate::account::explain(err));
    }
    reply
        .get("token")
        .and_then(|v| v.as_str())
        .filter(|s| !s.is_empty())
        .map(str::to_string)
        .ok_or_else(|| "没能登录。".to_string())
}

pub fn account_logout(relay: &str, token: &str) -> Result<(), String> {
    let alive = || true;
    let mut tcp = relay_tcp(relay, &alive).map_err(|_| net_err())?;
    let authed = exchange(&mut tcp, json!({"op": "auth_token", "token": token}), &alive).map_err(|_| net_err())?;
    if authed.get("ok") != Some(&Value::Bool(true)) {
        return Ok(());
    }
    let _ = exchange(&mut tcp, json!({"op": "logout", "token": token}), &alive);
    Ok(())
}

pub fn account_list(relay: &str, token: &str) -> Result<Vec<ListedDevice>, String> {
    let alive = || true;
    let mut tcp = relay_tcp(relay, &alive).map_err(|_| net_err())?;
    let authed = exchange(&mut tcp, json!({"op": "auth_token", "token": token}), &alive).map_err(|_| net_err())?;
    if authed.get("ok") != Some(&Value::Bool(true)) {
        let err = authed.get("error").and_then(|v| v.as_str()).unwrap_or("");
        return Err(crate::account::explain(err));
    }
    let listed = exchange(&mut tcp, json!({"op": "list_devices"}), &alive).map_err(|_| net_err())?;
    if listed.get("ok") != Some(&Value::Bool(true)) {
        let err = listed.get("error").and_then(|v| v.as_str()).unwrap_or("");
        return Err(crate::account::explain(err));
    }
    let mut out = Vec::new();
    let Some(rows) = listed.get("devices").and_then(|v| v.as_array()) else {
        return Ok(out);
    };
    for row in rows {
        let Some(id) = row.get("device_id").and_then(|v| v.as_str()).filter(|s| !s.is_empty()) else {
            continue;
        };
        out.push(ListedDevice {
            id: id.to_string(),
            name: row
                .get("display_name")
                .and_then(|v| v.as_str())
                .filter(|s| !s.is_empty())
                .unwrap_or(id)
                .to_string(),
            online: row.get("online") == Some(&Value::Bool(true)),
            unattended: row.get("unattended") == Some(&Value::Bool(true)),
            fp: row.get("fp").and_then(|v| v.as_str()).unwrap_or("").to_string(),
        });
    }
    Ok(out)
}

pub fn account_set_unattended(relay: &str, token: &str, device_id: &str, unattended: bool) -> Result<(), String> {
    let alive = || true;
    let mut tcp = relay_tcp(relay, &alive).map_err(|_| net_err())?;
    let authed = exchange(&mut tcp, json!({"op": "auth_token", "token": token}), &alive).map_err(|_| net_err())?;
    if authed.get("ok") != Some(&Value::Bool(true)) {
        let err = authed.get("error").and_then(|v| v.as_str()).unwrap_or("");
        return Err(crate::account::explain(err));
    }
    let reply = exchange(
        &mut tcp,
        json!({"op": "set_device_unattended", "device_id": device_id, "unattended": unattended}),
        &alive,
    )
    .map_err(|_| net_err())?;
    if reply.get("ok") != Some(&Value::Bool(true)) {
        let err = reply.get("error").and_then(|v| v.as_str()).unwrap_or("");
        return Err(crate::account::explain(err));
    }
    Ok(())
}

pub fn account_delete(relay: &str, token: &str, device_id: &str) -> Result<(), String> {
    let alive = || true;
    let mut tcp = relay_tcp(relay, &alive).map_err(|_| net_err())?;
    let authed = exchange(&mut tcp, json!({"op": "auth_token", "token": token}), &alive).map_err(|_| net_err())?;
    if authed.get("ok") != Some(&Value::Bool(true)) {
        let err = authed.get("error").and_then(|v| v.as_str()).unwrap_or("");
        return Err(crate::account::explain(err));
    }
    let reply = exchange(
        &mut tcp,
        json!({"op": "delete_device", "device_id": device_id}),
        &alive,
    )
    .map_err(|_| net_err())?;
    if reply.get("ok") != Some(&Value::Bool(true)) {
        let err = reply.get("error").and_then(|v| v.as_str()).unwrap_or("");
        return Err(crate::account::explain(err));
    }
    Ok(())
}

pub fn run_account_host(
    mut tcp: TcpStream,
    identity: &Identity,
    device_id: &str,
    display_name: &str,
    token: &str,
    unattended: bool,
    hooks: &dyn Hooks,
) -> HostEnd {
    if let Ok(clone) = tcp.try_clone() {
        hooks.bind_io(clone);
    }
    if !hooks.alive() {
        return HostEnd::Stop;
    }
    let alive = || hooks.alive();
    let authed = match exchange(&mut tcp, json!({"op": "auth_token", "token": token}), &alive) {
        Ok(v) => v,
        Err(()) => return end_io(hooks),
    };
    if authed.get("ok") != Some(&Value::Bool(true)) {
        return if hooks.alive() { HostEnd::Fail } else { HostEnd::Stop };
    }
    let online = match exchange(
        &mut tcp,
        json!({
            "op": "device_online",
            "device_id": device_id,
            "display_name": display_name,
            "fp": identity.fingerprint,
            "unattended": unattended,
        }),
        &alive,
    ) {
        Ok(v) => v,
        Err(()) => return end_io(hooks),
    };
    if online.get("ok") != Some(&Value::Bool(true)) {
        return if hooks.alive() { HostEnd::Fail } else { HostEnd::Stop };
    }
    hooks.emit(Ev::Waiting {
        code: String::new(),
        payload: String::new(),
    });
    let paired = match wire::read_json_line(&mut tcp, &alive) {
        Ok(v) => v,
        Err(_) => {
            return if hooks.alive() {
                HostEnd::Relisten { code: device_id.to_string() }
            } else {
                HostEnd::Stop
            };
        }
    };
    if !hooks.alive() {
        return HostEnd::Stop;
    }
    if paired.get("ok") != Some(&Value::Bool(true)) || paired.get("op").and_then(|v| v.as_str()) != Some("paired") {
        return HostEnd::Relisten { code: device_id.to_string() };
    }
    if !hooks.can_take_session() || !hooks.auto_allow_pair() {
        return HostEnd::Relisten { code: device_id.to_string() };
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
    let code = credential::normalize_code(device_id);
    match session_after_tls(&mut tls, &code, hooks) {
        Loop::Relisten | Loop::Dropped => {
            hooks.emit(Ev::Dropped);
            HostEnd::Relisten { code }
        }
        Loop::Stop => HostEnd::Stop,
        Loop::PeerEnded => {
            hooks.emit(Ev::PeerEnded);
            HostEnd::Stop
        }
        Loop::Fail => HostEnd::Fail,
    }
}

pub fn run_account_helper(relay: &str, token: &str, device_id: &str, fp: &str, jobs: &Receiver<Job>, hooks: &dyn Hooks) {
    hooks.emit(Ev::Status(copy::CONNECTING.into()));
    let alive = || hooks.alive();
    let mut tcp = match relay_tcp(relay, &alive) {
        Ok(stream) => stream,
        Err(()) => {
            if hooks.alive() {
                hooks.emit(Ev::Fail(copy::CONNECT_FAIL.into()));
            }
            return;
        }
    };
    if let Ok(clone) = tcp.try_clone() {
        hooks.bind_io(clone);
    }
    let authed = match exchange(&mut tcp, json!({"op": "auth_token", "token": token}), &alive) {
        Ok(v) => v,
        Err(()) => {
            fail_or_cancel(hooks);
            return;
        }
    };
    if !hooks.alive() {
        return;
    }
    if authed.get("ok") != Some(&Value::Bool(true)) {
        let err = authed.get("error").and_then(|v| v.as_str()).unwrap_or("");
        hooks.emit(Ev::Fail(crate::account::explain(err)));
        return;
    }
    let paired = match exchange(&mut tcp, json!({"op": "connect_device", "device_id": device_id}), &alive) {
        Ok(v) => v,
        Err(()) => {
            fail_or_cancel(hooks);
            return;
        }
    };
    if !hooks.alive() {
        return;
    }
    if paired.get("ok") != Some(&Value::Bool(true)) || paired.get("op").and_then(|v| v.as_str()) != Some("paired") {
        let err = paired.get("error").and_then(|v| v.as_str()).unwrap_or("");
        hooks.emit(Ev::Fail(crate::account::explain(err)));
        return;
    }
    let pin = paired
        .get("fp")
        .and_then(|v| v.as_str())
        .filter(|s| !s.is_empty())
        .unwrap_or(fp);
    if !credential::is_sha256_hex(&credential::normalize_fp(pin)) {
        hooks.emit(Ev::Fail("这台电脑还没准备好加密连接。".into()));
        return;
    }
    hooks.emit(Ev::Status("正在连接你的电脑…".into()));
    let config = match tlsutil::client_config(pin) {
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
        "sessionCode": device_id,
        "requestedPermission": "Assist",
        "clientName": "鹊桥",
        "ownerLink": false,
    });
    if wire::write_frame(&mut tls, "pair", &id, None, pair_body).is_err() {
        fail_or_cancel(hooks);
        return;
    }
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
        hooks.emit(Ev::Fail(copy::CONNECT_FAIL.into()));
        return;
    }
    let session_token = frame.payload.get("token").and_then(|v| v.as_str()).unwrap_or("").to_string();
    if !hooks.alive() {
        return;
    }
    hooks.emit(Ev::HelperSession);
    helper_jobs(&mut tls, &session_token, jobs, hooks);
}

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
        payload: Mutex<String>,
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
                payload: Mutex::new(String::new()),
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
                Ev::Waiting { payload, .. } => {
                    *self.payload.lock().unwrap() = payload;
                    "waiting".into()
                }
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
                Ev::Dropped => "dropped".into(),
                Ev::Status(s) => format!("status:{s}"),
                Ev::Audit { text, .. } => format!("audit:{text}"),
                Ev::Busy(_) => "busy".into(),
                Ev::ClearPropose => "clear-propose".into(),
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
            let tcp = connect_endpoint("127.0.0.1", port, &|| true).unwrap();
            run_host_once(tcp, &relay_h, &pwd, "AB12CD", &identity_host, host_hooks2.as_ref(), false)
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
                pake: false,
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
            let tcp = connect_endpoint("127.0.0.1", port, &|| true).unwrap();
            run_host_once(tcp, &format!("127.0.0.1:{port}"), "12345678", "AB12CD", &identity, host_hooks2.as_ref(), false)
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
                pake: false,
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

    fn start_real_relay() -> Option<(std::process::Child, u16)> {
        let Ok(bin) = std::env::var("QUEQIAO_RELAY_BIN") else {
            return None;
        };
        let listener = TcpListener::bind("127.0.0.1:0").unwrap();
        let port = listener.local_addr().unwrap().port();
        drop(listener);
        let db = std::env::temp_dir().join(format!("queqiao-relay-{}-{port}.db", std::process::id()));
        let _ = std::fs::remove_file(&db);
        let child = std::process::Command::new(bin)
            .env("RELAY_ADDR", format!("127.0.0.1:{port}"))
            .env("RELAY_DATA", &db)
            .env_remove("RELAY_AUDIT_ADDR")
            .env_remove("RELAY_AUDIT_TOKEN")
            .stdout(std::process::Stdio::null())
            .stderr(std::process::Stdio::null())
            .spawn()
            .expect("start relay");
        let start = std::time::Instant::now();
        loop {
            if TcpStream::connect_timeout(
                &format!("127.0.0.1:{port}").parse().unwrap(),
                Duration::from_millis(200),
            )
            .is_ok()
            {
                break;
            }
            if start.elapsed() > Duration::from_secs(5) {
                panic!("relay did not listen");
            }
            thread::sleep(Duration::from_millis(50));
        }
        Some((child, port))
    }

    #[test]
    fn pake_short_code_survives_helper_drop() {
        let Some((mut relay, port)) = start_real_relay() else {
            return;
        };
        let host_hooks = Arc::new(TestHooks::new(vec![Decision::Allow]));
        let helper_hooks = Arc::new(TestHooks::new(vec![]));
        let identity = tlsutil::create_identity().unwrap();
        let host_hooks2 = host_hooks.clone();
        let host_thread = thread::spawn(move || {
            let tcp = connect_endpoint("127.0.0.1", port, &|| true).unwrap();
            run_host_once(tcp, &format!("127.0.0.1:{port}"), "12345678", "", &identity, host_hooks2.as_ref(), true)
        });
        let start = std::time::Instant::now();
        while host_hooks.payload.lock().unwrap().is_empty() {
            if start.elapsed() > Duration::from_secs(8) {
                panic!("host did not publish a credential");
            }
            thread::sleep(Duration::from_millis(20));
        }
        let payload = host_hooks.payload.lock().unwrap().clone();
        assert!(!payload.contains("remoteassist:pair"), "{payload}");
        let credential::CredClass::Ok(Via::Relay(mut target)) = credential::classify(&payload) else {
            panic!("short credential not accepted: {payload}");
        };
        assert!(target.pake);
        assert!(target.fingerprint.is_empty());
        assert_eq!(target.password, "12345678");
        target.host = "127.0.0.1".into();
        target.port = port;
        let code = target.code.clone();
        let (tx, rx) = mpsc::channel();
        let helper_hooks2 = helper_hooks.clone();
        let helper_thread = thread::spawn(move || {
            run_helper(Via::Relay(target), &rx, helper_hooks2.as_ref());
        });
        let start = std::time::Instant::now();
        while !helper_hooks.connected.load(std::sync::atomic::Ordering::SeqCst) {
            if start.elapsed() > Duration::from_secs(8) {
                panic!("helper did not enter session: {:?}", helper_hooks.events.lock().unwrap());
            }
            thread::sleep(Duration::from_millis(20));
        }
        assert!(host_hooks.events.lock().unwrap().iter().any(|e| e == "host-session"));
        drop(tx);
        helper_hooks.alive.store(false, std::sync::atomic::Ordering::SeqCst);
        let _ = helper_thread.join();
        let start = std::time::Instant::now();
        while !host_hooks.events.lock().unwrap().iter().any(|e| e == "dropped") {
            if start.elapsed() > Duration::from_secs(8) {
                panic!("host did not see the drop: {:?}", host_hooks.events.lock().unwrap());
            }
            thread::sleep(Duration::from_millis(20));
        }
        match host_thread.join().unwrap() {
            HostEnd::Relisten { code: next } => assert_eq!(next, code),
            other => panic!("expected relisten, got {other:?}"),
        }
        let _ = relay.kill();
        let _ = relay.wait();
    }

    #[test]
    fn pake_wrong_password_does_not_enter_session() {
        let Some((mut relay, port)) = start_real_relay() else {
            return;
        };
        let host_hooks = Arc::new(TestHooks::new(vec![Decision::Allow]));
        let helper_hooks = Arc::new(TestHooks::new(vec![]));
        let identity = tlsutil::create_identity().unwrap();
        let host_hooks2 = host_hooks.clone();
        let host_thread = thread::spawn(move || {
            let tcp = connect_endpoint("127.0.0.1", port, &|| true).unwrap();
            run_host_once(tcp, &format!("127.0.0.1:{port}"), "12345678", "AB12CD", &identity, host_hooks2.as_ref(), true)
        });
        let start = std::time::Instant::now();
        while host_hooks.payload.lock().unwrap().is_empty() {
            if start.elapsed() > Duration::from_secs(8) {
                panic!("host did not publish a credential");
            }
            thread::sleep(Duration::from_millis(20));
        }
        let helper_hooks2 = helper_hooks.clone();
        let helper_thread = thread::spawn(move || {
            let via = Via::Relay(credential::RelayTarget {
                host: "127.0.0.1".into(),
                port,
                code: "AB12CD".into(),
                password: "00000000".into(),
                fingerprint: String::new(),
                pake: true,
            });
            let (_tx, rx) = mpsc::channel();
            run_helper(via, &rx, helper_hooks2.as_ref());
        });
        let _ = helper_thread.join();
        assert!(!helper_hooks.connected.load(std::sync::atomic::Ordering::SeqCst));
        assert!(helper_hooks.events.lock().unwrap().iter().any(|e| e.starts_with("fail:")));
        match host_thread.join().unwrap() {
            HostEnd::PakeFail => {}
            other => panic!("expected pake failure, got {other:?}"),
        }
        let _ = relay.kill();
        let _ = relay.wait();
    }

    #[test]
    fn absent_peer_is_not_a_network_error() {
        assert!(join_means_not_waiting("peer not online"));
        assert!(join_means_not_waiting("invalid code or password"));
        assert!(!join_means_not_waiting(""));
        assert!(!join_means_not_waiting("connection reset"));
    }
}
