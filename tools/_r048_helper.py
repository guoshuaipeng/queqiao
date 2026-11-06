from pathlib import Path
p = Path("src-net48/RemoteAssist.Helper.UI/MainForm.cs")
t = p.read_text(encoding="utf-8")

# Add field _mcpCard
if "private readonly Panel _mcpCard" not in t:
    t = t.replace(
        "    private readonly Panel _toolCard;\n",
        "    private readonly Panel _mcpCard;\n    private readonly Panel _toolCard;\n",
        1,
    )

# ClientSize taller
t = t.replace("ClientSize = new Size(780, 640);", "ClientSize = new Size(780, 780);", 1)

# Add install guide button after pair section title area - after btnClip
old_clip = '''        var btnClip = MakeButton("从剪贴板", 626, 60, 100);
        btnClip.Click += (_, _) => PasteClipboard();
        pairCard.Controls.Add(btnClip);'''
new_clip = '''        var btnClip = MakeButton("从剪贴板", 626, 60, 100);
        btnClip.Click += (_, _) => PasteClipboard();
        pairCard.Controls.Add(btnClip);

        // R048：亲友向「复制安装说明」轻按钮（不抢「连接」主按钮）
        var btnInstallGuide = MakeButton("复制安装说明", 450, 36, 140);
        btnInstallGuide.FlatStyle = FlatStyle.Flat;
        btnInstallGuide.BackColor = Color.White;
        btnInstallGuide.ForeColor = Color.FromArgb(0x3C, 0x40, 0x43);
        btnInstallGuide.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDC, 0xE0);
        btnInstallGuide.Click += (_, _) => CopyInstallInstructions();
        pairCard.Controls.Add(btnInstallGuide);'''
if old_clip not in t:
    raise SystemExit("clip block missing")
t = t.replace(old_clip, new_clip, 1)

# Insert mcp card before _toolCard creation
old_tool = '''        _toolCard = MakeCard(16, 310, 748, 300);
        _toolCard.Controls.Add(SectionTitle("2. 排障操作（连上后可用）", 12, 10));'''
new_tool = '''        // R048：MCP 安装卡主屏常显（不必等连上）；不自动对外监听 MCP
        _mcpCard = MakeCard(16, 310, 748, 168);
        _mcpCard.Controls.Add(SectionTitle(McpInstallGuide.SectionTitle, 12, 10));
        _mcpCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 40),
            Size = new Size(710, 64),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = McpInstallGuide.StepsBlock
        });
        var btnCopyMcp = MakeButton(McpInstallGuide.CopyConfigButton, 16, 116, 140);
        btnCopyMcp.FlatStyle = FlatStyle.Flat;
        btnCopyMcp.BackColor = Color.White;
        btnCopyMcp.ForeColor = Color.FromArgb(0x3C, 0x40, 0x43);
        btnCopyMcp.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDC, 0xE0);
        btnCopyMcp.Click += (_, _) => CopyMcpConfigFromUi();
        _mcpCard.Controls.Add(btnCopyMcp);
        var btnCopySkill = MakeButton(McpInstallGuide.CopySkillButton, 166, 116, 140);
        btnCopySkill.FlatStyle = FlatStyle.Flat;
        btnCopySkill.BackColor = Color.White;
        btnCopySkill.ForeColor = Color.FromArgb(0x3C, 0x40, 0x43);
        btnCopySkill.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDC, 0xE0);
        btnCopySkill.Click += (_, _) => CopySkillInstructions();
        _mcpCard.Controls.Add(btnCopySkill);
        Controls.Add(_mcpCard);

        _toolCard = MakeCard(16, 490, 748, 300);
        _toolCard.Controls.Add(SectionTitle("2. 排障操作（连上后可用）", 12, 10));'''
if old_tool not in t:
    raise SystemExit("tool card block missing")
t = t.replace(old_tool, new_tool, 1)

# LayoutPanels: account for mcp card
old_layout = '''    private void LayoutPanels()
    {
        _advancedPanel.Visible = _advancedOpen;
        var top = 310;
        if (_advancedOpen)
        {
            _advancedPanel.Top = 310;
            top = 310 + _advancedPanel.Height + 10;
        }
        _toolCard.Top = top;
        var need = top + _toolCard.Height + 24;
        if (ClientSize.Height < need)
            ClientSize = new Size(ClientSize.Width, Math.Min(need, 900));
    }'''
new_layout = '''    private void LayoutPanels()
    {
        _advancedPanel.Visible = _advancedOpen;
        // pairCard ends ~300; MCP 卡常显在 310
        _mcpCard.Top = 310;
        var top = 310 + _mcpCard.Height + 10;
        if (_advancedOpen)
        {
            _advancedPanel.Top = top;
            top = top + _advancedPanel.Height + 10;
        }
        _toolCard.Top = top;
        var need = top + _toolCard.Height + 24;
        if (ClientSize.Height < need)
            ClientSize = new Size(ClientSize.Width, Math.Min(need, 980));
    }'''
