#![allow(dead_code)]
//! 界面原文。安全规则见 docs/CLIENT-UI.md、docs/trust-levels.md、docs/quality-connection-credential.md。

pub const TAGLINE: &str = "亲友之间，发凭证或粘贴连接。";
pub const COPY_OK: &str = "已复制，请完整发给对方。";
pub const COPY_FAIL: &str = "没能复制，请再点一次。";
pub const OPENING: &str = "正在开启接受协助…";
pub const OPEN_FAIL: &str = "没能开启接受协助，请检查网络后重试。";
pub const STOPPING: &str = "正在停止接受协助…";
pub const STOP_FAIL: &str = "没能停止接受协助，请重试。";
pub const WAITING: &str = "等待对方连接";
pub const FIRST_HINT: &str = "开启后，把连接凭证发给来帮你的人，再等对方连接。";
pub const WAIT_NOTE: &str = "对方连上时会先弹窗问你，你点「允许协助」才算连上。";
pub const FORMAT_BAD: &str = "连接凭证格式不对，暂时连不上。请让对方重新复制后原样发来。";
pub const INCOMPLETE: &str =
    "连接凭证不完整，暂时无法连接。请让对方重新复制完整连接凭证后再发。";
#[allow(dead_code)]
pub const UNSUPPORTED_VIA: &str =
    "当前这版还不能直接连上这种凭证。请让对方重新复制完整连接凭证后再发。";
pub const CONNECTING: &str = "正在连接对方…";
pub const WAIT_ALLOW: &str = "已找到对方，等对方点「允许协助」…";
pub const PEER_DENIED: &str = "对方没有同意这次协助。";
pub const CONNECT_FAIL: &str = "没能连上，请检查网络后重试。";
pub const NOT_WAITING: &str = "对方现在没有在等待协助。请让对方重新开启，再发一次连接凭证。";
pub const PAIR_TITLE: &str = "有人想协助这台电脑";
pub const PAIR_BODY: &str =
    "对方可以查看进程、日志和文件，不能直接改你的电脑。真正要改动时，还会再问你。";
pub const WRITE_TITLE: &str = "对方想在这台电脑执行操作";
pub const DENIED: &str = "已拒绝，未做任何改动";
pub const TIMED_OUT: &str = "确认超时，未做任何改动";
pub const HELPER_TIMEOUT: &str = "对方没有回应，已自动取消。";
pub const WAIT_CONFIRM: &str = "等对方确认…";
pub const DONE: &str = "已完成";
pub const FULL_TITLE: &str = "这次协助不再询问了吗？";
pub const FULL_BODY: &str = "打开后，只对现在这位协助者、这一次有效。对方做的改动都不会再弹窗问你，但仍会记在这台电脑的记录里。你可以随时改回。这次结束后，或换一个人再连，不会继续完全信任，会回到你平时的选择。";
pub const FULL_OK: &str = "确定，这次不再询问";
pub const END_BODY: &str = "结束后对方会断开。这个连接凭证不能再用，下次需要重新开启。";
pub const SWITCH_WAIT_BODY: &str = "切换会停止当前的接受协助。";
pub const SWITCH_SESSION_BODY: &str = "切换会结束当前这次协助。";
pub const QUIT_BODY: &str = "退出会断开当前连接，并停止接受协助。";
pub const SAVED_RELAY: &str = "已保存。新的连接会使用新地址。";
pub const SAVED_RELAY_BUSY: &str =
    "当前这次协助仍走原来的服务器，结束后重新连接才会用新地址。";
pub const TRUST_LOOSE: &str = "当前是「宽松」：只有高风险改动才会问你。";
pub const TRUST_FULL: &str = "当前是「完全信任」：这次协助的改动不会再问你。";
pub const FULL_NEEDS_PEER: &str =
    "这次还没有连上的协助者，完全信任不会保存。连上之后可再打开。";
pub const HOST_CONNECTED: &str = "正在接受协助 · 已连接";
pub const HELPER_CONNECTED: &str = "正在协助对方 · 已连接";
pub const PEER_ENDED: &str = "对方已结束这次协助。";
pub const DROPPED: &str = "连接已断开。";
pub const DROPPED_NEXT: &str =
    "若对方仍在接受协助，可再点「连接」；若对方已结束，请对方重新打开接受协助并复制码发给你。";
