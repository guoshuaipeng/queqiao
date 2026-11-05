using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RemoteAssist.Helper.UI;

internal static class AppIcons
{
    /// <summary>协助端图标：绿色调，与被协助端蓝色区分。</summary>
    public static Icon CreateAppIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var brush = new SolidBrush(Color.FromArgb(0x0F, 0x9D, 0x58));
            g.FillEllipse(brush, 2, 2, 28, 28);
            using var pen = new Pen(Color.White, 3);
            // 简易「耳机/协助」：双弧
            g.DrawArc(pen, 8, 8, 16, 16, 200, 140);
            g.DrawLine(pen, 10, 16, 10, 22);
            g.DrawLine(pen, 22, 16, 22, 22);
        }
        using var tmp = Icon.FromHandle(bmp.GetHicon());
        return (Icon)tmp.Clone();
    }
}
