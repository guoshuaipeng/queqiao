const $ = (id) => document.getElementById(id);
let state = null;
let revealed = "";
let currentDlg = 0;
let dlgLeft = 0;
let dlgTimer = null;
let selectedTool = "";

function invoke(cmd, args) {
  const api = window.__TAURI__ && window.__TAURI__.core;
  if (!api || !api.invoke) {
    return Promise.reject(new Error("桌面通道不可用，不能假装已连接。"));
  }
  return api.invoke(cmd, args || {});
}

function show(el, on) {
  if (!el) return;
  el.hidden = !on;
}

function render(s) {
  state = s;
  const home = s.view === "home" || (s.view !== "settings" && s.view !== "host" && s.view !== "helper");
  const settings = s.view === "settings";
  const host = s.view === "host";
  const helper = s.view === "helper";
  show($("home"), home);
  show($("settings"), settings);
  show($("host"), host);
  show($("helper"), helper);
  show($("tagline"), home);
  show($("foot"), home);
  show($("btn-settings"), !settings);
  show($("btn-back"), settings);
  show($("settings-title"), settings);
  show($("endbar"), host || helper);

  const waiting = s.phase === "waiting";
  const opening = s.phase === "opening" || s.phase === "stopping";
  const leftBusy = waiting || opening || s.phase === "host_session";
  const rightBusy = s.phase === "connecting" || s.phase === "wait_allow" || s.phase === "helper_session";
  $("zone-left").classList.toggle("active", waiting || s.phase === "host_session");
  $("zone-left").classList.toggle("dim", rightBusy);
  $("zone-right").classList.toggle("active", rightBusy);
  $("zone-right").classList.toggle("dim", waiting || opening || s.phase === "host_session");

  show($("left-idle"), !waiting && s.phase !== "opening" && s.phase !== "stopping" && s.phase !== "open_fail" && s.phase !== "stop_fail");
  show($("left-wait"), waiting);
  $("session-code").textContent = s.code || "";
  if (!revealed) {
    $("session-pwd").textContent = "•".repeat(Math.max(8, s.passwordLen || 0));
    $("btn-reveal").textContent = "显示";
  }
  $("left-note").textContent = waiting ? "" : (s.leftNote || "");
  const startLabel = s.phase === "open_fail" ? "重试开启" : s.phase === "stop_fail" ? "重试停止" : "开启接受协助";
  $("btn-start").textContent = startLabel;
  show($("btn-start"), !waiting && s.phase !== "stopping");
  $("btn-start").disabled = s.phase === "opening" || s.phase === "stopping" || rightBusy;
  show($("btn-copy"), waiting);
  show($("btn-stop"), waiting);
  $("btn-copy").disabled = s.phase === "stopping";
  $("btn-stop").disabled = s.phase === "stopping";
  $("copy-hint").textContent = s.copyHint || "";
  show($("manual-copy"), !!s.manualCopy);
  $("manual-copy").value = s.manualCopy || "";
  show($("btn-history"), (s.history || []).length > 0 && !waiting);
  show($("history"), !!s.showHistory);
  $("history").innerHTML = (s.history || []).map(rowHtml).join("") || "";

  const cred = $("cred").value.trim();
  show($("btn-connect"), !rightBusy);
  $("btn-connect").disabled = !cred || leftBusy || s.phase === "opening";
  show($("btn-cancel"), s.phase === "connecting" || s.phase === "wait_allow");
  $("right-status").hidden = !(s.helperStatus || (s.phase === "connecting" || s.phase === "wait_allow"));
  $("right-status").innerHTML = s.phase === "wait_allow" || (s.helperStatus || "").includes("允许")
    ? '<span class="spin"></span>' + escapeHtml(s.helperStatus || "已找到对方，等对方点「允许协助」…")
    : escapeHtml(s.helperStatus || "");
  $("right-error").textContent = s.helperError || "";
  $("right-hint").textContent = waiting ? "点连接会先停止接受协助（会先问你）。" : "";
  const rightIdle = s.phase === "idle" || s.phase === "helper_fail" || s.phase === "open_fail" || s.phase === "stop_fail";
  show($("right-steps"), rightIdle);
  $("cred").disabled = rightBusy;

  const ended = !!s.peerEnded;
  $("host-title").textContent = ended && host ? "对方已结束这次协助。" : "正在接受协助 · 已连接";
  $("helper-title").textContent = ended && helper ? "对方已结束这次协助。" : "正在协助对方 · 已连接";
  $("host-dot").classList.toggle("gray", ended && host);
  $("helper-dot").classList.toggle("gray", ended && helper);
  $("btn-end").textContent = ended ? "返回首页" : "结束本次协助";
  show($("trust-full"), host && s.trustEffective === "full" && !ended);
  show($("trust-loose"), host && s.trustEffective === "relaxed" && !ended);
  $("audit").innerHTML = (s.audit || []).length
    ? s.audit.map(rowHtml).join("")
    : '<div><span class="t"></span>对方还没有做任何操作</div>';
  show($("helper-tools"), !ended);
  document.querySelectorAll(".ops .btn").forEach((b) => {
    b.disabled = !!s.opBusy || ended;
  });
  $("op-status").textContent = s.opBusy ? "正在执行…" : (s.opStatus || "");
  $("result").textContent = s.result || "点上面的操作，结果会显示在这里";
  $("btn-past").textContent = "▸ 之前的结果" + ((s.past || []).length ? "（" + s.past.length + "）" : "");
  $("past").innerHTML = (s.past || []).map((p) => "<pre class='result'>" + escapeHtml(p) + "</pre>").join("");

  $("relay").value = s.relay || "";
  document.querySelectorAll('input[name="trust"]').forEach((el) => {
    el.checked = el.value === (s.trustEffective === "full" ? "full" : s.trustSaved);
  });
  $("relay-hint").textContent = s.relayHint || "";
  $("settings-hint").textContent = s.settingsHint || "";
  $("version").textContent = "版本 " + (s.version || "");
  renderDialog(s.dialog);
}

