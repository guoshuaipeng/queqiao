# queqiao-tauri

鹊桥 Windows 桌面客户端（Tauri 2）。界面按 `docs/CLIENT-UI.md` 与 840×480 效果图：一屏两区、等待、配对确认、两侧会话、改动确认、设置。

旧的 `src-net48/` 与 `src/` 仍留在仓库里，本目录不替换它们，也不改 `relay/`。

## 现在能做的

- 冷启动不监听。点「开启接受协助」后才连接设置里的中继，生成 8 位密码和临时业务证书，复制完整 `remoteassist:pair`（含 `fp=`）。
- 协助端先在本地检查凭证。短码、缺安全验证信息不会联网。
- 配对和写操作默认在本机询问。拒绝是默认焦点，60 秒无操作按拒绝。取消或结束会作废这一代回调，迟到的成功不会进入会话。
- 读操作（进程、文件夹、日志、文件）和写操作（提议一项改动）走现有业务帧。写操作只在 Rust 侧确认通过后才执行。
- 每次询问、宽松和服务器地址会保存。完全信任只活在当前这次被协助会话里，结束或重启后回到已保存的档。

## 明确不做

- 不删旧 .NET 客户端。
- 不改中继协议，不实现 R050，不用短码单独连接。
- 不把密码、连接凭证或令牌写入审计。
- 效果图里的会话码、进程列表和改动记录只是示意，程序不会拿来当真实数据。

## 命令

在 `apps/queqiao-tauri`：

```bat
npm install
npm run check
cd src-tauri
cargo test
cd ..
npm run build
```

开发时打开窗口：

```bat
npm run dev
```

构建产物（不打安装包）：

`apps/queqiao-tauri/src-tauri/target/release/queqiao-tauri.exe`

直接双击这个 exe 即可。设置在 `%APPDATA%\queqiao\settings.json`，协助记录在同目录的 `history.json`。

默认服务器是 `121.196.45.81:17891`。本机没有中继时，「开启接受协助」会停在失败文案，不会显示已连接。
