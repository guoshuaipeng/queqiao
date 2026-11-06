from pathlib import Path

# --- AgentDesktopUi: embedding + stronger leave confirm ---
p = Path("src-net48/RemoteAssist.Agent/AgentTrayContext.cs")
t = p.read_text(encoding="utf-8")

# Add fields and Attach API after _enableReturnHome field
if "private Control? _embedHost" not in t:
    t = t.replace(
        "    private readonly bool _enableReturnHome;\n    private bool _disposed;\n",
        "    private readonly bool _enableReturnHome;\n    private bool _disposed;\n    private Control? _embedHost;\n    private Action? _activateShell;\n",
        1,
    )

# After constructing _mainForm, mark for embed capability via method
old_try = '''    private void TryReturnHome()
    {
        if (!_enableReturnHome) return;
        var n = _host.ListPairedClients().Count;
        if (n > 0)
        {
            var r = MessageBox.Show(
                $"当前有 {n} 个已配对协助方。返回首页将断开全部会话并停止被控监听。是否继续？",
                "返回首页",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (r != DialogResult.Yes) return;
        }

        HomeRequested?.Invoke();
    }'''

new_try = '''    /// <summary>R049：嵌入统一主窗口；托盘「打开主界面」激活外壳而非第二主窗。</summary>
    public void AttachToShell(Control hostPanel, Action activateShell)
    {
        _embedHost = hostPanel;
        _activateShell = activateShell;
        _mainForm.SetEmbeddedInShell(true);
        // 不在此处 Show：由外壳 ShowAcceptPage 负责可见性
    }

    public Form MainFormControl => _mainForm;

    /// <summary>若有活动被控会话，本机确认结束后才返回 true。</summary>
    public bool ConfirmEndSessionOrCancel(IWin32Window? owner)
    {
        var n = _host.ListPairedClients().Count;
        var listening = _started_or_active();
        if (n == 0 && !listening)
            return true;

        var msg = n > 0
            ? $"当前有 {n} 个已配对协助方。结束被控将断开全部会话并停止监听。是否结束？"
            : "正在接受协助（中继已连接/等待中）。返回或切换将结束本次被控。是否结束？";
        var r = MessageBox.Show(
            owner,
            msg,
            "结束接受协助",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        return r == DialogResult.Yes;
    }

    private bool _started_or_active()
    {
        // PreferRelay 启动后即视为活动会话（等待对方也算），避免留下隐藏监听
        try
        {
            if (_host.PreferRelay)
            {
                var phase = _host.RelayPhase ?? "";
                if (phase is not ("error" or ""))
                    return true;
            }
            return _host.ListPairedClients().Count > 0;
        }
        catch { return true; }
    }

    private void TryReturnHome()
    {
        if (!_enableReturnHome) return;
        IWin32Window? owner = _mainForm.Visible && !_mainForm.IsDisposed ? _mainForm : null;
        if (!ConfirmEndSessionOrCancel(owner))
            return; // 取消：保持原会话原页
        HomeRequested?.Invoke();
    }'''

# Fix: _started_or_active is invalid C# method name with underscore style ok but I used wrong - method names can't have that in the sense... `_started_or_active` is valid. But I referenced before defining ShowMain change.

if old_try not in t:
    raise SystemExit('TryReturnHome block missing')
t = t.replace(old_try, new_try)

# ShowMain when embedded
t = t.replace(
    "    public void ShowMain() => _mainForm.ShowAndActivate();\n",
    '''    public void ShowMain()
    {
        if (_embedHost is not null)
        {
            _activateShell?.Invoke();
            return;
        }
        _mainForm.ShowAndActivate();
    }
''',
)

# Balloon tip text for return home when embedded
t = t.replace(
    '''            enableReturnHome
                ? "被控模式已启动。可返回首页切换为控制端；关闭主窗口会留在托盘。"
                : "已在托盘运行。关闭主窗口不会退出，可随时双击托盘图标打开。",''',
    '''            enableReturnHome
                ? "被控模式已启动。可用「返回首页」切换模式（有会话须本机确认结束）。"
                : "已在托盘运行。关闭主窗口不会退出，可随时双击托盘图标打开。",''',
)

p.write_text(t, encoding='utf-8', newline='\n')
print('AgentDesktopUi OK')

# --- Agent MainForm SetEmbeddedInShell + FormClosing ---
p = Path("src-net48/RemoteAssist.Agent/MainForm.cs")
t = p.read_text(encoding="utf-8")
if "private bool _embeddedInShell" not in t:
    t = t.replace(
        "    public event Action? HomeRequested;\n",
        "    public event Action? HomeRequested;\n    private bool _embeddedInShell;\n",
        1,
    )
if "public void SetEmbeddedInShell" not in t:
    t = t.replace(
        "    public void ShowAndActivate()\n",
        '''    public void SetEmbeddedInShell(bool embedded)
    {
        _embeddedInShell = embedded;
        if (embedded)
        {
            // 关闭按钮由外壳统一处理，避免藏起第二窗体
            ControlBox = false;
        }
    }

    public void ShowAndActivate()
''',
        1,
    )

old_close = '''    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_exitApp || e.CloseReason != CloseReason.UserClosing)
            return;
        e.Cancel = true;
        Hide();
        SetStatus("已隐藏到托盘。双击托盘图标可重新打开。");
    }'''
new_close = '''    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_exitApp || e.CloseReason != CloseReason.UserClosing)
            return;
        e.Cancel = true;
        if (_embeddedInShell)
        {
            // 嵌入单主窗口：不 Hide 成第二窗；请用「返回首页」（含会话确认）
            return;
        }
        Hide();
        SetStatus("已隐藏到托盘。双击托盘图标可重新打开。");
    }'''
if old_close not in t:
    raise SystemExit('Agent OnFormClosing missing')
t = t.replace(old_close, new_close)

# Hide relay address on main path listen label
t = t.replace(
    '_listenValue.Text = "中继 " + _host.RelayEndpointAddress + " · " + _host.RelayPhase;',
    '_listenValue.Text = "经默认中继 · " + _host.RelayPhase; // R049：主路径不展示服务器地址',
)
t = t.replace(
    'Tip().SetToolTip(_tlsValue, "安全验证信息已就绪 · 中继 " + _host.RelayEndpointAddress);',
    'Tip().SetToolTip(_tlsValue, "安全验证信息已就绪");',
)
t = t.replace(
    'Tip().SetToolTip(_tlsValue, _host.RelayEndpointAddress);',
    'Tip().SetToolTip(_tlsValue, "明文调试模式");',
)

p.write_text(t, encoding='utf-8', newline='\n')
print('Agent MainForm OK')