function rowHtml(r) {
  const mark = r.kind === "y" ? '<span class="y">✔ </span>' : r.kind === "n" ? '<span class="n">✖ </span>' : "";
  return "<div><span class='t'>" + escapeHtml(r.t || "") + "</span><span>" + mark + escapeHtml(r.text || "") + "</span></div>";
}

function escapeHtml(s) {
  return String(s).replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
}

function renderDialog(d) {
  show($("mask"), !!d);
  if (!d) {
    clearInterval(dlgTimer);
    dlgTimer = null;
    currentDlg = 0;
    return;
  }
  if (currentDlg === d.id) return;
  currentDlg = d.id;
  $("dlg-title").textContent = d.title || "";
  $("dlg-body").textContent = d.body || "";
  const hasDetail = !!d.detail;
  show($("dlg-detail-toggle"), hasDetail);
  $("dlg-detail").textContent = d.detail || "";
  if (!hasDetail) show($("dlg-detail"), false);
  $("dlg-no").textContent = d.secondary || "取消";
  $("dlg-yes").textContent = d.primary || "允许";
  $("dlg-no").classList.add("focus");
  $("dlg-yes").classList.remove("focus");
  dlgLeft = d.seconds || 60;
  paintCount(d);
  clearInterval(dlgTimer);
  dlgTimer = setInterval(() => {
    dlgLeft -= 1;
    paintCount(d);
    if (dlgLeft <= 0) {
      clearInterval(dlgTimer);
      resolve(false);
    }
  }, 1000);
  setTimeout(() => $("dlg-no").focus(), 0);
}

function paintCount(d) {
  if (d.kind === "pair") {
    $("dlg-yes").textContent = (d.primary || "允许协助") + "（" + Math.max(0, dlgLeft) + "）";
  }
}

function resolve(allow) {
  if (!state || !state.dialog) return;
  const id = state.dialog.id;
  window.__queqiaoLastResolve = { id, allow };
  clearInterval(dlgTimer);
  dlgTimer = null;
  invoke("resolve_dialog", { id, allow }).then(render).catch(showErr);
}

function showErr(err) {
  $("banner").hidden = false;
  $("banner").textContent = (err && err.message) || String(err);
}

