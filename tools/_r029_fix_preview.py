from pathlib import Path
p = Path("src/RemoteAssist.Agent/MainForm.cs")
t = p.read_text(encoding="utf-8")
old = '''        _skillPreviewLabel = new Label
        {
            Name = "skillClipboardPreview",
            AutoSize = false,
            Location = new Point(16, 360),
            Size = new Size(720, 200),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(0x20, 0x24, 0x27),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font(Font.FontFamily, 9F),
            Visible = false
        };
        Controls.Add(_skillPreviewLabel);'''
new = '''        _skillPreviewLabel = new Label
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
        Controls.Add(_skillPreviewLabel);'''
if old not in t:
    raise SystemExit('preview label block not found')
p.write_text(t.replace(old, new), encoding='utf-8')
print('repositioned')
