# 鹊桥 Tauri 客户端 · 启动与迁移说明

工作树：`E:\IdeaProjects\remote-assist-wt-tauri`  
分支：`feat/tauri-client-migration`  
代码：`apps/queqiao-tauri/`

主检出 `E:\IdeaProjects\remote-assist` 仍停在 `master`，不要在那棵树上改 Tauri。`C:\Users\user\project\remote-assist` 有未提交改动，没有纳入这次实现。

## 和旧客户端的关系

- 保留 `src/`、`src-net48/`、`relay/`、`site/`。
- 新客户端自己实现访客 `guest_host` / `guest_join`、业务 TLS 指纹钉扎，以及既有长度前缀 JSON 帧（`pair` / `tool` / `tool_result`）。
- 接受协助由本进程当 Agent。协助端可以连本客户端，也可以连仍在听这些帧的现有 Agent。
- 不把任意命令暴露成界面能直接调用的本机接口。写操作只在被协助端的会话循环里、确认通过之后执行。

## 启动

见 `apps/queqiao-tauri/README.md`。发布安装包和上线不在这次范围内（`bundle.active` 仍为 false）。

## 还没做完的

- 没有和生产中继做一次真实双机协助。自动化测试使用本机回环的中继桩，外加错误指纹不能进入会话、拒绝写操作不会落盘。
- 局域网载荷可以解析；接受协助只主动走设置里的服务器。
- 屏幕、键鼠不在这版界面里。
- 旧 .NET 删除仍要等迁移验收，这次不动。
