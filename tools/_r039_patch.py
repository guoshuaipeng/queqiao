from pathlib import Path

# --- HelperWindow: move tool card after pairCard ---
p = Path("src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
t = p.read_text(encoding="utf-8")

marker_after_pair = "        pairCard.Child = pairStack;\n        body.Children.Add(pairCard);\n\n        // ---- R014 账号（可选）：权重低于粘贴区 ----\n"
if marker_after_pair not in t:
    raise SystemExit("pairCard marker missing")

tool_start = t.find("\n\n        _toolCard = new Border\n")
if tool_start < 0:
    raise SystemExit("tool card start missing")
tool_end = t.find("        body.Children.Add(_toolCard);\n\n        root.Children.Add(new ScrollViewer { Content = body });\n")
if tool_end < 0:
    raise SystemExit("tool card end missing")
tool_end_line = "        body.Children.Add(_toolCard);\n"
tool_block = t[tool_start + 2 : tool_end + len(tool_end_line)]  # without leading \n\n, include Add

# Remove tool block from old location (includes preceding blank lines after mcp)
# Old: mcp Add then blank blank then tool...
old_prefix = "        body.Children.Add(mcpCard);\n\n\n        _toolCard = new Border\n"
if old_prefix not in t and "        body.Children.Add(mcpCard);\n\n        _toolCard = new Border\n" not in t:
    # check exact whitespace
    idx = t.find("body.Children.Add(mcpCard);")
    print(repr(t[idx:idx+80]))
    raise SystemExit("mcp->tool join missing")

# Remove from after mcpCard
mcp_add = "        body.Children.Add(mcpCard);\n"
mcp_idx = t.find(mcp_add)
if mcp_idx < 0:
    raise SystemExit("mcp add missing")
# find tool block start after mcp
rest = t[mcp_idx + len(mcp_add):]
# strip leading newlines then tool until Add(_toolCard);
if not rest.lstrip("\n").startswith("_toolCard = new Border"):
    # already moved?
    if "R039" in t and "主状态下面" in t:
        print("maybe already moved")
    raise SystemExit("tool not after mcp: " + repr(rest[:60]))

# Build new file: cut tool from after mcp, insert after pair
without_tool = t[:mcp_idx + len(mcp_add)] + "\n" + t[tool_end + len(tool_end_line):]
# Now insert after pair in without_tool
insert = (
    "        pairCard.Child = pairStack;\n"
    "        body.Children.Add(pairCard);\n"
    "\n"
    "        // ---- R039：常用操作紧挨主状态下方（账号 / MCP 之前），连上后首屏可见 ----\n"
    + tool_block
    + "\n"
    "        // ---- R014 账号（可选）：权重低于粘贴区 ----\n"
)
if marker_after_pair not in without_tool:
    raise SystemExit("pair marker gone after cut")
without_tool = without_tool.replace(marker_after_pair, insert, 1)

# Use ShowCommonTools for visibility
old_vis = "        _toolCard.IsVisible = connected;"
new_vis = "        _toolCard.IsVisible = HelperSessionCopy.ShowCommonTools(connected);"
if old_vis in without_tool:
    without_tool = without_tool.replace(old_vis, new_vis, 1)
elif new_vis not in without_tool:
    raise SystemExit("tool visibility line missing")

p.write_text(without_tool, encoding="utf-8", newline="\n")
print("HelperWindow reorder OK")

# Verify order in body.Children.Add
adds = []
for line in without_tool.splitlines():
    if "body.Children.Add(" in line:
        adds.append(line.strip())
print("body adds:", adds)

# --- HelperSessionCopy: lock tool label order ---
p = Path("src/RemoteAssist.Protocol/HelperSessionCopy.cs")
t = p.read_text(encoding="utf-8")
if "CommonToolButtonLabels" not in t:
    old = '''    /// <summary>R016：仅已连接时展示五个常用操作。</summary>
    public static bool ShowCommonTools(bool connected) => connected;
}
'''
    new = '''    /// <summary>R016：仅已连接时展示五个常用操作。</summary>
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
'''
    if old not in t:
        raise SystemExit("ShowCommonTools anchor missing")
    t = t.replace(
        "去协助端会话主状态与会话码主按钮文案（R035/R036/R037）。",
        "去协助端会话主状态与会话码主按钮文案（R035/R036/R037）；R039 锁定五键文案顺序。",
        1,
    )
    t = t.replace(old, new, 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("HelperSessionCopy labels OK")
else:
    print("labels already present")

# Wire Avalonia buttons to use CommonToolButtonLabels[i] for text (keep order)
p = Path("src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
t = p.read_text(encoding="utf-8")
replacements = [
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
for a, b in replacements:
    if a not in t:
        if b in t:
            continue
        raise SystemExit("missing: " + a)
    t = t.replace(a, b, 1)
p.write_text(t, encoding="utf-8", newline="\n")
print("Avalonia labels wired")

# --- tests ---
p = Path("tests/RemoteAssist.Protocol.Tests/HelperSessionCopyTests.cs")
t = p.read_text(encoding="utf-8")
if "CommonToolButtonLabels_OrderLocked" not in t:
    insert = '''
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
        // R035–R037 未回退
        Assert.Equal("已连接对方电脑", HelperSessionCopy.StatusConnected);
        Assert.Equal("已连接", HelperSessionCopy.ConnectedButtonText);
        Assert.Equal("粘贴后点「连接」", HelperSessionCopy.PasteThenConnectHint);
    }
}
'''
    if not t.rstrip().endswith("}"):
        raise SystemExit("tests end weird")
    # replace final class closing
    idx = t.rfind("\n}\n")
    if idx < 0:
        idx = t.rfind("\n}")
        t = t[:idx] + "\n" + insert
    else:
        # remove last closing brace of class - the insert includes class close
        # Find last method's closing then class
        marker = "        Assert.Equal(\"连接失败\", HelperSessionCopy.StatusConnectFailed);\n    }\n}\n"
        if marker in t:
            t = t.replace(marker, marker[:-3] + insert.lstrip() + "\n", 1)
        else:
            # paste before final }
            t = t.rstrip()
            if t.endswith("}"):
                # remove outermost class }
                t = t[:-1].rstrip()
                if t.endswith("}"):
                    # end of last method
                    t = t + "\n" + insert
                else:
                    raise SystemExit("structure odd")
            else:
                raise SystemExit("no brace")
    p.write_text(t, encoding="utf-8", newline="\n")
    # verify compile-ability by reading
    tt = p.read_text(encoding="utf-8")
    if tt.count("public void CommonToolButtonLabels_OrderLocked") != 1:
        raise SystemExit("test insert failed count=" + str(tt.count("CommonToolButtonLabels_OrderLocked")))
    # brace balance
    if tt.count("{") != tt.count("}"):
        raise SystemExit(f"brace imbalance {{={tt.count('{')} }}={tt.count('}')}")
    print("tests OK")
else:
    print("test already present")
