//! 鹊桥 MCP：stdio JSON-RPC 2.0 + Content-Length。
//! 写操作仍须被控本机确认。

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
        eprintln!("Env: REMOTE_ASSIST_PAYLOAD=<remoteassist:pair?...>");
        eprintln!("Or:  RemoteAssist.Mcp.exe --payload \"remoteassist:pair?...\"");
        eprintln!("See docs/MCP.md");
        std::process::exit(0);
    }

    let payload = resolve_payload(&args);
    if payload.is_empty() {
        eprintln!("[mcp] 缺少 REMOTE_ASSIST_PAYLOAD 或 --payload");
        std::process::exit(2);
    }

    match AssistSession::connect(&payload, "鹊桥 MCP") {
        Ok(session) => {
            eprintln!("[mcp] paired ok");
            *SESSION.lock().expect("session") = Some(session);
        }
        Err(e) => {
            eprintln!("[mcp] 配对失败: {e}");
            eprintln!("  请确认对方已开启接受协助、本机确认了配对，且凭证含完整 fp=。");
            std::process::exit(3);
        }
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
                Err(e) => (
                    None,
                    Some(json!({"code": -32000, "message": e})),
                ),
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

fn handle_initialize() -> Value {
    json!({
        "protocolVersion": "2024-11-05",
        "capabilities": { "tools": {} },
        "serverInfo": { "name": "remote-assist", "version": "0.1.0" }
    })
}

fn handle_tools_list() -> Value {
    json!({
        "tools": [
            tool_def("list_processes", "列出远端 Windows 进程", json!({"type":"object","properties":{}})),
            tool_def("list_services", "列出远端 Windows 服务", json!({"type":"object","properties":{}})),
            tool_def("list_dir", "列出目录", json!({
                "type":"object",
                "properties":{"path":{"type":"string"}},
                "required":["path"]
            })),
            tool_def("read_file", "读取文本文件", json!({
                "type":"object",
                "properties":{"path":{"type":"string"}},
                "required":["path"]
            })),
            tool_def("read_log_tail", "读取日志文件尾部", json!({
                "type":"object",
                "properties":{
                    "path":{"type":"string"},
                    "lines":{"type":"integer"}
                },
                "required":["path"]
            })),
            tool_def(
                "run_command",
                "提议在远端执行命令（需对方本机确认，不可绕过）",
                json!({
                    "type":"object",
                    "properties":{
                        "command":{"type":"string"},
                        "summary":{"type":"string"}
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

fn handle_tools_call(params: Option<&Value>) -> Result<Value, String> {
    let params = params.ok_or_else(|| "missing params".to_string())?;
    let name = params
        .get("name")
        .and_then(|v| v.as_str())
        .ok_or_else(|| "missing tool name".to_string())?;
    let tool = map_mcp_tool(name).ok_or_else(|| format!("unknown tool: {name}"))?;
    let args_node = params.get("arguments").cloned().unwrap_or(json!({}));

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
    let session = guard.as_mut().ok_or_else(|| "not paired".to_string())?;
    let resp = session
        .invoke(tool, args)
        .map_err(|e| e.to_string())?;

    if resp.ok {
        Ok(json!({
            "content": [{ "type": "text", "text": resp.text }],
            "isError": false
        }))
    } else {
        Ok(json!({
            "content": [{ "type": "text", "text": resp.text }],
            "isError": true
        }))
    }
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
