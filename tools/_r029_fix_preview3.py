from pathlib import Path

# Improve MainForm skill preview: use Panel+Label covering center
p = Path("src/RemoteAssist.Agent/MainForm.cs")
t = p.read_text(encoding="utf-8")

# change field type comment - keep Label but fix Ensure method
old = '''    private void EnsureSkillPreviewForShot()
    {
        if (_skillPreviewLabel is not null) return;
        _skillPreviewLabel = new Label
        {
            Name = "skillClipboardPreview",
            AutoSize = false,
            Location = new Point(16, 250),
            Size = new Size(744, 220),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(0x20, 0x24, 0x27),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font(Font.FontFamily, 9F),
            Visible = false
        };
        Controls.Add(_skillPreviewLabel);
    }'''

new = '''    private void EnsureSkillPreviewForShot()
    {
        if (_skillPreviewLabel is not null) return;
        _skillPreviewLabel = new Label
        {
            Name = "skillClipboardPreview",
            AutoSize = false,
            Location = new Point(24, 120),
            Size = new Size(ClientSize.Width - 48, 360),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(0x20, 0x24, 0x27),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font(Font.FontFamily, 10F),
            Padding = new Padding(12),
            Visible = false
        };
        Controls.Add(_skillPreviewLabel);
    }'''

if old not in t:
    raise SystemExit('ensure block missing')
t = t.replace(old, new)
p.write_text(t, encoding='utf-8')
print('mainform preview ok')

# Update R029Shots to use screen capture for clipboard preview too
rs = Path("tools/UiShot/R029Shots.cs")
rt = rs.read_text(encoding='utf-8')
rt = rt.replace(
'''            agent.PrepareSkillClipboardPreviewForShot();
            Application.DoEvents();
            Thread.Sleep(150);
            Application.DoEvents();
            Save(agent, Path.Combine(outDir, "r029-skill-clipboard-preview.png"));''',
'''            agent.PrepareSkillClipboardPreviewForShot();
            Application.DoEvents();
            Thread.Sleep(200);
            Application.DoEvents();
            // 叠层预览用实拍，避免 DrawToBitmap 丢 z-order
            SaveFormWithMenu(agent, Path.Combine(outDir, "r029-skill-clipboard-preview.png"));''')
rs.write_text(rt, encoding='utf-8')
print('r029shots ok')
