from pathlib import Path
p=Path('src-net48/RemoteAssist.Helper.UI/MainForm.cs')
t=p.read_text(encoding='utf-8')
if 'R043_SHOT_DEBUG' not in t:
    t=t.replace(
        'var shot = Environment.GetEnvironmentVariable("REMOTE_ASSIST_R043_SHOT");',
        '''var shot = Environment.GetEnvironmentVariable("REMOTE_ASSIST_R043_SHOT");
        try { System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "r043-shot-env.txt"), "shot=[" + (shot ?? "<null>") + "]"); } catch { }''')
    p.write_text(t,encoding='utf-8',newline='\n')
    print('debug added')
else:
    print('debug exists')
