using System.Windows.Forms;
using RemoteAssist;

namespace RemoteAssist.Helper.UI;

/// <summary>
/// 控制端桌面模式公共入口（统一 App 复用）。
/// </summary>
public sealed class HelperDesktopController : IDisposable
{
    private readonly MainForm _form;
    private bool _disposed;

    public event EventHandler? HomeRequested;

    public HelperDesktopController(bool enableReturnHome = true)
    {
        _form = new MainForm(enableReturnHome);
        if (enableReturnHome)
            _form.HomeRequested += () => HomeRequested?.Invoke(this, EventArgs.Empty);
    }

    public Form Form => _form;

    public void Show()
    {
        UiWindowPlacement.MainWindow = _form;
        _form.Show();
        if (_form.WindowState == FormWindowState.Minimized)
            _form.WindowState = FormWindowState.Normal;
        _form.Activate();
        _form.BringToFront();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (!_form.IsDisposed)
        {
            try { UiWindowPlacement.Save(_form); } catch { /* ignore */ }
            _form.Dispose();
        }
    }
}
