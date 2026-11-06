//! 被协助端本机工具。只由会话循环在确认通过后调用，不暴露给界面命令。

use std::io::Read;
use std::path::{Path, PathBuf};
use std::process::{Command, Stdio};
use std::time::Duration;

use crate::policy::Tool;

pub fn execute(tool: Tool, args: &serde_json::Map<String, serde_json::Value>) -> Result<String, String> {
    match tool {
        Tool::ListProcesses => list_processes(),
        Tool::ListDir => list_dir(&req(args, "path")?),
        Tool::ReadFile => read_file(&req(args, "path")?),
        Tool::ReadLogTail => {
            let lines = args
                .get("lines")
                .and_then(|v| v.as_str().and_then(|s| s.parse().ok()).or_else(|| v.as_u64()))
                .unwrap_or(80)
                .clamp(1, 500) as usize;
            read_log(&req(args, "path")?, lines)
        }
        Tool::RunCommand => run_command(&req(args, "command")?),
    }
}

fn req(args: &serde_json::Map<String, serde_json::Value>, key: &str) -> Result<String, String> {
    let v = args
        .get(key)
        .and_then(|v| v.as_str())
        .map(str::trim)
        .filter(|s| !s.is_empty())
        .ok_or_else(|| format!("缺少参数: {key}"))?;
    Ok(v.to_string())
}

fn list_processes() -> Result<String, String> {
    if !cfg!(windows) {
        return Err("这台电脑暂时列不出正在运行的程序。".into());
    }
    let out = Command::new("tasklist")
        .args(["/FO", "CSV", "/NH"])
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .output()
        .map_err(|_| "没能查看正在运行的程序。".to_string())?;
    if !out.status.success() {
        return Err("没能查看正在运行的程序。".into());
    }
    let text = String::from_utf8_lossy(&out.stdout);
    let mut lines = Vec::new();
    for line in text.lines().take(200) {
        if line.trim().is_empty() {
            continue;
        }
        lines.push(line.trim().to_string());
    }
    if lines.is_empty() {
        return Err("没有读到正在运行的程序。".into());
    }
    Ok(lines.join("\n"))
}

fn list_dir(path: &str) -> Result<String, String> {
    let full = allowed_path(path)?;
    let dir = std::fs::read_dir(&full).map_err(|_| "没能查看这个文件夹。".to_string())?;
    let mut names = Vec::new();
    for ent in dir.flatten().take(500) {
        let name = ent.file_name().to_string_lossy().to_string();
        let kind = if ent.path().is_dir() { "DIR" } else { "FILE" };
        names.push(format!("{kind}\t{name}"));
    }
    names.sort();
    Ok(names.join("\n"))
}

fn read_file(path: &str) -> Result<String, String> {
    read_limited(path, 64 * 1024, false)
}

fn read_log(path: &str, lines: usize) -> Result<String, String> {
    let text = read_limited(path, 256 * 1024, true)?;
    let all: Vec<&str> = text.lines().collect();
    let start = all.len().saturating_sub(lines);
    Ok(all[start..].join("\n"))
}

fn read_limited(path: &str, max: usize, raw_ok: bool) -> Result<String, String> {
    let full = allowed_path(path)?;
    let mut file = std::fs::File::open(&full).map_err(|_| "没能读取这个文件。".to_string())?;
    let mut buf = vec![0u8; max];
    let n = file.read(&mut buf).map_err(|_| "没能读取这个文件。".to_string())?;
    let text = String::from_utf8_lossy(&buf[..n]).into_owned();
    if !raw_ok && n == max {
        return Ok(format!("{text}\n…已截断"));
    }
    Ok(text)
}

pub fn run_command(command: &str) -> Result<String, String> {
    let command = command.trim();
    if command.is_empty() {
        return Err("没有要执行的命令。".into());
    }
    if command.len() > 2000 {
        return Err("命令太长。".into());
    }
    if !cfg!(windows) {
        return Err("当前系统还不能执行这项改动。".into());
    }
    let mut child = Command::new("cmd.exe")
        .args(["/c", command])
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .stdin(Stdio::null())
        .spawn()
        .map_err(|_| "没能执行这项改动。".to_string())?;
    let start = std::time::Instant::now();
    let limit = Duration::from_secs(15);
    loop {
        if let Some(status) = child.try_wait().map_err(|_| "没能执行这项改动。".to_string())? {
            let mut stdout = String::new();
            let mut stderr = String::new();
            if let Some(mut out) = child.stdout.take() {
                let _ = out.read_to_string(&mut stdout);
            }
            if let Some(mut err) = child.stderr.take() {
                let _ = err.read_to_string(&mut stderr);
            }
            return Ok(format_command_result(status.code().unwrap_or(-1), &stdout, &stderr));
        }
        if start.elapsed() > limit {
            let _ = child.kill();
            return Err("这项改动超时，已停掉。".into());
        }
        std::thread::sleep(Duration::from_millis(30));
    }
}

fn format_command_result(code: i32, stdout: &str, stderr: &str) -> String {
    let mut body = String::new();
    if !stdout.trim().is_empty() {
        body.push_str(stdout.trim());
        body.push('\n');
    }
    if !stderr.trim().is_empty() {
        body.push_str(stderr.trim());
        body.push('\n');
    }
    body.push_str(&format!("exit={code}"));
    body
}

fn allowed_path(path: &str) -> Result<PathBuf, String> {
    if path.trim().is_empty() {
        return Err("路径是空的。".into());
    }
    let full = std::fs::canonicalize(Path::new(path)).unwrap_or_else(|_| PathBuf::from(path));
    let text = full.to_string_lossy().to_ascii_lowercase();
    if text.contains("\\.ssh") || text.contains("/.ssh") || text.contains("\\credentials") {
        return Err("这个路径不能查看。".into());
    }
    Ok(full)
}

pub fn describe_write(summary: &str, command: &str) -> (String, String) {
    let headline = if summary.trim().is_empty() {
        if is_delete(command) {
            "删除一个文件或文件夹".to_string()
        } else {
            "运行一条命令".to_string()
        }
    } else {
        summary.trim().chars().take(80).collect()
    };
    let detail: String = command.trim().chars().take(160).collect();
    (headline, detail)
}

fn is_delete(command: &str) -> bool {
    let c = command.to_ascii_lowercase();
    c.contains("del ") || c.contains("rmdir") || c.contains("remove-item") || c.contains("rm -")
}

#[cfg(test)]
mod tests {
    use super::format_command_result;

    #[test]
    fn command_output_comes_before_the_exit_line() {
        let text = format_command_result(0, "queqiao-body\n", "");
        assert!(text.starts_with("queqiao-body\n"));
        assert!(text.ends_with("exit=0"));
        assert!(!text.contains("--- stdout ---"));
        assert!(text.find("queqiao-body").unwrap() < text.find("exit=").unwrap());
    }

    #[test]
    fn empty_output_is_only_the_exit_line() {
        assert_eq!(format_command_result(1, "  \n", ""), "exit=1");
    }
}
