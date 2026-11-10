//! 短码握手。密码只留在两端，中继只看到会话码和握手报文。
//! 线格式与 Tauri 客户端 `pake.rs` 一致（SPAKE2 + HMAC）。

use std::io::{Read, Write};
use std::net::TcpStream;

use hmac::{Hmac, Mac};
use sha2::Sha256;
use spake2::{Ed25519Group, Identity, Password, Spake2};

type HmacSha256 = Hmac<Sha256>;

/// 协助端：读出经 PAKE 绑定的证书指纹。
pub fn confirm_helper(
    stream: &mut TcpStream,
    password: &str,
    code: &str,
    alive: &dyn Fn() -> bool,
) -> Result<String, ()> {
    let (id_a, id_b) = identities(code);
    let msg_a = read_blob(stream, alive)?;
    let (state, msg_b) =
        Spake2::<Ed25519Group>::start_b(&Password::new(password.as_bytes()), &id_a, &id_b);
    write_blob(stream, &msg_b)?;
    let key = state.finish(&msg_a).map_err(|_| ())?;
    let packed = read_blob(stream, alive)?;
    if packed.len() < 32 + 64 {
        return Err(());
    }
    let (tag, fp_bytes) = packed.split_at(32);
    let fp = std::str::from_utf8(fp_bytes).map_err(|_| ())?;
    if !crate::credential::is_sha256_hex(fp) {
        return Err(());
    }
    let expect = mac(&key, fp.as_bytes())?;
    if tag != expect.as_slice() {
        return Err(());
    }
    write_blob(stream, &mac(&key, b"queqiao-ok")?)?;
    Ok(fp.to_string())
}

/// 被控端：向协助端证明指纹（单测与对称校验用）。
pub fn confirm_agent(
    stream: &mut TcpStream,
    password: &str,
    code: &str,
    fingerprint: &str,
    alive: &dyn Fn() -> bool,
) -> Result<(), ()> {
    let (id_a, id_b) = identities(code);
    let (state, msg) =
        Spake2::<Ed25519Group>::start_a(&Password::new(password.as_bytes()), &id_a, &id_b);
    write_blob(stream, &msg)?;
    let peer = read_blob(stream, alive)?;
    let key = state.finish(&peer).map_err(|_| ())?;
    let mut body = mac(&key, fingerprint.as_bytes())?.to_vec();
    body.extend_from_slice(fingerprint.as_bytes());
    write_blob(stream, &body)?;
    let ack = read_blob(stream, alive)?;
    let expect = mac(&key, b"queqiao-ok")?;
    if ack.as_slice() != expect.as_slice() {
        return Err(());
    }
    Ok(())
}

fn identities(code: &str) -> (Identity, Identity) {
    let agent = format!("queqiao-v1|{code}|agent").into_bytes();
    let helper = format!("queqiao-v1|{code}|helper").into_bytes();
    (Identity::new(&agent), Identity::new(&helper))
}

fn mac(key: &[u8], msg: &[u8]) -> Result<[u8; 32], ()> {
    let mut hmac = HmacSha256::new_from_slice(key).map_err(|_| ())?;
    hmac.update(msg);
    let bytes = hmac.finalize().into_bytes();
    let mut out = [0u8; 32];
    out.copy_from_slice(&bytes);
    Ok(out)
}

fn write_blob(stream: &mut TcpStream, bytes: &[u8]) -> Result<(), ()> {
    if bytes.is_empty() || bytes.len() > 512 {
        return Err(());
    }
    let len = (bytes.len() as u32).to_le_bytes();
    stream.write_all(&len).map_err(|_| ())?;
    stream.write_all(bytes).map_err(|_| ())?;
    stream.flush().map_err(|_| ())
}

fn read_blob(stream: &mut TcpStream, alive: &dyn Fn() -> bool) -> Result<Vec<u8>, ()> {
    let mut header = [0u8; 4];
    read_exact(stream, &mut header, alive)?;
    let len = u32::from_le_bytes(header) as usize;
    if len == 0 || len > 512 {
        return Err(());
    }
    let mut buf = vec![0u8; len];
    read_exact(stream, &mut buf, alive)?;
    Ok(buf)
}

fn read_exact(stream: &mut TcpStream, buf: &mut [u8], alive: &dyn Fn() -> bool) -> Result<(), ()> {
    let mut off = 0;
    while off < buf.len() {
        if !alive() {
            return Err(());
        }
        match stream.read(&mut buf[off..]) {
            Ok(0) => return Err(()),
            Ok(n) => off += n,
            Err(e)
                if e.kind() == std::io::ErrorKind::TimedOut
                    || e.kind() == std::io::ErrorKind::WouldBlock => {}
            Err(_) => return Err(()),
        }
    }
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::net::TcpListener;
    use std::thread;

    #[test]
    fn matching_password_reveals_fingerprint() {
        let listener = TcpListener::bind("127.0.0.1:0").unwrap();
        let port = listener.local_addr().unwrap().port();
        let fp = "AB".repeat(32);
        let agent = thread::spawn(move || {
            let (mut sock, _) = listener.accept().unwrap();
            crate::wire::prepare(&sock);
            confirm_agent(&mut sock, "12345678", "AB12CD", &fp, &|| true).unwrap();
        });
        let mut helper = TcpStream::connect(("127.0.0.1", port)).unwrap();
        crate::wire::prepare(&helper);
        let got = confirm_helper(&mut helper, "12345678", "AB12CD", &|| true).unwrap();
        assert_eq!(got, "AB".repeat(32));
        agent.join().unwrap();
    }

    #[test]
    fn wrong_password_rejected() {
        let listener = TcpListener::bind("127.0.0.1:0").unwrap();
        let port = listener.local_addr().unwrap().port();
        let fp = "CD".repeat(32);
        let agent = thread::spawn(move || {
            let (mut sock, _) = listener.accept().unwrap();
            crate::wire::prepare(&sock);
            let _ = confirm_agent(&mut sock, "12345678", "AB12CD", &fp, &|| true);
        });
        let mut helper = TcpStream::connect(("127.0.0.1", port)).unwrap();
        crate::wire::prepare(&helper);
        let rejected = confirm_helper(&mut helper, "00000000", "AB12CD", &|| true);
        drop(helper);
        assert!(rejected.is_err());
        agent.join().unwrap();
    }
}
