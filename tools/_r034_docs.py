from pathlib import Path

p = Path("docs/round-034.md")
t = p.read_text(encoding="utf-8")
# Keep ready until B done, or note A client-delivered — R030 kept ready until server then delivered. For A-only, status often stays ready with client section. Looking at R026/R030 client-only push they used delivered for client section while status might stay ready. Round-015 style: update status to note. I'll set status to keep **ready** for B but add client delivery. Or like R030 first client commit set status still ready until accept. Check R030 first client commit... They had client delivery with status still involving both. I'll use: 状态：**A 已交付（待服务端上架）** or keep ready and append delivery.

t = t.replace("> 状态：**ready**", "> 状态：**A 已交付（待服务端上架）**", 1)

delivery = """

## 交付说明（鹊桥-客户端 · 2026-11-06 Asia/Shanghai）

### 实现摘要

- 基于已推送 HEAD `7bf7924`（祖先含 R033 `3b01e7f`，因而也含 R031 `e40e70a` / R032 `b78fc1e`）打出 Windows Inno 与 Linux framework-dependent 包；**未改**业务代码与已验收 InstallInstructions / Skill 常量 / R031–R033 文案 / trust-levels「才问你」。
- Linux 包内已核对：`RemoteAssist.Agent.Avalonia` + `运行说明.txt`；`runtimeconfig.json` 声明 `Microsoft.NETCore.App` 8.0（**FD 非 SC**）；无 `libhostfxr`/coreclr 自包含痕迹。
- **未改** `site/`、`relay/`、未上生产机；公网上架交鹊桥-服务端。未把 macOS 当正式可下。

### 本机产物

| 项 | Windows | Linux |
|------|---------|-------|
| 绝对路径 | `E:\\IdeaProjects\\remote-assist\\artifacts\\installer\\RemoteAssist-Setup-0.1.0.exe` | `E:\\IdeaProjects\\remote-assist\\artifacts\\queqiao-linux-x64-fd.tar.gz` |
| 公网文件名 | `RemoteAssist-Setup-0.1.0.exe` | `queqiao-linux-x64.tar.gz`（**去掉内部 `-fd`**） |
| 字节 | 75,584,964 | 8,002,690 |
| MB | 72.08 | 7.63 |
| mtime | 2026-11-06 02:50:51 Asia/Shanghai | 2026-11-06 02:51:01 Asia/Shanghai |
| SHA256 | `64A3231617963B4D6100036E34FA8A74DAB6FFC6978AE6E5142FE82A8B5EBCD0` | `B20257AD6E7A6CFB4E5048BBD2099A8D0B5906DA96802C5C7BFFC60B8E789108` |
| 构建命令 | `scripts\\build-installer.ps1` | `scripts\\publish-agent-avalonia.ps1 -Runtimes linux-x64` |
| 形态 | Inno Setup（与现官网同形态） | framework-dependent（含 `RemoteAssist.Agent.Avalonia` + `运行说明.txt`） |

出包 HEAD：`7bf7924d012da7c68dba434f23a8e8f785a1eb3a` — `docs(R033): 补回验收文档里被吞掉的截图文件名`（`git merge-base --is-ancestor 3b01e7f HEAD` 为真）。

对照公网 R030：Windows 75,582,164 / `EEA41DA2…C1E5` → 新 75,584,964 / `64A32316…BCD0`；Linux 8,001,712 / `D659BF24…080D` → 新 8,002,690 / `B20257AD…9108` — **字节与 SHA256 均不同**。

公网替换与首页不回退核对：**交鹊桥-服务端（B 节）**。未开下一轮。

### 明确未做

- 未开下一轮；未改 `relay/`、`site/`、AI-SSH-Pro；未上生产机
- 未改安装说明 / Skill 常量 / R031–R033 已验收文案 / trust-levels「才问你」；未改业务功能代码
- 未把 macOS 当正式可下；未打 Linux SC 冒充本轮产物；未联络服务端/产品
"""

if "## 交付说明（鹊桥-客户端" not in t:
    t = t.rstrip() + "\n" + delivery
    p.write_text(t, encoding="utf-8", newline="\n")
    print("round-034 client delivery OK")
else:
    print("delivery already present")

# roadmap
rp = Path("docs/roadmap.md")
rt = rp.read_text(encoding="utf-8")
lines = rt.splitlines(True)
out = []
for line in lines:
    if line.startswith("| R034 |") and "官网安装包" in line:
        line = "| R034 | 高频：官网安装包跟上含 R031–R033 的客户端；打磨：替换后首页旁注与导语不回退 | 鹊桥-客户端 / 鹊桥-服务端 | **A 已交付（待上架）** | `docs/round-034.md` |\n"
    if "R034 ready 跟上 R031–R033" in line:
        line = line.replace("R034 ready 跟上 R031–R033", "R034 A 已出包（待服务端上架）跟上 R031–R033")
    if "下一步 R034：官网安装包跟上；未开再下一轮。" in line:
        line = line.replace(
            "下一步 R034：官网安装包跟上；未开再下一轮。",
            "R034 A 已出包（HEAD `7bf7924`，含祖先 `3b01e7f`）；待服务端上架；未开下一轮。",
        )
    # also handle if wording differs
    if "下一步 R034" in line and "官网安装包" in line:
        line = line.split("下一步 R034")[0] + "R034 A 已出包（HEAD `7bf7924`，含祖先 `3b01e7f`）；待服务端上架；未开下一轮。\n"
    out.append(line)
rp.write_text("".join(out), encoding="utf-8", newline="\n")
print("roadmap updated")
# show R034 lines
for i,l in enumerate(Path("docs/roadmap.md").read_text(encoding="utf-8").splitlines()):
    if "R034" in l and (l.startswith("| R034") or "出包" in l or "ready" in l or "上架" in l):
        print(f"{i}:{l[:160]}")
