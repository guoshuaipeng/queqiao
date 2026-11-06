from pathlib import Path
hw = Path("src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
ht = hw.read_text(encoding="utf-8")
if "private async Task CopySkillInstructionsAsync()" not in ht:
    method = '''
    private async Task CopySkillInstructionsAsync()
    {
        try
        {
            var clip = Clipboard;
            if (clip is null)
            {
                _failHint.Text = "复制失败：剪贴板不可用";
                return;
            }
            await clip.SetTextAsync(McpInstallGuide.SkillInstructionsClipboard).ConfigureAwait(true);
            // 反馈只写 MCP 卡内提示一次，不写预览行/状态行（对齐 R017 不叠字）
            _mcpHint.Text = McpInstallGuide.SkillCopiedHint;
            _mcpHint.Foreground = Brush.Parse("#0F9D58");
            _failHint.Text = "";
        }
        catch (Exception ex)
        {
            _failHint.Text = "复制 Skill 说明失败：" + ex.Message;
        }
    }

'''
    anchor = "    private async Task PasteFromClipboardAsync()"
    if anchor not in ht:
        raise SystemExit("anchor missing")
    hw.write_text(ht.replace(anchor, method + anchor), encoding="utf-8")
    print("method inserted")
else:
    print("method already present")
