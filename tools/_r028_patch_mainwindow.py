from pathlib import Path

mw = Path("src/RemoteAssist.Agent.Avalonia/Views/MainWindow.cs")
t = mw.read_text(encoding="utf-8")

old_ui = '''        var btnCopyMcp = new Button
        {
            Name = "copyMcpConfigBtn",
            Content = McpInstallGuide.CopyConfigButton,
            Width = 140,
            Height = 32,
            Background = Brushes.White,
            Foreground = Brush.Parse("#1A73E8"),
            BorderBrush = Brush.Parse("#DADEE3"),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeight.Normal
        };
        btnCopyMcp.Click += async (_, _) => await CopyMcpConfigAsync();
        mcpRow.Children.Add(btnCopyMcp);
        mcpRow.Children.Add(new TextBlock
        {
            Name = "mcpSideNote",
            Text = "详细参数见产品文档；写操作仍须本机确认",
            FontSize = 12,
            Foreground = Brush.Parse("#5F6368"),
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(mcpRow);'''

new_ui = '''        var btnCopyMcp = new Button
        {
            Name = "copyMcpConfigBtn",
            Content = McpInstallGuide.CopyConfigButton,
            Width = 140,
            Height = 32,
            Background = Brushes.White,
            Foreground = Brush.Parse("#1A73E8"),
            BorderBrush = Brush.Parse("#DADEE3"),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeight.Normal
        };
        btnCopyMcp.Click += async (_, _) => await CopyMcpConfigAsync();
        mcpRow.Children.Add(btnCopyMcp);
        var btnCopySkill = new Button
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
        btnCopySkill.Click += async (_, _) => await CopySkillInstructionsAsync();
        mcpRow.Children.Add(btnCopySkill);
        stack.Children.Add(mcpRow);
        stack.Children.Add(new TextBlock
        {
            Name = "mcpSideNote",
            Text = "详细参数见产品文档；写操作仍须本机确认",
            FontSize = 12,
            Foreground = Brush.Parse("#5F6368"),
            TextWrapping = TextWrapping.Wrap
        });'''

if old_ui not in t:
    raise SystemExit("MainWindow MCP UI block not found")
t = t.replace(old_ui, new_ui)
print("ui ok")

if "private async Task CopySkillInstructionsAsync()" not in t:
    method = '''
    private async Task CopySkillInstructionsAsync()
    {
        try
        {
            var clip = Clipboard;
            if (clip is null)
            {
                _mcpFeedback.Text = "复制失败：剪贴板不可用";
                _mcpFeedback.Foreground = Brush.Parse("#D93025");
                return;
            }
            await clip.SetTextAsync(McpInstallGuide.SkillInstructionsClipboard).ConfigureAwait(true);
            // 只写 MCP 反馈行一次，不写进状态行，避免重复（R017/R018 教训）
            _mcpFeedback.Text = McpInstallGuide.SkillCopiedHint;
            _mcpFeedback.Foreground = Brush.Parse("#0F9D58");
        }
        catch (Exception ex)
        {
            _mcpFeedback.Text = "复制 Skill 说明失败：" + ex.Message;
            _mcpFeedback.Foreground = Brush.Parse("#D93025");
        }
    }

'''
    anchor = "    private async Task CopyShareAsync()"
    if anchor not in t:
        raise SystemExit("CopyShareAsync anchor missing")
    t = t.replace(anchor, method + anchor)
    print("method ok")

if "PrepareSkillEntryForShot" not in t:
    helpers = '''
    /// <summary>UiShot：高级内 MCP +「复制 Skill 说明」轻按钮；R018 导语仍在。</summary>
    internal void PrepareSkillEntryForShot()
    {
        PrepareMcpEntryForShot();
        _mcpFeedback.Text = "";
        _scroll.Offset = new Vector(0, 260);
    }

    /// <summary>UiShot：复制 Skill 说明后人话反馈（只出现一次）。</summary>
    internal void PrepareSkillCopiedForShot()
    {
        PrepareSkillEntryForShot();
        _mcpFeedback.Text = McpInstallGuide.SkillCopiedHint;
        _mcpFeedback.Foreground = Brush.Parse("#0F9D58");
        // 故意不改 _status，避免与反馈句重复
        _scroll.Offset = new Vector(0, 300);
    }

    /// <summary>UiShot：MCP 反馈区展示 Skill 说明剪贴板正文（与 R027 常量同一份）。</summary>
    internal void PrepareSkillClipboardPreviewForShot()
    {
        PrepareSkillEntryForShot();
        _mcpFeedback.Text = McpInstallGuide.SkillInstructionsClipboard;
        _mcpFeedback.Foreground = Brush.Parse("#202124");
        Height = Math.Max(Height, 1120);
        _scroll.Offset = new Vector(0, 240);
    }

'''
    marker = '    /// <summary>UiShot：高级内 MCP 入口 + 可选/不自动开启说明；「复制码+密码」仍为主按钮。</summary>'
    if marker not in t:
        raise SystemExit("PrepareMcpEntry marker missing")
    t = t.replace(marker, helpers + marker)
    print("shots ok")

mw.write_text(t, encoding="utf-8")
print("MainWindow done")

# Verify optional note and side note still exact
text = mw.read_text(encoding="utf-8")
assert '可选。接受协助时不会自动对外开启 MCP；只有你主动复制配置给智能体时才用得上。' in text
assert '详细参数见产品文档；写操作仍须本机确认' in text
assert 'McpInstallGuide.SkillCopiedHint' in text
assert 'McpInstallGuide.SkillInstructionsClipboard' in text
# Ensure HelperWindow not touched this turn beyond what's already there - we won't stage helper changes
print("assertions ok")
