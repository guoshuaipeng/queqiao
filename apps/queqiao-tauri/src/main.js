const $ = (id) => document.getElementById(id);
let state = null;
let currentDlg = 0;
let dlgLeft = 0;
let dlgTimer = null;
let selectedTool = "";
let accountOpen = false;
let accountMode = "login";
let accountFormError = "";

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
  if (s && typeof s.seq === "number") {
    if (window.__queqiaoSeq > s.seq) return;
    window.__queqiaoSeq = s.seq;
  }
  state = s;
  window.__lastSnap = s;
  const home = s.view === "home" || (s.view !== "settings" && s.view !== "host" && s.view !== "helper");
  const settings = s.view === "settings";
  const host = s.view === "host";
  const helper = s.view === "helper";
  if (host || helper) accountOpen = false;
  const accountPage = accountOpen && home && !settings;
  show($("home"), home && !accountPage);
  show($("account"), accountPage);
  show($("settings"), settings);
  show($("host"), host);
  show($("helper"), helper);
  show($("tagline"), home && !accountPage);
  show($("foot"), home && !accountPage);
  document.body.classList.toggle("view-home", home && !accountPage && !settings);
  document.body.classList.toggle("view-settings", settings);
  document.body.classList.toggle("view-account", accountPage);
  show($("top-right"), !settings && !accountPage);
  show($("btn-settings"), !settings && !accountPage);
  show($("btn-account"), !settings && !accountPage);
  show($("brand-home"), !settings && !accountPage);
  show($("btn-back"), settings || accountPage);
  show($("settings-title"), settings || accountPage);
  if ($("settings-title")) $("settings-title").textContent = accountPage ? "个人中心" : "设置";
  const session = s.phase === "host_session" || s.phase === "helper_session";
  show($("endbar"), host || (settings && session));

  const waiting = s.phase === "waiting";
  const opening = s.phase === "opening" || s.phase === "stopping";
  const leftBusy = waiting || opening || s.phase === "host_session";
  const rightBusy = s.phase === "connecting" || s.phase === "wait_allow" || s.phase === "helper_session";
  $("zone-left").classList.toggle("active", waiting || s.phase === "host_session");
  $("zone-left").classList.toggle("dim", rightBusy);
  $("zone-right").classList.toggle("active", rightBusy);
  $("zone-right").classList.toggle("dim", waiting || opening || s.phase === "host_session");

  show($("left-idle"), s.phase !== "waiting" && s.phase !== "opening" && s.phase !== "stopping");
  show($("left-wait"), waiting);
  $("left-note").textContent = waiting ? "" : (s.leftNote || "");
  const startLabel = s.phase === "opening" ? "正在开启…" : s.phase === "open_fail" ? "重试开启" : s.phase === "stop_fail" ? "重试停止" : "开启接受协助";
  $("btn-start").textContent = startLabel;
  show($("btn-start"), !waiting && s.phase !== "stopping");
  $("btn-start").disabled = s.phase === "opening" || s.phase === "stopping" || rightBusy;
  show($("long-box"), !!s.longOn);
  if ($("long-code")) {
    $("long-code").textContent = s.longCode ? "长久码 " + s.longCode : "正在准备长久码…";
  }
  if ($("long-hint")) {
    $("long-hint").textContent = s.longSkip ? "写操作不再询问，仍会记下。" : "要改电脑时仍会问你。";
  }
  show($("btn-stop"), waiting);
  if ($("btn-copy")) $("btn-copy").disabled = s.phase === "stopping";
  $("btn-stop").disabled = s.phase === "stopping";
  if ($("wait-code")) $("wait-code").textContent = waiting ? (s.code || "…") : "";
  if ($("wait-pass")) {
    const len = Math.max(0, Number(s.passwordLen) || 0);
    const pass = s.password || "";
    const showPass = waiting && !!window.__passReveal && !!pass;
    $("wait-pass").textContent = waiting
      ? (showPass ? pass : "•".repeat(len || 8))
      : "";
    $("wait-pass").classList.toggle("revealed", showPass);
  }
  if ($("btn-pass-eye")) {
    const showPass = waiting && !!window.__passReveal && !!(s.password || "");
    const on = $("btn-pass-eye").querySelector(".eye-on");
    const off = $("btn-pass-eye").querySelector(".eye-off");
    if (on) on.hidden = showPass;
    if (off) off.hidden = !showPass;
    $("btn-pass-eye").setAttribute("aria-label", showPass ? "隐藏密码" : "显示密码");
    $("btn-pass-eye").title = showPass ? "隐藏密码" : "显示密码";
  }
  if (!waiting) window.__passReveal = false;
  $("copy-hint").textContent = s.copyHint || "";
  show($("manual-copy"), !!s.manualCopy && !s.dialog);
  $("manual-copy").value = s.dialog ? "" : (s.manualCopy || "");
  if (s.manualCopy && !s.dialog && s.copyHint === "没能复制，请再点一次。" && window.__manualFocused !== s.manualCopy) {
    window.__manualFocused = s.manualCopy;
    const box = $("manual-copy");
    box.focus();
    box.select();
  }
  if (!s.manualCopy) window.__manualFocused = "";
  show($("btn-history"), (s.history || []).length > 0 && !waiting);
  $("btn-history").textContent = s.showHistory ? "收起" : "查看上次协助记录";
  const history = $("history");
  const historyOpen = !!s.showHistory && !history.hidden;
  watchScroll(history, "__historyStick");
  if (s.showHistory && !historyOpen) window.__historyStick = "end";
  if (!s.showHistory) window.__historyStick = "";
  history.__applying = true;
  history.innerHTML = (s.history || []).map(rowHtml).join("") || "";
  show(history, !!s.showHistory);
  if (s.showHistory) requestAnimationFrame(() => applyScroll(history, "__historyStick"));
  else history.__applying = false;

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
  show($("right-steps"), rightIdle && !s.helperError);
  // 连接失败也保留 MCP 入口：未连接就能装，不应被 helperError 藏掉。
  show($("home-mcp-strip"), !rightBusy && (rightIdle || waiting || opening));
  $("cred").disabled = rightBusy;

  const ended = !!s.peerEnded;
  $("host-title").textContent = ended && host ? "对方已结束这次协助。" : "正在接受协助 · 已连接";
  $("helper-title").textContent = ended && helper ? "对方已结束这次协助。" : "正在协助对方 · 已连接";
  $("host-dot").classList.toggle("gray", ended && host);
  $("helper-dot").classList.toggle("gray", ended && helper);
  const endLabel = ended ? "返回首页" : "结束本次协助";
  $("btn-end").textContent = endLabel;
  if ($("btn-end-helper")) {
    $("btn-end-helper").textContent = endLabel;
    show($("btn-end-helper").closest(".helper-foot") || $("btn-end-helper"), helper);
  }
  show($("host-meta"), host && !ended);
  paintHostDuration(host && !ended ? Number(s.connectedSecs || 0) : 0);
  tickHostDuration(host && !ended);
  document.querySelectorAll('input[name="host-trust"]').forEach((el) => {
    el.disabled = !!ended;
  });
  show($("trust-full"), host && s.trustEffective === "full" && !ended);
  show($("trust-loose"), host && s.trustEffective === "relaxed" && !ended);
  const audit = $("audit");
  watchScroll(audit, "__auditStick");
  if (!host) window.__auditStick = "";
  const nextAudit = (s.audit || []).length;
  if (host && nextAudit > (window.__auditSeen || 0)) window.__auditSeen = nextAudit;
  if (!host) window.__auditSeen = 0;
  audit.__applying = true;
  audit.innerHTML = nextAudit
    ? (host ? auditGroupsHtml(s.audit) : s.audit.map(rowHtml).join(""))
    : '<div><span class="t"></span>对方还没有做任何操作</div>';
  if (host) requestAnimationFrame(() => applyScroll(audit, "__auditStick"));
  else audit.__applying = false;
  show($("helper-tools"), !ended);
  show($("helper-mcp-actions"), helper && !ended);
  document.querySelectorAll(".ops .btn").forEach((b) => {
    b.disabled = !!s.opBusy || ended;
  });
  $("op-status").textContent = s.opBusy ? "正在执行…" : (s.opStatus || "");
  paintResult(s.result || "点左侧的操作，结果会显示在这里");
  if ($("tool-title")) {
    const titles = {
      ListProcesses: "正在运行的程序",
      ListServices: "服务状态",
      ListDir: "文件夹",
      ReadLogTail: "日志",
      ReadFile: "文件",
      RunCommand: "提议一项改动",
    };
    $("tool-title").textContent = titles[selectedTool] || "选择一项操作";
  }
  if (typeof s.formEpoch === "number" && s.formEpoch > (window.__formEpoch || 0)) {
    window.__formEpoch = s.formEpoch;
    $("summary").value = "";
    $("command").value = "";
  }
  const pastCount = (s.past || []).length;
  $("btn-past").textContent = ($("past").hidden ? "▸ " : "▾ ") + "之前的结果" + (pastCount ? "（" + pastCount + "）" : "");
  paintPast(s.past || []);

  if ($("relay") && document.activeElement !== $("relay")) $("relay").value = s.relay || "";
  const trustUi = s.trustEffective === "full"
    ? "full"
    : (s.dialog && s.dialog.kind === "full" ? "full" : s.trustSaved);
  document.querySelectorAll('input[name="trust"], input[name="host-trust"]').forEach((el) => {
    el.checked = el.value === trustUi;
  });
  if ($("relay-hint")) $("relay-hint").textContent = s.relayHint || "";
  const settingsHint = s.settingsHint || "";
  const trustNeedPeer = settingsHint.indexOf("完全信任不会保存") >= 0;
  if ($("trust-hint")) {
    $("trust-hint").textContent = trustNeedPeer ? settingsHint : "";
    show($("trust-hint"), trustNeedPeer);
  }
  if ($("settings-hint")) $("settings-hint").textContent = trustNeedPeer ? "" : settingsHint;
  const mcpHint = !trustNeedPeer && /MCP|使用说明|没能复制|装到 Cursor|缺 MCP|再改路径/.test(settingsHint) ? settingsHint : "";
  if ($("helper-mcp-hint")) $("helper-mcp-hint").textContent = helper && !ended ? mcpHint : "";
  const homeMcpStrip = $("home-mcp-strip");
  const homeMcpSub = $("home-mcp-sub");
  const homeMcpBtn = $("btn-mcp-home");
  if (homeMcpStrip && homeMcpSub && homeMcpBtn) {
    const stripOn = home && !accountPage && !rightBusy && (rightIdle || waiting || opening);
    const homeMcpMsg = stripOn ? mcpHint : "";
    const homeMcpWarn = /缺 MCP|没能复制|没有 RemoteAssist|发布/.test(homeMcpMsg);
    const homeMcpOk = !!homeMcpMsg && !homeMcpWarn;
    homeMcpStrip.classList.toggle("is-ok", homeMcpOk);
    homeMcpStrip.classList.toggle("is-warn", homeMcpWarn);
    // 副文案固定说明，不拿成功/失败长句占位，避免挤到「连接」。
    homeMcpSub.textContent = "未连接也能装；装好后把短码发给 AI";
    homeMcpBtn.disabled = false;
    homeMcpBtn.textContent = homeMcpOk ? "已复制" : "复制 MCP 配置";
    homeMcpBtn.title = homeMcpOk ? "再点可再次复制" : "";
  }
  if ($("version")) $("version").textContent = "版本 " + (s.version || "");
  if ($("auto-update")) $("auto-update").checked = !!s.autoUpdate;
  if ($("update-hint")) $("update-hint").textContent = s.updateHint || "";
  if ($("btn-check-update")) $("btn-check-update").disabled = !!s.updateBusy;
  if ($("btn-apply-update")) {
    show($("btn-apply-update"), !!s.updateAvailable);
    $("btn-apply-update").disabled = !!s.updateBusy || !s.updateAvailable;
    $("btn-apply-update").textContent = s.updateBusy
      ? "正在更新…"
      : (s.updateVersion ? "更新到 " + s.updateVersion : "立即更新");
  }
  const logged = !!(s.accountUser);
  const signup = accountMode === "signup";
  const mark = logged ? s.accountUser.slice(0, 1).toUpperCase() : "";
  if ($("acct-chip-label")) $("acct-chip-label").textContent = logged ? "个人中心" : "登录";
  if ($("acct-mark")) {
    show($("acct-mark"), logged);
    $("acct-mark").textContent = mark;
    $("acct-mark").classList.toggle("on", !!s.accountOnline);
    $("acct-mark").classList.toggle("wait", !!s.accountAllow && !s.accountOnline);
  }
  show($("acct-guest"), !logged);
  show($("acct-in"), logged);
  if ($("btn-mode-login")) $("btn-mode-login").classList.toggle("on", !signup);
  if ($("btn-mode-signup")) $("btn-mode-signup").classList.toggle("on", signup);
  if ($("acct-lead")) {
    $("acct-lead").textContent = signup
      ? "注册后直接登录。只能连你自己允许过的电脑。"
      : "自己的电脑之间可以免密连接。发给亲友，仍然不用登录。";
  }
  show($("acct-pass2-field"), signup);
  show($("acct-rule"), signup);
  if ($("acct-pass")) $("acct-pass").autocomplete = signup ? "new-password" : "current-password";
  if ($("acct-hello")) $("acct-hello").textContent = s.accountUser || "";
  if ($("acct-avatar")) {
    $("acct-avatar").textContent = mark;
    $("acct-avatar").classList.toggle("on", !!s.accountOnline);
    $("acct-avatar").classList.toggle("wait", !!s.accountAllow && !s.accountOnline);
  }
  if ($("acct-error")) $("acct-error").textContent = s.accountError || accountFormError || "";
  if ($("btn-acct-submit")) $("btn-acct-submit").textContent = signup ? "注册并登录" : "登录";
  if ($("device-name") && document.activeElement !== $("device-name")) $("device-name").value = s.deviceName || "";
  if ($("btn-allow")) {
    $("btn-allow").classList.toggle("on", !!s.accountAllow);
    $("btn-allow").setAttribute("aria-pressed", s.accountAllow ? "true" : "false");
    $("btn-allow").title = s.accountAllow ? "停止允许" : "允许连接";
  }
  if ($("allow-dot")) {
    $("allow-dot").classList.toggle("gray", !s.accountOnline);
    $("allow-dot").classList.toggle("wait", !!s.accountAllow && !s.accountOnline);
  }
  if ($("allow-state")) {
    $("allow-state").textContent = s.accountOnline
      ? "这个账号现在可以连上这台电脑。"
      : s.accountAllow
        ? "正在连接服务器…"
        : "还没有允许。允许后，同一账号不用会话码就能连上，改动前仍会问你。";
  }
  if ($("acct-skip")) $("acct-skip").checked = !!s.accountSkip;
  if ($("acct-autostart")) {
    $("acct-autostart").checked = !!s.accountAutostart;
    $("acct-autostart").disabled = !s.accountUnattended;
  }
  show($("btn-acct-unattended"), !s.accountUnattended);
  show($("btn-acct-unattended-off"), !!s.accountUnattended);
  if ($("btn-acct-unattended")) $("btn-acct-unattended").disabled = !s.accountAllow;
  if ($("acct-unattended-state")) {
    $("acct-unattended-state").textContent = !s.accountAllow
      ? "先允许这台电脑。"
      : s.accountUnattended
        ? (s.accountSkip ? "已开启。人不在时，改动不再询问。" : "已开启。人不在时也能连上，改动仍会询问。")
        : "未开启。";
  }
  const devices = $("devices");
  if (devices) {
    const rows = s.devices || [];
    const remote = rows.filter((d) => !d.selfDevice);
    const unattendedCount = remote.filter((d) => d.unattended).length;
    if ($("device-count")) {
      $("device-count").textContent = rows.length
        ? (unattendedCount ? "(" + rows.length + " · 无人值守 " + unattendedCount + ")" : "(" + rows.length + ")")
        : "";
    }
    devices.innerHTML = rows.length
      ? rows.map((d) => {
          const name = escapeHtml(d.name || "未命名");
          const online = !!d.online;
          const mine = !!d.selfDevice;
          const tags = [];
          if (d.unattended) tags.push("无人值守");
          tags.push(online ? "在线" : "离线");
          const title = name + (mine ? '<span class="pill">本机</span>' : "");
          const action = !mine && online
            ? '<button type="button" class="btn primary sm" data-dev="' + escapeHtml(d.id) + '">连接</button>'
            : mine
              ? '<span class="hint">当前这台</span>'
              : '<button type="button" class="btn sm" disabled>连接</button>';
          return '<div class="device' + (online ? "" : " off") + '"><span class="dot' + (online ? "" : " gray") + '"></span><span class="name">' + title + "<small>" + tags.join(" · ") + "</small></span>" + action + "</div>";
        }).join("")
      : '<p class="hint">还没有电脑加入。在这台或另一台电脑登录后，打开「允许我的账号连接这台电脑」。</p>';
  }
  if ($("host-lead")) {
    $("host-lead").textContent = s.hostBanner || "对方已连接。请核对对方身份，不要随便信任陌生人。";
  }
  renderDialog(s.dialog);
}

