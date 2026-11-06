from pathlib import Path
p = Path("src-net48/RemoteAssist.Agent/AgentHost.cs")
t = p.read_text(encoding="utf-8")
old = '''            _audit.Write("tool_error", $"tool={tool}; {FriendConnectGuide.RedactSecureMaterial(ex.Message)}");
            await FrameIo.WriteAsync(stream, "tool_result",
                new ToolResponse { Ok = false, Error = ex.Message }, id: envelope.Id).ConfigureAwait(false);'''
new = '''            var safeErr = FriendConnectGuide.RedactSecureMaterial(ex.Message);
            _audit.Write("tool_error", $"tool={tool}; {safeErr}");
            await FrameIo.WriteAsync(stream, "tool_result",
                new ToolResponse { Ok = false, Error = safeErr }, id: envelope.Id).ConfigureAwait(false);'''
if old not in t:
    raise SystemExit('tool_error block missing')
p.write_text(t.replace(old, new), encoding='utf-8', newline='\n')
print('tool_error OK')

# round-043 note
rp = Path("docs/round-043.md")
rt = rp.read_text(encoding="utf-8")
sec = """

## 凭证日志脱敏（同轮安全补丁）

net48 控制台/审计/错误路径不得输出完整 `PairQrPayload`、密码、会话码、业务指纹：

- `AgentHost.PrintBanner` / 会话刷新：仅非敏感状态；引导界面「复制连接凭证」
- `AuditLog.Write` 统一经 `RedactSecureMaterial`；调用点去掉 `code=`/`fp=` 明文
- UI `SetStatus` / Helper 失败输出 / MCP 复制失败 / 启动失败 MessageBox：脱敏
- `SessionPasswordGenerator` / `SessionCodeGenerator`：拒绝采样消除 `byte%N` 模偏

未删界面分享（复制连接凭证 / 二维码 / 口述短码展示）。
"""
if "凭证日志脱敏" not in rt:
    if "## 参考" in rt:
        rt = rt.replace("## 参考", sec + "\n## 参考", 1)
    else:
        rt += sec
    rp.write_text(rt, encoding="utf-8", newline="\n")
    print("round-043 docs OK")
else:
    print("round-043 already has section")
