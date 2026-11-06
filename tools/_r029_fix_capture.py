from pathlib import Path
rs = Path("tools/UiShot/R029Shots.cs")
rt = rs.read_text(encoding="utf-8")

# Replace SaveFormWithMenu and add composite save; fix clipboard shot call
old_save = '''    private static void SaveFormWithMenu(Form form, string path)
    {
        form.Update();
        Application.DoEvents();
        // 菜单在屏幕坐标；用 CopyFromScreen 实拍（勿涂鸦）
        var formRect = form.RectangleToScreen(form.ClientRectangle);
        // 扩大一点以盖住「更多」下拉
        var capture = new Rectangle(
            formRect.X,
            formRect.Y,
            Math.Max(formRect.Width, 780),
            Math.Max(formRect.Height, 560));
        using var bmp = new Bitmap(capture.Width, capture.Height);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(capture.Location, Point.Empty, capture.Size);
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"saved {path} screen-capture {capture.Width}x{capture.Height}");
    }
}'''

new_save = '''    private static void SaveFormWithMenu(Form form, string path)
    {
        form.TopMost = true;
        form.Activate();
        form.BringToFront();
        form.Update();
        Application.DoEvents();
        Thread.Sleep(120);
        Application.DoEvents();
        var formRect = form.RectangleToScreen(form.ClientRectangle);
        var capture = new Rectangle(
            formRect.X,
            formRect.Y,
            Math.Max(formRect.Width, 780),
            Math.Max(formRect.Height + 40, 560));
        using var bmp = new Bitmap(capture.Width, capture.Height);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(capture.Location, Point.Empty, capture.Size);
        form.TopMost = false;
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"saved {path} screen-capture {capture.Width}x{capture.Height}");
    }

    private static void SaveFormWithOverlay(Form form, string path)
    {
        form.TopMost = true;
        form.Activate();
        form.BringToFront();
        form.Update();
        Application.DoEvents();
        Thread.Sleep(120);
        Application.DoEvents();

        using var bmp = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height));
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));

        // 手动叠上剪贴板预览 Label（DrawToBitmap 可能丢后加控件 z-order）
        var preview = FindNamed(form, "skillClipboardPreview");
        if (preview is { Visible: true })
        {
            using var overlay = new Bitmap(Math.Max(1, preview.Width), Math.Max(1, preview.Height));
            preview.DrawToBitmap(overlay, new Rectangle(0, 0, preview.Width, preview.Height));
            using var g = Graphics.FromImage(bmp);
            g.FillRectangle(Brushes.White, preview.Bounds);
            g.DrawImage(overlay, preview.Location);
            using var pen = new Pen(Color.FromArgb(0xDA, 0xDE, 0xE3));
            g.DrawRectangle(pen, preview.Bounds);
        }

        form.TopMost = false;
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"saved {path} overlay-composite {form.Width}x{form.Height}");
    }

    private static Control? FindNamed(Control root, string name)
    {
        if (root.Name == name) return root;
        foreach (Control c in root.Controls)
        {
            var f = FindNamed(c, name);
            if (f is not null) return f;
        }
        return null;
    }
}'''

if old_save not in rt:
    raise SystemExit('SaveFormWithMenu block not found')
rt = rt.replace(old_save, new_save)

rt = rt.replace(
'''            // 叠层预览用实拍，避免 DrawToBitmap 丢 z-order
            SaveFormWithMenu(agent, Path.Combine(outDir, "r029-skill-clipboard-preview.png"));''',
'''            SaveFormWithOverlay(agent, Path.Combine(outDir, "r029-skill-clipboard-preview.png"));''')
rs.write_text(rt, encoding='utf-8')
print('R029Shots updated')
