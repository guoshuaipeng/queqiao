from pathlib import Path
p = Path('src-net48/RemoteAssist.Helper.UI/MainForm.cs')
t = p.read_text(encoding='utf-8')
# Find broken ctor tail
bad = '''        LayoutPanels();
        RefreshPreview();
        SetConnectedUi(false);
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
good = '''        LayoutPanels();
        RefreshPreview();
        SetConnectedUi(false);
    }

'''
if bad not in t:
    # show context
    i = t.find('SetConnectedUi(false);')
    print(repr(t[i:i+500]))
    raise SystemExit('bad block not exact')
t = t.replace(bad, good, 1)
# Remove any remaining shot symbols
for s in ['REMOTE_ASSIST_R043_SHOT', 'ApplyR043Shot', 'r043-shot-env']:
    if s in t:
        raise SystemExit('still has '+s)
p.write_text(t, encoding='utf-8', newline='\n')
print('Helper MainForm ctor fixed')