function pinnedToEnd(box) {
  const max = box.scrollHeight - box.clientHeight;
  return max <= 1 || max - box.scrollTop < 8;
}

function watchScroll(box, key) {
  if (box.__scrollWatch) return;
  box.__scrollWatch = true;
  if (window[key] == null) window[key] = "end";
  box.addEventListener("scroll", () => {
    if (box.__applying) return;
    window[key] = pinnedToEnd(box) ? "end" : "place";
    window[key + "Top"] = box.scrollTop;
  });
}

function applyScroll(box, key) {
  box.__applying = true;
  if ((window[key] || "end") === "end") snapEnd(box);
  else box.scrollTop = window[key + "Top"] || 0;
  box.__applying = false;
}

function rowStep(box) {
  const row = box.querySelector(":scope > div");
  if (!row) return 0;
  const raw = getComputedStyle(box).rowGap;
  const gap = raw && raw.endsWith("px") ? parseFloat(raw) : 0;
  return row.getBoundingClientRect().height + gap;
}

function snapEnd(box) {
  const max = Math.max(0, box.scrollHeight - box.clientHeight);
  const step = rowStep(box);
  box.scrollTop = step > 1 ? Math.min(max, Math.round(max / step) * step) : max;
}

function snapNearest(box) {
  const max = Math.max(0, box.scrollHeight - box.clientHeight);
  const step = rowStep(box);
  if (step <= 1) return;
  const x = box.__rowTail || box.scrollLeft;
  const keep = () => {
    if ((x || 0) > 1 && Math.abs(box.scrollLeft - x) > 1) box.scrollLeft = x;
    shiftShortRows(box);
  };
  if (max - box.scrollTop < step) {
    if (Math.abs(box.scrollTop - max) >= 1) box.scrollTop = max;
    keep();
    return;
  }
  let snapped = Math.round(box.scrollTop / step) * step;
  if (snapped > max) snapped = Math.floor(box.scrollTop / step) * step;
  snapped = Math.max(0, Math.min(max, snapped));
  if (Math.abs(snapped - box.scrollTop) >= 1) box.scrollTop = snapped;
  keep();
}

