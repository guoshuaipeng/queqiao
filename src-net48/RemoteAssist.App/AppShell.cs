using System;
using System.IO;
using System.Windows.Forms;
using RemoteAssist.Agent;
using RemoteAssist.Helper.UI;
using RemoteAssist.Protocol;

namespace RemoteAssist.App;

/// <summary>
/// R049：统一 App 外壳 — 单主窗口页内切换首页/接受协助/去协助。
/// 活动会话结束须本机确认；安全确认对话框仍独立弹出，不并入主窗。
/// </summary>
internal sealed class AppShell : ApplicationContext
{
    private readonly int _port;
    private readonly bool _insecure;
    private readonly int _confirmTimeoutSeconds;
    private readonly ShellMainForm _shell;

    private AgentDesktopController? _agent;
    private HelperDesktopController? _helper;
    private enum Mode { Home, Accept, Control }
    private Mode _mode = Mode.Home;

    public AppShell(int port, bool insecure, int confirmTimeoutSeconds, AppMode? initialMode)
    {
        _port = port;
        _insecure = insecure;
        _confirmTimeoutSeconds = confirmTimeoutSeconds;

        _shell = new ShellMainForm();
        MainForm = _shell;
        _shell.AcceptRequested += () => EnterAcceptMode();
        _shell.ControlRequested += () => EnterControlMode();
        _shell.ExitRequested += () => HandleShellClose();

        if (initialMode == AppMode.Accept)
            EnterAcceptMode();
        else if (initialMode == AppMode.Control)
            EnterControlMode();
        else
        {
            _mode = Mode.Home;
            _shell.ShowHome();
            _shell.Show();
        }
    }

