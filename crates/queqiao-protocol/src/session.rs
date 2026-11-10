//! 无头协助端：中继 join → TLS → pair → tool。

use std::collections::BTreeMap;
use std::io::{Read, Write};
use std::net::{SocketAddr, TcpStream, ToSocketAddrs};
use std::time::Duration;

use rustls::StreamOwned;
use serde_json::{json, Value};

use crate::credential::{CredClass, Via};
use crate::tls;
use crate::tools::ToolKind;
use crate::wire;

#[derive(Debug)]
pub enum SessionError {
    CredFormat,
    CredIncomplete,
    Connect(String),
    PairDenied(String),
    Protocol(String),
    Io(String),
}

impl std::fmt::Display for SessionError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::CredFormat => write!(f, "连接凭证格式不对。"),
            Self::CredIncomplete => {
                write!(f, "连接凭证不完整（须含完整 remoteassist:pair 与证书指纹 fp=）。")
            }
            Self::Connect(m) => write!(f, "{m}"),
            Self::PairDenied(m) => write!(f, "{m}"),
            Self::Protocol(m) => write!(f, "{m}"),
            Self::Io(m) => write!(f, "{m}"),
        }
    }
}

impl std::error::Error for SessionError {}

#[derive(Debug, Clone)]
pub struct ToolResult {
    pub ok: bool,
    pub text: String,
    pub denied_by_user: bool,
}

type TlsStream = StreamOwned<rustls::ClientConnection, TcpStream>;

pub struct AssistSession {
    tls: TlsStream,
    token: String,
}

impl AssistSession {
    /// 解析完整载荷并完成配对。client_name 会出现在被控端配对提示侧。
    pub fn connect(payload: &str, client_name: &str) -> Result<Self, SessionError> {
        let via = match crate::credential::classify(payload) {
            CredClass::Ok(v) => v,
            CredClass::Format => return Err(SessionError::CredFormat),
            CredClass::Incomplete => return Err(SessionError::CredIncomplete),
        };
        Self::connect_via(via, client_name)
    }

    pub fn connect_via(via: Via, client_name: &str) -> Result<Self, SessionError> {
        let (host, port, code, fp, relay_step, password) = match via {
            Via::Relay(t) => (
                t.host,
                t.port,
                t.code,
                t.fingerprint,
                true,
                t.password,
            ),
            Via::Lan {
                host,
                port,
                code,
                fingerprint,
            } => (host, port, code, fingerprint, false, String::new()),
        };

        let mut tcp = connect_tcp(&host, port)?;
        wire::prepare(&tcp);

        if relay_step {
            let req = json!({
                "op": "guest_join",
                "code": code,
                "password": password,
            });
            wire::write_json_line(&mut tcp, &req).map_err(|e| SessionError::Io(e.to_string()))?;
            let alive = || true;
            let reply = wire::read_json_line(&mut tcp, &alive)
                .map_err(|e| SessionError::Connect(format!("中继无响应：{e}")))?;
            if reply.get("ok") != Some(&Value::Bool(true))
                || reply.get("op").and_then(|v| v.as_str()) != Some("paired")
            {
                let err = reply.get("error").and_then(|v| v.as_str()).unwrap_or("");
                let msg = if err.contains("too many") {
                    "这组凭证尝试次数过多，已经停用。请让对方重新开启后再发一次。"
                } else if join_means_not_waiting(err) {
                    "对方还没在等待连接。请让对方先开启接受协助，再发连接凭证。"
                } else {
                    "没能连上，请检查网络与凭证后重试。"
                };
                return Err(SessionError::Connect(msg.into()));
            }
        }

        let config = tls::client_config(&fp).map_err(|_| SessionError::CredIncomplete)?;
        let conn = rustls::ClientConnection::new(config, tls::server_name())
            .map_err(|_| SessionError::Connect("没能建立加密连接。".into()))?;
        let mut tls_stream = StreamOwned::new(conn, tcp);

        let id = wire::new_id();
        let pair_body = json!({
            "sessionCode": code,
            "requestedPermission": "Assist",
            "clientName": client_name,
            "ownerLink": false,
        });
        wire::write_frame(&mut tls_stream, "pair", &id, None, pair_body)
            .map_err(|e| SessionError::Io(e.to_string()))?;

        let alive = || true;
        let frame = wire::read_frame(&mut tls_stream, &alive)
            .map_err(|e| SessionError::Connect(format!("等待对方允许时断开：{e}")))?;

        let ok = frame.kind.eq_ignore_ascii_case("pair_result")
            && frame.payload.get("ok") == Some(&Value::Bool(true))
            && frame
                .payload
                .get("token")
                .and_then(|v| v.as_str())
                .is_some();
        if !ok {
            let err = frame
                .payload
                .get("error")
                .and_then(|v| v.as_str())
                .unwrap_or("");
            if err.contains("拒绝") || err.contains("超时") {
                return Err(SessionError::PairDenied(
                    "对方没有同意这次协助。".into(),
                ));
            }
            return Err(SessionError::Connect(
                if err.is_empty() {
                    "没能完成配对。".into()
                } else {
                    err.into()
                },
            ));
        }
        let token = frame
            .payload
            .get("token")
            .and_then(|v| v.as_str())
            .unwrap_or("")
            .to_string();

        Ok(Self {
            tls: tls_stream,
            token,
        })
    }