function placeRail(box) {
  if (!box) return;
  const hold = box.closest(".box-hold");
  const rail = hold && hold.querySelector(":scope > .rail");
  if (!rail) return;
  const barX = Math.max(0, box.offsetWidth - box.clientWidth);
  const barY = Math.max(0, box.offsetHeight - box.clientHeight);
  rail.style.right = (barX + 1) + "px";
  rail.style.bottom = (barY + 1) + "px";
}

function pinShortRows(box) {
  if (!box) return;
  const limit = box.clientWidth;
  const x = box.scrollLeft;
  for (const row of box.children) {
    const text = row.firstChild;
    let wide = false;
    if (text && text.nodeType === Node.TEXT_NODE && text.length) {
      const range = document.createRange();
      range.selectNodeContents(text);
      wide = range.getBoundingClientRect().width > limit - 24;
    }
    row.classList.toggle("stick-x", !wide);
    row.style.transform = wide ? "" : "translateX(" + x + "px)";
  }
}

function slideBy(box, delta) {
  const y = box.scrollTop;
  const max = Math.max(0, box.scrollWidth - box.clientWidth);
  const next = box.scrollLeft + delta;
  if (next >= max - 1) box.scrollLeft = box.scrollWidth;
  else box.scrollLeft = Math.max(0, next);
  if (box.scrollTop !== y) box.scrollTop = y;
  box.__keepY = y;
  box.__rowTail = box.scrollLeft;
  shiftShortRows(box);
  requestAnimationFrame(() => {
    if (Math.abs(box.scrollTop - y) > 1) box.scrollTop = y;
    box.__keepY = null;
  });
}