    private void EnterAcceptMode()
    {
        if (_mode == Mode.Control)
        {
            if (_helper is not null && !_helper.ConfirmEndSessionOrCancel(_shell))
                return; // 取消：留在去协助页
            LeaveControlMode();
        }

        try
        {
            if (_agent is null)
            {
                _agent = new AgentDesktopController(
                    port: _port,
                    insecure: _insecure,
                    confirmTimeoutSeconds: _confirmTimeoutSeconds,
                    enableReturnHome: true);
                _agent.HomeRequested += (_, _) => ReturnHomeFromAccept();
                _agent.ExitRequested += (_, _) =>
                {
                    DisposeModes();
                    ExitThread();
                };
                _agent.Start();
                if (_agent.MainFormControl is { } af)
                {
                    _agent.AttachToShell(_shell, () =>
                    {
                        _shell.ShowAcceptPage(af);
                        _shell.ActivateShell();
                    });
                    _shell.ShowAcceptPage(af);
                }
            }
            else if (_agent.MainFormControl is { } af2)
            {
                _shell.ShowAcceptPage(af2);
            }

            _mode = Mode.Accept;
            _shell.ActivateShell();
            _agent?.ShowMain();
        }
        catch (Exception ex)
        {
            LogUiError("accept_start", ex);
            MessageBox.Show(BuildAcceptError(ex), "鹊桥", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _agent?.Dispose();
            _agent = null;
            _mode = Mode.Home;
            _shell.ShowHome();
            _shell.ActivateShell();
        }
    }

    private void EnterControlMode()
    {
        if (_mode == Mode.Accept)
        {
            if (_agent is not null && !_agent.ConfirmEndSessionOrCancel(_shell))
                return;
            LeaveAcceptMode();
        }

        try
        {
            if (_helper is null)
            {
                _helper = new HelperDesktopController(enableReturnHome: true);
                _helper.HomeRequested += (_, _) => ReturnHomeFromControl();
                _helper.AttachToShell(_shell, () =>
                {
                    _shell.ShowControlPage(_helper.MainFormControl);
                    _shell.ActivateShell();
                });
                _shell.ShowControlPage(_helper.MainFormControl);
            }
            else
            {
                _shell.ShowControlPage(_helper.MainFormControl);
            }

            _mode = Mode.Control;
            _shell.ActivateShell();
        }
        catch (Exception ex)
        {
            LogUiError("control_start", ex);
            var human = FriendConnectGuide.FriendlyOrRaw(ex);
            MessageBox.Show(
                human + Environment.NewLine + Environment.NewLine + "下一步：关闭后重试；仍失败请查看 %LocalAppData%\\RemoteAssist\\ui-errors.log。",
                "鹊桥",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            _helper?.Dispose();
            _helper = null;
            _mode = Mode.Home;
            _shell.ShowHome();
            _shell.ActivateShell();
        }
    }

    private void ReturnHomeFromAccept()
    {
        // 确认已在 AgentDesktopUi.TryReturnHome 完成；此处结束会话并回首页
        LeaveAcceptMode();
        _mode = Mode.Home;
        _shell.ShowHome();
        _shell.ActivateShell();
    }

    private void ReturnHomeFromControl()
    {
        // 确认已在 Helper 按钮完成
        LeaveControlMode();
        _mode = Mode.Home;
        _shell.ShowHome();
        _shell.ActivateShell();
    }

    private void HandleShellClose()
    {
        if (_mode == Mode.Accept && _agent is not null)
        {
            if (!_agent.ConfirmEndSessionOrCancel(_shell))
                return; // 拒绝/关闭确认 → 不结束会话、不关主窗
            LeaveAcceptMode();
        }
        else if (_mode == Mode.Control && _helper is not null)
        {
            if (!_helper.ConfirmEndSessionOrCancel(_shell))
                return;
            LeaveControlMode();
        }

        DisposeModes();
        if (!_shell.IsDisposed)
        {
            _shell.FormClosing -= null; // no-op safeguard
            // allow real close
            _shell.Hide();
        }
        ExitThread();
    }

    private void LeaveAcceptMode()
    {
        if (_agent is null) return;
        _agent.Dispose();
        _agent = null;
    }

    private void LeaveControlMode()
    {
        if (_helper is null) return;
        _helper.Dispose();
        _helper = null;
    }

    private void DisposeModes()
    {
        LeaveAcceptMode();
        LeaveControlMode();
    }

    private static void LogUiError(string tag, Exception ex)
    {
        try
        {
            var auditDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RemoteAssist");
            Directory.CreateDirectory(auditDir);
            File.AppendAllText(
                Path.Combine(auditDir, "ui-errors.log"),
                DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz") + "\t" + tag + "\t" + ex + Environment.NewLine);
        }
        catch { /* ignore */ }
    }

    private static string BuildAcceptError(Exception ex)
    {
        var human = FriendConnectGuide.FriendlyOrRaw(ex);
        if (ex is System.Net.Sockets.SocketException ||
            (ex.InnerException is System.Net.Sockets.SocketException) ||
            (ex.Message?.IndexOf("address already in use", StringComparison.OrdinalIgnoreCase) >= 0) ||
            (ex.Message?.IndexOf("通常每个套接字地址", StringComparison.Ordinal) >= 0))
        {
            return "本机协助端口已被占用。请关掉其他鹊桥窗口后再试；或在高级里改用默认公网中继（不必开局域网端口）。";
        }
        if (string.IsNullOrWhiteSpace(human) || human == ex.Message)
        {
            return "无法开始接受协助。" + Environment.NewLine + Environment.NewLine +
                   FriendConnectGuide.FriendlyOrRaw(ex) + Environment.NewLine + Environment.NewLine +
                   "下一步：确认网络可用后重试；若反复失败，把日志目录 %LocalAppData%\\RemoteAssist 发给协助方。";
        }
        return human + Environment.NewLine + Environment.NewLine +
               "下一步：确认网络可用后重试；仍不行时查看 %LocalAppData%\\RemoteAssist\\ui-errors.log。";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeModes();
            if (!_shell.IsDisposed)
                _shell.Dispose();
        }
        base.Dispose(disposing);
    }
}
