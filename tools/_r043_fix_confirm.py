from pathlib import Path
p=Path('src-net48/RemoteAssist.Agent/ConfirmPrompt.cs')
t=p.read_text(encoding='utf-8')
old='''    internal static void EnsureWinFormsInitialized()
    {
        if (Interlocked.Exchange(ref _winFormsInit, 1) == 0)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }
    }'''
new='''    internal static void EnsureWinFormsInitialized()
    {
        if (Interlocked.Exchange(ref _winFormsInit, 1) == 0)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
            catch (InvalidOperationException)
            {
                // 已由宿主 Program / 首页 Form 初始化过 WinForms（R043 实测根因）。
            }
        }
    }'''
if old not in t:
    raise SystemExit('EnsureWinForms block missing')
p.write_text(t.replace(old,new,1),encoding='utf-8',newline='\n')
print('ConfirmPrompt fixed')

# Fix Program.cs indentation
pp=Path('src-net48/RemoteAssist.App/Program.cs')
pt=pp.read_text(encoding='utf-8')
pt=pt.replace('                Application.EnableVisualStyles();\n        Application.SetCompatibleTextRenderingDefault(false);',
              '        Application.EnableVisualStyles();\n        Application.SetCompatibleTextRenderingDefault(false);')
pp.write_text(pt,encoding='utf-8',newline='\n')
print('Program indent OK')
