from pathlib import Path
import re
t = Path("src/RemoteAssist.Protocol/WriteConfirmCopy.cs").read_text(encoding="utf-8")
# extract InstallInstructions string parts
parts = re.findall(r'"([^"]*)\\n"', t.split("InstallInstructions =")[1].split(";")[0])
# also last line without \n maybe
body = t.split("InstallInstructions =",1)[1].split(";",1)[0]
strs = re.findall(r'"((?:\\.|[^"\\])*)"', body)
text = "\n".join(s.replace("\\n","") for s in strs)  # already had \n in const pieces as literal \n at end... 
# Actually each piece ends with \n except we included \\n in the C# string content
out = []
for s in strs:
    out.append(s.replace("\\n",""))
Path(r"%s\r021-clipboard-text.txt" % Path.home().joinpath("Downloads/r021-shots")).write_text("\n".join(out)+"\n", encoding="utf-8")
hint = re.search(r'InstallCopiedHint = "([^"]+)"', t).group(1)
Path(r"%s\r021-hint.txt" % Path.home().joinpath("Downloads/r021-shots")).write_text(hint+"\n", encoding="utf-8")
print("HINT:", hint)
print("CLIP:")
print("\n".join(out))
# compare entry to r017
import hashlib
def md5(p):
    return hashlib.md5(Path(p).read_bytes()).hexdigest().upper()
e021 = md5("docs/screenshots/r021-install-entry.png")
e017 = md5("docs/screenshots/r017-install-entry.png")
print("entry_same_as_r017", e021==e017, e021)
for name in ["r021-install-entry.png","r021-install-copied.png","r021-clipboard-preview.png"]:
    print(name, md5(f"docs/screenshots/{name}"), Path(f"docs/screenshots/{name}").stat().st_size)
