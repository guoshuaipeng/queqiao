//! 中继 JSONL 与业务帧。

use std::io::{self, Read, Write};
use std::net::TcpStream;
use std::time::Duration;

use serde_json::{json, Value};
use socket2::{SockRef, TcpKeepalive};

pub fn prepare(stream: &TcpStream) {
    let _ = stream.set_nodelay(true);
    let _ = stream.set_read_timeout(Some(Duration::from_secs(1)));
    let _ = stream.set_write_timeout(Some(Duration::from_secs(8)));
    let sock = SockRef::from(stream);
    let _ = sock.set_keepalive(true);
    let params = TcpKeepalive::new()
        .with_time(Duration::from_secs(30))
        .with_interval(Duration::from_secs(10));
    #[cfg(unix)]
    let params = params.with_retries(3);
    let _ = sock.set_tcp_keepalive(&params);
}

pub fn write_json_line(stream: &mut TcpStream, value: &Value) -> io::Result<()> {
    let mut bytes =
        serde_json::to_vec(value).map_err(|_| io::Error::new(io::ErrorKind::InvalidData, "json"))?;
    bytes.push(b'\n');
    stream.write_all(&bytes)?;
    stream.flush()?;
    Ok(())
}

pub fn read_json_line(stream: &mut TcpStream, alive: &dyn Fn() -> bool) -> io::Result<Value> {
    let mut buf = Vec::new();
    let mut byte = [0u8; 1];
    loop {
        if !alive() {
            return Err(io::Error::new(io::ErrorKind::Interrupted, "cancelled"));
        }
        match stream.read(&mut byte) {
            Ok(0) => return Err(io::Error::new(io::ErrorKind::UnexpectedEof, "eof")),
            Ok(_) => {
                if byte[0] == b'\n' {
                    break;
                }
                if byte[0] != b'\r' {
                    buf.push(byte[0]);
                }
                if buf.len() > 65_536 {
                    return Err(io::Error::new(io::ErrorKind::InvalidData, "line"));
                }
            }
            Err(e) if is_timeout(&e) => continue,
            Err(e) => return Err(e),
        }
    }
    serde_json::from_slice(&buf).map_err(|_| io::Error::new(io::ErrorKind::InvalidData, "bad json"))
}

pub fn write_frame(
    stream: &mut dyn Write,
    kind: &str,
    id: &str,
    token: Option<&str>,
    payload: Value,
) -> io::Result<()> {
    let mut env = json!({
        "type": kind,
        "id": id,
        "payload": payload,
    });
    if let Some(token) = token {
        env["token"] = json!(token);
    }
    let bytes =
        serde_json::to_vec(&env).map_err(|_| io::Error::new(io::ErrorKind::InvalidData, "json"))?;
    if bytes.len() > 8 * 1024 * 1024 {
        return Err(io::Error::new(io::ErrorKind::InvalidData, "frame"));
    }
    let len = (bytes.len() as u32).to_le_bytes();
    stream.write_all(&len)?;
    stream.write_all(&bytes)?;
    stream.flush()?;
    Ok(())
}

pub struct Frame {
    pub kind: String,
    pub id: String,
    pub token: String,
    pub payload: Value,
}

pub fn read_frame(stream: &mut dyn Read, alive: &dyn Fn() -> bool) -> io::Result<Frame> {
    let mut header = [0u8; 4];
    read_exact(stream, &mut header, alive)?;
    let len = u32::from_le_bytes(header) as usize;
    if len == 0 || len > 8 * 1024 * 1024 {
        return Err(io::Error::new(io::ErrorKind::InvalidData, "frame length"));
    }
    let mut body = vec![0u8; len];
    read_exact(stream, &mut body, alive)?;
    let value: Value =
        serde_json::from_slice(&body).map_err(|_| io::Error::new(io::ErrorKind::InvalidData, "bad json"))?;
    Ok(Frame {
        kind: value
            .get("type")
            .and_then(|v| v.as_str())
            .unwrap_or("")
            .to_string(),
        id: value
            .get("id")
            .and_then(|v| v.as_str())
            .unwrap_or("")
            .to_string(),
        token: value
            .get("token")
            .and_then(|v| v.as_str())
            .unwrap_or("")
            .to_string(),
        payload: value.get("payload").cloned().unwrap_or(Value::Null),
    })
}

fn read_exact(stream: &mut dyn Read, buf: &mut [u8], alive: &dyn Fn() -> bool) -> io::Result<()> {
    let mut off = 0;
    while off < buf.len() {
        if !alive() {
            return Err(io::Error::new(io::ErrorKind::Interrupted, "cancelled"));
        }
        match stream.read(&mut buf[off..]) {
            Ok(0) => return Err(io::Error::new(io::ErrorKind::UnexpectedEof, "eof")),
            Ok(n) => off += n,
            Err(e) if is_timeout(&e) => continue,
            Err(e) => return Err(e),
        }
    }
    Ok(())
}

fn is_timeout(err: &io::Error) -> bool {
    matches!(
        err.kind(),
        io::ErrorKind::TimedOut | io::ErrorKind::WouldBlock
    )
}

pub fn new_id() -> String {
    let mut b = [0u8; 16];
    let _ = getrandom::getrandom(&mut b);
    b.iter().map(|x| format!("{x:02x}")).collect()
}
