/**
 * D0 前端：仅展示布局方向与禁用态。
 * 不调用任何原生能力；不解析凭证；不发起网络。
 */

function bindDisabledGuard(id, reason) {
  const el = document.getElementById(id);
  if (!el) return;
  el.addEventListener("click", (event) => {
    event.preventDefault();
    console.info(`[D0] ${reason}`);
  });
}

bindDisabledGuard("btn-start-accept", "开启接受协助未实现（D0）");
bindDisabledGuard("btn-stop-accept", "停止接受未实现（D0）");
bindDisabledGuard("btn-copy-cred", "复制凭证未实现（D0）");
bindDisabledGuard("btn-connect", "连接未实现（D0）");
bindDisabledGuard("btn-cancel-connect", "取消连接未实现（D0）");

const credInput = document.getElementById("cred-input");
if (credInput) {
  credInput.addEventListener("input", () => {
    // 有输入也不启用连接：真实连接属 D1+；粘贴 ≠ 同意连接。
    const connect = document.getElementById("btn-connect");
    if (connect) connect.disabled = true;
  });
}

console.info("[鹊桥 D0] 空壳已加载：冷启动零监听，未接 relay/pair。");
