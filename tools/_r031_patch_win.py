from pathlib import Path

# --- tests ---
p = Path("tests/RemoteAssist.Protocol.Tests/FriendConnectGuideTests.cs")
t = p.read_text(encoding="utf-8")
if "StatusPeerConnected" not in t:
    insert = """
    [Fact]
    public void StatusPeerConnected_ExactCopy_NoTechnicalLeak()
    {
        Assert.Equal(\"对方已连上。改动这台电脑前会先问你。\", FriendConnectGuide.StatusPeerConnected);
        Assert.Equal(\"已连上网络，可以发给对方了\", FriendConnectGuide.StatusReadyToShare);
        Assert.False(FriendConnectGuide.LooksTechnicalLeak(FriendConnectGuide.StatusPeerConnected));
        Assert.DoesNotContain(\"17890\", FriendConnectGuide.StatusPeerConnected);
        Assert.DoesNotContain(\"token\", FriendConnectGuide.StatusPeerConnected);
    }

    [Theory]
    [InlineData(null, false, \"协助方\")]
    [InlineData(\"\", false, \"协助方\")]
    [InlineData(\"   \", false, \"协助方\")]
    [InlineData(\"helper\", false, \"协助方\")]
    [InlineData(\"Helper\", false, \"协助方\")]
    [InlineData(\"小明\", false, \"小明\")]
    [InlineData(\"小明\", true, \"账号免密 · 小明\")]
    [InlineData(null, true, \"账号免密 · 协助方\")]
    public void FormatPairedClientLabel_HumanName(string? name, bool ownerLink, string expected)
    {
        Assert.Equal(expected, FriendConnectGuide.FormatPairedClientLabel(name, ownerLink));
    }

    [Fact]
    public void PairedClientsEmpty_IsPlaceholder()
    {
        Assert.Equal(\"（暂无）\", FriendConnectGuide.PairedClientsEmpty);
        Assert.Equal(\"协助方\", FriendConnectGuide.PairedClientFallbackName);
    }
"""
    # append before final closing braces of class
    idx = t.rfind("}")
    # find class closing: last two }
    # insert before last method's class end — after last }
    # Better: insert before the final `}\n` of the class (second-to-last brace content)
    marker = "        Assert.Equal(-1, FriendConnectGuide.OpenFirewallButtonText.IndexOf('\\uFEFF'));\n    }\n"
    if marker not in t:
        # fallback: append before last closing of class
        last = t.rstrip()
        if not last.endswith("}"):
            raise SystemExit("unexpected test file end")
        # strip trailing namespace/class closes
        parts = last.rsplit("}", 2)
        t = parts[0] + insert + "}\n}\n"
    else:
        t = t.replace(marker, marker + insert, 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("tests OK")
else:
    print("tests already present")

# --- MainForm UpdateFriendStatus + RefreshClients ---
p = Path("src/RemoteAssist.Agent/MainForm.cs")
t = p.read_text(encoding="utf-8")
old_ufs = """    private void UpdateFriendStatus()
    {
        var phase = _host.RelayPhase;
        var clients = _host.ListPairedClients().Count;
        if (clients > 0 || phase == \"paired\")
            _listenValue.Text = \"对方已连接\";
        else if (phase == \"waiting\")
            _listenValue.Text = FriendConnectGuide.StatusWaitingPeer;
        else if (phase is \"ready\")
            _listenValue.Text = FriendConnectGuide.StatusReadyToShare;
        else if (phase == \"error\")
        {
            var friendly = FriendConnectGuide.Classify(_host.LastRelayError);
            _listenValue.Text = friendly is FriendConnectFailKind.None or FriendConnectFailKind.Other
                ? FriendConnectGuide.FailRelayUnreachable
                : FriendConnectGuide.MessageFor(friendly);
        }
        else if (phase is \"dialing\" or \"starting\")
            _listenValue.Text = \"正在连网…\";
        else
            _listenValue.Text = FriendConnectGuide.StatusReadyToShare;
"""
new_ufs = """    private void UpdateFriendStatus()
    {
        var phase = _host.RelayPhase;
        var clients = _host.ListPairedClients().Count;
        // R031：以真正配对进本机的协助方为准，不是只连上中继的 phase=paired
        if (clients > 0)
            _listenValue.Text = FriendConnectGuide.StatusPeerConnected;
        else if (phase == \"error\")
        {
            var friendly = FriendConnectGuide.Classify(_host.LastRelayError);
            _listenValue.Text = friendly is FriendConnectFailKind.None or FriendConnectFailKind.Other
                ? FriendConnectGuide.FailRelayUnreachable
                : FriendConnectGuide.MessageFor(friendly);
        }
        else if (phase is \"dialing\" or \"starting\")
            _listenValue.Text = \"正在连网…\";
        else
            // ready / waiting / paired-but-no-client / 断开后：回到可发给对方
            _listenValue.Text = FriendConnectGuide.StatusReadyToShare;
"""
if old_ufs not in t:
    raise SystemExit("UpdateFriendStatus block missing")
t = t.replace(old_ufs, new_ufs, 1)

old_rc = """            foreach (var c in _host.ListPairedClients())
            {
                var shortTok = c.Token.Length >= 8 ? c.Token[..8] : c.Token;
                var ownerTag = c.OwnerLink ? \"账号免密 · \" : \"\";
                _clientsView.Items.Add(new ListViewItem($\"{ownerTag}{c.ClientName} · {shortTok}…\") { Tag = c.Token });
            }
            if (_clientsView.Items.Count == 0)
                _clientsView.Items.Add(new ListViewItem(\"（暂无）\") { ForeColor = Color.Gray });
"""
new_rc = """            foreach (var c in _host.ListPairedClients())
            {
                var label = FriendConnectGuide.FormatPairedClientLabel(c.ClientName, c.OwnerLink);
                _clientsView.Items.Add(new ListViewItem(label) { Tag = c.Token });
            }
            if (_clientsView.Items.Count == 0)
                _clientsView.Items.Add(new ListViewItem(FriendConnectGuide.PairedClientsEmpty) { ForeColor = Color.Gray });
"""
if old_rc not in t:
    raise SystemExit("RefreshClients block missing")
t = t.replace(old_rc, new_rc, 1)

# Increase listen label height slightly for longer copy
old_listen = """        _listenValue = new Label
        {
            AutoSize = false,
            Location = new Point(16, 40),
            Size = new Size(320, 44),
            Text = FriendConnectGuide.StatusReadyToShare
        };
"""
new_listen = """        _listenValue = new Label
        {
            AutoSize = false,
            Location = new Point(16, 36),
            Size = new Size(320, 48),
            Text = FriendConnectGuide.StatusReadyToShare
        };
"""
if old_listen in t:
    t = t.replace(old_listen, new_listen, 1)

# Shot helpers before CopySkillInstructions
shot_helpers = """
    /// <summary>UiShot：还没人连上——主状态可发给对方；列表（暂无）。</summary>
    internal void PreparePeerIdleForShot()
    {
        _listenValue.Text = FriendConnectGuide.StatusReadyToShare;
        _clientsView.BeginUpdate();
        try
        {
            _clientsView.Items.Clear();
            _clientsView.Items.Add(new ListViewItem(FriendConnectGuide.PairedClientsEmpty) { ForeColor = Color.Gray });
        }
        finally { _clientsView.EndUpdate(); }
        SetStatus(\"\");
    }

    /// <summary>UiShot：对方已连上——主状态人话 + 列表称呼（无内部编号）。</summary>
    internal void PreparePeerConnectedForShot(string? displayName = null)
    {
        _listenValue.Text = FriendConnectGuide.StatusPeerConnected;
        var label = FriendConnectGuide.FormatPairedClientLabel(displayName);
        _clientsView.BeginUpdate();
        try
        {
            _clientsView.Items.Clear();
            _clientsView.Items.Add(new ListViewItem(label) { Tag = \"shot-token\" });
        }
        finally { _clientsView.EndUpdate(); }
        SetStatus(\"\");
    }

    /// <summary>UiShot：对方断开后——回到可发给对方；（暂无）。</summary>
    internal void PreparePeerDisconnectedForShot() => PreparePeerIdleForShot();

"""
if "PreparePeerConnectedForShot" not in t:
    anchor = "    private void CopySkillInstructions()"
    if anchor not in t:
        raise SystemExit("CopySkillInstructions anchor missing")
    t = t.replace(anchor, shot_helpers + anchor, 1)

p.write_text(t, encoding="utf-8", newline="\n")
print("MainForm OK")
