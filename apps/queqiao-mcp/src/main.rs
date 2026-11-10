//! 鹊桥 MCP：stdio JSON-RPC 2.0 + Content-Length。
//! 启动即可用；用 connect 传入短码/完整载荷再连远端。写操作仍须被控本机确认。

use std::collections::BTreeMap;
use std::io::{self, Read, Write};
use std::sync::Mutex;

use queqiao_protocol::session::AssistSession;
use queqiao_protocol::tools::{map_mcp_tool, ToolKind};
use serde_json::{json, Value};

static SESSION: Mutex<Option<AssistSession>> = Mutex::new(None);

fn main() {
    let args: Vec<String> = std::env::args().skip(1).collect();
    if args.iter().any(|a| a == "-h" || a == "--help" || a == "help") {
        eprintln!("RemoteAssist MCP (stdio)");
        eprintln!("启动后未连接也可被 Cursor 拉起；用工具 connect 传短码/完整载荷再连远端。");
        eprintln!("Env: REMOTE_ASSIST_PAYLOAD=<remoteassist:pair?... | CODE-########>  # 可选，有则启动时尝试配对");
        eprintln!("     REMOTE_ASSIST_RELAY=<host:port>   # 短码默认中继");
        eprintln!("Or:  RemoteAssist.Mcp.exe [--payload \"...\"] [--relay host:port]");
        eprintln!("See docs/MCP.md");
        std::process::exit(0);
    }

    let boot_payload = resolve_payload(&args);
    let boot_relay = resolve_relay_arg(&args);
    if !boot_payload.is_empty() {
        match AssistSession::connect_with_relay(&boot_payload, "鹊桥 MCP", &boot_relay) {
            Ok(session) => {
                eprintln!("[mcp] paired ok (boot)");
                *SESSION.lock().expect("session") = Some(session);
            }
            Err(e) => {
                eprintln!("[mcp] 启动时配对失败（MCP 仍可用，请用 connect 重试）: {e}");
            }
        }
    } else {
        eprintln!("[mcp] ready (disconnected); call connect with payload/short code");
    }

    let stdin = io::stdin();
    let mut stdin = stdin.lock();
    let stdout = io::stdout();
    let mut stdout = stdout.lock();

    loop {
        let msg = match read_lsp_message(&mut stdin) {
            Ok(Some(m)) => m,
            Ok(None) => break,
            Err(e) => {
                eprintln!("[mcp] read error: {e}");
                break;
            }
        };
        let node: Value = match serde_json::from_str(&msg) {
            Ok(v) => v,
            Err(_) => continue,
        };
        let id = node.get("id").cloned();
        let Some(method) = node.get("method").and_then(|m| m.as_str()) else {
            continue;
        };
        // notifications (no id)
        if id.is_none() {
            continue;
        }

        let (result, error) = match method {
            "initialize" => (Some(handle_initialize()), None),
            "tools/list" => (Some(handle_tools_list()), None),
            "tools/call" => match handle_tools_call(node.get("params")) {
                Ok(v) => (Some(v), None),
                Err(e) => (None, Some(json!({"code": -32000, "message": e}))),
            },
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
        if let Err(e) = write_lsp_message(&mut stdout, &response.to_string()) {
            eprintln!("[mcp] write error: {e}");
            break;
        }
    }
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

fn handle_initialize() -> Value {
    json!({
        "protocolVersion": "2024-11-05",
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

fn read_lsp_message(stdin: &mut impl Read) -> io::Result<Option<String>> {
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
    Ok(Some(String::from_utf8_lossy(&body).into_owned()))
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

fn write_lsp_message(stdout: &mut impl Write, body: &str) -> io::Result<()> {
    let bytes = body.as_bytes();
    write!(stdout, "Content-Length: {}\r\n\r\n", bytes.len())?;
    stdout.write_all(bytes)?;
    stdout.flush()?;
    Ok(())
}
