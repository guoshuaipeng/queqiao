from pathlib import Path

mf = Path("src/RemoteAssist.Agent/MainForm.cs")
t = mf.read_text(encoding="utf-8")

# 1) field for btnMore
if "private readonly Button _btnMore;" not in t:
    t = t.replace(
        "    private readonly ContextMenuStrip _moreMenu;",
        "    private readonly ContextMenuStrip _moreMenu;\n    private readonly Button _btnMore;\n    private Label? _skillPreviewLabel;",
    )
    print("fields ok")

# 2) replace menu construction block
old = '''        var btnMore = MakeButton("更多", 248, 168, 80);
        btnMore.Height = 28;
        btnMore.FlatStyle = FlatStyle.Flat;
        btnMore.BackColor = Color.White;
        btnMore.ForeColor = Color.FromArgb(0x5F, 0x63, 0x68);
        btnMore.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        btnMore.FlatAppearance.BorderSize = 1;
        _moreMenu = new ContextMenuStrip();
        _moreMenu.Items.Add("复制载荷", null, (_, _) => CopyPayload());
        _moreMenu.Items.Add("复制 MCP 配置", null, (_, _) =>
        {
            var outcome = McpConfigCopyHelper.Copy(_host.QrPayload, this);
            SetStatus(outcome.Message);
        });
        _moreMenu.Items.Add("复制会话码", null, (_, _) =>
        {
            try { Clipboard.SetText(_host.SessionCode); SetStatus("已复制会话码。"); }
            catch (Exception ex) { SetStatus("复制失败：" + ex.Message); }
        });
        btnMore.Click += (_, _) => _moreMenu.Show(btnMore, new Point(0, btnMore.Height));
        sessionCard.Controls.Add(btnMore);'''

new = '''        _btnMore = MakeButton("更多", 248, 168, 80);
        _btnMore.Height = 28;
        _btnMore.FlatStyle = FlatStyle.Flat;
        _btnMore.BackColor = Color.White;
        _btnMore.ForeColor = Color.FromArgb(0x5F, 0x63, 0x68);
        _btnMore.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        _btnMore.FlatAppearance.BorderSize = 1;
        _moreMenu = new ContextMenuStrip();
        _moreMenu.Items.Add("复制载荷", null, (_, _) => CopyPayload());
        _moreMenu.Items.Add("复制 MCP 配置", null, (_, _) =>
        {
            var outcome = McpConfigCopyHelper.Copy(_host.QrPayload, this);
            SetStatus(outcome.Message);
        });
        _moreMenu.Items.Add(McpInstallGuide.CopySkillButton, null, (_, _) => CopySkillInstructions());
        _moreMenu.Items.Add("复制会话码", null, (_, _) =>
        {
            try { Clipboard.SetText(_host.SessionCode); SetStatus("已复制会话码。"); }
            catch (Exception ex) { SetStatus("复制失败：" + ex.Message); }
        });
        _btnMore.Click += (_, _) => _moreMenu.Show(_btnMore, new Point(0, _btnMore.Height));
        sessionCard.Controls.Add(_btnMore);'''

if old not in t:
    raise SystemExit("menu block not found")
t = t.replace(old, new)
print("menu ok")

# 3) CopySkillInstructions + shot helpers before SetStatus
if "void CopySkillInstructions()" not in t:
    helpers = '''
    private void CopySkillInstructions()
    {
        try
        {
            Clipboard.SetText(McpInstallGuide.SkillInstructionsClipboard);
            // 反馈只写状态栏一次，不另弹气泡/预览叠字
            HideSkillPreviewForShot();
            SetStatus(McpInstallGuide.SkillCopiedHint);
        }
        catch (Exception ex)
        {
            SetStatus("复制失败：" + ex.Message);
        }
    }

    /// <summary>UiShot：展开「更多」菜单，露出「复制 Skill 说明」。</summary>
    internal void ShowMoreMenuForShot()
    {
        HideSkillPreviewForShot();
        _moreMenu.Show(_btnMore, new Point(0, _btnMore.Height));
    }

    /// <summary>UiShot：复制 Skill 说明后人话反馈（只出现一次）。</summary>
    internal void PrepareSkillCopiedForShot()
    {
        _moreMenu.Close();
        Clipboard.SetText(McpInstallGuide.SkillInstructionsClipboard);
        HideSkillPreviewForShot();
        SetStatus(McpInstallGuide.SkillCopiedHint);
    }

    /// <summary>UiShot：预览区展示 Skill 说明剪贴板正文（与 R027/R028 常量同一份）。</summary>
    internal void PrepareSkillClipboardPreviewForShot()
    {
        _moreMenu.Close();
        Clipboard.SetText(McpInstallGuide.SkillInstructionsClipboard);
        EnsureSkillPreviewForShot();
        _skillPreviewLabel!.Text = McpInstallGuide.SkillInstructionsClipboard;
        _skillPreviewLabel.Visible = true;
        _skillPreviewLabel.BringToFront();
        // 状态栏不写同一段正文，避免叠字
        SetStatus("状态栏仅提示复制结果；下方为剪贴板正文预览。");
    }

    private void EnsureSkillPreviewForShot()
    {
        if (_skillPreviewLabel is not null) return;
        _skillPreviewLabel = new Label
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
        Controls.Add(_skillPreviewLabel);
    }

    private void HideSkillPreviewForShot()
    {
        if (_skillPreviewLabel is not null)
            _skillPreviewLabel.Visible = false;
    }

'''
    anchor = "    private void SetStatus(string text) => _statusLabel.Text = text;"
    if anchor not in t:
        raise SystemExit("SetStatus anchor missing")
    t = t.replace(anchor, helpers + "\n    " + anchor)
    print("helpers ok")

mf.write_text(t, encoding="utf-8")
print("MainForm done")
