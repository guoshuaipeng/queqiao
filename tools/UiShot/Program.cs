using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RemoteAssist.Agent;
using RemoteAssist.Helper.UI;
using RemoteAssist.Protocol;

namespace UiShot;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var outDir = args.Length > 0 ? args[0] : @"E:\IdeaProjects\remote-assist\docs\screenshots";
        Directory.CreateDirectory(outDir);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var runDesc = WriteConfirmCopy.Describe(ToolKind.RunCommand, new Dictionary<string, string?>
        {
            ["command"] = "echo hello"
        });
        using (var confirm = new ConfirmDialog(new ConfirmRequestInfo(
                   Source: "协助方",
                   Tool: "RunCommand",
                   Summary: runDesc.Headline,
                   TimeoutSeconds: 60,
                   Detail: runDesc.Detail)))
        {
            confirm.Show();
            Application.DoEvents();
            Thread.Sleep(200);
            Application.DoEvents();
            SaveForm(confirm, Path.Combine(outDir, "r004-write-confirm.png"));
            confirm.Hide();
        }

        var delDesc = WriteConfirmCopy.Describe(ToolKind.RunCommand, new Dictionary<string, string?>
        {
            ["command"] = "del C:\\temp\\demo.txt"
        });
        using (var confirmDel = new ConfirmDialog(new ConfirmRequestInfo(
                   Source: "协助方",
                   Tool: "RunCommand",
                   Summary: delDesc.Headline,
                   TimeoutSeconds: 60,
                   Detail: delDesc.Detail)))
        {
            confirmDel.Show();
            Application.DoEvents();
            Thread.Sleep(150);
            Application.DoEvents();
            SaveForm(confirmDel, Path.Combine(outDir, "r004-write-confirm-delete.png"));
            confirmDel.Hide();
        }

        using (var helper = new HelperDesktopController(enableReturnHome: true))
        {
            helper.Show();
            Application.DoEvents();
            // simulate post-copy tip for screenshot visibility
            var form = helper.Form;
            var preview = FindPreview(form);
            if (preview is not null)
            {
                preview.Text = WriteConfirmCopy.InstallCopiedHint;
                preview.ForeColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            }
            Application.DoEvents();
            Thread.Sleep(300);
            Application.DoEvents();
            SaveForm(form, Path.Combine(outDir, "r004-helper-install-guide.png"));
            form.Hide();
        }

        Console.WriteLine("OK wrote r004 shots to " + outDir);
        return 0;
    }

    private static Label? FindPreview(Control root)
    {
        foreach (Control c in root.Controls)
        {
            if (c is Label lbl && lbl.Text.Contains("粘贴后点", StringComparison.Ordinal))
                return lbl;
            var nested = FindPreview(c);
            if (nested is not null) return nested;
        }
        // also match after we may have already changed text — search by size/location heuristic
        foreach (Control c in root.Controls)
        {
            if (c is Panel)
            {
                foreach (Control child in c.Controls)
                {
                    if (child is Label l && l.Location is { X: 16, Y: 164 })
                        return l;
                }
            }
        }
        return null;
    }

    private static void SaveForm(Form form, string path)
    {
        form.Update();
        using var bmp = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height));
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"saved {path} {form.Width}x{form.Height} title={form.Text}");
    }
}