function bindUi() {
  $("btn-settings").onclick = () => invoke("show_settings", { open: true }).then(render);
  $("btn-back").onclick = () => invoke("show_settings", { open: false }).then(render);
  $("btn-start").onclick = () => invoke("start_accept").then(render).catch(showErr);
  $("btn-stop").onclick = () => invoke("stop_accept").then((s) => { revealed = ""; render(s); }).catch(showErr);
  $("btn-copy").onclick = () => invoke("copy_credential").then(render).catch(showErr);
  $("btn-reveal").onclick = () => {
    if (revealed) {
      revealed = "";
      render(state);
      return;
    }
    invoke("reveal_password").then((pwd) => {
      revealed = pwd;
      $("session-pwd").textContent = pwd;
      $("btn-reveal").textContent = "隐藏";
    }).catch(showErr);
  };
  $("btn-paste").onclick = () => invoke("paste_clipboard").then((text) => {
    $("cred").value = text;
    render(state);
  }).catch(showErr);
  $("cred").addEventListener("input", () => { if (state) render(state); });
  $("btn-connect").onclick = () => invoke("connect", { payload: $("cred").value }).then(render).catch(showErr);
  $("btn-cancel").onclick = () => invoke("cancel_connect").then(render).catch(showErr);
  $("btn-install").onclick = () => invoke("copy_install").then(render).catch(showErr);
  $("btn-end").onclick = () => {
    if (state && state.peerEnded) invoke("return_home").then(render).catch(showErr);
    else invoke("request_end").then(render).catch(showErr);
  };
  $("btn-end-full").onclick = () => invoke("end_full_trust").then(render).catch(showErr);
  $("btn-back-ask").onclick = () => invoke("set_trust", { trust: "ask" }).then(render).catch(showErr);
  document.querySelectorAll(".ops .btn").forEach((b) => {
    b.onclick = () => {
      selectedTool = b.dataset.tool;
      document.querySelectorAll(".ops .btn").forEach((x) => x.classList.toggle("sel", x === b));
      const pathTool = selectedTool === "ListDir" || selectedTool === "ReadLogTail" || selectedTool === "ReadFile";
      show($("path-row"), pathTool);
      show($("propose"), selectedTool === "RunCommand");
      if (selectedTool === "ListProcesses") {
        invoke("run_tool", { name: "ListProcesses", path: "", summary: "", command: "" }).then(render).catch(showErr);
      }
    };
  });
  $("btn-path-go").onclick = () => invoke("run_tool", { name: selectedTool, path: $("path").value, summary: "", command: "" }).then(render).catch(showErr);
  $("btn-propose").onclick = () => invoke("run_tool", {
    name: "RunCommand", path: "", summary: $("summary").value, command: $("command").value
  }).then(render).catch(showErr);
  $("btn-copy-result").onclick = () => invoke("copy_result").then(render).catch(showErr);
  $("btn-past").onclick = () => { $("past").hidden = !$("past").hidden; };
  document.querySelectorAll('input[name="trust"]').forEach((el) => {
    el.onchange = () => { if (el.checked) invoke("set_trust", { trust: el.value }).then(render).catch(showErr); };
  });
  $("btn-save-relay").onclick = () => invoke("save_relay", { relay: $("relay").value }).then(render).catch(showErr);
  $("btn-reset-relay").onclick = () => invoke("reset_relay").then(render).catch(showErr);
  $("btn-mcp").onclick = () => invoke("copy_mcp", { draft: $("cred").value }).then(render).catch(showErr);
  $("btn-usage").onclick = () => invoke("copy_usage").then(render).catch(showErr);
  $("btn-site").onclick = () => invoke("open_site").catch(showErr);
  $("btn-records").onclick = () => invoke("open_history_dir").then(render).catch(showErr);
  $("btn-history").onclick = () => invoke("toggle_history").then(render).catch(showErr);
  $("dlg-no").onclick = () => resolve(false);
  $("dlg-yes").onclick = () => resolve(true);
  $("dlg-detail-toggle").onclick = () => { $("dlg-detail").hidden = !$("dlg-detail").hidden; };
  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && state && state.dialog) {
      e.preventDefault();
      resolve(false);
    }
  });
}

function boot() {
  const api = window.__TAURI__ && window.__TAURI__.core;
  if (!api) {
    $("banner").hidden = false;
    $("banner").textContent = "这是桌面客户端界面。请用鹊桥程序打开，连接状态才来自真实后端。";
    return;
  }
  invoke("get_state").then(render).catch(showErr);
  window.__TAURI__.event.listen("ui", (ev) => render(ev.payload));
}

window.queqiaoRender = render;
bindUi();

// 正式窗口不会走 file:。浏览器截图脚本另放在 test/fixtures.js，不打进安装包。
const fileShot = location.protocol === "file:" ? new URLSearchParams(location.search).get("uitest") : "";
if (!window.__QUEQIAO_SKIP_BOOT__ && !fileShot) boot();
if (fileShot) {
    const loader = document.createElement("script");
    loader.src = "../test/fixtures.js";
    loader.onload = () => {
      const sample = window.QUEQIAO_SHOTS && window.QUEQIAO_SHOTS[fileShot];
      if (sample) render(sample);
    };
    document.body.appendChild(loader);
}
