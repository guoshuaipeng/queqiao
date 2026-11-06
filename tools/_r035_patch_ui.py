from pathlib import Path

def patch_win():
    p = Path("src/RemoteAssist.Helper.UI/MainForm.cs")
    t = p.read_text(encoding="utf-8")
    if "_selfDisconnect" not in t:
        t = t.replace(
            "    private bool _busy;\n    private readonly bool _enableReturnHome;\n",
            "    private bool _busy;\n    private bool _selfDisconnect;\n    private readonly bool _enableReturnHome;\n",
            1,
        )

    # Attach after pair in ConnectAsync
    old = """            var client = await AssistClient.PairAsync(opts, \"helper-ui\").ConfigureAwait(true);
            _client = client;
            _settings.LastHost = opts.Host;
            _settings.LastPort = opts.Port;
            _settings.PreferRelay = true;
            _settings.Save();
            SetConnectedUi(true);
            SetStatus(\"已连接对方电脑\");
"""
    new = """            var client = await AssistClient.PairAsync(opts, \"helper-ui\").ConfigureAwait(true);
            AttachClient(client);
            _settings.LastHost = opts.Host;
            _settings.LastPort = opts.Port;
            _settings.PreferRelay = true;
            _settings.Save();
            SetConnectedUi(true);
            SetStatus(HelperSessionCopy.StatusConnected);
"""
    if "AttachClient(client)" not in t.split("helper-ui")[1][:400] if "helper-ui" in t else "":
        pass
    if old in t:
        t = t.replace(old, new, 1)
    else:
        print("WARN ConnectAsync pair block")

    # fail status
    t = t.replace('SetStatus("连接失败");', 'SetStatus(HelperSessionCopy.StatusConnectFailed);')

    # Disconnect
    old_d = """    private void Disconnect()
    {
        try { _client?.Dispose(); } catch { }
        _client = null;
        SetConnectedUi(false);
        SetStatus(\"状态：未连接\");
    }
"""
    new_d = """    private void Disconnect()
    {
        _selfDisconnect = true;
        try
        {
            if (_client is not null)
                _client.ConnectionLost -= OnClientConnectionLost;
            _client?.Dispose();
        }
        catch { /* ignore */ }
        _client = null;
        _selfDisconnect = false;
        SetConnectedUi(false);
        SetStatus(HelperSessionCopy.StatusDisconnectedSelf);
    }

    private void AttachClient(AssistClient client)
    {
        if (_client is not null)
        {
            try { _client.ConnectionLost -= OnClientConnectionLost; } catch { /* ignore */ }
            try { _client.Dispose(); } catch { /* ignore */ }
        }
        _client = client;
        _client.ConnectionLost += OnClientConnectionLost;
    }

    private void OnClientConnectionLost()
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            try { BeginInvoke(OnClientConnectionLost); } catch { /* closing */ }
            return;
        }
        if (_selfDisconnect) return;
        if (_client is null) return;
        try { _client.ConnectionLost -= OnClientConnectionLost; } catch { /* ignore */ }
        try { _client.Dispose(); } catch { /* ignore */ }
        _client = null;
        SetConnectedUi(false);
        SetStatus(HelperSessionCopy.StatusPeerEndedAssist);
        ClearFailHint();
    }
"""
    if "OnClientConnectionLost" not in t:
        if old_d not in t:
            raise SystemExit("Disconnect block missing win")
        t = t.replace(old_d, new_d, 1)

    # owner connect attach
    old_o = """            var client = await AssistClient.PairAsync(opts, \"helper-ui-owner\").ConfigureAwait(true);
            _client = client;
            SetConnectedUi(true);
"""
    new_o = """            var client = await AssistClient.PairAsync(opts, \"helper-ui-owner\").ConfigureAwait(true);
            AttachClient(client);
            SetConnectedUi(true);
"""
    if old_o in t:
        t = t.replace(old_o, new_o, 1)

    # SetConnectedUi already uses toolCard.Visible = connected - good
    # Shot helpers
    helpers = """
    /// <summary>UiShot：已连上对方电脑 + 常用操作可见。</summary>
    internal void PreparePeerConnectedForShot()
    {
        SetConnectedUi(true);
        SetStatus(HelperSessionCopy.StatusConnected);
        ClearFailHint();
        _outputBox.Text = \"PAIR_OK\\r\\n\";
        LayoutForAdvanced();
    }

    /// <summary>UiShot：对方结束后主状态 + 五键收起。</summary>
    internal void PreparePeerEndedAssistForShot()
    {
        SetConnectedUi(false);
        SetStatus(HelperSessionCopy.StatusPeerEndedAssist);
        ClearFailHint();
        LayoutForAdvanced();
    }

    /// <summary>UiShot：自己断开 — 原文「状态：未连接」。</summary>
    internal void PrepareSelfDisconnectedForShot()
    {
        SetConnectedUi(false);
        SetStatus(HelperSessionCopy.StatusDisconnectedSelf);
        ClearFailHint();
        LayoutForAdvanced();
    }

    /// <summary>UiShot：尚未连上失败 — 「连接失败」+ R003 原因句。</summary>
    internal void PrepareConnectFailedForShot(string failHint)
    {
        SetConnectedUi(false);
        SetStatus(HelperSessionCopy.StatusConnectFailed);
        SetFailHint(failHint);
        LayoutForAdvanced();
    }

"""
    if "PreparePeerEndedAssistForShot" not in t:
        anchor = "    /// <summary>UiShot：展开常用操作卡并写入输出区（不真连）。</summary>"
        if anchor not in t:
            raise SystemExit("shot anchor missing win")
        t = t.replace(anchor, helpers + anchor, 1)

    p.write_text(t, encoding="utf-8", newline="\n")
    print("Win MainForm OK")

