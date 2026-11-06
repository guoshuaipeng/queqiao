from pathlib import Path

# --- ProposeChangeCopy.cs ---
Path("src/RemoteAssist.Protocol/ProposeChangeCopy.cs").write_text("""namespace RemoteAssist.Protocol;

/// <summary>
/// R041：去协助端「提议一项改动」四态人话 + 刚连上结果区提示。
/// 拒绝/超时原文仍锁定在 <see cref="WriteConfirmCopy"/>。
/// </summary>
public static class ProposeChangeCopy
{
    /// <summary>发出提议后、对方尚未允许/拒绝/超时。</summary>
    public const string WaitingStatus = "正在等对方在本机确认…";

    /// <summary>对方允许且执行成功。</summary>
    public const string AllowedStatus = "对方已允许，改动已执行";

    /// <summary>配对刚成功、尚未点操作时的结果区人话（代替 PAIR_OK）。</summary>
    public const string ConnectedResultHint = "已连上。建议先点「查看正在运行的程序」。";

    /// <summary>
    /// DeniedByUser 时优先展示非空 Error（超时句来自被控端）；否则拒绝句。
    /// 非用户拒绝失败则展示 Error 或「失败」。
    /// </summary>
    public static string FailureStatus(bool deniedByUser, string? error)
    {
        if (deniedByUser)
        {
            if (!string.IsNullOrWhiteSpace(error))
                return error.Trim();
            return WriteConfirmCopy.DeniedMessage;
        }

        return string.IsNullOrWhiteSpace(error) ? "失败" : error.Trim();
    }

    /// <summary>提议改动成功主状态；只读操作请调用方自行处理。</summary>
    public static string AllowedStatusFor(ToolKind tool) =>
        tool == ToolKind.RunCommand ? AllowedStatus : "完成";

    /// <summary>是否应对该工具显示「正在等对方确认」等待句（仅写操作提议）。</summary>
    public static bool UsesWaitingStatus(ToolKind tool) => tool == ToolKind.RunCommand;
}
""", encoding="utf-8", newline="\n")
print("ProposeChangeCopy OK")

# --- Tests ---
Path("tests/RemoteAssist.Protocol.Tests/ProposeChangeCopyTests.cs").write_text("""using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class ProposeChangeCopyTests
{
    [Fact]
    public void FourStates_ExactCopy_Locked()
    {
        Assert.Equal("正在等对方在本机确认…", ProposeChangeCopy.WaitingStatus);
        Assert.Equal("对方已允许，改动已执行", ProposeChangeCopy.AllowedStatus);
        Assert.Equal("已拒绝，未做任何改动", WriteConfirmCopy.DeniedMessage);
        Assert.Equal("确认超时，未做任何改动", WriteConfirmCopy.TimedOutMessage);
        Assert.Equal("已连上。建议先点「查看正在运行的程序」。", ProposeChangeCopy.ConnectedResultHint);
        Assert.DoesNotContain("RunCommand", ProposeChangeCopy.WaitingStatus);
        Assert.DoesNotContain("PAIR_OK", ProposeChangeCopy.ConnectedResultHint);
        Assert.DoesNotContain("已提议一项改动", ProposeChangeCopy.AllowedStatus);
    }

    [Fact]
    public void FailureStatus_DeniedByUser_PrefersNonEmptyError_TimeoutNotCoveredAsDenied()
    {
        Assert.Equal(
            WriteConfirmCopy.TimedOutMessage,
            ProposeChangeCopy.FailureStatus(deniedByUser: true, error: WriteConfirmCopy.TimedOutMessage));
        Assert.Equal(
            WriteConfirmCopy.DeniedMessage,
            ProposeChangeCopy.FailureStatus(deniedByUser: true, error: WriteConfirmCopy.DeniedMessage));
        Assert.Equal(
            WriteConfirmCopy.DeniedMessage,
            ProposeChangeCopy.FailureStatus(deniedByUser: true, error: null));
        Assert.Equal(
            WriteConfirmCopy.DeniedMessage,
            ProposeChangeCopy.FailureStatus(deniedByUser: true, error: "  "));
        Assert.Equal("网络中断", ProposeChangeCopy.FailureStatus(deniedByUser: false, error: "网络中断"));
        Assert.Equal("失败", ProposeChangeCopy.FailureStatus(deniedByUser: false, error: null));
    }

    [Fact]
    public void WaitingOnlyForPropose_NotReadonlyTools()
    {
        Assert.True(ProposeChangeCopy.UsesWaitingStatus(ToolKind.RunCommand));
        Assert.False(ProposeChangeCopy.UsesWaitingStatus(ToolKind.ListProcesses));
        Assert.False(ProposeChangeCopy.UsesWaitingStatus(ToolKind.ListDir));
        Assert.False(ProposeChangeCopy.UsesWaitingStatus(ToolKind.ReadFile));
        Assert.False(ProposeChangeCopy.UsesWaitingStatus(ToolKind.ReadLogTail));
        Assert.Equal(ProposeChangeCopy.AllowedStatus, ProposeChangeCopy.AllowedStatusFor(ToolKind.RunCommand));
    }

    [Fact]
    public void R035_R037_R039_Unchanged()
    {
        Assert.Equal("对方已结束这次协助。", HelperSessionCopy.StatusPeerEndedAssist);
        Assert.Equal("已连接对方电脑", HelperSessionCopy.StatusConnected);
        Assert.Equal("状态：未连接", HelperSessionCopy.StatusDisconnectedSelf);
        Assert.Equal("连接失败", HelperSessionCopy.StatusConnectFailed);
        Assert.Equal("已连接", HelperSessionCopy.ConnectedButtonText);
        Assert.Equal("连接", HelperSessionCopy.ConnectButtonText);
        Assert.Equal("粘贴后点「连接」", HelperSessionCopy.PasteThenConnectHint);
        Assert.Equal(5, HelperSessionCopy.CommonToolButtonLabels.Length);
        Assert.True(HelperSessionCopy.ShowCommonTools(true));
    }
}
""", encoding="utf-8", newline="\n")
print("tests OK")

