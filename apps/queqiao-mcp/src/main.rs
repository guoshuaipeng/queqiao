//! 鹊桥 MCP：stdio JSON-RPC 2.0。
//! 帧格式兼容：
//! - Content-Length（MCP 规范 / Claude Desktop / 官方 SDK）
//! - NDJSON 一行一 JSON（Cursor 等部分宿主）
//! 启动即可用；用 connect 传短码/完整载荷再连远端。写操作仍须被控本机确认。

use std::collections::BTreeMap;
use std::io::{self, Read, Write};
use std::sync::Mutex;

use queqiao_protocol::session::AssistSession;
use queqiao_protocol::tools::{map_mcp_tool, ToolKind};
use serde_json::{json, Value};

static SESSION: Mutex<Option<AssistSession>> = Mutex::new(None);

/// 本进程明确支持的协议版本（按新→旧）。未知客户端版本时回退到最旧稳定版。
const SUPPORTED_PROTOCOL_VERSIONS: &[&str] = &["2025-11-25", "2025-06-18", "2024-11-05"];

fn main() {
    let args: Vec<String> = std::env::args().skip(1).collect();
    if args.iter().any(|a| a == "-h" || a == "--help" || a == "help") {
        eprintln!("RemoteAssist MCP (stdio)");
        eprintln!("兼容 Content-Length 与 NDJSON；未连接也可被宿主拉起，用 connect 传短码/完整载荷。");
        eprintln!("Env: REMOTE_ASSIST_PAYLOAD=<remoteassist:pair?... | CODE-########>  # 可选，有则启动时尝试配对");
        eprintln!("     REMOTE_ASSIST_RELAY=<host:port>   # 短码默认中继");
        eprintln!("     REMOTE_ASSIST_MCP_DEBUG_LOG=<path>  # 可选，追加帧调试日志");
        eprintln!("Or:  RemoteAssist.Mcp.exe [--payload \"...\"] [--relay host:port]");
        eprintln!("See docs/MCP.md");
        std::process::exit(0);
    }

    // 首个 MCP 请求前不要写 stderr：部分宿主（如 Cursor）会因此推迟/卡住 initialize。
    let boot_payload = resolve_payload(&args);
    let boot_relay = resolve_relay_arg(&args);
    let mut boot_note: Option<String> = None;
    if !boot_payload.is_empty() {
        match AssistSession::connect_with_relay(&boot_payload, "鹊桥 MCP", &boot_relay) {
            Ok(session) => {
                *SESSION.lock().expect("session") = Some(session);
                boot_note = Some("[mcp] paired ok (boot)".into());
            }
            Err(e) => {
                boot_note = Some(format!(
                    "[mcp] 启动时配对失败（MCP 仍可用，请用 connect 重试）: {e}"
                ));
            }
        }
    }

    let stdin = io::stdin();
    let mut stdin = stdin.lock();
    let stdout = io::stdout();
    let mut stdout = stdout.lock();
    let mut logged_boot = false;
    let debug_log = std::env::var_os("REMOTE_ASSIST_MCP_DEBUG_LOG")
        .map(std::path::PathBuf::from)
        .filter(|p| !p.as_os_str().is_empty());
    // 同一会话内帧格式锁定：首包判定后全程同帧回写，避免混用导致宿主拆包失败。
    let mut session_framing: Option<Framing> = None;

    loop {
        let (msg, detected) = match read_mcp_message(&mut stdin) {
            Ok(Some(m)) => m,
            Ok(None) => break,
            Err(e) => {
                eprintln!("[mcp] read error: {e}");
                break;
            }
        };
        let framing = *session_framing.get_or_insert(detected);
        if let Some(path) = debug_log.as_ref() {
            let _ = append_debug_log(path, framing, &msg);
        }
        if !logged_boot {
            logged_boot = true;
            if let Some(note) = boot_note.take() {
                eprintln!("{note}");
            }
        }
        let node: Value = match serde_json::from_str(&msg) {
            Ok(v) => v,
            Err(_) => continue,
        };
        let id = node.get("id").cloned();
        let Some(method) = node.get("method").and_then(|m| m.as_str()) else {
            continue;
        };
        // notifications（无 id）直接吞掉
        if id.is_none() {
            continue;
        }

        let (result, error) = match method {
            "initialize" => (Some(handle_initialize(node.get("params"))), None),
            "tools/list" => (Some(handle_tools_list()), None),
            "tools/call" => match handle_tools_call(node.get("params")) {
                Ok(v) => (Some(v), None),
                Err(e) => (None, Some(json!({"code": -32000, "message": e}))),
            },
            // 部分宿主会探测未声明能力；返回空集比 -32601 更稳。
            "resources/list" => (Some(json!({"resources": []})), None),
            "resources/templates/list" => (Some(json!({"resourceTemplates": []})), None),
            "prompts/list" => (Some(json!({"prompts": []})), None),
            "logging/setLevel" => (Some(json!({})), None),
            "ping" => (Some(json!({})), None),
            other => (
                None,
                Some(json!({"code": -32601, "message": format!("Method not found: {other}")})),
            ),
        };

        let mut response = json!({
            "jsonrpc": "2.0",
            "id": id,
        });
        if let Some(err) = error {
            response["error"] = err;
        } else if let Some(res) = result {
            response["result"] = res;
        }
        if let Err(e) = write_mcp_message(&mut stdout, &response.to_string(), framing) {
            eprintln!("[mcp] write error: {e}");
            break;
        }
    }
}

