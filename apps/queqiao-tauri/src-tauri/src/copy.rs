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
pub const CANCELLED_CONNECT: &str = "已取消，没有连上。";
pub const BUSY_OP: &str = "正在执行…";
pub const EMPTY_AUDIT: &str = "对方还没有做任何操作";
pub const EMPTY_RESULT: &str = "点上面的操作，结果会显示在这里";
pub const INSTALL_HINT: &str = "已复制安装说明，请发给对方。";
pub const MCP_NEED: &str = "请先开启接受协助或粘贴完整连接凭证，再复制 MCP 配置。";
pub const MCP_OK: &str = "已复制 MCP 配置。请粘贴到 Cursor 的 MCP 设置并保存。";
pub const MCP_PLACEHOLDER: &str =
    "已复制 MCP 配置（还没找到本机的 RemoteAssist.Mcp.exe，路径需要改成实际程序）。";
pub const USAGE_OK: &str = "已复制使用说明。";
pub const NO_RESULT: &str = "还没有结果可复制。";
pub const RESULT_COPIED: &str = "已复制结果。";
pub const DEFAULT_RELAY: &str = "121.196.45.81:17891";
pub const SITE: &str = "http://121.196.45.81/";
pub const CONFIRM_SECS: u64 = 60;

pub fn install_text() -> String {
    format!(
        "请打开官网下载鹊桥：{SITE}\n装好后打开，点「开启接受协助」，再点「复制连接凭证」，把整段发给我。"
    )
}

pub fn usage_text() -> &'static str {
    "1. 在鹊桥里点「复制 MCP 配置」。\n2. 打开 Cursor（或同类工具）的 MCP 设置，粘贴进去并保存。\n3. 刷新或重启后再用智能体协助。\n对方电脑上要改动的操作仍会弹确认，智能体不能绕过。"
}
