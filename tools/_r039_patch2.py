from pathlib import Path

p = Path("src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
t = p.read_text(encoding="utf-8")

pair_anchor = "        body.Children.Add(pairCard);\n"
mcp_anchor = "        body.Children.Add(mcpCard);\n"
tool_start_marker = "        _toolCard = new Border\n"
tool_add = "        body.Children.Add(_toolCard);\n"

if "R039：常用操作紧挨主状态下方" in t:
    print("already reordered")
else:
    pair_i = t.find(pair_anchor)
    mcp_i = t.find(mcp_anchor)
    tool_i = t.find(tool_start_marker)
    tool_add_i = t.find(tool_add, tool_i)
    if min(pair_i, mcp_i, tool_i, tool_add_i) < 0:
        raise SystemExit(f"anchors missing pair={pair_i} mcp={mcp_i} tool={tool_i} add={tool_add_i}")
    if not (pair_i < mcp_i < tool_i < tool_add_i):
        raise SystemExit(f"order unexpected pair={pair_i} mcp={mcp_i} tool={tool_i}")

    tool_block = t[tool_i : tool_add_i + len(tool_add)]
    # Remove tool block and any blank lines immediately before it (after mcp)
    before = t[:tool_i].rstrip("\n") + "\n"
    after = t[tool_add_i + len(tool_add):]
    # Ensure mcp section ends cleanly
    if not before.rstrip().endswith("body.Children.Add(mcpCard);"):
        # find last non-empty
        raise SystemExit("before tool unexpected end: " + repr(before[-80:]))
    t2 = before + "\n" + after

    # Insert after pairCard add
    insert = (
        pair_anchor
        + "\n"
        + "        // ---- R039：常用操作紧挨主状态下方（账号 / MCP 之前），连上后首屏可见 ----\n"
        + tool_block
        + "\n"
    )
    if pair_anchor not in t2:
        raise SystemExit("pair anchor lost")
    # Only replace first occurrence after which comes account
    idx = t2.find(pair_anchor)
    # what follows should be blank + R014 account
    t2 = t2[:idx] + insert + t2[idx + len(pair_anchor):]
    t = t2
    print("reordered")

# ShowCommonTools visibility
old_vis = "        _toolCard.IsVisible = connected;"
new_vis = "        _toolCard.IsVisible = HelperSessionCopy.ShowCommonTools(connected);"
if old_vis in t:
    t = t.replace(old_vis, new_vis, 1)
    print("visibility wired to ShowCommonTools")
elif new_vis in t:
    print("visibility already ShowCommonTools")
else:
    raise SystemExit("visibility line missing")

# Wire labels
reps = [
    ('_btnProcs = MakeToolButton("查看正在运行的程序", 180);',
     '_btnProcs = MakeToolButton(HelperSessionCopy.CommonToolButtonLabels[0], 180);'),
    ('_btnLs = MakeToolButton("查看文件夹", 110);',
     '_btnLs = MakeToolButton(HelperSessionCopy.CommonToolButtonLabels[1], 110);'),
    ('_btnLogs = MakeToolButton("查看日志", 100);',
     '_btnLogs = MakeToolButton(HelperSessionCopy.CommonToolButtonLabels[2], 100);'),
    ('_btnCat = MakeToolButton("查看文件", 100);',
     '_btnCat = MakeToolButton(HelperSessionCopy.CommonToolButtonLabels[3], 100);'),
    ('_btnRun = MakeToolButton("提议一项改动", 120);',
     '_btnRun = MakeToolButton(HelperSessionCopy.CommonToolButtonLabels[4], 120);'),
]
for a, b in reps:
    if a in t:
        t = t.replace(a, b, 1)
    elif b not in t:
        raise SystemExit("label wire missing " + a)

p.write_text(t, encoding="utf-8", newline="\n")
adds = [ln.strip() for ln in t.splitlines() if "body.Children.Add(" in ln]
print("body adds:", adds)

# Protocol labels
p = Path("src/RemoteAssist.Protocol/HelperSessionCopy.cs")
t = p.read_text(encoding="utf-8")
if "CommonToolButtonLabels" not in t:
    old = """    /// <summary>R016：仅已连接时展示五个常用操作。</summary>
    public static bool ShowCommonTools(bool connected) => connected;
}
"""
    new = """    /// <summary>R016：仅已连接时展示五个常用操作。</summary>
    public static bool ShowCommonTools(bool connected) => connected;

    /// <summary>R016/R039：五个常用操作按钮文案与顺序（锁定）。</summary>
    public static readonly string[] CommonToolButtonLabels =
    {
        "查看正在运行的程序",
        "查看文件夹",
        "查看日志",
        "查看文件",
        "提议一项改动",
    };
}
"""
    if old not in t:
        raise SystemExit("ShowCommonTools anchor missing")
    t = t.replace(
        "去协助端会话主状态与会话码主按钮文案（R035/R036/R037）。",
        "去协助端会话主状态与会话码主按钮文案（R035/R036/R037）；R039 锁定五键文案顺序。",
        1,
    )
    t = t.replace(old, new, 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("Protocol labels OK")
else:
    print("Protocol labels exist")

# Tests - rewrite whole file cleanly
Path("tests/RemoteAssist.Protocol.Tests/HelperSessionCopyTests.cs").write_text("""using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class HelperSessionCopyTests
{
    [Fact]
    public void PeerEnded_ExactCopy_NotConnectFailed()
    {
        Assert.Equal("对方已结束这次协助。", HelperSessionCopy.StatusPeerEndedAssist);
        Assert.NotEqual(HelperSessionCopy.StatusConnectFailed, HelperSessionCopy.StatusPeerEndedAssist);
        Assert.DoesNotContain("连接失败", HelperSessionCopy.StatusPeerEndedAssist);
        Assert.DoesNotContain("失败", HelperSessionCopy.StatusPeerEndedAssist);
    }

    [Fact]
    public void ThreeScenarios_StatusBranches()
    {
        Assert.Equal(
            HelperSessionCopy.StatusPeerEndedAssist,
            HelperSessionCopy.StatusAfterConnectedSessionEnd(selfInitiatedDisconnect: false));
        Assert.Equal(
            HelperSessionCopy.StatusDisconnectedSelf,
            HelperSessionCopy.StatusAfterConnectedSessionEnd(selfInitiatedDisconnect: true));
        Assert.Equal("状态：未连接", HelperSessionCopy.StatusDisconnectedSelf);
        Assert.Equal("连接失败", HelperSessionCopy.StatusConnectFailed);
        Assert.Equal("已连接对方电脑", HelperSessionCopy.StatusConnected);
    }

    [Fact]
    public void ConnectFailHints_R003_Unchanged()
    {
        Assert.Equal(
            "对方已刷新会话，旧码失效了。请对方重新复制并发给你。",
            FriendConnectGuide.FailStaleCode);
        Assert.Equal(
            "对方还没打开「接受协助」。请对方在首页选择「接受协助」，再把码和密码发给你。",
            FriendConnectGuide.FailPeerNotAccepting);
        Assert.NotEqual(FriendConnectGuide.FailStaleCode, HelperSessionCopy.StatusPeerEndedAssist);
        Assert.NotEqual(FriendConnectGuide.FailPeerNotAccepting, HelperSessionCopy.StatusPeerEndedAssist);
    }

    [Fact]
    public void CommonTools_HiddenWhenNotConnected()
    {
        Assert.True(HelperSessionCopy.ShowCommonTools(connected: true));
        Assert.False(HelperSessionCopy.ShowCommonTools(connected: false));
        Assert.False(HelperSessionCopy.ShowCommonTools(
            connected: HelperSessionCopy.ShowCommonTools(false)));
    }

    [Fact]
    public void SessionConnectButton_ConnectedLabel_NotSolidConnect()
    {
        Assert.Equal("连接", HelperSessionCopy.ConnectButtonText);
        Assert.Equal("已连接", HelperSessionCopy.ConnectedButtonText);
        Assert.Equal("已连接", HelperSessionCopy.SessionConnectButtonLabel(connected: true));
        Assert.Equal("连接", HelperSessionCopy.SessionConnectButtonLabel(connected: false));
        Assert.NotEqual(HelperSessionCopy.ConnectButtonText, HelperSessionCopy.ConnectedButtonText);
        Assert.Equal("对方已结束这次协助。", HelperSessionCopy.StatusPeerEndedAssist);
        Assert.Equal("状态：未连接", HelperSessionCopy.StatusDisconnectedSelf);
        Assert.Equal("连接失败", HelperSessionCopy.StatusConnectFailed);
        Assert.Equal("已连接对方电脑", HelperSessionCopy.StatusConnected);
    }

    [Fact]
    public void PasteThenConnectHint_HiddenWhenConnected_ExactCopyUnchanged()
    {
        Assert.Equal("粘贴后点「连接」", HelperSessionCopy.PasteThenConnectHint);
        Assert.False(HelperSessionCopy.ShowPasteThenConnectHint(sessionConnected: true));
        Assert.True(HelperSessionCopy.ShowPasteThenConnectHint(sessionConnected: false));
        Assert.Equal("已连接", HelperSessionCopy.ConnectedButtonText);
        Assert.Equal("连接", HelperSessionCopy.ConnectButtonText);
        Assert.Equal("对方已结束这次协助。", HelperSessionCopy.StatusPeerEndedAssist);
        Assert.Equal("状态：未连接", HelperSessionCopy.StatusDisconnectedSelf);
        Assert.Equal("连接失败", HelperSessionCopy.StatusConnectFailed);
    }

    [Fact]
    public void CommonToolButtonLabels_OrderLocked()
    {
        Assert.Equal(
            new[]
            {
                "查看正在运行的程序",
                "查看文件夹",
                "查看日志",
                "查看文件",
                "提议一项改动",
            },
            HelperSessionCopy.CommonToolButtonLabels);
        Assert.True(HelperSessionCopy.ShowCommonTools(connected: true));
        Assert.False(HelperSessionCopy.ShowCommonTools(connected: false));
        Assert.Equal("已连接对方电脑", HelperSessionCopy.StatusConnected);
        Assert.Equal("已连接", HelperSessionCopy.ConnectedButtonText);
        Assert.Equal("粘贴后点「连接」", HelperSessionCopy.PasteThenConnectHint);
    }
}
""", encoding="utf-8", newline="\n")
print("tests rewritten")