fn append_debug_log(path: &std::path::Path, framing: Framing, msg: &str) -> io::Result<()> {
    use std::io::Write;
    let mut f = std::fs::OpenOptions::new()
        .create(true)
        .append(true)
        .open(path)?;
    let ts = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .map(|d| d.as_millis())
        .unwrap_or(0);
    writeln!(
        f,
        "[{ts}] framing={framing:?} msg_preview={}",
        msg.chars().take(300).collect::<String>()
    )
}

fn resolve_payload(args: &[String]) -> String {
    let mut i = 0;
    while i < args.len() {
        if args[i] == "--payload" {
            if let Some(v) = args.get(i + 1) {
                return v.clone();
            }
        }
        if let Some(rest) = args[i].strip_prefix("--payload=") {
            return rest.to_string();
        }
        i += 1;
    }
    std::env::var("REMOTE_ASSIST_PAYLOAD").unwrap_or_default()
}

fn resolve_relay_arg(args: &[String]) -> String {
    let mut i = 0;
    while i < args.len() {
        if args[i] == "--relay" {
            if let Some(v) = args.get(i + 1) {
                return v.clone();
            }
        }
        if let Some(rest) = args[i].strip_prefix("--relay=") {
            return rest.to_string();
        }
        i += 1;
    }
    queqiao_protocol::session::resolve_relay(None)
}

fn negotiate_protocol_version(client_ver: &str) -> &'static str {
    for &v in SUPPORTED_PROTOCOL_VERSIONS {
        if v == client_ver {
            return v;
        }
    }
    // 未知版本：回退到最广泛兼容的稳定版。
    "2024-11-05"
}

fn handle_initialize(params: Option<&Value>) -> Value {
    let client_ver = params
        .and_then(|p| p.get("protocolVersion"))
        .and_then(|v| v.as_str())
        .unwrap_or("2024-11-05");
    json!({
        "protocolVersion": negotiate_protocol_version(client_ver),
        "capabilities": { "tools": {} },
        "serverInfo": {
            "name": "remote-assist",
            "version": "0.1.0",
            "title": "鹊桥远程协助"
        },
        "instructions": SERVER_INSTRUCTIONS
    })
}

/// 给宿主（Cursor 等）的系统说明：装好后 Agent 应据此自我介绍与选工具。
const SERVER_INSTRUCTIONS: &str = r#"你是通过「鹊桥 / remote-assist」MCP 协助远端电脑的助手。

连接状态：
- MCP 可在未连接时启动。若尚未配对，先调用 connect，传入用户给的短码（如 AB12CD-12345678）或完整 remoteassist:pair?... 载荷；短码可另传 relay（默认用环境变量 REMOTE_ASSIST_RELAY 或内置中继）。
- 用 connection_status 查看是否已连上；换人/换会话时先 disconnect 再 connect。
- 未连接时调用业务工具会失败并提示先 connect，不要假装已连上。

能力边界：
- 可以：查看进程、服务、目录、文本文件、日志尾部；提议在远端执行命令。
- 不可以：远程桌面、键鼠操控、绕过对方本机确认窗、假装写操作已成功。
- run_command 会在对方电脑弹出确认；对方拒绝或超时则失败，不要重试刷屏。

