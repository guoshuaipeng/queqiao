from pathlib import Path

p = Path("src/RemoteAssist.Protocol/FriendConnectGuide.cs")
t = p.read_text(encoding="utf-8")
old = """    public const string StatusReadyToShare = \"已连上网络，可以发给对方了\";
    public const string StatusWaitingPeer = \"等待对方连接\";
"""
new = """    public const string StatusReadyToShare = \"已连上网络，可以发给对方了\";
    public const string StatusWaitingPeer = \"等待对方连接\";

    /// <summary>协助方已真正连上本机（不是只连上中继还在等人）。</summary>
    public const string StatusPeerConnected = \"对方已连上。改动这台电脑前会先问你。\";

    /// <summary>已连接协助方列表：没有称呼时的人话回退。</summary>
    public const string PairedClientFallbackName = \"协助方\";

    /// <summary>已连接协助方列表空态。</summary>
    public const string PairedClientsEmpty = \"（暂无）\";
"""
if old not in t:
    raise SystemExit("FriendConnectGuide anchor missing")
t = t.replace(old, new, 1)

anchor = "    public static string FormatShareClipboard(string code, string password) =>"
helper = """    /// <summary>
    /// 已连接协助方列表显示名：有称呼用称呼；空/默认 helper 写「协助方」；账号免密前缀可读。
    /// </summary>
    public static string FormatPairedClientLabel(string? clientName, bool ownerLink = false)
    {
        var name = string.IsNullOrWhiteSpace(clientName) ? PairedClientFallbackName : clientName.Trim();
        if (string.Equals(name, \"helper\", StringComparison.OrdinalIgnoreCase))
            name = PairedClientFallbackName;
        return ownerLink ? \"账号免密 · \" + name : name;
    }

"""
if "FormatPairedClientLabel" not in t:
    if anchor not in t:
        raise SystemExit("FormatShareClipboard anchor missing")
    t = t.replace(anchor, helper + anchor, 1)
p.write_text(t, encoding="utf-8", newline="\n")
print("FriendConnectGuide OK")