function shiftShortRows(box) {
  const x = box.scrollLeft;
  for (const row of box.children) {
    if (!row.classList.contains("stick-x")) continue;
    row.style.transform = "translateX(" + x + "px)";
  }
}

function watchShortPin(box) {
  if (!box || box.__pin) return;
  box.__pin = true;
  box.__rowTail = box.scrollLeft || 0;
  box.__prevX = box.scrollLeft || 0;
  box.__prevY = box.scrollTop || 0;
  box.addEventListener("scroll", () => {
    if (box.__restoring) return;
    const x = box.scrollLeft;
    const y = box.scrollTop;
    const movedX = Math.abs(x - box.__prevX) > 1;
    const movedY = Math.abs(y - box.__prevY) > 1;
    if (movedX && !movedY) box.__rowTail = x;
    if (movedY && (box.__rowTail || 0) > 1 && Math.abs(x - box.__rowTail) > 1) {
      box.__restoring = true;
      box.scrollLeft = box.__rowTail;
      box.__restoring = false;
    }
    box.__prevX = box.scrollLeft;
    box.__prevY = box.scrollTop;
    shiftShortRows(box);
  });
}

function watchResultWheel(result) {
  if (!result || result.__wheel) return;
  result.__wheel = true;
  result.addEventListener("scrollend", () => {
    if (result.__keepY != null) {
      if (Math.abs(result.scrollTop - result.__keepY) > 1) result.scrollTop = result.__keepY;
      result.__keepY = null;
      return;
    }
    snapNearest(result);
  });
  result.addEventListener("wheel", (event) => {
    const maxX = result.scrollWidth - result.clientWidth;
    const maxY = result.scrollHeight - result.clientHeight;
    if (Math.abs(event.deltaX) > Math.abs(event.deltaY) && maxX > 1) {
      slideBy(result, event.deltaX);
      event.preventDefault();
      return;
    }
    if (maxY > 1) {
      result.scrollTop = Math.min(maxY, Math.max(0, result.scrollTop + event.deltaY));
    }
    if ((result.__rowTail || 0) > 1 && Math.abs(result.scrollLeft - result.__rowTail) > 1) {
      result.scrollLeft = result.__rowTail;
    }
    shiftShortRows(result);
    event.preventDefault();
  }, { passive: false });
}