推荐工作方式：
1. 未连接则 connect；已连接再只读排查（list_processes / list_services / list_dir / read_log_tail / read_file）。
2. 向用户用中文说明发现与建议；需要改动时再用 run_command，并写清 summary。
3. 路径用远端机器上的真实路径（Windows 如 C:\Users\...）。
4. 同一时间对方通常只接受一个协助端。

开场建议（用户未指定任务时）：先 connection_status；未连接则请用户给短码并 connect；已连接则 list_processes，再给 3～5 条可执行排查建议。"#;

fn handle_tools_list() -> Value {
    json!({
        "tools": [
            tool_def(
                "connect",
                "连接远端被控端。参数 payload 为短码（如 AB12CD-12345678）或完整 remoteassist:pair?... 凭证；短码可另传 relay（host:port），缺省用 REMOTE_ASSIST_RELAY / 内置中继。已连接时会先断开再连新会话。对方须已开启接受协助并点允许。",
                json!({
                    "type":"object",
                    "properties":{
                        "payload":{"type":"string","description":"短码或完整 pair 载荷"},
                        "relay":{"type":"string","description":"短码中继 host:port，可选"}
                    },
                    "required":["payload"]
                }),
            ),
            tool_def(
                "disconnect",
                "断开当前远端会话。未连接时也安全。无需参数。",
                json!({"type":"object","properties":{}}),
            ),
            tool_def(
                "connection_status",
                "查看是否已配对远端。返回 connected 或 disconnected。无需参数。",
                json!({"type":"object","properties":{}}),
            ),
            tool_def(
                "list_processes",
                "列出远端正在运行的进程（名称与数量）。排查卡顿、重复进程、是否在跑某程序时优先使用。需已 connect。无需参数。",
                json!({"type":"object","properties":{}}),
            ),
            tool_def(
                "list_services",
                "列出远端 Windows 服务及状态。排查开机服务、某个服务是否在跑时使用。需已 connect。无需参数。",
                json!({"type":"object","properties":{}}),
            ),
            tool_def(
                "list_dir",
                "列出远端某目录下的文件与子目录。参数 path 为远端绝对路径。需已 connect。",
                json!({
                    "type":"object",
                    "properties":{
                        "path":{"type":"string","description":"远端目录绝对路径，如 C:\\\\Users\\\\foo\\\\AppData\\\\Local"}
                    },
                    "required":["path"]
                }),
            ),
            tool_def(
                "read_file",
                "读取远端文本文件全文（适合配置、小日志）。大文件请改用 read_log_tail。参数 path 为远端文件绝对路径。需已 connect。",
                json!({
                    "type":"object",
                    "properties":{
                        "path":{"type":"string","description":"远端文件绝对路径"}
                    },
                    "required":["path"]
                }),
            ),
            tool_def(
                "read_log_tail",
                "读取远端日志文件末尾若干行。参数 path 必填；lines 可选，默认约 80。需已 connect。",
                json!({
                    "type":"object",
                    "properties":{
                        "path":{"type":"string","description":"远端日志文件绝对路径"},
                        "lines":{"type":"integer","description":"读取末尾行数，默认 80"}
                    },
                    "required":["path"]
                }),
            ),
            tool_def(
                "run_command",
                "向远端提议执行一条命令。对方本机必须点允许才会真正执行，不可绕过。请提供对人友好的 summary，以及要执行的 command。高风险操作（删文件、关机、改权限）务必先征询用户。需已 connect。",
                json!({
                    "type":"object",
                    "properties":{
                        "command":{"type":"string","description":"要在远端执行的命令行"},
                        "summary":{"type":"string","description":"给人看的一句话说明，会显示在对方确认窗"}
                    },
                    "required":["command"]
                }),
            ),
        ]
    })
}

fn tool_def(name: &str, description: &str, input_schema: Value) -> Value {
    json!({
        "name": name,
        "description": description,
        "inputSchema": input_schema
    })
}

fn tool_text(text: impl Into<String>, is_error: bool) -> Value {
    json!({
        "content": [{ "type": "text", "text": text.into() }],
        "isError": is_error
    })
}