pub const DROPPED_HOST: &str = "连接已断开。这组凭证还能用，对方可以再连接。";
pub const TOO_MANY: &str = "这组凭证尝试次数过多，已经停用。请让对方重新开启后再发一次。";
pub const LOCKED_OPEN: &str = "这组凭证尝试次数过多，已经停用。请重新开启接受协助。";
pub const CANCELLED_CONNECT: &str = "已取消，没有连上。";
pub const BUSY_OP: &str = "正在执行…";
pub const EMPTY_AUDIT: &str = "对方还没有做任何操作";
pub const EMPTY_RESULT: &str = "点上面的操作，结果会显示在这里";
pub const INSTALL_HINT: &str = "已复制安装说明，请发给对方。";
pub const MCP_OK: &str = "已复制。装到 Cursor 后，把对方短码发给 AI。";
pub const MCP_PLACEHOLDER: &str =
    "已复制，但本机还缺 MCP 程序。请先发布后再改路径。";
pub const USAGE_OK: &str = "已复制使用说明。";
pub const NO_RESULT: &str = "还没有结果可复制。";
pub const RESULT_COPIED: &str = "已复制结果。";
/// 开源默认走本机中继。正式发版可设编译期环境变量 `QUEQIAO_DEFAULT_RELAY` / `QUEQIAO_SITE`（见 `private.example/deploy.md`）。
pub const DEFAULT_RELAY: &str = match option_env!("QUEQIAO_DEFAULT_RELAY") {
    Some(v) => v,
    None => "127.0.0.1:17891",
};
pub const SITE: &str = match option_env!("QUEQIAO_SITE") {
    Some(v) => v,
    None => "http://127.0.0.1/",
};
pub const CONFIRM_SECS: u64 = 60;

pub fn install_text() -> String {
    format!(
        "请打开官网下载鹊桥：{SITE}\n装好后打开，点「开启接受协助」，再点「复制链接码」，把整段发给我。"
    )
}

pub fn usage_text() -> &'static str {
    "1. 本机先有 RemoteAssist.Mcp.exe（.\\scripts\\publish-mcp.ps1 或安装包内 resources\\mcp）。\n2. 在首页「我帮别人」浅色条或设置里点「复制 MCP 配置」（未连接也可）。\n3. 用复制内容里的一键链接安装，或把 mcp.json 写入 Cursor（~/.cursor/mcp.json）。\n4. 启用 MCP 后，把开场白发给 Agent；再把对方短码给它，用 connect 连接。\n写操作仍要对方本机确认，智能体不能绕过。详见 docs/MCP.md。"
}

/// 发给 Cursor Agent 的开场白（装好 MCP 后粘贴）。
pub fn mcp_agent_prompt() -> &'static str {
    "我已安装并启用「remote-assist」（鹊桥）MCP。请先确认工具列表里能看到 connect、disconnect、connection_status，以及 list_processes、list_services、list_dir、read_file、read_log_tail、run_command。然后：\n1）用 connection_status 看是否已连上；未连接则用 connect 传入我给的短码/凭证（短码可带 relay）；\n2）用几句话介绍你能对远端做什么、不能做什么（尤其强调写操作必须对方本机确认）；\n3）按「先只读排查、后提议改动」给出 3～5 条本轮建议；\n4）已连接后先调用 list_processes，把结果摘要给我。\n不要跳过对方确认去执行危险命令，也不要假装已经改成功。"
}

/// 组装「复制 MCP」剪贴板全文：一键安装链接 + mcp.json + 开场白。
pub fn mcp_share_text(command: &str, payload: &str, relay: Option<&str>) -> String {
    let config_json = mcp_server_config_json(command, payload, relay);
    let deeplink = mcp_install_deeplink(&config_json);
    let mcp_json = format!(
        "{{\n  \"mcpServers\": {{\n    \"remote-assist\": {config_json}\n  }}\n}}"
    );
    format!(
        "【鹊桥 MCP · 安装到 Cursor】\n\n\
1) 一键安装（用浏览器打开，或粘贴到地址栏；Cursor 会弹出确认）：\n{deeplink}\n\n\
2) 或手动写入 ~/.cursor/mcp.json / 项目 .cursor/mcp.json：\n{mcp_json}\n\n\
3) 装好并在 Cursor 里启用 remote-assist 后，把下面整段发给 Agent：\n\n\
---\n{}\n---\n",
        mcp_agent_prompt()
    )
}

