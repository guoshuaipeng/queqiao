from pathlib import Path
p=Path('src-net48/RemoteAssist.Helper.UI/MainForm.cs')
t=p.read_text(encoding='utf-8')
old='''        // R043 截图辅助：真实 WinForms 进程，经环境变量预置状态（非 Avalonia/ui-shot 假窗）
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
new='''        // R043 截图辅助：真实 WinForms，环境变量预置（等 Shown，避免无句柄 BeginInvoke 崩溃）
        var shot = Environment.GetEnvironmentVariable("REMOTE_ASSIST_R043_SHOT");
        if (!string.IsNullOrWhiteSpace(shot))
        {
            Shown += (_, _) => ApplyR043Shot(shot!);
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
if old not in t:
    raise SystemExit('block missing')
p.write_text(t.replace(old,new,1),encoding='utf-8',newline='\n')
print('fixed Shown')
