using System.Windows.Forms;
using RemoteAssist.Agent;
using RemoteAssist.Helper.UI;

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
            MessageBox.Show(ex.ToString(), "启动被控模式失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _agent?.Dispose();
            _agent = null;
            _home.Show();
            _home.Activate();
        }
    }

    private void EnterControlMode()
    {
        // 进入控制前若被控在跑，先干净退出被控
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
            MessageBox.Show(ex.ToString(), "启动控制模式失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