fn mcp_server_config_json(command: &str, payload: &str, relay: Option<&str>) -> String {
    let cmd = json_escape(command);
    let mut env = Vec::new();
    if !payload.trim().is_empty() {
        env.push(format!(
            "\"REMOTE_ASSIST_PAYLOAD\": \"{}\"",
            json_escape(payload.trim())
        ));
    }
    if let Some(r) = relay {
        if !r.trim().is_empty() {
            env.push(format!(
                "\"REMOTE_ASSIST_RELAY\": \"{}\"",
                json_escape(r.trim())
            ));
        }
    }
    if env.is_empty() {
        format!("{{\n      \"command\": \"{cmd}\",\n      \"args\": []\n    }}")
    } else {
        format!(
            "{{\n      \"command\": \"{cmd}\",\n      \"args\": [],\n      \"env\": {{\n        {}\n      }}\n    }}",
            env.join(",\n        ")
        )
    }
}

fn mcp_install_deeplink(server_config_json: &str) -> String {
    // Cursor：name 独立；config 为单台 server 对象的 JSON 再 base64。
    let compact = server_config_json
        .chars()
        .filter(|c| !c.is_whitespace())
        .collect::<String>();
    let b64 = base64_encode(compact.as_bytes());
    format!(
        "cursor://anysphere.cursor-deeplink/mcp/install?name=remote-assist&config={}",
        url_encode_component(&b64)
    )
}

fn json_escape(s: &str) -> String {
    s.replace('\\', "\\\\").replace('"', "\\\"")
}

fn url_encode_component(s: &str) -> String {
    let mut out = String::with_capacity(s.len() * 3);
    for b in s.bytes() {
        match b {
            b'A'..=b'Z' | b'a'..=b'z' | b'0'..=b'9' | b'-' | b'_' | b'.' | b'~' => {
                out.push(b as char);
            }
            _ => out.push_str(&format!("%{b:02X}")),
        }
    }
    out
}

fn base64_encode(data: &[u8]) -> String {
    const T: &[u8] = b"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    let mut out = String::with_capacity((data.len() + 2) / 3 * 4);
    let mut i = 0;
    while i + 3 <= data.len() {
        let n = ((data[i] as u32) << 16) | ((data[i + 1] as u32) << 8) | (data[i + 2] as u32);
        out.push(T[((n >> 18) & 63) as usize] as char);
        out.push(T[((n >> 12) & 63) as usize] as char);
        out.push(T[((n >> 6) & 63) as usize] as char);
        out.push(T[(n & 63) as usize] as char);
        i += 3;
    }
    match data.len() - i {
        1 => {
            let n = (data[i] as u32) << 16;
            out.push(T[((n >> 18) & 63) as usize] as char);
            out.push(T[((n >> 12) & 63) as usize] as char);
            out.push('=');
            out.push('=');
        }
        2 => {
            let n = ((data[i] as u32) << 16) | ((data[i + 1] as u32) << 8);
            out.push(T[((n >> 18) & 63) as usize] as char);
            out.push(T[((n >> 12) & 63) as usize] as char);
            out.push(T[((n >> 6) & 63) as usize] as char);
            out.push('=');
        }
        _ => {}
    }
    out
}

#[cfg(test)]
mod mcp_share_tests {
    use super::*;

    #[test]
    fn share_text_contains_deeplink_and_prompt() {
        let t = mcp_share_text(
            r"E:\mcp\RemoteAssist.Mcp.exe",
            "AB12CD-12345678",
            Some("127.0.0.1:17891"),
        );
        assert!(t.contains("cursor://anysphere.cursor-deeplink/mcp/install?name=remote-assist&config="));
        assert!(t.contains("REMOTE_ASSIST_PAYLOAD"));
        assert!(t.contains("REMOTE_ASSIST_RELAY"));
        assert!(t.contains("list_processes"));
        assert!(t.contains("connect"));
        assert!(t.contains("发给 Agent"));
    }

    #[test]
    fn share_text_install_only_without_payload() {
        let t = mcp_share_text(r"E:\mcp\RemoteAssist.Mcp.exe", "", Some("121.196.45.81:17891"));
        assert!(t.contains("REMOTE_ASSIST_RELAY"));
        assert!(!t.contains("REMOTE_ASSIST_PAYLOAD"));
        assert!(t.contains("connect"));
    }

    #[test]
    fn base64_roundtrip_shape() {
        assert_eq!(base64_encode(b"abc"), "YWJj");
        assert_eq!(base64_encode(b"a"), "YQ==");
    }
}
