from pathlib import Path

# AgentDesktopUi: skip ShowMain when return-home (shell will show)
p = Path("src-net48/RemoteAssist.Agent/AgentTrayContext.cs")
t = p.read_text(encoding="utf-8")
old = '''        _host.PairedClientsChanged += OnPairedClientsChanged;

        ShowMain();
        _tray.ShowBalloonTip(2500, "鹊桥",
            enableReturnHome
                ? "被控模式已启动。可用「返回首页」切换模式（有会话须本机确认结束）。"
                : "已在托盘运行。关闭主窗口不会退出，可随时双击托盘图标打开。",
            ToolTipIcon.Info);
    }'''
new = '''        _host.PairedClientsChanged += OnPairedClientsChanged;

        // 统一 App（enableReturnHome）：由外壳嵌入展示，避免第二主窗
        if (!enableReturnHome)
            ShowMain();
        _tray.ShowBalloonTip(2500, "鹊桥",
            enableReturnHome
                ? "被控模式已启动。可用「返回首页」切换模式（有会话须本机确认结束）。"
                : "已在托盘运行。关闭主窗口不会退出，可随时双击托盘图标打开。",
            ToolTipIcon.Info);
    }'''
if old not in t:
    raise SystemExit('balloon block missing')
t = t.replace(old, new)
p.write_text(t, encoding='utf-8', newline='\n')
print('defer ShowMain OK')

# AgentDesktopController expose attach/confirm
p = Path("src-net48/RemoteAssist.Agent/AgentDesktopController.cs")
t = p.read_text(encoding="utf-8")
if "AttachToShell" not in t:
    t = t.replace(
        "    public void ShowMain() => _ui?.ShowMain();\n",
        '''    public void ShowMain() => _ui?.ShowMain();

    public Form? MainFormControl => _ui?.MainFormControl;

    public void AttachToShell(Control hostPanel, Action activateShell) =>
        _ui?.AttachToShell(hostPanel, activateShell);

    public bool ConfirmEndSessionOrCancel(IWin32Window? owner) =>
        _ui is null || _ui.ConfirmEndSessionOrCancel(owner);
''',
        1,
    )
    # need using for Control - System.Windows.Forms already
    p.write_text(t, encoding='utf-8', newline='\n')
    print('AgentDesktopController OK')
else:
    print('AgentDesktopController already')

# Helper MainForm: confirm before HomeRequested; hide relay on status; embed flag
p = Path("src-net48/RemoteAssist.Helper.UI/MainForm.cs")
t = p.read_text(encoding="utf-8")
if "private bool _embeddedInShell" not in t:
    t = t.replace(
        "    public event Action? HomeRequested;\n",
        "    public event Action? HomeRequested;\n    private bool _embeddedInShell;\n",
        1,
    )

# Find return home button click
if "btnHome.Click += (_, _) =>" in t and "ConfirmEndSessionOrCancel" not in t:
    t = t.replace(
        "btnHome.Click += (_, _) =>\n            {\n                _returningHome = true;\n                HomeRequested?.Invoke();\n            };",
        '''btnHome.Click += (_, _) =>
            {
                if (!ConfirmEndSessionOrCancel(this))
                    return; // 取消：保持原会话原页
                _returningHome = true;
                HomeRequested?.Invoke();
            };''',
        1,
    )

# Add methods before OnFormClosing
if "ConfirmEndSessionOrCancel" not in t:
    marker = "    private void OnFormClosing(object? sender, FormClosingEventArgs e)"
    methods = '''    public void SetEmbeddedInShell(bool embedded)
    {
        _embeddedInShell = embedded;
        if (embedded)
            ControlBox = false;
    }

    /// <summary>R049：有活动协助连接时，结束须本机确认；取消则保持会话。</summary>
    public bool ConfirmEndSessionOrCancel(IWin32Window? owner)
    {
        if (_client is null && !_busy)
            return true;
        var r = MessageBox.Show(
            owner,
            _busy
                ? "正在连接对方。返回或切换将取消本次连接。是否结束？"
                : "当前已连接对方。返回或切换将断开协助连接。是否结束？",
            "结束去协助",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        return r == DialogResult.Yes;
    }

    public bool HasActiveSession => _client is not null || _busy;

'''
    if marker not in t:
        raise SystemExit('Helper OnFormClosing marker missing')
    t = t.replace(marker, methods + marker, 1)

# FormClosing: when embedded, don't dispose client on user close of child
old_fc = '''    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_returningHome)
            return;
        if (e.CloseReason == CloseReason.UserClosing)
        {
            // 独立 Helper 关闭即退出；统一 App 由外壳处理
        }
        try { _client?.Dispose(); } catch { /* ignore */ }
    }'''
new_fc = '''    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_returningHome)
            return;
        if (_embeddedInShell && e.CloseReason == CloseReason.UserClosing)
        {
            // 嵌入单主窗口：不关会话；由外壳「返回首页」确认
            e.Cancel = true;
            return;
        }
        if (e.CloseReason == CloseReason.UserClosing)
        {
            // 独立 Helper 关闭即退出
        }
        try { _client?.Dispose(); } catch { /* ignore */ }
    }'''
if old_fc not in t:
    raise SystemExit('Helper FormClosing missing')
t = t.replace(old_fc, new_fc)

# Hide relay address on main status
t = t.replace(
    'Text = "状态：未连接 · 默认中继 " + RelayEndpoint.DefaultEndpoint',
    'Text = "状态：未连接 · 经默认中继"',
)
t = t.replace(
    'SetStatus("状态：未连接 · 默认中继 " + RelayEndpoint.DefaultEndpoint);',
    'SetStatus("状态：未连接 · 经默认中继");',
)

p.write_text(t, encoding='utf-8', newline='\n')
print('Helper MainForm OK')

# HelperDesktopController
p = Path("src-net48/RemoteAssist.Helper.UI/HelperDesktopController.cs")
t = p.read_text(encoding="utf-8")
if "AttachToShell" not in t:
    t = t.replace(
        '''    public void Show()
    {
        _form.Show();
        _form.WindowState = FormWindowState.Normal;
        _form.Activate();
        _form.BringToFront();
    }
''',
        '''    public void Show()
    {
        _form.Show();
        _form.WindowState = FormWindowState.Normal;
        _form.Activate();
        _form.BringToFront();
    }

    public Form MainFormControl => _form;

    public void AttachToShell(Control hostPanel, Action activateShell)
    {
        _form.SetEmbeddedInShell(true);
        // 可见性由外壳页切换控制
        _ = hostPanel;
        _ = activateShell;
    }

    public bool ConfirmEndSessionOrCancel(IWin32Window? owner) =>
        _form.ConfirmEndSessionOrCancel(owner);
''',
    )
    p.write_text(t, encoding='utf-8', newline='\n')
    print('HelperDesktopController OK')