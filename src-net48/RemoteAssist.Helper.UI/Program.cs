using System.Windows.Forms;

namespace RemoteAssist.Helper.UI;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
                Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
