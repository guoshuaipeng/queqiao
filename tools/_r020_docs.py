from pathlib import Path

delivery = '''
## 交付说明（鹊桥-客户端 · 2026-11-05 Asia/Shanghai）

### 实现摘要（客户端出包）

- 基于当前已验收 HEAD `9217e4c`（含 R015 `e11a8a7`、R016 `f083f51`、R017 `e46b29f`/`5bcab34`、R018 `b8c215c`）用 `scripts/publish-agent-avalonia.ps1 -Runtimes linux-x64` 打出 **framework-dependent** Linux 小包（`--self-contained false`）。
- **未改** `src/` 业务逻辑；未改 R013–R018 已验收原文；未改 `docs/trust-levels.md`「才问你」。
- **未重打** Windows `RemoteAssist-Setup-*.exe`；**未把 macOS 包当正式可下**（本轮仅打 linux-x64）。
- **未改** `site/`、未上生产机；新包本机路径交鹊桥-服务端替换上架。

### 本机产物

| 项 | 值 |
|------|------|
| 绝对路径 | `E:\\IdeaProjects\\remote-assist\\artifacts\\queqiao-linux-x64-fd.tar.gz` |
| 字节 | 7,999,640 |
| MB | 7.63 |
| mtime | 2026-11-05 23:22:18 Asia/Shanghai |
| SHA256 | `EC1C7211E5B1045E7E945756035CCE426C5AB4F7CB102523ECA41BDB9DCE3650` |
| 出包 HEAD | `9217e4c4af582e1b89053d3c12e0320a73beb777` |
| 形态 | framework-dependent（非 self-contained）；约 7.63 MB gzip，远小于自包含体量 |
| 包内 | `鹊桥远程协助/RemoteAssist.Agent.Avalonia`、`.dll`、`.deps.json`、`.runtimeconfig.json`、`运行说明.txt`（脚本既有约定文案） |

对照 R019 旧包 7,993,604 字节 / SHA256 `34CC363BAD28192895FCED74929FDE0A8C858D6E6A489EA40201D6DE552C827D`：**大小与 SHA256 均不同**。

源码抽查仍含 R015 设备列表人话/连接态、R016 五键引导、R017「复制安装说明」、R018「接受协助时不会自动对外开启 MCP」。

### 测试

`dotnet test`：169 通过 / 0 失败（RemoteAssist.sln Release）。

### 明确未做（客户端）

- 未开下一轮；未改 `relay/`、官网 `site/`、AI-SSH-Pro；未上生产机
- 未重做 R015–R018 功能；未改 R013–R018 已验收原文
- 未把 macOS 包当正式产物；未换 Windows 安装包
- 公网替换与首页「目前以 Windows 为主」文案打磨：**交鹊桥-服务端**（客户端只交出包路径）

'''

p = Path("docs/round-020.md")
text = p.read_text(encoding="utf-8")
# Keep wip overall (server not done); mark client package ready in delivery
if "## 交付说明" in text:
    text = text[:text.index("## 交付说明")] + delivery.lstrip()
else:
    text = text.rstrip() + "\n" + delivery
# Note client package ready without claiming full round done (server pending)
if "> 状态：**wip**" in text:
    text = text.replace(
        "> 状态：**wip**",
        "> 状态：**wip**（客户端 Linux 包已打出，待服务端上架 / 首页打磨）",
        1,
    )
p.write_text(text, encoding="utf-8")
print("round-020 updated")

rm = Path("docs/roadmap.md")
rt = rm.read_text(encoding="utf-8")
# Don't mark R020 done — server still needed. Optional note only if there's a natural place.
old = "已开 R020 官网 Linux 包跟上 R015–R018 已验收界面（见 `docs/round-020.md`）。"
new = "R020：客户端已打出含 R015–R018 的 Linux FD 包（见 `docs/round-020.md` 交付说明），待服务端替换上架与首页表述；未开下一轮。"
if old in rt:
    rt = rt.replace(old, new)
    rm.write_text(rt, encoding="utf-8")
    print("roadmap note updated")
else:
    print("roadmap phrase not found, skip")
