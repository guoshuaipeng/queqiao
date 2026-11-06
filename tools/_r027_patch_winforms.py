from pathlib import Path

mf = Path("src/RemoteAssist.Helper.UI/MainForm.cs")
mt = mf.read_text(encoding="utf-8")

old = '''        var btnCopyMcp = MakeButton(McpInstallGuide.CopyConfigButton, 16, 122, 140);
        btnCopyMcp.Height = 32;
        btnCopyMcp.FlatStyle = FlatStyle.Flat;
        btnCopyMcp.BackColor = Color.White;
        btnCopyMcp.ForeColor = Color.FromArgb(0x1A, 0x73, 0xE8);
        btnCopyMcp.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        btnCopyMcp.FlatAppearance.BorderSize = 1;
        btnCopyMcp.Click += (_, _) => CopyMcpConfigFromUi();
        mcpCard.Controls.Add(btnCopyMcp);
        mcpCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(170, 128),
            Size = new Size(550, 28),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "详细参数见产品文档；写操作仍须对方本机确认"
        });
        Controls.Add(mcpCard);'''

new = '''        var btnCopyMcp = MakeButton(McpInstallGuide.CopyConfigButton, 16, 122, 140);
        btnCopyMcp.Height = 32;
        btnCopyMcp.FlatStyle = FlatStyle.Flat;
        btnCopyMcp.BackColor = Color.White;
        btnCopyMcp.ForeColor = Color.FromArgb(0x1A, 0x73, 0xE8);
        btnCopyMcp.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        btnCopyMcp.FlatAppearance.BorderSize = 1;
        btnCopyMcp.Click += (_, _) => CopyMcpConfigFromUi();
        mcpCard.Controls.Add(btnCopyMcp);
        var btnCopySkill = MakeButton(McpInstallGuide.CopySkillButton, 166, 122, 140);
        btnCopySkill.Height = 32;
        btnCopySkill.FlatStyle = FlatStyle.Flat;
        btnCopySkill.BackColor = Color.White;
        btnCopySkill.ForeColor = Color.FromArgb(0x1A, 0x73, 0xE8);
        btnCopySkill.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        btnCopySkill.FlatAppearance.BorderSize = 1;
        btnCopySkill.Cursor = Cursors.Hand;
        btnCopySkill.Click += (_, _) => CopySkillInstructions();
        mcpCard.Controls.Add(btnCopySkill);
        mcpCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 158),
            Size = new Size(710, 28),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "详细参数见产品文档；写操作仍须对方本机确认"
        });
        // 略增高 MCP 卡以容纳 Skill 按钮行
        mcpCard.Height = Math.Max(mcpCard.Height, 200);
        Controls.Add(mcpCard);'''

if old not in mt:
    raise SystemExit("MainForm MCP UI block not found")
mt = mt.replace(old, new)
print("MainForm UI ok")

if "void CopySkillInstructions()" not in mt:
    method = '''
    private void CopySkillInstructions()
    {
        try
        {
            Clipboard.SetText(McpInstallGuide.SkillInstructionsClipboard);
            // 反馈只写预览行一次，不写状态行（对齐 R017 不叠字）
            _previewLabel.Text = McpInstallGuide.SkillCopiedHint;
            _previewLabel.ForeColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            SetFailHint("");
        }
        catch (Exception ex)
        {
            SetFailHint("复制 Skill 说明失败：" + ex.Message);
        }
    }
'''
    anchor = "    private void CopyInstallInstructions()"
    if anchor not in mt:
        raise SystemExit("CopyInstallInstructions anchor missing")
    mt = mt.replace(anchor, method + "\n" + anchor)
    print("MainForm method ok")

mf.write_text(mt, encoding="utf-8")
print("MainForm done")