if old_layout not in t:
    raise SystemExit("layout missing")
t = t.replace(old_layout, new_layout, 1)

# Insert copy methods before PasteClipboard
old_paste = '''    private void PasteClipboard()
    {
        try
        {
            if (!Clipboard.ContainsText())'''
# find PasteClipboard
idx = t.find("    private void PasteClipboard()")
if idx < 0:
    raise SystemExit("PasteClipboard missing")
methods = r'''
    private void CopyInstallInstructions()
    {
        try
        {
            Clipboard.SetText(WriteConfirmCopy.InstallInstructionsClipboard);
            _previewLabel.Text = WriteConfirmCopy.InstallCopiedHint;
            _previewLabel.ForeColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            ClearFailHint();
            SetStatus(WriteConfirmCopy.InstallCopiedHint);
        }
        catch (Exception ex)
        {
            SetFailHint("复制失败：" + FriendConnectGuide.RedactSecureMaterial(ex.Message));
            SetStatus("复制失败");
        }
    }

    /// <summary>
    /// R048：复制可粘贴的 MCP JSON。必须完整安全载荷（含业务 fp）；禁止缺指纹降明文。
    /// 不启动 MCP、不对外监听。
    /// </summary>
    private void CopyMcpConfigFromUi()
    {
        try
        {
            if (!TryBuildOptions(out var opts, out _, out var err))
            {
                SetFailHint(string.IsNullOrEmpty(err)
                    ? McpInstallGuide.NeedCredentialHint
                    : err);
                _previewLabel.ForeColor = Color.FromArgb(0xC5, 0x22, 0x1F);
                return;
            }

            // 禁止 Insecure / 缺 fp 生成「可连」明文 MCP 配置
            if (opts.Insecure || !TlsCertUtil.IsWellFormedFingerprint(opts.Fingerprint))
            {
                SetFailHint(FriendConnectGuide.MissingSecurityInfoHint);
                _previewLabel.ForeColor = Color.FromArgb(0xC5, 0x22, 0x1F);
                SetStatus("无法复制 MCP 配置");
                return;
            }

            string payload;
            if (opts.ViaRelay)
            {
                var relay = string.IsNullOrWhiteSpace(opts.RelayEndpoint)
                    ? RelayEndpoint.DefaultEndpoint
                    : opts.RelayEndpoint!;
                var pwd = opts.Password ?? "";
                if (string.IsNullOrWhiteSpace(pwd))
                {
                    SetFailHint(McpInstallGuide.NeedCredentialHint);
                    return;
                }
                payload = PairQrPayload.BuildRelay(
                    relay, opts.Code, pwd, opts.Fingerprint, useTls: true);
            }
            else
            {
                payload = PairQrPayload.BuildLan(
                    opts.Host, opts.Port, opts.Code, opts.Fingerprint, useTls: true);
            }

            var exists = McpExeLocator.TryResolveExisting(out var exe);
            if (!exists)
                exe = McpExeLocator.Resolve();

            Clipboard.SetText(McpConfigSnippet.BuildClipboardBundle(exe, payload));
            // 反馈不回显完整载荷
            _previewLabel.Text = exists ? McpInstallGuide.CopiedOkHint : McpInstallGuide.CopiedPlaceholderHint;
            _previewLabel.ForeColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            ClearFailHint();
            SetStatus(exists ? McpInstallGuide.CopiedOkHint : McpInstallGuide.CopiedPlaceholderHint);
        }
        catch (Exception ex)
        {
            SetFailHint("复制 MCP 配置失败：" + FriendConnectGuide.RedactSecureMaterial(ex.Message));
            SetStatus("复制失败");
        }
    }

    private void CopySkillInstructions()
    {
        try
        {
            Clipboard.SetText(McpInstallGuide.SkillInstructionsClipboard);
            _previewLabel.Text = McpInstallGuide.SkillCopiedHint;
            _previewLabel.ForeColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            ClearFailHint();
            SetStatus(McpInstallGuide.SkillCopiedHint);
        }
        catch (Exception ex)
        {
            SetFailHint("复制 Skill 说明失败：" + FriendConnectGuide.RedactSecureMaterial(ex.Message));
            SetStatus("复制失败");
        }
    }

'''
t = t[:idx] + methods + t[idx:]

# Ensure System.Drawing for Color if needed - already have
p.write_text(t, encoding="utf-8", newline="\n")
print("Helper MainForm patched")
# sanity
for s in ["CopyMcpConfigFromUi", "CopyInstallInstructions", "CopySkillInstructions", "_mcpCard", "NeedCredentialHint", "useTls: true"]:
    print(s, s in t)