function watchPastSnap(past) {
  if (!past || past.__snap) return;
  past.__snap = true;
  past.addEventListener("scrollend", () => {
    if (past.__keepY != null) {
      if (Math.abs(past.scrollTop - past.__keepY) > 1) past.scrollTop = past.__keepY;
      past.__keepY = null;
      return;
    }
    snapNearest(past);
  });
  past.addEventListener("wheel", (event) => {
    const maxX = past.scrollWidth - past.clientWidth;
    const maxY = past.scrollHeight - past.clientHeight;
    if (Math.abs(event.deltaX) > Math.abs(event.deltaY) && maxX > 1) {
      slideBy(past, event.deltaX);
      event.preventDefault();
      return;
    }
    if (maxY > 1) {
      past.scrollTop = Math.min(maxY, Math.max(0, past.scrollTop + event.deltaY));
    }
    if ((past.__rowTail || 0) > 1 && Math.abs(past.scrollLeft - past.__rowTail) > 1) {
      past.scrollLeft = past.__rowTail;
    }
    shiftShortRows(past);
    event.preventDefault();
  }, { passive: false });
}

function rowBox(text) {
  const lines = String(text || "").replace(/(?:\r?\n)+$/, "").split(/\r?\n/);
  return lines.length ? lines : [];
}

function paintPast(items) {
  const past = $("past");
  const lines = rowBox(items && items[0]);
  past.replaceChildren();
  for (const line of lines) {
    const row = document.createElement("div");
    row.textContent = line.length ? line : " ";
    past.appendChild(row);
  }
  watchPastSnap(past);
  watchShortPin(past);
  pinShortRows(past);
}

function fitSpan() {
  const scroller = $("helper-scroll");
  const result = $("result");
  const section = result && result.closest("section");
  if (!scroller || !result || !section || section.hidden) {
    document.body.classList.remove("tight");
    return;
  }
  const past = $("past") && !$("past").hidden && $("past").childElementCount ? $("past") : null;
  const propose = $("propose");
  const tight = window.innerHeight <= 400;
  document.body.classList.toggle("tight", tight);
  scroller.style.gap = "";
  scroller.style.paddingTop = "";
  scroller.style.paddingBottom = "";
  if (propose) propose.style.gap = "";

  const settle = (box, stickEnd) => {
    if (!box) return;
    const maxX = Math.max(0, box.scrollWidth - box.clientWidth);
    const tail = box.__rowTail || 0;
    if (tail > 1 && maxX - tail <= 48 && maxX - tail >= -1) {
      box.scrollLeft = maxX;
      box.__rowTail = box.scrollLeft;
    } else if (tail > 1) {
      box.scrollLeft = Math.min(tail, maxX);
    }
    if (stickEnd) snapEnd(box);
    else snapNearest(box);
    pinShortRows(box);
  };

  if (section.classList.contains("helper-console")) {
    const hold = result.closest(".box-hold") || result;
    const view = scroller.getBoundingClientRect();
    const holdTop = hold.getBoundingClientRect().top;
    const pastRows = tight ? 2 : 4;
    if (past) {
      const widePast = past.scrollWidth > past.clientWidth + 1;
      past.style.maxHeight = (pastRows * 20 + 2 + (widePast ? 12 : 0)) + "px";
    }
    const pastBlock = past ? (past.closest(".box-hold") || past) : null;
    const pastH = pastBlock ? Math.max(pastBlock.getBoundingClientRect().height, pastRows * 20 + 18) : 0;
    const available = Math.max(tight ? 48 : 120, Math.floor(view.bottom - holdTop - pastH - 8));
    const wide = result.scrollWidth > result.clientWidth + 1;
    result.style.maxHeight = (available - (wide ? 0 : 0)) + "px";
    if (past && past.__openEnd) {
      snapEnd(past);
      past.__openEnd = false;
    } else if (past) snapNearest(past);
    placeRail(result);
    pinShortRows(result);
    if (past) {
      placeRail(past);
      pinShortRows(past);
    }
    settle(result, (window.__resultStick || "end") === "end");
    if (past) settle(past, false);
    placeRail(result);
    if (past) placeRail(past);
    scroller.scrollTop = 0;
    scroller.style.overflowY = "hidden";
    return;
  }

  const summary = $("propose") && !$("propose").hidden ? $("summary") : null;
  const bar = $("ops-bar");
  const barInScroll = bar && scroller.contains(bar);
  const barH = barInScroll ? bar.getBoundingClientRect().height : 0;
  const topEl = summary || result;
  const bottomEl = past || result;
  if (tight) {
    scroller.style.gap = "0px";
    scroller.style.paddingTop = "0px";
    scroller.style.paddingBottom = "0px";
    if (barInScroll) bar.style.paddingBottom = "0px";
    if (propose && !propose.hidden) propose.style.gap = "0px";
  } else if (past) {
    scroller.style.gap = "4px";
    scroller.style.paddingTop = "0px";
    scroller.style.paddingBottom = "16px";
    if (barInScroll) bar.style.paddingBottom = "0px";
    if (propose && !propose.hidden) propose.style.gap = "4px";
  } else if (barInScroll) {
    bar.style.paddingBottom = "";
  }
  const rowHeight = (n) => n * 20 + 2 + 16;
  let resultRows = 8;
  let pastRows = 4;
  const apply = () => {
    result.style.maxHeight = rowHeight(resultRows) + "px";
    if (past) past.style.maxHeight = rowHeight(pastRows) + "px";
  };
  const span = () => {
    const top = topEl.getBoundingClientRect();
    const bottom = bottomEl.getBoundingClientRect();
    return { top: top.top, bottom: bottom.bottom };
  };
  apply();
  const view = scroller.getBoundingClientRect();
  const viewTop = view.top + barH;
  const viewBottom = view.bottom - (past ? 16 : 0);
  const room = viewBottom - viewTop;
  const resultMin = tight ? 1 : 2;
  const pastMin = tight ? 1 : 2;
  let guard = 0;
  while (guard < 12 && span().bottom - span().top > room - 8) {
    if (resultRows > resultMin) resultRows -= 1;
    else if (past && pastRows > pastMin) pastRows -= 1;
    else break;
    apply();
    guard += 1;
  }
  let box = span();
  if (box.top < viewTop - 1) scroller.scrollTop -= viewTop - box.top;
  box = span();
  if (box.bottom > viewBottom - 1) scroller.scrollTop += box.bottom - viewBottom;
  box = span();
  if (box.top < viewTop - 1) scroller.scrollTop -= viewTop - box.top;
  if (past && past.__openEnd) {
    snapEnd(past);
    past.__openEnd = false;
  } else if (past) snapNearest(past);
  placeRail(result);
  pinShortRows(result);
  if (past) {
    placeRail(past);
    pinShortRows(past);
  }
  const fitRows = (el, rows) => {
    el.style.maxHeight = (rows * 20 + 2) + "px";
    const wide = el.scrollWidth > el.clientWidth + 1;
    el.style.maxHeight = (rows * 20 + 2 + (wide ? 12 : 0)) + "px";
  };
  fitRows(result, resultRows);
  if (past) fitRows(past, pastRows);
  settle(result, (window.__resultStick || "end") === "end");
  if (past) settle(past, false);
  placeRail(result);
  if (past) placeRail(past);
  const extra = scroller.scrollHeight - scroller.clientHeight;
  if (extra <= 4) scroller.scrollTop = 0;
  scroller.style.overflowY = extra <= 4 ? "hidden" : "auto";
}