fn handle_tools_call(params: Option<&Value>) -> Result<Value, String> {
    let params = params.ok_or_else(|| "missing params".to_string())?;
    let name = params
        .get("name")
        .and_then(|v| v.as_str())
        .ok_or_else(|| "missing tool name".to_string())?;
    let args_node = params.get("arguments").cloned().unwrap_or(json!({}));

    match name {
        "connect" => return handle_connect(&args_node),
        "disconnect" => return handle_disconnect(),
        "connection_status" => return handle_connection_status(),
        _ => {}
    }

    let tool = map_mcp_tool(name).ok_or_else(|| format!("unknown tool: {name}"))?;
    let mut args = BTreeMap::new();
    match tool {
        ToolKind::ReadLogTail => {
            let path = arg_str(&args_node, "path").ok_or_else(|| "path required".to_string())?;
            args.insert("path".into(), Value::String(path));
            let lines = arg_str(&args_node, "lines").unwrap_or_else(|| "80".into());
            args.insert("lines".into(), Value::String(lines));
        }
        ToolKind::ListDir | ToolKind::ReadFile => {
            let path = arg_str(&args_node, "path").ok_or_else(|| "path required".to_string())?;
            args.insert("path".into(), Value::String(path));
        }
        ToolKind::RunCommand => {
            let command =
                arg_str(&args_node, "command").ok_or_else(|| "command required".to_string())?;
            args.insert("command".into(), Value::String(command));
            if let Some(summary) = arg_str(&args_node, "summary") {
                args.insert("summary".into(), Value::String(summary));
            }
        }
        ToolKind::ListProcesses | ToolKind::ListServices => {}
    }

    let mut guard = SESSION.lock().map_err(|_| "session lock".to_string())?;
    let Some(session) = guard.as_mut() else {
        return Ok(tool_text(
            "尚未连接远端。请先调用 connect，传入短码或完整 pair 载荷（短码可另传 relay）。",
            true,
        ));
    };
    let resp = session.invoke(tool, args).map_err(|e| e.to_string())?;

    Ok(tool_text(resp.text, !resp.ok))
}

fn handle_connect(args: &Value) -> Result<Value, String> {
    let payload = arg_str(args, "payload").ok_or_else(|| "payload required".to_string())?;
    let relay = arg_str(args, "relay").unwrap_or_else(|| queqiao_protocol::session::resolve_relay(None));

    // 先清旧会话，避免换码时占着旧连接。
    {
        let mut guard = SESSION.lock().map_err(|_| "session lock".to_string())?;
        *guard = None;
    }

    match AssistSession::connect_with_relay(&payload, "鹊桥 MCP", &relay) {
        Ok(session) => {
            *SESSION.lock().map_err(|_| "session lock".to_string())? = Some(session);
            eprintln!("[mcp] paired ok (connect)");
            Ok(tool_text(
                "已连接远端。可开始 list_processes / list_dir 等只读排查。",
                false,
            ))
        }
        Err(e) => {
            eprintln!("[mcp] connect failed: {e}");
            Ok(tool_text(
                format!(
                    "配对失败: {e}\n请确认对方已开启接受协助并点了允许；短码还需 relay 与对方同中继。"
                ),
                true,
            ))
        }
    }
}

fn handle_disconnect() -> Result<Value, String> {
    let mut guard = SESSION.lock().map_err(|_| "session lock".to_string())?;
    let was = guard.is_some();
    *guard = None;
    Ok(tool_text(
        if was {
            "已断开远端会话。"
        } else {
            "当前本未连接。"
        },
        false,
    ))
}

fn handle_connection_status() -> Result<Value, String> {
    let guard = SESSION.lock().map_err(|_| "session lock".to_string())?;
    Ok(tool_text(
        if guard.is_some() {
            "connected"
        } else {
            "disconnected"
        },
        false,
    ))
}

fn arg_str(args: &Value, key: &str) -> Option<String> {
    match args.get(key)? {
        Value::String(s) => {
            let t = s.trim();
            if t.is_empty() {
                None
            } else {
                Some(t.to_string())
            }
        }
        Value::Number(n) => Some(n.to_string()),
        other => {
            let s = other.to_string();
            let t = s.trim().trim_matches('"');
            if t.is_empty() {
                None
            } else {
                Some(t.to_string())
            }
        }
    }
}

#[derive(Debug, Clone, Copy)]
enum Framing {
    ContentLength,
    Ndjson,
}

