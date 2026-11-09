//! 被协助端本机工具。只由会话循环在确认通过后调用，不暴露给界面命令。

use std::io::{Read, Seek, SeekFrom};
use std::path::{Path, PathBuf};
use std::process::{Command, Stdio};
use std::time::Duration;

use crate::policy::Tool;

pub fn execute(tool: Tool, args: &serde_json::Map<String, serde_json::Value>) -> Result<String, String> {
    match tool {
        Tool::ListProcesses => list_processes(),
        Tool::ListDir => list_dir(&req(args, "path")?),
        Tool::ReadFile => read_file(&req(args, "path")?),
        Tool::ListServices => list_services(),
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
    format_process_names(&text)
}

fn process_name(line: &str) -> Option<String> {
    let line = line.trim();
    let rest = line.strip_prefix('"')?;
    let end = rest.find('"')?;
    let name = rest[..end].trim();
    if name.is_empty() {
        None
    } else {
        Some(name.to_string())
    }
}

fn format_process_names(csv: &str) -> Result<String, String> {
    let mut counts = std::collections::BTreeMap::<String, usize>::new();
    for line in csv.lines().take(400) {
        if let Some(name) = process_name(line) {
            *counts.entry(name).or_insert(0) += 1;
        }
    }
    if counts.is_empty() {
        return Err("没有读到正在运行的程序。".into());
    }
    let lines: Vec<String> = counts
        .into_iter()
        .map(|(name, n)| if n == 1 { name } else { format!("{name}（{n}）") })
        .collect();
    Ok(lines.join("\n"))
}

fn list_services() -> Result<String, String> {
    if !cfg!(windows) {
        return Err("这台电脑暂时列不出服务。".into());
    }
    let out = Command::new("powershell.exe")
        .args([
            "-NoProfile",
            "-NonInteractive",
            "-Command",
            "Get-Service | Sort-Object Status,Name | Select-Object -First 40 | ForEach-Object { '{0}`t{1}' -f $_.Status, $_.Name }",
        ])
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .output()
        .map_err(|_| "没能查看服务。".to_string())?;
    if !out.status.success() {
        return Err("没能查看服务。".into());
    }
    let text = String::from_utf8_lossy(&out.stdout);
    let lines: Vec<&str> = text.lines().map(str::trim).filter(|line| !line.is_empty()).take(40).collect();
    if lines.is_empty() {
        return Err("没有读到服务。".into());
    }
    Ok(lines.join("\n"))
}

fn list_dir(path: &str) -> Result<String, String> {
    let full = allowed_path(path)?;
    let dir = std::fs::read_dir(&full).map_err(|_| "没能查看这个文件夹。".to_string())?;
    let mut names = Vec::new();
    for ent in dir.flatten().take(500) {
        let name = ent.file_name().to_string_lossy().to_string();
        let kind = if ent.path().is_dir() { "文件夹" } else { "文件" };
        names.push(format!("{kind}\t{name}"));
    }
    names.sort();
    if names.is_empty() {
        return Ok("这个文件夹是空的。".into());
    }
    Ok(names.join("\n"))
}

fn read_file(path: &str) -> Result<String, String> {
    read_limited(path, 64 * 1024, false)
}

fn read_log(path: &str, lines: usize) -> Result<String, String> {
    let full = allowed_path(path)?;
    let mut file = std::fs::File::open(&full).map_err(|_| "没能读取这个文件。".to_string())?;
    let len = file.metadata().map(|m| m.len()).unwrap_or(0);
    let max = 256 * 1024u64;
    let start = len.saturating_sub(max);
    file.seek(SeekFrom::Start(start)).map_err(|_| "没能读取这个文件。".to_string())?;
    let mut buf = Vec::new();
    file.read_to_end(&mut buf).map_err(|_| "没能读取这个文件。".to_string())?;
    let text = String::from_utf8_lossy(&buf);
    let text = if start > 0 {
        text.split_once('\n').map(|(_, rest)| rest).unwrap_or("")
    } else {
        text.as_ref()
    };
    let all: Vec<&str> = text.lines().collect();
    if all.is_empty() {
        return Ok("这个日志是空的。".into());
    }
    let from = all.len().saturating_sub(lines.max(1));
    let shown = &all[from..];
    let body = shown.join("\n");
    if from > 0 {
        Ok(format!("{body}\n只显示了末尾 {} 行。", shown.len()))
    } else {
        Ok(body)
    }
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
    use super::{format_command_result, format_process_names, list_dir, read_file, read_log};

    #[test]
    fn directory_entries_say_file_and_folder() {
        let dir = std::env::temp_dir().join(format!("queqiao-kind-{}", std::process::id()));
        std::fs::create_dir_all(dir.join("sub")).unwrap();
        std::fs::write(dir.join("note.txt"), b"queqiao-file-body").unwrap();
        let listed = list_dir(dir.to_str().unwrap()).unwrap();
        assert!(listed.contains("文件\tnote.txt"));
        assert!(listed.contains("文件夹\tsub"));
        assert!(!listed.contains("FILE"));
        assert!(!listed.contains("DIR"));
        let body = read_file(dir.join("note.txt").to_str().unwrap()).unwrap();
        assert!(body.contains("queqiao-file-body"));
        std::fs::remove_dir_all(&dir).ok();
    }

    #[test]
    fn log_tail_is_the_end_and_empty_dir_is_not_blank() {
        let dir = std::env::temp_dir().join(format!("queqiao-log-{}", std::process::id()));
        std::fs::create_dir_all(&dir).unwrap();
        let empty = dir.join("empty");
        std::fs::create_dir_all(&empty).unwrap();
        assert_eq!(list_dir(empty.to_str().unwrap()).unwrap(), "这个文件夹是空的。");
        let log = dir.join("app.log");
        let mut body = String::new();
        for i in 1..=100 {
            if i == 1 {
                body.push_str("queqiao-log-head\n");
            } else if i == 100 {
                body.push_str("queqiao-log-tail\n");
            } else {
                body.push_str(&format!("line-{i}\n"));
            }
        }
        std::fs::write(&log, body).unwrap();
        let tail = read_log(log.to_str().unwrap(), 80).unwrap();
        assert!(tail.contains("queqiao-log-tail"));
        assert!(tail.contains("只显示了末尾 80 行。"));
        assert!(tail.trim_end().ends_with("只显示了末尾 80 行。"));
        assert!(!tail.contains("queqiao-log-head"));
        let short = dir.join("short.log");
        std::fs::write(&short, "one\ntwo\n").unwrap();
        let whole = read_log(short.to_str().unwrap(), 80).unwrap();
        assert_eq!(whole, "one\ntwo");
        assert!(!whole.contains("只显示了"));
        std::fs::remove_dir_all(&dir).ok();
    }

    #[test]
    fn process_list_uses_names_not_the_raw_table() {
        let csv = "\"svchost.exe\",\"100\",\"Services\",\"0\",\"1 K\"\n\"svchost.exe\",\"200\",\"Services\",\"0\",\"2 K\"\n\"notepad.exe\",\"300\",\"Console\",\"1\",\"3 K\"\n";
        let text = format_process_names(csv).unwrap();
        assert!(text.contains("svchost.exe（2）"));
        assert!(text.contains("notepad.exe"));
        assert!(!text.contains("Services"));
        assert!(!text.contains("\",\""));
    }

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