# --- Patch MainForm ---
p = Path("src/RemoteAssist.Helper.UI/MainForm.cs")
t = p.read_text(encoding="utf-8")

old_pair = '            AppendOutput("PAIR_OK\\r\\n");'
new_pair = '            _outputBox.Text = ProposeChangeCopy.ConnectedResultHint;'
# Prefer replace Set+Append for connect - there may be two PAIR_OK
if t.count('PAIR_OK') < 1:
    raise SystemExit("no PAIR_OK in MainForm")
# connect success uses AppendOutput
old_conn = '''            SetConnectedUi(true);
            SetStatus(HelperSessionCopy.StatusConnected);
            AppendOutput("PAIR_OK\\r\\n");
            ClearFailHint();
'''
new_conn = '''            SetConnectedUi(true);
            SetStatus(HelperSessionCopy.StatusConnected);
            _outputBox.Text = ProposeChangeCopy.ConnectedResultHint;
            ClearFailHint();
'''
if old_conn not in t:
    raise SystemExit("win connect block missing: " + repr(t[t.find("PAIR_OK")-80:t.find("PAIR_OK")+40]))
t = t.replace(old_conn, new_conn, 1)

old_run = '''    private async Task RunToolAsync(ToolKind tool, Dictionary<string, string?> args)
    {
        if (_client is null || _busy) return;
        _busy = true;
        SetStatus($"执行 {tool} …");
        try
        {
            var resp = await _client.InvokeToolAsync(tool, args).ConfigureAwait(true);
            if (!resp.Ok)
            {
                var msg = resp.Error ?? "失败";
                if (resp.DeniedByUser) msg = WriteConfirmCopy.DeniedMessage;
                SetStatus(msg);
                AppendOutput($"[{tool}] FAIL: {msg}\\r\\n");
                return;
            }
            SetStatus($"{tool} 成功");
            AppendOutput($"[{tool}] OK\\r\\n{resp.Result}\\r\\n");
        }
'''
new_run = '''    private async Task RunToolAsync(ToolKind tool, Dictionary<string, string?> args)
    {
        if (_client is null || _busy) return;
        _busy = true;
        // R041：仅「提议一项改动」显示等待确认人话；只读操作不套等待句
        if (ProposeChangeCopy.UsesWaitingStatus(tool))
            SetStatus(ProposeChangeCopy.WaitingStatus);
        else
            SetStatus($"执行 {tool} …");
        try
        {
            var resp = await _client.InvokeToolAsync(tool, args).ConfigureAwait(true);
            if (!resp.Ok)
            {
                var msg = ProposeChangeCopy.FailureStatus(resp.DeniedByUser, resp.Error);
                SetStatus(msg);
                AppendOutput($"[{tool}] FAIL: {msg}\\r\\n");
                return;
            }
            if (tool == ToolKind.RunCommand)
                SetStatus(ProposeChangeCopy.AllowedStatus);
            else
                SetStatus($"{tool} 成功");
            AppendOutput($"[{tool}] OK\\r\\n{resp.Result}\\r\\n");
        }
'''
if old_run not in t:
    raise SystemExit("win RunTool missing")