def patch_ava():
    p = Path("src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
    t = p.read_text(encoding="utf-8")
    if "_selfDisconnect" not in t:
        # find _busy or _uiShotConnected
        if "private bool _uiShotConnected;" in t:
            t = t.replace(
                "private bool _uiShotConnected;",
                "private bool _uiShotConnected;\n    private bool _selfDisconnect;",
                1,
            )
        else:
            raise SystemExit("uiShotConnected missing")

    old = """            var client = await AssistClient.PairAsync(opts, \"helper-avalonia\").ConfigureAwait(true);
            _client = client;
            SetConnectedUi(true);
            _status.Text = \"已连接对方电脑\";
"""
    new = """            var client = await AssistClient.PairAsync(opts, \"helper-avalonia\").ConfigureAwait(true);
            AttachClient(client);
            SetConnectedUi(true);
            _status.Text = HelperSessionCopy.StatusConnected;
"""
    if old in t:
        t = t.replace(old, new, 1)
    else:
        print("WARN ava connect block")

    # shot mode connected status
    t = t.replace('_status.Text = "已连接对方电脑";', '_status.Text = HelperSessionCopy.StatusConnected;')
    t = t.replace('_status.Text = "连接失败";', '_status.Text = HelperSessionCopy.StatusConnectFailed;')

    old_d = """    private void Disconnect()
    {
        try { _client?.Dispose(); } catch { /* ignore */ }
        _client = null;
        _uiShotConnected = false;
        SetConnectedUi(false);
        _status.Text = \"状态：未连接\";
    }
"""
    new_d = """    private void Disconnect()
    {
        _selfDisconnect = true;
        try
        {
            if (_client is not null)
                _client.ConnectionLost -= OnClientConnectionLost;
            _client?.Dispose();
        }
        catch { /* ignore */ }
        _client = null;
        _uiShotConnected = false;
        _selfDisconnect = false;
        SetConnectedUi(false);
        _status.Text = HelperSessionCopy.StatusDisconnectedSelf;
    }

    private void AttachClient(AssistClient client)
    {
        if (_client is not null)
        {
            try { _client.ConnectionLost -= OnClientConnectionLost; } catch { /* ignore */ }
            try { _client.Dispose(); } catch { /* ignore */ }
        }
        _client = client;
        _client.ConnectionLost += OnClientConnectionLost;
    }

    private void OnClientConnectionLost()
    {
        if (_selfDisconnect) return;
        void Apply()
        {
            if (_selfDisconnect) return;
            if (_client is null) return;
            try { _client.ConnectionLost -= OnClientConnectionLost; } catch { /* ignore */ }
            try { _client.Dispose(); } catch { /* ignore */ }
            _client = null;
            _uiShotConnected = false;
            SetConnectedUi(false);
            _status.Text = HelperSessionCopy.StatusPeerEndedAssist;
            _failHint.Text = "";
        }
        if (Dispatcher.UIThread.CheckAccess())
            Apply();
        else
            Dispatcher.UIThread.Post(Apply);
    }
"""
    if "OnClientConnectionLost" not in t:
        if old_d not in t:
            # try alternate whitespace
            raise SystemExit("Disconnect block missing ava: " + repr([line for line in t.splitlines() if "Disconnect()" in line or "状态：未连接" in line][:5]))
        t = t.replace(old_d, new_d, 1)

    old_o = """            var client = await AssistClient.PairAsync(opts, \"helper-avalonia-owner\").ConfigureAwait(true);
            _client = client;
            SetConnectedUi(true);
"""
    new_o = """            var client = await AssistClient.PairAsync(opts, \"helper-avalonia-owner\").ConfigureAwait(true);
            AttachClient(client);
            SetConnectedUi(true);
"""
    if old_o in t:
        t = t.replace(old_o, new_o, 1)

    # PrepareConnectedReadonlyForShot already may use StatusConnected after replace
    helpers = """
    /// <summary>UiShot：已连上 + 常用操作可见。</summary>
    internal void PreparePeerConnectedSessionForShot()
    {
        _uiShotConnected = true;
        SetConnectedUi(true);
        _status.Text = HelperSessionCopy.StatusConnected;
        _failHint.Text = "";
        _output.Text = "PAIR_OK\\n";
    }

    /// <summary>UiShot：对方结束后主状态 + 五键收起。</summary>
    internal void PreparePeerEndedAssistForShot()
    {
        _uiShotConnected = false;
        SetConnectedUi(false);
        _status.Text = HelperSessionCopy.StatusPeerEndedAssist;
        _failHint.Text = "";
    }

    /// <summary>UiShot：自己断开原文。</summary>
    internal void PrepareSelfDisconnectedForShot()
    {
        _uiShotConnected = false;
        SetConnectedUi(false);
        _status.Text = HelperSessionCopy.StatusDisconnectedSelf;
        _failHint.Text = "";
    }

"""
    if "PreparePeerEndedAssistForShot" not in t:
        anchor = '    /// <summary>UiShot：未连接空态。</summary>'
        if anchor not in t:
            raise SystemExit("ava shot anchor missing")
        t = t.replace(anchor, helpers + anchor, 1)

    # PrepareIdle/Fail use constants
    t = t.replace('_status.Text = "状态：未连接";', '_status.Text = HelperSessionCopy.StatusDisconnectedSelf;')
    # PrepareConnectedReadonly - already replaced 已连接对方电脑 globally

    p.write_text(t, encoding="utf-8", newline="\n")
    print("Avalonia HelperWindow OK")

patch_win()
patch_ava()
