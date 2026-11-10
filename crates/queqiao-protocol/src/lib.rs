//! 鹊桥协助端协议库：完整载荷解析、中继 JSONL、业务帧、TLS 钉扎会话。
//! 不依赖 Tauri；写操作确认仍在被控端 UI。

pub mod credential;
pub mod session;
pub mod tls;
pub mod tools;
pub mod wire;

pub use credential::{classify, CredClass, RelayTarget, Via};
pub use session::{AssistSession, SessionError, ToolResult};
pub use tools::{map_mcp_tool, ToolKind};
