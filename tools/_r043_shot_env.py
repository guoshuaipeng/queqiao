from pathlib import Path
p=Path('src-net48/RemoteAssist.Helper.UI/MainForm.cs')
t=p.read_text(encoding='utf-8')
# After LayoutPanels(); RefreshPreview(); SetConnectedUi(false);
needle='        LayoutPanels();\n        RefreshPreview();\n        SetConnectedUi(false);\n    }'
insert='''        LayoutPanels();
        RefreshPreview();
        SetConnectedUi(false);

        // R043 截图辅助：真实 WinForms 进程，经环境变量预置状态（非 Avalonia/ui-shot 假窗）
        var shot = Environment.GetEnvironmentVariable("REMOTE_ASSIST_R043_SHOT");
        if (!string.IsNullOrWhiteSpace(shot))
        {
            BeginInvoke(new Action(() => ApplyR043Shot(shot!)));
        }
    }

    private void ApplyR043Shot(string shot)
    {
        shot = shot.Trim().ToLowerInvariant();
        if (shot == "advanced")
        {
            if (!_advancedOpen) ToggleAdvanced();
            return;
        }
        if (shot == "pasted")
        {
            _inputBox.Text = "AB12CD-00481273";
            RefreshPreview();
            return;
        }
        if (shot == "fail")
        {
            _inputBox.Text = "AB12";
            RefreshPreview();
            _ = ConnectAsync();
            return;
        }
    }
'''
# Careful - the closing of ctor - replace only first occurrence at end of ctor
if 'ApplyR043Shot' in t:
    print('already has shot helper')
else:
    if needle not in t:
        raise SystemExit('ctor end needle missing')
    t=t.replace(needle, insert, 1)
    p.write_text(t,encoding='utf-8',newline='\n')
    print('shot helper added')
