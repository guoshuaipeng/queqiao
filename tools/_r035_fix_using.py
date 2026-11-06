from pathlib import Path
p = Path("src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
t = p.read_text(encoding="utf-8")
if "using Avalonia.Threading;" not in t:
    t = t.replace("using Avalonia.Media;\n", "using Avalonia.Media;\nusing Avalonia.Threading;\n", 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("using added")
else:
    print("using present")
# Also verify AttachClient was applied for ConnectAsync
Select-String -Path src\RemoteAssist.Agent.Avalonia\Views\HelperWindow.cs -Pattern "AttachClient|OnClientConnectionLost|PreparePeerEnded" | ForEach-Object { "$($_.LineNumber):$($_.Line.Trim())" }
Select-String -Path src\RemoteAssist.Helper.UI\MainForm.cs -Pattern "AttachClient|OnClientConnectionLost|PreparePeerEnded|StatusConnectFailed" | ForEach-Object { "$($_.LineNumber):$($_.Line.Trim())" }