window.addEventListener("resize", () => {
  const helper = $("helper");
  if (!helper || helper.hidden) return;
  revealResult(true);
});

function revealResult(sync) {
  const run = () => {
    const scroller = $("helper-scroll");
    const box = $("result");
    const section = box && box.closest("section");
    if (!scroller || !box || !section || section.hidden) return;
    const view = scroller.getBoundingClientRect();
    const rect = box.getBoundingClientRect();
    const pastOpen = $("past") && !$("past").hidden;
    const place = (window.__resultStick || "end") !== "end";
    const outside = rect.bottom <= view.top + 8 || rect.top >= view.bottom - 8;
    if (place && outside && !pastOpen) return;
    if (rect.height < 8 || view.height < 8) return;
    fitSpan();
  };
  if (sync) run();
  else requestAnimationFrame(run);
}

function paintResult(text) {
  const result = $("result");
  watchScroll(result, "__resultStick");
  watchResultWheel(result);
  if (!result.__applying) {
    window.__resultStick = pinnedToEnd(result) ? "end" : "place";
    window.__resultStickTop = result.scrollTop;
  }
  const follow = (window.__resultStick || "end") === "end";
  const keptTop = window.__resultStickTop || 0;
  result.__applying = true;
  const lines = String(text).replace(/(?:\r?\n)+$/, "").split(/\r?\n/);
  result.replaceChildren();
  for (const line of lines) {
    const row = document.createElement("div");
    row.textContent = line.length ? line : " ";
    result.appendChild(row);
  }
  watchShortPin(result);
  pinShortRows(result);
  requestAnimationFrame(() => {
    window.__resultStick = follow ? "end" : "place";
    window.__resultStickTop = keptTop;
    applyScroll(result, "__resultStick");
    revealResult(true);
  });
}

function rowHtml(r) {
  const mark = r.kind === "y" ? '<span class="y">✔ </span>' : r.kind === "n" ? '<span class="n">✖ </span>' : "";
  return "<div><span class='t'>" + escapeHtml(r.t || "") + "</span><span>" + mark + escapeHtml(r.text || "") + "</span></div>";
}

function isWriteAudit(r) {
  return r && (r.kind === "y" || r.kind === "n");
}

function auditGroupsHtml(rows) {
  const views = [];
  const writes = [];
  (rows || []).forEach((r) => {
    if (isWriteAudit(r)) writes.push(r);
    else views.push(r);
  });
  let html = "";
  if (views.length) {
    html += '<div class="log-group"><div class="log-group-label">查看</div>' + views.map(rowHtml).join("") + "</div>";
  }
  if (writes.length) {
    html += '<div class="log-group"><div class="log-group-label write">改动</div>' + writes.map(rowHtml).join("") + "</div>";
  }
  return html || '<div><span class="t"></span>对方还没有做任何操作</div>';
}

function formatConnected(secs) {
  const n = Math.max(0, Math.floor(Number(secs) || 0));
  const h = Math.floor(n / 3600);
  const m = Math.floor((n % 3600) / 60);
  const s = n % 60;
  if (h > 0) {
    return "已连接 " + String(h).padStart(2, "0") + ":" + String(m).padStart(2, "0") + ":" + String(s).padStart(2, "0");
  }
  return "已连接 " + String(m).padStart(2, "0") + ":" + String(s).padStart(2, "0");
}

function paintHostDuration(secs) {
  if (!$("host-duration")) return;
  window.__hostConnectedBase = Number(secs) || 0;
  window.__hostConnectedAt = Date.now();
  $("host-duration").textContent = formatConnected(window.__hostConnectedBase);
}