t = t.replace(old_run, new_run, 1)

old_shot = '''    internal void PreparePeerConnectedForShot()
    {
        SetConnectedUi(true);
        SetStatus(HelperSessionCopy.StatusConnected);
        ClearFailHint();
        _outputBox.Text = "PAIR_OK\\r\\n";
        LayoutForAdvanced();
    }
'''
new_shot = '''    internal void PreparePeerConnectedForShot()
    {
        SetConnectedUi(true);
        SetStatus(HelperSessionCopy.StatusConnected);
        ClearFailHint();
        _outputBox.Text = ProposeChangeCopy.ConnectedResultHint;
        LayoutForAdvanced();
    }

    /// <summary>UiShot R041：提议等待确认。</summary>
    internal void PrepareProposeWaitingForShot()
    {
        SetConnectedUi(true);
        SetStatus(ProposeChangeCopy.WaitingStatus);
        ClearFailHint();
        _outputBox.Text = ProposeChangeCopy.ConnectedResultHint;
        LayoutForAdvanced();
    }

    /// <summary>UiShot R041：对方已允许。</summary>
    internal void PrepareProposeAllowedForShot()
    {
        SetConnectedUi(true);
        SetStatus(ProposeChangeCopy.AllowedStatus);
        ClearFailHint();
        _outputBox.Text = "[RunCommand] OK\\r\\necho r041-ok\\r\\n";
        LayoutForAdvanced();
    }

    /// <summary>UiShot R041：对方拒绝。</summary>
    internal void PrepareProposeDeniedForShot()
    {
        SetConnectedUi(true);
        SetStatus(WriteConfirmCopy.DeniedMessage);
        ClearFailHint();
        _outputBox.Text = "[RunCommand] FAIL: " + WriteConfirmCopy.DeniedMessage + "\\r\\n";
        LayoutForAdvanced();
    }

    /// <summary>UiShot R041：确认超时。</summary>
    internal void PrepareProposeTimedOutForShot()
    {
        SetConnectedUi(true);
        SetStatus(WriteConfirmCopy.TimedOutMessage);
        ClearFailHint();
        _outputBox.Text = "[RunCommand] FAIL: " + WriteConfirmCopy.TimedOutMessage + "\\r\\n";
        LayoutForAdvanced();
    }
'''
if old_shot not in t:
    raise SystemExit("win PreparePeerConnected missing")
t = t.replace(old_shot, new_shot, 1)
if "PAIR_OK" in t:
    raise SystemExit("PAIR_OK still in MainForm")
p.write_text(t, encoding="utf-8", newline="\n")
print("MainForm OK")

