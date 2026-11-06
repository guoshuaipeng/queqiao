from pathlib import Path
Path('src-net48/RemoteAssist.App/AppShell.cs').write_text(r'''using System.Windows.Forms;
using RemoteAssist.Agent;
using RemoteAssist.Helper.UI;
using RemoteAssist.Protocol;

namespace RemoteAssist.App;

/// <summary>
/// 统一 App 外壳：首页 ↔ 被控 ↔ 控制。
/// </summary>
internal sealed class AppShell : ApplicationContext
{
    private readonly int _port;
    private readonly bool _insecure;
    private readonly int _confirmTimeoutSeconds;
    private readonly ModeSelectForm _home;

    private AgentDesktopController? _agent;
    private HelperDesktopController? _helper;

    public AppShell(int port, bool insecure, int confirmTimeoutSeconds, AppMode? initialMode)
    {
        _port = port;
        _insecure = insecure;
        _confirmTimeoutSeconds = confirmTimeoutSeconds;

        _home = new ModeSelectForm();
        _home.AcceptClicked += () => EnterAcceptMode();
        _home.ControlClicked += () => EnterControlMode();
        _home.FormClosed += (_, e) =>
        {
            if (e.CloseReason != CloseReason.None)
                ExitThread();
        };

        if (initialMode == AppMode.Accept)
            EnterAcceptMode();
        else if (initialMode == AppMode.Control)
            EnterControlMode();
        else
            _home.Show();
    }

    private void EnterAcceptMode()
    {
        LeaveControlMode();
        _home.Hide();

        if (_agent is not null)
        {
            _agent.ShowMain();
            return;
        }

        try
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
        }
        catch (Exception ex)
        {
            try
            {
                var auditDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RemoteAssist");
                Directory.CreateDirectory(auditDir);
                File.AppendAllText(
                    Path.Combine(auditDir, "ui-errors.log"),
                    DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz") + "\taccept_start\t" + ex + Environment.NewLine);
            }
            catch { /* ignore */ }

            var human = FriendConnectGuide.FriendlyOrRaw(ex);
            if (ex is System.Net.Sockets.SocketException ||
                (ex.InnerException is System.Net.Sockets.SocketException) ||
                (ex.Message?.IndexOf("address already in use", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (ex.Message?.IndexOf("通常每个套接字地址", StringComparison.Ordinal) >= 0))
            {
                human = "本机协助端口已被占用。请关掉其他鹊桥窗口后再试；或在高级里改用默认公网中继（不必开局域网端口）。";
            }
            else if (string.IsNullOrWhiteSpace(human) || human == ex.Message)
            {
                human = "无法开始接受协助。" + Environment.NewLine + Environment.NewLine +
                        FriendConnectGuide.FriendlyOrRaw(ex) + Environment.NewLine + Environment.NewLine +
                        "下一步：确认网络可用后重试；若反复失败，把日志目录 %LocalAppData%\\RemoteAssist 发给协助方。";
            }
            else
            {
                human = human + Environment.NewLine + Environment.NewLine +
                        "下一步：确认网络可用后重试；仍不行时查看 %LocalAppData%\\RemoteAssist\\ui-errors.log。";
            }

            MessageBox.Show(human, "鹊桥", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _agent?.Dispose();
            _agent = null;
            _home.Show();
            _home.Activate();
        }
    }

    private void EnterControlMode()
    {
        LeaveAcceptMode();
        _home.Hide();

        if (_helper is not null)
        {
            _helper.Show();
            return;
        }

        try
        {
            _helper = new HelperDesktopController(enableReturnHome: true);
            _helper.HomeRequested += (_, _) => ReturnHomeFromControl();
            _helper.Show();
        }
        catch (Exception ex)
        {
            try
            {
                var auditDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RemoteAssist");
                Directory.CreateDirectory(auditDir);
                File.AppendAllText(
                    Path.Combine(auditDir, "ui-errors.log"),
                    DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz") + "\tcontrol_start\t" + ex + Environment.NewLine);
            }
            catch { /* ignore */ }

            var human = FriendConnectGuide.FriendlyOrRaw(ex);
            MessageBox.Show(
                human + Environment.NewLine + Environment.NewLine + "下一步：关闭后重试；仍失败请查看 %LocalAppData%\\RemoteAssist\\ui-errors.log。",
                "鹊桥",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            _helper?.Dispose();
            _helper = null;
            _home.Show();
            _home.Activate();
        }
    }

    private void ReturnHomeFromAccept()
    {
        LeaveAcceptMode();
        _home.Show();
        _home.Activate();
        _home.BringToFront();
    }

    private void ReturnHomeFromControl()
    {
        LeaveControlMode();
        _home.Show();
        _home.Activate();
        _home.BringToFront();
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeModes();
            if (!_home.IsDisposed)
                _home.Dispose();
        }
        base.Dispose(disposing);
    }
}
''', encoding='utf-8', newline='\n')
print('AppShell OK')
