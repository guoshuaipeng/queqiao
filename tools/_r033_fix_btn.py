from pathlib import Path
p = Path("src/RemoteAssist.Agent/MainForm.cs")
t = p.read_text(encoding="utf-8")
old = '''        tipCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 12),
            Size = new Size(320, 64),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "点「复制码+密码」发给对方。对方连上后本机弹出确认，你点允许才开始协助。"
        });

        _btnAdvanced = MakeButton("高级 ▸", 16, y0 + 376, 100);
'''
new = '''        tipCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 12),
            Size = new Size(320, 64),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "点「复制码+密码」发给对方。对方连上后本机弹出确认，你点允许才开始协助。"
        });

        // R033：轻按钮「结束这次协助」（非实心主按钮；复制码+密码仍唯一实心主按钮）
        _btnEndAssist = new Button
        {
            Text = EndAssistCopy.ButtonText,
            Name = "btnEndAssist",
            Location = new Point(16, y0 + 312),
            Size = new Size(728, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Font = new Font(Font.FontFamily, 9.5F),
            Cursor = Cursors.Hand
        };
        _btnEndAssist.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        _btnEndAssist.FlatAppearance.BorderSize = 1;
        _btnEndAssist.Click += (_, _) => ConfirmAndEndAssist();

        _btnAdvanced = MakeButton("高级 ▸", 16, y0 + 376, 100);
'''
if "_btnEndAssist = new Button" in t:
    print("already created")
elif old not in t:
    # show nearby bytes
    idx = t.find("tipCard.Controls.Add")
    print("anchor missing; nearby:\n", repr(t[idx:idx+350]))
    raise SystemExit(1)
else:
    t = t.replace(old, new, 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("created OK")