function tickHostDuration(on) {
  if (!on) {
    if (window.__hostDurationTimer) {
      clearInterval(window.__hostDurationTimer);
      window.__hostDurationTimer = null;
    }
    return;
  }
  if (window.__hostDurationTimer) return;
  window.__hostDurationTimer = setInterval(() => {
    if (!$("host") || $("host").hidden || !$("host-duration")) return;
    const base = Number(window.__hostConnectedBase) || 0;
    const started = Number(window.__hostConnectedAt) || Date.now();
    const secs = base + Math.floor((Date.now() - started) / 1000);
    $("host-duration").textContent = formatConnected(secs);
  }, 1000);
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
  const detail = $("dlg-detail");
  show($("dlg-detail-toggle"), false);
  detail.textContent = d.detail || "";
  show(detail, hasDetail);
  if (hasDetail) requestAnimationFrame(() => { detail.scrollLeft = detail.scrollWidth; });
  $("dlg-no").textContent = d.secondary || "取消";
  $("dlg-yes").textContent = d.primary || "允许";
  $("dlg-no").classList.add("focus");
  $("dlg-yes").classList.remove("focus");
  dlgLeft = Number.isFinite(d.seconds) ? d.seconds : 60;
  paintCount(d);
  clearInterval(dlgTimer);
  dlgTimer = null;
  if (dlgLeft > 0 && (d.kind === "pair" || d.kind === "write")) {
    dlgTimer = setInterval(() => {
      dlgLeft -= 1;
      paintCount(d);
      if (dlgLeft <= 0) clearInterval(dlgTimer);
    }, 1000);
  }
  setTimeout(() => $("dlg-no").focus(), 0);
}

