async () => {
  const base = {
    phase: "idle",
    view: "home",
    code: "",
    passwordLen: 0,
    leftNote: "",
    helperStatus: "",
    helperError: "",
    copyHint: "",
    manualCopy: "",
    dialog: null,
    trustSaved: "ask",
    trustEffective: "ask",
    relay: "121.196.45.81:17891",
    relayHint: "",
    audit: [],
    result: "",
    past: [],
    opBusy: false,
    opStatus: "",
    peerEnded: false,
    history: [],
    showHistory: false,
    settingsHint: "",
    version: "0.1.0",
    pathOpen: "",
    active: false,
  };
  const shown = (id) => {
    const el = document.getElementById(id);
    const cs = getComputedStyle(el);
    const r = el.getBoundingClientRect();
    return cs.display !== "none" && cs.visibility !== "hidden" && r.width > 0 && r.height > 0;
  };
  const box = (id) => {
    const r = document.getElementById(id).getBoundingClientRect();
    return { x: Math.round(r.x), y: Math.round(r.y), w: Math.round(r.width), h: Math.round(r.height), display: getComputedStyle(document.getElementById(id)).display };
  };
  const pages = ["home", "host", "helper", "settings"];
  const exclusive = () => pages.filter(shown);
  const fail = [];
  const check = (name, cond, detail) => {
    if (!cond) fail.push(name + ": " + detail);
  };
  const render = (patch) => window.queqiaoRender(Object.assign({}, base, patch));

  render({});
  check("idle-pages", exclusive().join() === "home", exclusive().join());
  check("idle-mask", !shown("mask"), "mask");
  check("idle-trust", !shown("trust-full") && !shown("trust-loose"), "trust");
  check("idle-end", !shown("endbar"), "end");
  check("idle-session", !shown("host") && !shown("helper"), "sessions");
  check("idle-connect-disabled", document.getElementById("btn-connect").disabled, "connect");
  const left = box("zone-left");
  const right = box("zone-right");
  check("idle-side-by-side", left.x + left.w <= right.x + 2 && left.w > 200 && right.w > 200, JSON.stringify({ left, right }));
  check("idle-fit", document.getElementById("app").scrollHeight <= document.getElementById("app").clientHeight + 1, "scroll");

  render({ phase: "waiting", code: "AB1 2CD", passwordLen: 8, leftNote: "对方连上时会先弹窗问你，你点「允许协助」才算连上。" });
  check("wait-pages", exclusive().join() === "home", exclusive().join());
  check("wait-code", shown("left-wait") && document.getElementById("session-code").textContent === "AB1 2CD", document.getElementById("session-code").textContent);
  check("wait-mask", !shown("mask") && !shown("host"), "extra");
  check("wait-copy", shown("btn-copy") && !shown("btn-start"), "buttons");

  render({ phase: "wait_allow", helperStatus: "已找到对方，等对方点「允许协助」…" });
  check("allow-pages", exclusive().join() === "home", exclusive().join());
  check("allow-cancel", shown("btn-cancel") && !shown("btn-connect"), "cancel");
  check("allow-no-session", !shown("helper") && !shown("mask"), "session");

  render({
    phase: "waiting",
    dialog: {
      id: 7,
      kind: "pair",
      title: "有人想协助这台电脑",
      body: "对方可以查看进程、日志和文件，不能直接改你的电脑。真正要改动时，还会再问你。",
      detail: "",
      primary: "允许协助",
      secondary: "拒绝",
      seconds: 60,
    },
  });
  await new Promise((r) => setTimeout(r, 50));
  check("pair-mask", shown("mask"), "mask");
  check("pair-title", document.getElementById("dlg-title").textContent === "有人想协助这台电脑", document.getElementById("dlg-title").textContent);
  check("pair-body", document.getElementById("dlg-body").textContent.includes("不能直接改"), document.getElementById("dlg-body").textContent);
  check("pair-focus", document.activeElement === document.getElementById("dlg-no"), document.activeElement && document.activeElement.id);
  check("pair-count", document.getElementById("dlg-yes").textContent.includes("（"), document.getElementById("dlg-yes").textContent);
  document.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape", bubbles: true }));
  check("esc", window.__queqiaoLastResolve && window.__queqiaoLastResolve.allow === false && window.__queqiaoLastResolve.id === 7, JSON.stringify(window.__queqiaoLastResolve));

  render({ view: "host", phase: "host_session", trustEffective: "full", audit: [{ t: "16:32", text: "查看正在运行的程序", kind: "info" }] });
  check("host-pages", exclusive().join() === "host", exclusive().join());
  check("host-full", shown("trust-full") && !shown("trust-loose") && !shown("home") && !shown("settings"), "trust");
  check("host-end", shown("endbar") && !shown("mask"), "end");

  render({ view: "host", phase: "host_session", trustEffective: "relaxed", trustSaved: "relaxed" });
  check("loose", shown("trust-loose") && !shown("trust-full"), "loose");

  render({ view: "helper", phase: "helper_session" });
  check("helper-pages", exclusive().join() === "helper", exclusive().join());
  check("helper-empty", document.getElementById("result").textContent.includes("点上面的操作"), document.getElementById("result").textContent);
  check("helper-no-home", !shown("home") && !shown("trust-full") && !shown("mask"), "extra");

  render({
    view: "host",
    phase: "host_session",
    trustEffective: "ask",
    dialog: {
      id: 9,
      kind: "write",
      title: "对方想在这台电脑执行操作",
      body: "重启打印服务",
      detail: "sc start spooler",
      primary: "允许",
      secondary: "拒绝",
      seconds: 60,
    },
  });
  await new Promise((r) => setTimeout(r, 50));
  check("write-title", document.getElementById("dlg-title").textContent.startsWith("对方想在"), document.getElementById("dlg-title").textContent);
  check("write-body", document.getElementById("dlg-body").textContent === "重启打印服务", document.getElementById("dlg-body").textContent);
  check("write-focus", document.activeElement === document.getElementById("dlg-no"), document.activeElement && document.activeElement.id);
  check("write-detail-hidden", !shown("dlg-detail"), "detail");
  document.getElementById("dlg-detail-toggle").click();
  check("write-detail-open", shown("dlg-detail") && document.getElementById("dlg-detail").textContent.includes("spooler"), document.getElementById("dlg-detail").textContent);

  render({ view: "settings", phase: "idle" });
  check("settings-pages", exclusive().join() === "settings", exclusive().join());
  check("settings-back", shown("btn-back") && !shown("btn-settings") && !shown("foot"), "chrome");
  check("settings-ask", document.querySelector("input[name=trust][value=ask]").checked, "ask");

  render({ view: "helper", phase: "helper_session", peerEnded: true, opStatus: "对方已结束这次协助。" });
  check("ended-title", document.getElementById("helper-title").textContent === "对方已结束这次协助。", document.getElementById("helper-title").textContent);
  check("ended-btn", document.getElementById("btn-end").textContent === "返回首页", document.getElementById("btn-end").textContent);
  check("ended-tools", !shown("helper-tools"), "tools");

  window.__queqiaoLastResolve = null;
  render({
    view: "home",
    phase: "idle",
    dialog: { id: 3, kind: "pair", title: "有人想协助这台电脑", body: "测试倒计时", detail: "", primary: "允许协助", secondary: "拒绝", seconds: 1 },
  });
  await new Promise((r) => setTimeout(r, 1300));
  check("timeout-deny", window.__queqiaoLastResolve && window.__queqiaoLastResolve.allow === false && window.__queqiaoLastResolve.id === 3, JSON.stringify(window.__queqiaoLastResolve));

  render({});
  check("back-idle", exclusive().join() === "home" && !shown("mask") && !shown("host") && !shown("helper") && !shown("settings"), exclusive().join());
  return { fail, viewport: { w: innerWidth, h: innerHeight }, left, right };
}
