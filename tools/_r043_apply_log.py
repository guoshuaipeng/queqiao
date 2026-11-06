from pathlib import Path
p=Path('src-net48/RemoteAssist.Helper.UI/MainForm.cs')
t=p.read_text(encoding='utf-8')
old='''    private void ApplyR043Shot(string shot)
    {
        shot = shot.Trim().ToLowerInvariant();
        if (shot == "advanced")
        {
            if (!_advancedOpen) ToggleAdvanced();
            return;
        }'''
new='''    private void ApplyR043Shot(string shot)
    {
        shot = shot.Trim().ToLowerInvariant();
        try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "r043-shot-env.txt"), "\\napply=" + shot + " advancedOpen=" + _advancedOpen); } catch { }
        if (shot == "advanced")
        {
            if (!_advancedOpen) ToggleAdvanced();
            try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "r043-shot-env.txt"), " afterToggle=" + _advancedOpen + " h=" + ClientSize.Height); } catch { }
            return;
        }'''
if old not in t: raise SystemExit('apply missing')
p.write_text(t.replace(old,new,1),encoding='utf-8',newline='\n')
print('apply log ok')