fn read_mcp_message(stdin: &mut impl Read) -> io::Result<Option<(String, Framing)>> {
    let mut content_length: Option<usize> = None;
    let mut saw_any = false;
    loop {
        let mut line_buf = Vec::new();
        read_line(stdin, &mut line_buf)?;
        if line_buf.is_empty() {
            return if saw_any {
                Err(io::Error::new(io::ErrorKind::UnexpectedEof, "eof in headers"))
            } else {
                Ok(None)
            };
        }
        saw_any = true;
        while line_buf.last().is_some_and(|b| *b == b'\n' || *b == b'\r') {
            line_buf.pop();
        }
        if line_buf.is_empty() {
            break;
        }
        let line = String::from_utf8_lossy(&line_buf);
        let trimmed = line.trim_start();
        // NDJSON：首行即 JSON 对象（Cursor 等）；规范宿主走 Content-Length 头。
        if content_length.is_none() && trimmed.starts_with('{') {
            return Ok(Some((trimmed.to_string(), Framing::Ndjson)));
        }
        let lower = line.to_ascii_lowercase();
        if let Some(rest) = lower.strip_prefix("content-length:") {
            content_length = rest.trim().parse().ok();
        }
    }
    let Some(len) = content_length else {
        return Err(io::Error::new(
            io::ErrorKind::InvalidData,
            "missing Content-Length",
        ));
    };
    let mut body = vec![0u8; len];
    stdin.read_exact(&mut body)?;
    Ok(Some((
        String::from_utf8_lossy(&body).into_owned(),
        Framing::ContentLength,
    )))
}

fn read_line(stdin: &mut impl Read, buf: &mut Vec<u8>) -> io::Result<()> {
    let mut byte = [0u8; 1];
    loop {
        match stdin.read(&mut byte)? {
            0 => {
                if buf.is_empty() {
                    return Ok(());
                }
                buf.push(b'\n');
                return Ok(());
            }
            _ => {
                buf.push(byte[0]);
                if byte[0] == b'\n' {
                    return Ok(());
                }
                if buf.len() > 64 * 1024 {
                    return Err(io::Error::new(io::ErrorKind::InvalidData, "header too long"));
                }
            }
        }
    }
}

fn write_mcp_message(stdout: &mut impl Write, body: &str, framing: Framing) -> io::Result<()> {
    let bytes = body.as_bytes();
    match framing {
        Framing::ContentLength => {
            write!(stdout, "Content-Length: {}\r\n\r\n", bytes.len())?;
            stdout.write_all(bytes)?;
        }
        Framing::Ndjson => {
            stdout.write_all(bytes)?;
            stdout.write_all(b"\n")?;
        }
    }
    stdout.flush()?;
    Ok(())
}

#[cfg(test)]
mod framing_tests {
    use super::*;
    use std::io::Cursor;

    #[test]
    fn reads_content_length_frame() {
        let body = r#"{"jsonrpc":"2.0","id":1,"method":"ping"}"#;
        let raw = format!("Content-Length: {}\r\n\r\n{}", body.len(), body);
        let mut cur = Cursor::new(raw.into_bytes());
        let (msg, framing) = read_mcp_message(&mut cur).unwrap().unwrap();
        assert!(matches!(framing, Framing::ContentLength));
        assert_eq!(msg, body);
    }

    #[test]
    fn reads_ndjson_frame() {
        let body = r#"{"jsonrpc":"2.0","id":1,"method":"ping"}"#;
        let raw = format!("{body}\n");
        let mut cur = Cursor::new(raw.into_bytes());
        let (msg, framing) = read_mcp_message(&mut cur).unwrap().unwrap();
        assert!(matches!(framing, Framing::Ndjson));
        assert_eq!(msg, body);
    }

    #[test]
    fn writes_matching_framing() {
        let body = r#"{"ok":true}"#;
        let mut cl = Vec::new();
        write_mcp_message(&mut cl, body, Framing::ContentLength).unwrap();
        assert!(cl.starts_with(b"Content-Length:"));
        let mut nd = Vec::new();
        write_mcp_message(&mut nd, body, Framing::Ndjson).unwrap();
        assert_eq!(nd, b"{\"ok\":true}\n");
    }

    #[test]
    fn negotiates_known_and_unknown_protocol_versions() {
        assert_eq!(negotiate_protocol_version("2025-11-25"), "2025-11-25");
        assert_eq!(negotiate_protocol_version("2024-11-05"), "2024-11-05");
        assert_eq!(negotiate_protocol_version("2099-01-01"), "2024-11-05");
    }
}