function paintCount(d) {
  if (d.kind === "pair" || d.kind === "write") {
    const base = d.secondary || "取消";
    $("dlg-no").textContent = base + "（" + Math.max(0, dlgLeft) + "）";
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
  $("btn-settings").onclick = () => {
    accountOpen = false;
    invoke("show_settings", { open: true }).then(render);
  };
  $("btn-back").onclick = () => {
    if (accountOpen) {
      accountOpen = false;
      if (state) render(state);
      return;
    }
    invoke("show_settings", { open: false }).then(render);
  };
  $("btn-start").onclick = () => invoke("start_accept").then(render).catch(showErr);
  if ($("btn-account")) {
    $("btn-account").onclick = () => {
      if (state && (state.view === "host" || state.view === "helper")) return;
      const openAccount = (next) => {
        accountOpen = true;
        render(next || state);
      };
      if (state && state.view === "settings") {
        invoke("show_settings", { open: false }).then(openAccount).catch(showErr);
      } else if (state) {
        openAccount(state);
      }
    };
  }
  const setAccountMode = (mode) => {
    accountMode = mode;
    accountFormError = "";
    if (state) render(state);
  };
  if ($("btn-mode-login")) $("btn-mode-login").onclick = () => setAccountMode("login");
  if ($("btn-mode-signup")) $("btn-mode-signup").onclick = () => setAccountMode("signup");
  if ($("acct-form")) {
    $("acct-form").onsubmit = (event) => {
      event.preventDefault();
      const username = $("acct-user").value;
      const password = $("acct-pass").value;
      const cmd = accountMode === "signup" ? "account_signup" : "account_login";
      const args = accountMode === "signup"
        ? { username, password, confirm: $("acct-pass2").value }
        : { username, password };
      accountFormError = "";
      $("banner").hidden = true;
      if ($("acct-error")) $("acct-error").textContent = "";
      invoke(cmd, args).then((next) => {
        $("acct-pass").value = "";
        $("acct-pass2").value = "";
        accountFormError = "";
        $("banner").hidden = true;
        $("banner").textContent = "";
        render(next);
      }).catch((err) => {
        accountFormError = (err && err.message) || String(err);
        if ($("acct-error")) $("acct-error").textContent = accountFormError;
      });
    };
  }
  if ($("btn-acct-out")) {
    $("btn-acct-out").onclick = () => invoke("account_logout").then(render).catch(showErr);
  }
  if ($("btn-allow")) {
    $("btn-allow").onclick = () => invoke("ask_account_allow").then(render).catch(showErr);
  }
  if ($("btn-device-name")) {
    $("btn-device-name").onclick = () => invoke("set_device_name", { name: $("device-name").value }).then(render).catch(showErr);
  }
  if ($("btn-remove-device")) {
    $("btn-remove-device").onclick = () => invoke("ask_account_remove").then(render).catch(showErr);
  }
  if ($("btn-refresh-devices")) {
    $("btn-refresh-devices").onclick = () => invoke("refresh_devices").then(render).catch(showErr);
  }
  if ($("devices")) {
    $("devices").onclick = (event) => {
      const button = event.target.closest("button[data-dev]");
      if (!button) return;
      accountOpen = false;
      invoke("connect_account", { deviceId: button.getAttribute("data-dev") }).then(render).catch(showErr);
    };
  }
  if ($("btn-acct-unattended")) {
    $("btn-acct-unattended").onclick = () => invoke("ask_account_unattended", { skipWrite: !!($("acct-skip") && $("acct-skip").checked) }).then(render).catch(showErr);
  }
  if ($("btn-acct-unattended-off")) {
    $("btn-acct-unattended-off").onclick = () => invoke("stop_account_unattended").then(render).catch(showErr);
  }
  if ($("acct-autostart")) {
    $("acct-autostart").onchange = () => invoke("set_account_autostart", { on: $("acct-autostart").checked }).then(render).catch(showErr);
  }
  if ($("btn-long-on")) {
    $("btn-long-on").onclick = () => invoke("ask_unattended", { skipWrite: !!($("long-skip") && $("long-skip").checked) }).then(render).catch(showErr);
  }
  if ($("btn-long-off")) {
    $("btn-long-off").onclick = () => invoke("ask_revoke_unattended").then(render).catch(showErr);
  }
  if ($("btn-long-copy")) {
    $("btn-long-copy").onclick = () => invoke("copy_long_credential").then(render).catch(showErr);
  }
  $("btn-stop").onclick = () => invoke("stop_accept").then(render).catch(showErr);
  if ($("btn-pass-eye")) {
    $("btn-pass-eye").onclick = () => {
      window.__passReveal = !window.__passReveal;
      if (window.__lastSnap) render(window.__lastSnap);
    };
  }
  $("btn-copy").onclick = async () => {
    try {
      const snap = await invoke("copy_credential");
      render(snap);
      // 原生已成功则不会带 manualCopy。失败时再试浏览器剪贴板，仍以读回为准。
      if (snap && snap.manualCopy && snap.copyHint === "没能复制，请再点一次。" && navigator.clipboard && navigator.clipboard.writeText) {
        try {
          await navigator.clipboard.writeText(snap.manualCopy);
          const checked = await invoke("confirm_copy");
          // confirm_copy 读回不一致时仍是失败文案，不得在前端改成成功
          render(checked);
        } catch (err) {
          const box = $("manual-copy");
          if (box) { box.focus(); box.select(); }
        }
      }
    } catch (err) { showErr(err); }
  };
  $("btn-paste").onclick = () => invoke("paste_clipboard").then((text) => {
    $("cred").value = text;
    render(state);
  }).catch(showErr);
  $("cred").addEventListener("input", () => { if (state) render(state); });
  $("btn-connect").onclick = () => invoke("connect", { payload: $("cred").value }).then(render).catch(showErr);
  $("btn-cancel").onclick = () => invoke("cancel_connect").then(render).catch(showErr);
  $("btn-install").onclick = () => invoke("copy_install").then(render).catch(showErr);
  const endSession = () => {
    if (state && state.peerEnded) invoke("return_home").then(render).catch(showErr);
    else invoke("request_end").then(render).catch(showErr);
  };
  $("btn-end").onclick = endSession;
  if ($("btn-end-helper")) $("btn-end-helper").onclick = endSession;
  $("btn-end-full").onclick = () => invoke("end_full_trust").then(render).catch(showErr);
  $("btn-back-ask").onclick = () => invoke("set_trust", { trust: "ask" }).then(render).catch(showErr);
  document.querySelectorAll(".ops .btn").forEach((b) => {
    b.onclick = () => {
      selectedTool = b.dataset.tool;
      document.querySelectorAll(".ops .btn").forEach((x) => x.classList.toggle("sel", x === b));
      const titles = {
        ListProcesses: "正在运行的程序",
        ListServices: "服务状态",
        ListDir: "文件夹",
        ReadLogTail: "日志",
        ReadFile: "文件",
        RunCommand: "提议一项改动",
      };
      if ($("tool-title")) $("tool-title").textContent = titles[selectedTool] || "选择一项操作";
      const pathTool = selectedTool === "ListDir" || selectedTool === "ReadLogTail" || selectedTool === "ReadFile";
      show($("path-row"), pathTool);
      show($("propose"), selectedTool === "RunCommand");
      revealResult(false);
      if (selectedTool === "ListProcesses" || selectedTool === "ListServices") {
        invoke("run_tool", { name: selectedTool, path: "", summary: "", command: "" }).then(render).catch(showErr);
      }
    };
  });
  $("btn-path-go").onclick = () => invoke("run_tool", { name: selectedTool, path: $("path").value, summary: "", command: "" }).then(render).catch(showErr);
  $("btn-propose").onclick = () => invoke("run_tool", {
    name: "RunCommand", path: "", summary: $("summary").value, command: $("command").value
  }).then(render).catch(showErr);
  $("btn-copy-result").onclick = () => invoke("copy_result").then(render).catch(showErr);
  $("btn-past").onclick = () => {
    $("past").hidden = !$("past").hidden;
    const n = state && state.past ? state.past.length : 0;
    $("btn-past").textContent = ($("past").hidden ? "▸ " : "▾ ") + "之前的结果" + (n ? "（" + n + "）" : "");
    if (!$("past").hidden) {
      $("past").__openEnd = true;
      snapEnd($("past"));
    }
    revealResult(false);
  };
  const onTrustChange = (el) => {
    if (!el.checked) return;
    invoke("set_trust", { trust: el.value }).then(render).catch(showErr);
  };
  document.querySelectorAll('input[name="trust"]').forEach((el) => {
    el.onchange = () => onTrustChange(el);
  });
  document.querySelectorAll('input[name="host-trust"]').forEach((el) => {
    el.onchange = () => onTrustChange(el);
  });
  $("btn-save-relay").onclick = () => invoke("save_relay", { relay: $("relay").value }).then(render).catch(showErr);
  $("btn-reset-relay").onclick = () => invoke("reset_relay").then(render).catch(showErr);
  const copyMcp = () => invoke("copy_mcp", { draft: $("cred").value }).then(render).catch(showErr);
  $("btn-mcp").onclick = copyMcp;
  if ($("btn-mcp-session")) $("btn-mcp-session").onclick = copyMcp;
  if ($("btn-mcp-home")) $("btn-mcp-home").onclick = copyMcp;
  $("btn-usage").onclick = () => invoke("copy_usage").then(render).catch(showErr);
  $("btn-site").onclick = () => invoke("open_site").catch(showErr);
  if ($("btn-check-update")) {
    $("btn-check-update").onclick = () => invoke("check_update").then(render).catch(showErr);
  }
  if ($("btn-apply-update")) {
    $("btn-apply-update").onclick = () => invoke("apply_update").then(render).catch(showErr);
  }
  if ($("auto-update")) {
    $("auto-update").onchange = () => {
      invoke("set_auto_update", { on: !!$("auto-update").checked }).then(render).catch(showErr);
    };
  }
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

window.addEventListener("wheel", (event) => {
  const home = $("home");
  if (!home || home.hidden || window.innerWidth >= 760) return;
  const box = $("manual-copy");
  if (box && !box.hidden && (event.target === box || box.contains(event.target))) return;
  if (home.scrollHeight <= home.clientHeight + 1) return;
  const line = event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? home.clientHeight : 1;
  const max = home.scrollHeight - home.clientHeight;
  const next = Math.min(max, Math.max(0, home.scrollTop + event.deltaY * line));
  if (next === home.scrollTop) return;
  home.scrollTop = next;
  event.preventDefault();
}, { passive: false, capture: true });

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
