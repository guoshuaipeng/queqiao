from pathlib import Path

# --- MainForm: light end button + ConfirmAndEndAssist + ExitRequested + shot helpers ---
p = Path("src/RemoteAssist.Agent/MainForm.cs")
t = p.read_text(encoding="utf-8")

# Add ExitRequested event next to HomeRequested
if "event Action? ExitRequested" not in t:
    old = "    public event Action? HomeRequested;\n"
    new = "    public event Action? HomeRequested;\n    public event Action? ExitRequested;\n"
    if old not in t:
        raise SystemExit("HomeRequested event missing")
    t = t.replace(old, new, 1)

# Add field for end button (optional for shots)
if "_btnEndAssist" not in t:
    old = "    private readonly Button _btnMore;\n"
    new = "    private readonly Button _btnMore;\n    private readonly Button _btnEndAssist;\n"
    if old not in t:
        raise SystemExit("_btnMore field missing")
    t = t.replace(old, new, 1)

# Insert light button after tipCard label, before _btnAdvanced
anchor = '''        tipCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 12),
            Size = new Size(320, 64),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "点「复制码+密码」发给对方。对方连上后本机弹出确认，你点允许才开始协助。"
        });

        _btnAdvanced = MakeButton("高级 ▸", 16, y0 + 376, 100);
'''
insert = '''        tipCard.Controls.Add(new Label
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
if "btnEndAssist" not in t:
    if anchor not in t:
        raise SystemExit("tipCard/advanced anchor missing")
    t = t.replace(anchor, insert, 1)

# Add control to form - find where tipCard is added to Controls
# Search for Controls.Add(_btnAdvanced) or similar sequence
if "Controls.Add(_btnEndAssist)" not in t:
    # find Controls.Add for tipCard or advanced
    # Look for pattern after tipCard creation area - usually batch add at end of ctor section
    # Read around Controls.Add(_btnAdvanced)
    needle = "Controls.Add(_btnAdvanced);"
    if needle not in t:
        raise SystemExit("Controls.Add(_btnAdvanced) missing")
    t = t.replace(needle, "Controls.Add(_btnEndAssist);\n        Controls.Add(_btnAdvanced);", 1)

# ConfirmAndEndAssist method before ConfirmAndRefreshSession
method = '''
    private void ConfirmAndEndAssist()
    {
        using var dlg = new EndAssistConfirmDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _host.DisconnectAllSessions();
            _host.Stop();
        }
        catch (Exception ex)
        {
            SetStatus("结束失败：" + ex.Message);
            return;
        }

        SetStatus("已结束这次协助。");
        if (_enableReturnHome)
            HomeRequested?.Invoke();
        else
            ExitRequested?.Invoke();
    }

'''
if "ConfirmAndEndAssist" not in t or "private void ConfirmAndEndAssist()" not in t:
    # may have been partially added as click handler only
    if "private void ConfirmAndEndAssist()" not in t:
        anchor2 = "    private void ConfirmAndRefreshSession()"
        if anchor2 not in t:
            raise SystemExit("ConfirmAndRefreshSession missing")
        t = t.replace(anchor2, method + anchor2, 1)

# Shot helpers before PrepareTrayStayIdleForShot or PreparePeerIdle
helpers = '''
    /// <summary>UiShot：露出轻按钮「结束这次协助」；复制码+密码仍为实心主按钮。</summary>
    internal void PrepareEndAssistEntryForShot()
    {
        PreparePeerIdleForShot();
        SetStatus("");
        if (_advancedOpen) ToggleAdvanced();
    }

    /// <summary>UiShot：弹出结束确认窗（非模态 Show，便于同屏截图）。</summary>
    internal EndAssistConfirmDialog ShowEndAssistConfirmForShot()
    {
        var dlg = new EndAssistConfirmDialog();
        dlg.Show(this);
        return dlg;
    }

'''
if "PrepareEndAssistEntryForShot" not in t:
    # insert before PrepareTrayStayIdleForShot if present, else PreparePeerIdle
    for a in [
        "    /// <summary>UiShot：接受协助就绪态——底栏空白，不含常驻托盘句。</summary>",
        "    /// <summary>UiShot：还没人连上——主状态可发给对方；列表（暂无）。</summary>",
    ]:
        if a in t:
            t = t.replace(a, helpers + a, 1)
            break
    else:
        raise SystemExit("shot helper anchor missing")

p.write_text(t, encoding="utf-8", newline="\n")
print("MainForm OK")

# AgentTrayContext / AgentDesktopUi wire ExitRequested from MainForm
p = Path("src/RemoteAssist.Agent/AgentTrayContext.cs")
t = p.read_text(encoding="utf-8")
old = '''        _mainForm = new MainForm(host, enableReturnHome);
        if (enableReturnHome)
            _mainForm.HomeRequested += TryReturnHome;
'''
new = '''        _mainForm = new MainForm(host, enableReturnHome);
        if (enableReturnHome)
            _mainForm.HomeRequested += TryReturnHome;
        _mainForm.ExitRequested += () => ExitRequested?.Invoke();
'''
if "_mainForm.ExitRequested" not in t:
    if old not in t:
        raise SystemExit("MainForm ctor wire missing")
    t = t.replace(old, new, 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("AgentTrayContext OK")
else:
    print("AgentTrayContext already wired")
