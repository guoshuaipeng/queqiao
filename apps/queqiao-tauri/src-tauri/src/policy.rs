//! 信任档、高风险写、取消世代。执行与否只在这里判定，界面按钮不能单独放行。

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Trust {
    Ask,
    Relaxed,
    Full,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Tool {
    ListProcesses,
    ReadLogTail,
    ListDir,
    ReadFile,
    RunCommand,
}

impl Tool {
    pub fn parse(name: &str) -> Option<Self> {
        match name {
            "ListProcesses" => Some(Self::ListProcesses),
            "ReadLogTail" => Some(Self::ReadLogTail),
            "ListDir" => Some(Self::ListDir),
            "ReadFile" => Some(Self::ReadFile),
            "RunCommand" => Some(Self::RunCommand),
            _ => None,
        }
    }

    pub fn as_str(self) -> &'static str {
        match self {
            Self::ListProcesses => "ListProcesses",
            Self::ReadLogTail => "ReadLogTail",
            Self::ListDir => "ListDir",
            Self::ReadFile => "ReadFile",
            Self::RunCommand => "RunCommand",
        }
    }

    pub fn is_read(self) -> bool {
        !matches!(self, Self::RunCommand)
    }
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Decision {
    Allow,
    Deny,
    Timeout,
    Cancelled,
}

/// 持久化只允许每次询问 / 宽松。完全信任一律落回每次询问。
pub fn persistable(trust: Trust) -> Trust {
    match trust {
        Trust::Full => Trust::Ask,
        other => other,
    }
}

pub fn parse_saved_trust(raw: &str) -> Trust {
    match raw.trim() {
        "relaxed" => Trust::Relaxed,
        _ => Trust::Ask,
    }
}

pub fn trust_token(trust: Trust) -> &'static str {
    match persistable(trust) {
        Trust::Relaxed => "relaxed",
        _ => "ask",
    }
}

pub fn is_high_risk(command: &str) -> bool {
    let c = command.trim().to_ascii_lowercase();
    if c.is_empty() {
        return false;
    }
    let has = |s: &str| c.contains(s);
    let starts = |s: &str| c.starts_with(s);
    if has("del ")
        || starts("del ")
        || has("rmdir")
        || has("rd ")
        || has("remove-item")
        || has("rm -")
        || has(" erase ")
        || starts("erase ")
        || has("unlink")
        || has("shred ")
        || has("format ")
        || starts("format ")
        || has("format.com")
        || has("diskpart")
        || has("mkfs")
        || has("shutdown")
        || has("restart-computer")
        || has("stop-computer")
        || has("reboot")
        || has("init 0")
        || has("init 6")
        || has("poweroff")
        || (has("wmic os where") && has("shutdown"))
        || has("net user")
        || has("net localgroup")
        || has("adduser")
        || has("usermod")
        || has("icacls")
        || has("takeown")
        || has("cacls")
        || has("setacl")
        || has("runas ")
        || has("chmod ")
        || has("chown ")
        || has("passwd")
        || has("reg add")
        || has("reg delete")
        || has("reg import")
        || has("regedit")
        || (has("set-itemproperty") && has("hk"))
        || has("new-itemproperty")
        || has("\\hkey_")
        || has("hklm")
        || has("hkcu\\")
    {
        return true;
    }
    false
}

pub fn needs_local_confirm(trust: Trust, tool: Tool, command: &str) -> bool {
    if tool.is_read() {
        return false;
    }
    match trust {
        Trust::Full => false,
        Trust::Relaxed => is_high_risk(command),
        Trust::Ask => true,
    }
}

/// 写操作是否可以真正执行。世代已失效或未允许时一律不执行。
pub fn may_execute(needs_confirm: bool, decision: Option<Decision>, alive: bool) -> bool {
    if !alive {
        return false;
    }
    if !needs_confirm {
        return true;
    }
    matches!(decision, Some(Decision::Allow))
}

pub fn write_was_timeout(decision: Decision) -> bool {
    matches!(decision, Decision::Timeout)
}

pub fn same_generation(current: u64, task: u64) -> bool {
    current == task && task != 0
}

/// 完全信任只在「这一次被协助会话 + 这一代」里生效。结束、换代、离开会话都回到已保存的档。
pub fn effective_trust(saved: Trust, full_gen: u64, current_gen: u64, in_host_session: bool) -> Trust {
    if in_host_session && full_gen != 0 && same_generation(current_gen, full_gen) {
        Trust::Full
    } else {
        persistable(saved)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn default_ask_confirms_every_write() {
        assert!(needs_local_confirm(Trust::Ask, Tool::RunCommand, "echo hi"));
        assert!(!needs_local_confirm(Trust::Ask, Tool::ListProcesses, ""));
    }

    #[test]
    fn relaxed_still_asks_high_risk() {
        assert!(!needs_local_confirm(
            Trust::Relaxed,
            Tool::RunCommand,
            "echo hi"
        ));
        assert!(needs_local_confirm(
            Trust::Relaxed,
            Tool::RunCommand,
            "del C:\\temp\\a.txt"
        ));
        assert!(needs_local_confirm(
            Trust::Relaxed,
            Tool::RunCommand,
            "shutdown /r"
        ));
    }

    #[test]
    fn full_trust_skips_prompt_but_cancel_blocks_execute() {
        assert!(!needs_local_confirm(
            Trust::Full,
            Tool::RunCommand,
            "del C:\\temp\\a.txt"
        ));
        assert!(!may_execute(false, None, false));
        assert!(may_execute(false, None, true));
        assert!(!may_execute(true, Some(Decision::Deny), true));
        assert!(!may_execute(true, Some(Decision::Allow), false));
        assert!(may_execute(true, Some(Decision::Allow), true));
        assert!(!may_execute(true, None, true));
    }

    #[test]
    fn full_trust_is_not_persistable() {
        assert_eq!(persistable(Trust::Full), Trust::Ask);
        assert_eq!(parse_saved_trust("full"), Trust::Ask);
        assert_eq!(parse_saved_trust("relaxed"), Trust::Relaxed);
        assert_eq!(trust_token(Trust::Full), "ask");
    }

    #[test]
    fn stale_generation_does_not_count() {
        assert!(!same_generation(2, 1));
        assert!(same_generation(3, 3));
        assert!(!same_generation(0, 0));
    }

    #[test]
    fn timeout_cancel_and_stale_allow_do_not_execute() {
        assert!(!may_execute(true, Some(Decision::Timeout), true));
        assert!(!may_execute(true, Some(Decision::Cancelled), true));
        assert!(!may_execute(true, Some(Decision::Deny), true));
        assert!(!may_execute(true, Some(Decision::Allow), false));
    }

    #[test]
    fn full_trust_ends_with_the_session_generation() {
        assert_eq!(
            effective_trust(Trust::Ask, 4, 4, true),
            Trust::Full
        );
        assert_eq!(
            effective_trust(Trust::Relaxed, 4, 5, true),
            Trust::Relaxed
        );
        assert_eq!(effective_trust(Trust::Ask, 4, 4, false), Trust::Ask);
        assert_eq!(effective_trust(Trust::Full, 4, 4, true), Trust::Full);
        assert_eq!(effective_trust(Trust::Full, 0, 0, false), Trust::Ask);
    }
}