# --- Patch HelperWindow ---
p = Path("src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
t = p.read_text(encoding="utf-8")

old_conn = '''            SetConnectedUi(true);
            _status.Text = HelperSessionCopy.StatusConnected;
            _failHint.Text = "";
            AppendOutput("PAIR_OK\\n");
            _ = preview;
'''
new_conn = '''            SetConnectedUi(true);
            _status.Text = HelperSessionCopy.StatusConnected;
            _failHint.Text = "";
            _output.Text = ProposeChangeCopy.ConnectedResultHint;
            _ = preview;
'''
if old_conn not in t:
    raise SystemExit("ava connect missing")
t = t.replace(old_conn, new_conn, 1)

# shot mode connect clears output - set hint instead when preparing connected shot
old_run = '''        _busy = true;
        try
        {
            var resp = await _client.InvokeToolAsync(tool, args).ConfigureAwait(true);
            if (!resp.Ok)
            {
                var msg = resp.Error ?? "失败";
                if (resp.DeniedByUser)
                    msg = WriteConfirmCopy.DeniedMessage;
                _status.Text = msg;
                AppendOutput($"[{tool}] FAIL: {msg}\\n");
                return;
            }

            _status.Text = tool switch
            {
                ToolKind.ListProcesses => "已查看正在运行的程序",
                ToolKind.ListDir => "已查看文件夹",
                ToolKind.ReadLogTail => "已查看日志",
                ToolKind.ReadFile => "已查看文件",
                ToolKind.RunCommand => "已提议一项改动",
                _ => "完成"
            };
            AppendOutput($"[{tool}] OK\\n{resp.Result}\\n");
        }
'''
new_run = '''        _busy = true;
        // R041：仅提议改动显示等待确认；只读不套等待句
        if (ProposeChangeCopy.UsesWaitingStatus(tool))
            _status.Text = ProposeChangeCopy.WaitingStatus;
        try
        {
            var resp = await _client.InvokeToolAsync(tool, args).ConfigureAwait(true);
            if (!resp.Ok)
            {
                var msg = ProposeChangeCopy.FailureStatus(resp.DeniedByUser, resp.Error);
                _status.Text = msg;
                AppendOutput($"[{tool}] FAIL: {msg}\\n");
                return;
            }

            _status.Text = tool switch
            {
                ToolKind.ListProcesses => "已查看正在运行的程序",
                ToolKind.ListDir => "已查看文件夹",
                ToolKind.ReadLogTail => "已查看日志",
                ToolKind.ReadFile => "已查看文件",
                ToolKind.RunCommand => ProposeChangeCopy.AllowedStatus,
                _ => "完成"
            };
            AppendOutput($"[{tool}] OK\\n{resp.Result}\\n");
        }
'''
if old_run not in t:
    raise SystemExit("ava RunTool missing")
t = t.replace(old_run, new_run, 1)

old_shot = '''    internal void PreparePeerConnectedSessionForShot()
    {
        _uiShotConnected = true;
        SetConnectedUi(true);
        _status.Text = HelperSessionCopy.StatusConnected;
        _failHint.Text = "";
        _output.Text = "PAIR_OK\\n";
    }
'''
new_shot = '''    internal void PreparePeerConnectedSessionForShot()
    {
        _uiShotConnected = true;
        SetConnectedUi(true);
        _status.Text = HelperSessionCopy.StatusConnected;
        _failHint.Text = "";
        _output.Text = ProposeChangeCopy.ConnectedResultHint;
    }

    /// <summary>UiShot R041：提议等待确认。</summary>
    internal void PrepareProposeWaitingForShot()
    {
        _uiShotConnected = true;
        SetConnectedUi(true);
        _status.Text = ProposeChangeCopy.WaitingStatus;
        _failHint.Text = "";
        _output.Text = ProposeChangeCopy.ConnectedResultHint;
    }

    /// <summary>UiShot R041：对方已允许。</summary>
    internal void PrepareProposeAllowedForShot()
    {
        _uiShotConnected = true;
        SetConnectedUi(true);
        _status.Text = ProposeChangeCopy.AllowedStatus;
        _failHint.Text = "";
        _output.Text = "[RunCommand] OK\\necho r041-ok\\n";
    }

    /// <summary>UiShot R041：对方拒绝。</summary>
    internal void PrepareProposeDeniedForShot()
    {
        _uiShotConnected = true;
        SetConnectedUi(true);
        _status.Text = WriteConfirmCopy.DeniedMessage;
        _failHint.Text = "";
        _output.Text = "[RunCommand] FAIL: " + WriteConfirmCopy.DeniedMessage + "\\n";
    }

    /// <summary>UiShot R041：确认超时。</summary>
    internal void PrepareProposeTimedOutForShot()
    {
        _uiShotConnected = true;
        SetConnectedUi(true);
        _status.Text = WriteConfirmCopy.TimedOutMessage;
        _failHint.Text = "";
        _output.Text = "[RunCommand] FAIL: " + WriteConfirmCopy.TimedOutMessage + "\\n";
    }
'''
if old_shot not in t:
    raise SystemExit("ava PreparePeerConnected missing")
t = t.replace(old_shot, new_shot, 1)

# Update PrepareDeniedForShot if it still uses old wording - leave as is (already DeniedMessage)
# Remove remaining PAIR_OK
if "PAIR_OK" in t:
    # may remain elsewhere?
    for i,l in enumerate(t.splitlines(),1):
        if "PAIR_OK" in l:
            print("PAIR_OK left line", i, l)
    raise SystemExit("PAIR_OK still in HelperWindow")
p.write_text(t, encoding="utf-8", newline="\n")
print("HelperWindow OK")
