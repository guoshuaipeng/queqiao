using System.Drawing;
using System.Windows.Forms;
using RemoteAssist.Protocol;

namespace RemoteAssist;

/// <summary>
/// WinForms 窗口位置记忆：还原 / 保存 / 确认窗跟随主窗所在屏。
/// 通过 csproj Link 挂到 App / Agent / Helper.UI。
/// </summary>
internal static class UiWindowPlacement
{
    private static Form? _main;

    /// <summary>当前主窗口（首页或被控/控制主界面），供确认窗跟随。</summary>
    public static Form? MainWindow
    {
        get
        {
            var f = _main;
            if (f is null || f.IsDisposed) return null;
            return f;
        }
        set => _main = value;
    }

    public static void AttachMainWindow(Form form)
    {
        MainWindow = form;
        form.Shown += (_, _) => MainWindow = form;
        form.Activated += (_, _) => MainWindow = form;
        form.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(_main, form))
                _main = null;
        };
    }

    /// <summary>
    /// 启动时还原；关闭/隐藏前保存。Fixed 窗体默认只还原位置，不改设计尺寸。
    /// </summary>
    public static void Bind(Form form, bool restoreSize = false)
    {
        AttachMainWindow(form);
        form.StartPosition = FormStartPosition.Manual;

        void Apply()
        {
            try
            {
                Restore(form, restoreSize);
            }
            catch
            {
                // 忽略设置损坏等情况，保留设计位置
            }
        }

        // 每次显示都按最新本机设置还原（模式切换后首页/主窗共用同一份位置）
        form.Shown += (_, _) => Apply();
        if (form.IsHandleCreated && form.Visible)
            Apply();

        form.FormClosing += (_, _) =>
        {
            try { Save(form); } catch { /* ignore */ }
        };

        // 隐藏到托盘 / 切换模式前 Visible=false：此时仍可读到 Bounds
        form.VisibleChanged += (_, _) =>
        {
            if (form.Visible || form.IsDisposed) return;
            if (form.WindowState == FormWindowState.Minimized) return;
            try { Save(form); } catch { /* ignore */ }
        };

        form.ResizeEnd += (_, _) =>
        {
            if (!form.Visible || form.WindowState == FormWindowState.Minimized) return;
            try { Save(form); } catch { /* ignore */ }
        };
    }

    public static void Restore(Form form, bool restoreSize = false)
    {
        var settings = HelperUiSettings.Load();
        var preferred = ToRect(settings.WindowPlacement);
        var areas = GetWorkingAreas();
        var primary = ToRectI(Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1024, 768));
        var design = new RectI(form.Location.X, form.Location.Y, form.Width, form.Height);

        var resolved = WindowPlacementMath.Resolve(
            preferred,
            areas,
            primary,
            design.Width,
            design.Height,
            restoreSize);

        form.Bounds = new Rectangle(resolved.X, resolved.Y, resolved.Width, resolved.Height);

        if (string.Equals(settings.WindowPlacement?.State, "Maximized", StringComparison.OrdinalIgnoreCase)
            && form.MaximizeBox)
        {
            form.WindowState = FormWindowState.Maximized;
        }
    }

    public static void Save(Form form)
    {
        if (form.WindowState == FormWindowState.Minimized)
            return;

        var bounds = form.WindowState == FormWindowState.Maximized
            ? form.RestoreBounds
            : form.Bounds;

        // 再校验可见性：不可见则不覆盖已有好设置
        var areas = GetWorkingAreas();
        var rect = new RectI(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        if (!WindowPlacementMath.IsSufficientlyVisible(rect, areas))
            return;

        var settings = HelperUiSettings.Load();
        settings.WindowPlacement = new WindowPlacementState
        {
            X = bounds.X,
            Y = bounds.Y,
            Width = bounds.Width,
            Height = bounds.Height,
            State = form.WindowState == FormWindowState.Maximized ? "Maximized" : "Normal"
        };
        settings.Save();
    }

    /// <summary>确认窗 / 模态框：跟随主窗口所在屏居中。</summary>
    public static void PlaceDialog(Form dialog)
    {
        dialog.StartPosition = FormStartPosition.Manual;
        void Apply()
        {
            try
            {
                var areas = GetWorkingAreas();
                var primary = ToRectI(Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1024, 768));
                RectI? host = null;
                var main = MainWindow;
                if (main is not null && main.Visible)
                    host = new RectI(main.Bounds.X, main.Bounds.Y, main.Bounds.Width, main.Bounds.Height);
                else
                {
                    var saved = HelperUiSettings.Load().WindowPlacement;
                    if (saved is not null)
                        host = new RectI(saved.X, saved.Y, Math.Max(80, saved.Width), Math.Max(60, saved.Height));
                }

                var r = WindowPlacementMath.CenterDialogOnScreen(
                    dialog.Width, dialog.Height, host, areas, primary);
                dialog.Location = new Point(r.X, r.Y);
            }
            catch
            {
                dialog.StartPosition = FormStartPosition.CenterScreen;
            }
        }

        if (dialog.IsHandleCreated)
            Apply();
        else
            dialog.HandleCreated += (_, _) => Apply();
    }

    private static IReadOnlyList<RectI> GetWorkingAreas()
    {
        var list = new List<RectI>();
        foreach (var s in Screen.AllScreens)
            list.Add(ToRectI(s.WorkingArea));
        if (list.Count == 0)
            list.Add(new RectI(0, 0, 1024, 768));
        return list;
    }

    private static RectI ToRectI(Rectangle r) => new(r.X, r.Y, r.Width, r.Height);

    private static RectI? ToRect(WindowPlacementState? s)
    {
        if (s is null) return null;
        // 尺寸可在 Resolve 时回落到设计值；坐标 0,0 也是合法主屏位置
        return new RectI(s.X, s.Y, s.Width > 0 ? s.Width : 0, s.Height > 0 ? s.Height : 0);
    }
}

