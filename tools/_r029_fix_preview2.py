from pathlib import Path
p = Path("src/RemoteAssist.Agent/MainForm.cs")
t = p.read_text(encoding="utf-8")
old = '''        _skillPreviewLabel!.Text = McpInstallGuide.SkillInstructionsClipboard;
        _skillPreviewLabel.Visible = true;
        _skillPreviewLabel.BringToFront();
        // 状态栏不写同一段正文，避免叠字
        SetStatus("状态栏仅提示复制结果；下方为剪贴板正文预览。");'''
new = '''        _skillPreviewLabel!.Text = McpInstallGuide.SkillInstructionsClipboard;
        _skillPreviewLabel.Visible = true;
        _skillPreviewLabel.BringToFront();
        Height = Math.Max(Height, 700);
        // 状态栏不写同一段正文，避免叠字
        SetStatus("状态栏仅提示复制结果；下方为剪贴板正文预览。");'''
if old not in t:
    raise SystemExit('prepare preview block not found')
p.write_text(t.replace(old, new), encoding='utf-8')
print('height ok')
