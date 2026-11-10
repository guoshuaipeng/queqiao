//! MCP snake_case ↔ 业务 PascalCase。

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ToolKind {
    ListProcesses,
    ListServices,
    ListDir,
    ReadFile,
    ReadLogTail,
    RunCommand,
}

impl ToolKind {
    pub fn as_wire(self) -> &'static str {
        match self {
            Self::ListProcesses => "ListProcesses",
            Self::ListServices => "ListServices",
            Self::ListDir => "ListDir",
            Self::ReadFile => "ReadFile",
            Self::ReadLogTail => "ReadLogTail",
            Self::RunCommand => "RunCommand",
        }
    }

    pub fn is_write(self) -> bool {
        matches!(self, Self::RunCommand)
    }
}

/// 将 MCP 工具名映射到业务工具；未知返回 None。
pub fn map_mcp_tool(name: &str) -> Option<ToolKind> {
    match name {
        "list_processes" => Some(ToolKind::ListProcesses),
        "list_services" => Some(ToolKind::ListServices),
        "list_dir" => Some(ToolKind::ListDir),
        "read_file" => Some(ToolKind::ReadFile),
        "read_log_tail" => Some(ToolKind::ReadLogTail),
        "run_command" => Some(ToolKind::RunCommand),
        _ => None,
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn maps_snake_to_pascal() {
        assert_eq!(
            map_mcp_tool("list_processes").map(|t| t.as_wire()),
            Some("ListProcesses")
        );
        assert_eq!(
            map_mcp_tool("run_command").map(|t| t.as_wire()),
            Some("RunCommand")
        );
        assert_eq!(map_mcp_tool("nope"), None);
    }
}