    pub fn invoke(
        &mut self,
        tool: ToolKind,
        args: BTreeMap<String, Value>,
    ) -> Result<ToolResult, SessionError> {
        let id = wire::new_id();
        let payload = json!({
            "tool": tool.as_wire(),
            "arguments": args,
        });
        wire::write_frame(&mut self.tls, "tool", &id, Some(&self.token), payload)
            .map_err(|e| SessionError::Io(e.to_string()))?;

        // 写操作可能等对方确认约 60s；读超时循环由 wire::read_frame 处理。
        let alive = || true;
        let frame = wire::read_frame(&mut self.tls, &alive).map_err(|e| {
            SessionError::Io(format!("等待工具结果时断开：{e}"))
        })?;

        if !frame.kind.eq_ignore_ascii_case("tool_result") {
            return Err(SessionError::Protocol(format!(
                "unexpected frame type: {}",
                frame.kind
            )));
        }

        let ok = frame.payload.get("ok") == Some(&Value::Bool(true));
        let denied = frame.payload.get("deniedByUser") == Some(&Value::Bool(true));
        if ok {
            let text = frame
                .payload
                .get("result")
                .and_then(|v| v.as_str())
                .unwrap_or("")
                .to_string();
            return Ok(ToolResult {
                ok: true,
                text,
                denied_by_user: false,
            });
        }
        let err = frame
            .payload
            .get("error")
            .and_then(|v| v.as_str())
            .unwrap_or("failed")
            .to_string();
        let denied_by_user = denied || err.contains("拒绝");
        let text = if denied_by_user {
            format!("{err} (denied by local user confirmation)")
        } else if err.contains("超时") {
            format!("{err} (timed out waiting for local confirmation)")
        } else {
            err
        };
        Ok(ToolResult {
            ok: false,
            text,
            denied_by_user,
        })
    }
}

fn connect_tcp(host: &str, port: u16) -> Result<TcpStream, SessionError> {
    let addr = format!("{host}:{port}");
    let mut last = None;
    let addrs: Vec<SocketAddr> = addr
        .to_socket_addrs()
        .map_err(|e| SessionError::Connect(format!("无法解析地址：{e}")))?
        .collect();
    if addrs.is_empty() {
        return Err(SessionError::Connect("无法解析中继地址。".into()));
    }
    for a in addrs {
        match TcpStream::connect_timeout(&a, Duration::from_secs(12)) {
            Ok(s) => return Ok(s),
            Err(e) => last = Some(e),
        }
    }
    Err(SessionError::Connect(format!(
        "没能连上 {addr}：{}",
        last.map(|e| e.to_string()).unwrap_or_default()
    )))
}

fn join_means_not_waiting(err: &str) -> bool {
    let e = err.to_ascii_lowercase();
    e.contains("not found")
        || e.contains("no host")
        || e.contains("not waiting")
        || e.contains("unknown code")
        || err.contains("未开启")
        || err.contains("不在等待")
}

// Keep Read/Write bound used by StreamOwned visible to rustc unused lint in some editions.
#[allow(dead_code)]
fn _assert_rw<T: Read + Write>(_: &T) {}
