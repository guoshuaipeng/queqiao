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
  if (s && typeof s.seq === "number") {
    if (window.__queqiaoSeq > s.seq) return;
    window.__queqiaoSeq = s.seq;
  }
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

  show($("left-idle"), s.phase !== "waiting" && s.phase !== "opening" && s.phase !== "stopping");
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
  const audit = $("audit");
  watchScroll(audit, "__auditStick");
  if (!host) window.__auditStick = "";
  const nextAudit = (s.audit || []).length;
  if (host && nextAudit > (window.__auditSeen || 0)) window.__auditSeen = nextAudit;
  if (!host) window.__auditSeen = 0;
  audit.__applying = true;
  audit.innerHTML = nextAudit
    ? s.audit.map(rowHtml).join("")
    : '<div><span class="t"></span>对方还没有做任何操作</div>';
  if (host) requestAnimationFrame(() => applyScroll(audit, "__auditStick"));
  else audit.__applying = false;
  show($("helper-tools"), !ended);
  document.querySelectorAll(".ops .btn").forEach((b) => {
    b.disabled = !!s.opBusy || ended;
  });
  $("op-status").textContent = s.opBusy ? "正在执行…" : (s.opStatus || "");
  paintResult(s.result || "点上面的操作，结果会显示在这里");
  if (typeof s.formEpoch === "number" && s.formEpoch > (window.__formEpoch || 0)) {
    window.__formEpoch = s.formEpoch;
    $("summary").value = "";
    $("command").value = "";
  }
  const pastCount = (s.past || []).length;
  $("btn-past").textContent = ($("past").hidden ? "▸ " : "▾ ") + "之前的结果" + (pastCount ? "（" + pastCount + "）" : "");
  paintPast(s.past || []);

  $("relay").value = s.relay || "";
  document.querySelectorAll('input[name="trust"]').forEach((el) => {
    el.checked = el.value === (s.trustEffective === "full" ? "full" : s.trustSaved);
  });
  $("relay-hint").textContent = s.relayHint || "";
  $("settings-hint").textContent = s.settingsHint || "";
  $("version").textContent = "版本 " + (s.version || "");
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
  const summary = $("propose") && !$("propose").hidden ? $("summary") : null;
  const past = $("past") && !$("past").hidden && $("past").childElementCount ? $("past") : null;
  const bar = $("ops-bar");
  const barH = bar ? bar.getBoundingClientRect().height : 0;
  const topEl = summary || result;
  const bottomEl = past || result;
  const propose = $("propose");
  const tight = window.innerHeight <= 400;
  document.body.classList.toggle("tight", tight);
  if (tight) {
    scroller.style.gap = "0px";
    scroller.style.paddingTop = "0px";
    scroller.style.paddingBottom = "0px";
    if (bar) bar.style.paddingBottom = "0px";
    if (propose && !propose.hidden) propose.style.gap = "0px";
  } else if (past) {
    scroller.style.gap = "4px";
    scroller.style.paddingTop = "0px";
    scroller.style.paddingBottom = "16px";
    if (bar) bar.style.paddingBottom = "0px";
    if (propose && !propose.hidden) propose.style.gap = "4px";
  } else {
    scroller.style.gap = "";
    scroller.style.paddingTop = "";
    scroller.style.paddingBottom = "";
    if (bar) bar.style.paddingBottom = "";
    if (propose) propose.style.gap = "";
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
  let extra = scroller.scrollHeight - scroller.clientHeight;
  if (extra > 1 && bar) {
    bar.style.paddingBottom = "0px";
    extra = scroller.scrollHeight - scroller.clientHeight;
  }
  if (extra > 1) {
    scroller.style.paddingTop = "0px";
    extra = scroller.scrollHeight - scroller.clientHeight;
  }
  if (extra > 1 && extra <= 28) {
    scroller.style.paddingTop = "0px";
    if (!past) scroller.style.paddingBottom = "0px";
    extra = scroller.scrollHeight - scroller.clientHeight;
  }
  if (extra > 1 && extra <= 12) {
    const pad = parseFloat(scroller.style.paddingBottom) || 0;
    scroller.style.paddingBottom = Math.max(0, pad - extra) + "px";
    extra = scroller.scrollHeight - scroller.clientHeight;
  }
  if (!tight && extra > 1 && extra <= 40) {
    scroller.style.gap = "4px";
    extra = scroller.scrollHeight - scroller.clientHeight;
  }
  if (extra > 1 && extra <= 24) {
    scroller.style.paddingBottom = "0px";
    extra = scroller.scrollHeight - scroller.clientHeight;
  }
  const fitRows = (box, rows) => {
    box.style.maxHeight = (rows * 20 + 2) + "px";
    const wide = box.scrollWidth > box.clientWidth + 1;
    box.style.maxHeight = (rows * 20 + 2 + (wide ? 12 : 0)) + "px";
  };
  if (tight) {
    scroller.style.paddingTop = "0px";
    scroller.style.paddingBottom = "0px";
    scroller.style.gap = "0px";
    if (propose && !propose.hidden) propose.style.gap = "0px";
  }
  fitRows(result, resultRows);
  if (past) fitRows(past, pastRows);
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
  settle(result, (window.__resultStick || "end") === "end");
  if (past) settle(past, false);
  extra = scroller.scrollHeight - scroller.clientHeight;
  let shrinks = 0;
  while (extra > 4 && shrinks < 8) {
    scroller.style.paddingTop = "0px";
    scroller.style.paddingBottom = "0px";
    scroller.style.gap = "0px";
    extra = scroller.scrollHeight - scroller.clientHeight;
    if (extra <= 4) break;
    if (resultRows > resultMin) resultRows -= 1;
    else if (past && pastRows > pastMin) pastRows -= 1;
    else break;
    fitRows(result, resultRows);
    if (past) fitRows(past, pastRows);
    settle(result, (window.__resultStick || "end") === "end");
    if (past) settle(past, false);
    extra = scroller.scrollHeight - scroller.clientHeight;
    shrinks += 1;
  }
  placeRail(result);
  if (past) placeRail(past);
  extra = scroller.scrollHeight - scroller.clientHeight;
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
  dlgLeft = d.seconds || 60;
  paintCount(d);
  clearInterval(dlgTimer);
  dlgTimer = setInterval(() => {
    dlgLeft -= 1;
    paintCount(d);
    if (dlgLeft <= 0) clearInterval(dlgTimer);
  }, 1000);
  setTimeout(() => $("dlg-no").focus(), 0);
}

function paintCount(d) {
  if (d.kind === "pair" || d.kind === "write") {
    const base = d.primary || (d.kind === "pair" ? "允许协助" : "允许");
    $("dlg-yes").textContent = base + "（" + Math.max(0, dlgLeft) + "）";
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
  $("btn-copy").onclick = async () => {
    try {
      const snap = await invoke("copy_credential");
      render(snap);
      if (snap && snap.manualCopy && navigator.clipboard && navigator.clipboard.writeText) {
        try {
          await navigator.clipboard.writeText(snap.manualCopy);
          const checked = await invoke("confirm_copy");
          render(checked);
        } catch (err) {
          const box = $("manual-copy");
          if (box) { box.focus(); box.select(); }
        }
      }
    } catch (err) { showErr(err); }
  };
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
      revealResult(false);
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
