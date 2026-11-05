namespace RemoteAssist.Protocol;

/// <summary>
/// 主窗口位置/大小（本机用户设置；不写中继）。
/// 统一 App 的首页 / 被控 / 控制主窗共用一份，切换模式时仍落在同一屏附近。
/// </summary>
public sealed class WindowPlacementState
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>Normal / Maximized（当前主窗多为 FixedSingle，Maximized 预留）。</summary>
    public string? State { get; set; }

    public bool IsValidSize => Width >= 80 && Height >= 60;
}

/// <summary>整数矩形，避免 Protocol 依赖 WinForms / System.Drawing。</summary>
public readonly record struct RectI(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;

    public bool Intersects(RectI other) =>
        X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;

    public int IntersectionArea(RectI other)
    {
        var x1 = Math.Max(X, other.X);
        var y1 = Math.Max(Y, other.Y);
        var x2 = Math.Min(Right, other.Right);
        var y2 = Math.Min(Bottom, other.Bottom);
        if (x2 <= x1 || y2 <= y1) return 0;
        return (x2 - x1) * (y2 - y1);
    }

    public long Area => (long)Math.Max(0, Width) * Math.Max(0, Height);
}

/// <summary>
/// 窗口还原几何：显示器断开或还原后完全不可见时，回退到主屏可见区域。
/// </summary>
public static class WindowPlacementMath
{
    /// <summary>至少露出这么多面积比例才算「可见」。</summary>
    public const double MinVisibleRatio = 0.15;

    /// <summary>至少露出这么多像素才算「可见」。</summary>
    public const int MinVisiblePixels = 48;

    public static bool IsSufficientlyVisible(RectI window, IReadOnlyList<RectI> workingAreas)
    {
        if (window.Width <= 0 || window.Height <= 0 || workingAreas.Count == 0)
            return false;

        long visible = 0;
        foreach (var wa in workingAreas)
            visible += window.IntersectionArea(wa);

        if (visible < MinVisiblePixels)
            return false;

        var area = window.Area;
        if (area <= 0) return false;
        return (double)visible / area >= MinVisibleRatio;
    }

    /// <summary>
    /// 将偏好位置落到某个工作区；若完全不可见则居中放到 primaryWorking。
    /// keepSize=false 时只用 working 区来定位，宽高保持 preferred（调用方可先写入设计尺寸）。
    /// </summary>
    public static RectI Resolve(
        RectI? preferred,
        IReadOnlyList<RectI> workingAreas,
        RectI primaryWorking,
        int defaultWidth,
        int defaultHeight,
        bool restoreSize)
    {
        var w = defaultWidth;
        var h = defaultHeight;
        if (preferred is { } p)
        {
            if (restoreSize && p.Width >= 80 && p.Height >= 60)
            {
                w = p.Width;
                h = p.Height;
            }

            var candidate = new RectI(p.X, p.Y, w, h);
            if (IsSufficientlyVisible(candidate, workingAreas))
                return ClampToNearestWorkingArea(candidate, workingAreas, primaryWorking);
        }

        return CenterIn(primaryWorking, w, h);
    }

    public static RectI CenterIn(RectI area, int width, int height)
    {
        width = Math.Min(width, Math.Max(80, area.Width));
        height = Math.Min(height, Math.Max(60, area.Height));
        var x = area.X + Math.Max(0, (area.Width - width) / 2);
        var y = area.Y + Math.Max(0, (area.Height - height) / 2);
        return new RectI(x, y, width, height);
    }

    /// <summary>确认窗：在指定主窗所在工作区居中；找不到则主屏居中。</summary>
    public static RectI CenterDialogOnScreen(
        int dialogWidth,
        int dialogHeight,
        RectI? hostWindow,
        IReadOnlyList<RectI> workingAreas,
        RectI primaryWorking)
    {
        RectI target = primaryWorking;
        if (hostWindow is { } host && workingAreas.Count > 0)
        {
            // 取与主窗交集最大的工作区
            var best = primaryWorking;
            var bestArea = -1;
            foreach (var wa in workingAreas)
            {
                var a = host.IntersectionArea(wa);
                if (a > bestArea)
                {
                    bestArea = a;
                    best = wa;
                }
            }
            if (bestArea > 0)
                target = best;
            else
            {
                // 主窗点是否落在某屏
                foreach (var wa in workingAreas)
                {
                    if (host.X >= wa.X && host.X < wa.Right && host.Y >= wa.Y && host.Y < wa.Bottom)
                    {
                        target = wa;
                        break;
                    }
                }
            }
        }

        return CenterIn(target, dialogWidth, dialogHeight);
    }

    private static RectI ClampToNearestWorkingArea(
        RectI window,
        IReadOnlyList<RectI> workingAreas,
        RectI primaryWorking)
    {
        RectI best = primaryWorking;
        var bestArea = -1;
        foreach (var wa in workingAreas)
        {
            var a = window.IntersectionArea(wa);
            if (a > bestArea)
            {
                bestArea = a;
                best = wa;
            }
        }

        // 保证标题栏附近仍在工作区内：必要时钳制左上角
        var x = window.X;
        var y = window.Y;
        var w = Math.Min(window.Width, best.Width);
        var h = Math.Min(window.Height, best.Height);
        if (x + Math.Min(80, w) > best.Right) x = best.Right - Math.Min(80, w);
        if (y + Math.Min(40, h) > best.Bottom) y = best.Bottom - Math.Min(40, h);
        if (x < best.X) x = best.X;
        if (y < best.Y) y = best.Y;
        return new RectI(x, y, w, h);
    }
}

