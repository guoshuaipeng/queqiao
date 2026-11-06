from pathlib import Path

hw = Path("src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
ht = hw.read_text(encoding="utf-8")

# 1) field
if "private readonly Button _btnCopySkill;" not in ht:
    ht = ht.replace(
        "    private readonly Button _btnCopyMcp;\n    private readonly TextBlock _mcpHint;",
        "    private readonly Button _btnCopyMcp;\n    private readonly Button _btnCopySkill;\n    private readonly TextBlock _mcpHint;",
    )
    print("field ok")

# 2) UI row: add skill button after mcp button; move detail text to own line
old_ui = '''        _btnCopyMcp.Click += async (_, _) => await CopyMcpConfigFromUiAsync();
        mcpRow.Children.Add(_btnCopyMcp);
        mcpRow.Children.Add(new TextBlock
        {
            Text = "详细参数见产品文档；写操作仍须对方本机确认",
            Foreground = Brush.Parse("#5F6368"),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });
        mcpStack.Children.Add(mcpRow);'''

new_ui = '''        _btnCopyMcp.Click += async (_, _) => await CopyMcpConfigFromUiAsync();
        mcpRow.Children.Add(_btnCopyMcp);
        _btnCopySkill = new Button
        {
            Name = "copySkillGuideBtn",
            Content = McpInstallGuide.CopySkillButton,
            Width = 140,
            Height = 32,
            Background = Brushes.White,
            Foreground = Brush.Parse("#1A73E8"),
            BorderBrush = Brush.Parse("#DADEE3"),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeight.Normal
        };
        _btnCopySkill.Click += async (_, _) => await CopySkillInstructionsAsync();
        mcpRow.Children.Add(_btnCopySkill);
        mcpStack.Children.Add(mcpRow);
        mcpStack.Children.Add(new TextBlock
        {
            Text = "详细参数见产品文档；写操作仍须对方本机确认",
            Foreground = Brush.Parse("#5F6368"),
            TextWrapping = TextWrapping.Wrap
        });'''

if old_ui not in ht:
    raise SystemExit("HelperWindow UI block not found")
ht = ht.replace(old_ui, new_ui)
print("ui ok")

# 3) CopySkillInstructionsAsync after CopyMcpConfigFromUiAsync
if "CopySkillInstructionsAsync" not in ht:
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
        raise SystemExit("PasteFromClipboardAsync anchor missing")
    ht = ht.replace(anchor, method + anchor)
    print("method ok")

# 4) UiShot helpers
if "PrepareSkillGuideEntryForShot" not in ht:
    helpers = '''
    /// <summary>UiShot：MCP 卡可见「复制 Skill 说明」轻按钮（与「复制 MCP 配置」并列）。</summary>
    internal void PrepareSkillGuideEntryForShot()
    {
        PrepareInstallGuideEntryForShot();
        _mcpHint.Text = "";
        _failHint.Text = "";
        Height = Math.Max(Height, 980);
    }

    /// <summary>UiShot：已点「复制 Skill 说明」后人话反馈（只出现一次）。</summary>
    internal void PrepareSkillCopiedForShot()
    {
        PrepareSkillGuideEntryForShot();
        _mcpHint.Text = McpInstallGuide.SkillCopiedHint;
        _mcpHint.Foreground = Brush.Parse("#0F9D58");
        _failHint.Text = "";
        // 预览/状态不写同一句，避免叠字
    }

    /// <summary>UiShot：预览区展示 Skill 说明剪贴板正文（与常量同一份）。</summary>
    internal void PrepareSkillClipboardPreviewForShot()
    {
        PrepareSkillGuideEntryForShot();
        _preview.Text = McpInstallGuide.SkillInstructionsClipboard;
        _preview.Foreground = Brush.Parse("#202124");
        _mcpHint.Text = "";
        _failHint.Text = "";
        Height = Math.Max(Height, 1020);
    }

'''
    # insert before PrepareInstallGuideEntryForShot or after PrepareInstallClipboardPreviewForShot
    marker = "    /// <summary>UiShot：MCP / 复制安装说明入口可见（轻按钮，非实心绿）。</summary>"
    if marker not in ht:
        raise SystemExit("PrepareInstallGuideEntry marker missing")
    ht = ht.replace(marker, helpers + marker)
    print("shots ok")

hw.write_text(ht, encoding="utf-8")
print("HelperWindow done")
