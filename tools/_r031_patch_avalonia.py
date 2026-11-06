from pathlib import Path

p = Path("src/RemoteAssist.Agent.Avalonia/Views/MainWindow.cs")
t = p.read_text(encoding="utf-8")

# Add field
old_fields = """    private readonly TextBlock _status;
    private readonly TextBlock _hint;
"""
new_fields = """    private readonly TextBlock _status;
    private readonly TextBlock _hint;
    private readonly TextBlock _pairedHeading;
    private readonly ListBox _pairedList;
"""
if "_pairedList" not in t:
    if old_fields not in t:
        raise SystemExit("status/hint fields missing")
    t = t.replace(old_fields, new_fields, 1)

# Insert UI after _status in ctor — find where status is added then advanced button
old_ctor_status = """        _status = new TextBlock
        {
            Text = \"正在连接网络…\",
            Foreground = Brush.Parse(\"#5F6368\"),
            TextWrapping = TextWrapping.Wrap
        };
        root.Children.Add(_status);

        _btnAdvanced = new Button
"""
new_ctor_status = """        _status = new TextBlock
        {
            Text = \"正在连接网络…\",
            Foreground = Brush.Parse(\"#5F6368\"),
            TextWrapping = TextWrapping.Wrap
        };
        root.Children.Add(_status);

        _pairedHeading = new TextBlock
        {
            Text = \"已连接的协助方\",
            Foreground = Brush.Parse(\"#5F6368\"),
            Margin = new Thickness(0, 8, 0, 0)
        };
        root.Children.Add(_pairedHeading);
        _pairedList = new ListBox
        {
            Name = \"pairedClients\",
            Height = 56,
            MinHeight = 48
        };
        _pairedList.ItemsSource = new[] { FriendConnectGuide.PairedClientsEmpty };
        root.Children.Add(_pairedList);

        _btnAdvanced = new Button
"""
if "pairedClients" not in t:
    if old_ctor_status not in t:
        raise SystemExit("ctor status block missing")
    t = t.replace(old_ctor_status, new_ctor_status, 1)

# shotMode init already sets status; also ensure paired empty
old_shot_init = """            _codeBlock.Text = \"会话码：AB12CD\";
            _pwdBlock.Text = \"验证密码：3471\";
            _status.Text = \"已连上网络，可以发给对方了\";
            RefreshTrustUi();
"""
new_shot_init = """            _codeBlock.Text = \"会话码：AB12CD\";
            _pwdBlock.Text = \"验证密码：3471\";
            _status.Text = FriendConnectGuide.StatusReadyToShare;
            _pairedList.ItemsSource = new[] { FriendConnectGuide.PairedClientsEmpty };
            RefreshTrustUi();
"""
if old_shot_init in t:
    t = t.replace(old_shot_init, new_shot_init, 1)

# RefreshSessionUi
old_rs = """    private void RefreshSessionUi()
    {
        _codeBlock.Text = \"会话码：\" + _host.SessionCode;
        _pwdBlock.Text = \"验证密码：\" + _host.SessionPassword;
        var phase = _host.RelayPhase;
        var paired = _host.ListPairedClients().Count;
        if (paired > 0 || phase == \"paired\")
        {
            _hint.Text = \"已有人连上。对方可以查看进程、日志和文件；要改动时会再问你。\";
            _status.Text = FriendConnectGuide.StatusWaitingPeer;
            if (paired > 0)
                _status.Text = $\"已有 {paired} 位协助方连着。\";
        }
        else if (phase is \"ready\" or \"waiting\")
        {
            _status.Text = FriendConnectGuide.StatusReadyToShare;
        }
        else if (!string.IsNullOrWhiteSpace(_host.LastRelayError))
        {
            _status.Text = FriendConnectGuide.FailRelayUnreachable;
        }
        else
        {
            _status.Text = \"正在连接网络…\";
        }
    }
"""
new_rs = """    private void RefreshSessionUi()
    {
        _codeBlock.Text = \"会话码：\" + _host.SessionCode;
        _pwdBlock.Text = \"验证密码：\" + _host.SessionPassword;
        var phase = _host.RelayPhase;
        var clients = _host.ListPairedClients();
        RefreshPairedList(clients);
        // R031：以真正配对进本机的协助方为准
        if (clients.Count > 0)
        {
            _status.Text = FriendConnectGuide.StatusPeerConnected;
        }
        else if (phase is \"ready\" or \"waiting\" or \"paired\")
        {
            _status.Text = FriendConnectGuide.StatusReadyToShare;
        }
        else if (!string.IsNullOrWhiteSpace(_host.LastRelayError) || phase == \"error\")
        {
            _status.Text = FriendConnectGuide.FailRelayUnreachable;
        }
        else
        {
            _status.Text = \"正在连接网络…\";
        }
    }

    private void RefreshPairedList(System.Collections.Generic.IReadOnlyList<PairedClientInfo> clients)
    {
        if (clients.Count == 0)
        {
            _pairedList.ItemsSource = new[] { FriendConnectGuide.PairedClientsEmpty };
            return;
        }
        _pairedList.ItemsSource = clients
            .Select(c => FriendConnectGuide.FormatPairedClientLabel(c.ClientName, c.OwnerLink))
            .ToArray();
    }
"""
if old_rs not in t:
    raise SystemExit("RefreshSessionUi block missing")
t = t.replace(old_rs, new_rs, 1)

# Need using System.Linq if not present
if "using System.Linq;" not in t and ".Select(" in t:
    t = t.replace("using Avalonia;\n", "using System.Linq;\nusing Avalonia;\n", 1)

# Shot helpers before PrepareSkillEntryForShot
shot = """
    /// <summary>UiShot：还没人连上。</summary>
    internal void PreparePeerIdleForShot()
    {
        _status.Text = FriendConnectGuide.StatusReadyToShare;
        _pairedList.ItemsSource = new[] { FriendConnectGuide.PairedClientsEmpty };
        _scroll.Offset = new Vector(0, 0);
    }

    /// <summary>UiShot：对方已连上。</summary>
    internal void PreparePeerConnectedForShot(string? displayName = null)
    {
        _status.Text = FriendConnectGuide.StatusPeerConnected;
        _pairedList.ItemsSource = new[] { FriendConnectGuide.FormatPairedClientLabel(displayName) };
        _scroll.Offset = new Vector(0, 0);
    }

    /// <summary>UiShot：对方断开后。</summary>
    internal void PreparePeerDisconnectedForShot() => PreparePeerIdleForShot();

"""
if "PreparePeerConnectedForShot" not in t:
    anchor = '    /// <summary>UiShot：高级内 MCP +「复制 Skill 说明」轻按钮；R018 导语仍在。</summary>'
    if anchor not in t:
        raise SystemExit("PrepareSkillEntry anchor missing")
    t = t.replace(anchor, shot + anchor, 1)

# Height bump slightly for new list
t = t.replace("Height = 560;", "Height = 620;", 1)

p.write_text(t, encoding="utf-8", newline="\n")
print("MainWindow OK")
