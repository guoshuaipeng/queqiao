import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";

const html = fs.readFileSync(new URL("../src/index.html", import.meta.url), "utf8");
const js = fs.readFileSync(new URL("../src/main.js", import.meta.url), "utf8");
const css = fs.readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");
const rust = fs.readFileSync(new URL("../src-tauri/src/copy.rs", import.meta.url), "utf8");
const blob = html + js + css + rust;

const required = [
  "亲友之间，发凭证或粘贴连接。",
  "请人帮我",
  "让你信任的人帮你查电脑。对方要改动时会先问你。",
  "开启接受协助",
  "我帮别人",
  "粘贴连接凭证",
  "对方还没装鹊桥？复制安装说明",
  "只把连接凭证私下发给你信任的人。",
  "等待对方连接",
  "复制连接凭证",
  "停止接受",
  "已找到对方，等对方点「允许协助」",
  "有人想协助这台电脑",
  "正在接受协助 · 已连接",
  "当前是「完全信任」：这次协助的改动不会再问你。",
  "结束完全信任",
  "对方已连接。请核对对方身份，不要随便信任陌生人。",
  "对方做过的事",
  "结束本次协助",
  "正在协助对方 · 已连接",
  "查看正在运行的程序",
  "查看文件夹",
  "查看日志",
  "查看文件",
  "提议一项改动",
  "复制结果",
  "对方想在这台电脑执行操作",
  "查看详细",
  "对方要改这台电脑时",
  "每次询问",
  "宽松",
  "完全信任",
  "连接服务器",
  "恢复默认",
  "给 AI 助手用（MCP）",
  "复制 MCP 配置",
  "复制使用说明",
  "高风险包括删除文件、关机、改权限等。",
];

test("prototype copy is present", () => {
  for (const line of required) {
    assert.ok(blob.includes(line), "missing: " + line);
  }
});

test("demo credentials from the mock are not hardcoded as live data", () => {
  assert.equal(blob.includes("482 913 075"), false);
  assert.equal(blob.includes("chrome.exe"), false);
  assert.equal(blob.includes("Spooler"), false);
});

test("reject stays the default dialog button", () => {
  assert.ok(js.includes("dlg-no"));
  assert.ok(js.includes("focus"));
  assert.ok(js.includes('e.key === "Escape"'));
});

test("desktop channel missing is not treated as connected", () => {
  assert.ok(js.includes("不能假装已连接"));
});

test("hidden attribute wins over display flex", () => {
  assert.match(css, /\[hidden\]\s*\{\s*display:\s*none\s*!important;\s*\}/);
});

test("narrow home wheel scrolls the connect column", () => {
  assert.match(css, /max-width:\s*759px/);
  assert.match(css, /\.zones\s*\{[^}]*overflow-y:\s*auto/);
  assert.match(js, /home\.scrollTop/);
  assert.match(js, /innerWidth >= 760/);
});

test("pair dialog does not leave the credential readable behind it", () => {
  assert.match(css, /\.mask\s*\{[^}]*background:\s*#e8eef6/);
  assert.match(js, /manual-copy/);
  assert.ok(js.includes("s.dialog ? \"\""));
});

test("absent peer is not told to check the network", () => {
  assert.ok(blob.includes("对方现在没有在等待协助。请让对方重新开启，再发一次连接凭证。"));
  assert.ok(blob.includes("没能连上，请检查网络后重试。"));
});

test("denied propose clears the command fields", () => {
  assert.ok(js.includes("formEpoch"));
  assert.ok(js.includes('$("command").value = ""'));
});

test("countdown does not pretend to be a click on reject", () => {
  assert.match(js, /if \(dlgLeft <= 0\) clearInterval\(dlgTimer\);/);
  assert.ok(!js.includes("dlgLeft <= 0) {"));
});

test("write confirm shows the remaining seconds", () => {
  assert.match(js, /d\.kind === "pair" \|\| d\.kind === "write"/);
});

test("log result scrolls to the newest line", () => {
  assert.match(js, /paintResult\(/);
  assert.match(js, /applyScroll\(result, "__resultStick"\)/);
  assert.match(js, /if \(\(window\[key\] \|\| "end"\) === "end"\) snapEnd\(box\)/);
  assert.match(css, /#result\s*\{[^}]*overflow-y:\s*auto/);
});

test("path view button stays on one line", () => {
  assert.match(css, /#btn-path-go\s*\{[^}]*white-space:\s*nowrap/);
  assert.match(css, /\.path-row #path\s*\{[^}]*min-width:\s*0/);
});

test("write confirm shows the command without an extra click", () => {
  assert.match(js, /show\(\$\("dlg-detail-toggle"\), false\)/);
  assert.match(js, /show\(detail, hasDetail\)/);
});

test("confirm command does not break in the middle of a word", () => {
  assert.match(css, /#dlg-detail\s*\{[^}]*white-space:\s*pre\b/);
  assert.match(css, /#dlg-detail\s*\{[^}]*overflow-x:\s*auto/);
});

test("a long command opens scrolled to the path", () => {
  assert.match(js, /detail\.scrollLeft = detail\.scrollWidth/);
});

test("an open record button says collapse", () => {
  assert.match(js, /showHistory \? "收起"/);
});

test("a long record opens at the newest line", () => {
  assert.match(js, /__historyStick/);
  assert.match(js, /applyScroll\(history, "__historyStick"\)/);
  assert.match(css, /#history, #audit\s*\{[^}]*overflow-y:\s*auto/);
});

test("the live audit can scroll and lines stay whole", () => {
  assert.match(js, /applyScroll\(audit, "__auditStick"\)/);
  assert.match(js, /if \(box\.__applying\) return/);
  assert.match(css, /scroll-snap-align:\s*start/);
  assert.match(css, /#history > div, #audit > div\s*\{[^}]*height:\s*24px/);
});

test("steps come back when the record is closed", () => {
  assert.match(css, /#zone-left:has\(#history:not\(\[hidden\]\)\) #left-idle \.steps/);
});

test("a long result stays on the newest whole line", () => {
  assert.match(js, /function paintResult/);
  assert.match(js, /applyScroll\(result, "__resultStick"\)/);
  assert.match(css, /#result > div\s*\{[^}]*height:\s*20px/);
  assert.match(css, /#result\s*\{[^}]*8 \* 20px \+ 2px/);
  assert.match(css, /#result\s*\{[^}]*flex:\s*0 0 auto/);
});

test("reading earlier result lines is not pulled to the end", () => {
  assert.match(js, /if \(box\.__applying\) return/);
  assert.match(js, /watchScroll\(result, "__resultStick"\)/);
  assert.match(js, /const follow = \(window\.__resultStick \|\| "end"\) === "end"/);
  assert.match(js, /const keptTop = window\.__resultStickTop \|\| 0/);
  assert.match(js, /window\.__resultStick = follow \? "end" : "place"/);
});

test("scrolling back to the end follows the next result", () => {
  assert.match(js, /window\.__resultStick = pinnedToEnd\(result\) \? "end" : "place"/);
  assert.match(js, /if \(\(window\[key\] \|\| "end"\) === "end"\) snapEnd\(box\)/);
});

test("an early result box is not cut by the window", () => {
  assert.match(js, /function revealResult/);
  assert.match(js, /if \(place && outside && !pastOpen\) return/);
  assert.match(js, /revealResult\(true\)/);
  assert.match(js, /revealResult\(false\)/);
});

test("previous output opens without pushing the current result away", () => {
  assert.match(js, /function paintPast/);
  assert.match(js, /snapEnd\(\$\("past"\)\)/);
  assert.match(css, /#past\s*\{[^}]*max-height:\s*calc\(4 \* 20px \+ 2px \+ 16px\)/);
  assert.match(css, /#past > div\s*\{[^}]*height:\s*20px/);
});

test("the task field stays inside the window with the result", () => {
  assert.match(js, /function fitSpan/);
  assert.match(js, /const summary = \$\("propose"\) && !\$\("propose"\)\.hidden \? \$\("summary"\) : null/);
  assert.match(js, /const viewTop = view\.top \+ barH/);
  assert.match(js, /if \(box\.top < viewTop - 1\) scroller\.scrollTop -= viewTop - box\.top/);
});

test("the previous exit line and the tool row stay whole", () => {
  assert.match(js, /const bar = \$\("ops-bar"\)/);
  assert.match(js, /else if \(past && pastRows > pastMin\) pastRows -= 1/);
  assert.match(js, /scroller\.style\.paddingBottom = "16px"/);
  assert.match(js, /else if \(past\) snapNearest\(past\)/);
  assert.match(css, /#ops-bar\s*\{[^}]*position:\s*sticky/);
});

test("a long previous line stays clear of the scrollbar", () => {
  assert.match(css, /#past\s*\{[^}]*padding:\s*0 36px 0 12px/);
  assert.match(css, /#result::-webkit-scrollbar,\s*#past::-webkit-scrollbar\s*\{[^}]*width:\s*12px/);
});

test("the result wheel does not scroll the page", () => {
  assert.match(js, /function watchResultWheel/);
  assert.match(js, /result\.addEventListener\("wheel"/);
  assert.match(js, /event\.preventDefault\(\)/);
  assert.match(js, /watchResultWheel\(result\)/);
  assert.match(css, /#result\s*\{[^}]*overscroll-behavior:\s*contain/);
});

test("opening previous results lands on the newest lines", () => {
  assert.match(js, /\$\("past"\)\.__openEnd = true/);
  assert.match(js, /if \(past && past\.__openEnd\) \{\s*snapEnd\(past\);\s*past\.__openEnd = false;/);
  assert.match(js, /function placeRail/);
  assert.match(css, /\.rail\s*\{[^}]*width:\s*24px/);
});

test("a short line stays put while the long line slides", () => {
  assert.match(js, /function pinShortRows/);
  assert.match(js, /row\.classList\.toggle\("stick-x", !wide\)/);
  assert.match(js, /translateX\(/);
  assert.match(js, /pinShortRows\(past\)/);
  assert.match(css, /#result > div\.stick-x,\s*#past > div\.stick-x\s*\{[^}]*background:\s*#fbfdff/);
});

test("previous results at the first line do not scroll the page", () => {
  assert.match(js, /if \(maxY > 1\) \{\s*past\.scrollTop = Math\.min\(maxY, Math\.max\(0, past\.scrollTop \+ event\.deltaY\)\);\s*\}/);
  assert.match(js, /shiftShortRows\(past\);\s*event\.preventDefault\(\)/);
});

test("scrolling back keeps the long line at its tail", () => {
  assert.match(js, /if \(movedX && !movedY\) box\.__rowTail = x/);
  assert.match(js, /box\.scrollLeft = box\.__rowTail/);
});

test("the result box at either end does not scroll the page", () => {
  assert.match(js, /if \(maxY > 1\) \{\s*result\.scrollTop = Math\.min\(maxY, Math\.max\(0, result\.scrollTop \+ event\.deltaY\)\);\s*\}/);
  assert.match(js, /slideBy\(result, event\.deltaX\)/);
  assert.match(js, /box\.__rowTail = box\.scrollLeft/);
});

test("a short result wheel does not scroll the page", () => {
  assert.match(js, /if \(maxY > 1\) \{\s*result\.scrollTop = Math\.min\(maxY, Math\.max\(0, result\.scrollTop \+ event\.deltaY\)\);\s*\}\s*if \(\(result\.__rowTail \|\| 0\) > 1/);
  assert.match(js, /shiftShortRows\(result\);\s*event\.preventDefault\(\);\s*\}, \{ passive: false \}\);/);
});

test("a short previous result wheel does not scroll the page", () => {
  assert.match(js, /if \(maxY > 1\) \{\s*past\.scrollTop = Math\.min\(maxY, Math\.max\(0, past\.scrollTop \+ event\.deltaY\)\);\s*\}\s*if \(\(past\.__rowTail \|\| 0\) > 1/);
  assert.match(js, /shiftShortRows\(past\);\s*event\.preventDefault\(\);\s*\}, \{ passive: false \}\);/);
});

test("a horizontal wheel on previous results does not move the page", () => {
  assert.match(js, /const y = box\.scrollTop/);
  assert.match(js, /if \(box\.scrollTop !== y\) box\.scrollTop = y/);
  assert.match(js, /if \(next >= max - 1\) box\.scrollLeft = box\.scrollWidth/);
  assert.match(js, /slideBy\(past, event\.deltaX\)/);
});

test("the result snaps to a whole first line without losing the tail", () => {
  assert.match(js, /result\.addEventListener\("scrollend"/);
  assert.match(js, /snapNearest\(result\)/);
  assert.match(js, /result\.__keepY != null/);
});

test("horizontal bars sit under the text rows", () => {
  assert.match(css, /#result::-webkit-scrollbar,\s*#past::-webkit-scrollbar\s*\{[^}]*height:\s*12px/);
  assert.match(css, /#result\s*\{[^}]*8 \* 20px \+ 2px \+ 16px/);
  assert.match(css, /#past\s*\{[^}]*4 \* 20px \+ 2px \+ 16px/);
});

test("a long result line stays clear of its scrollbar", () => {
  assert.match(css, /#result\s*\{[^}]*padding:\s*0 36px 0 12px/);
  assert.match(css, /#result::-webkit-scrollbar,\s*#past::-webkit-scrollbar\s*\{[^}]*width:\s*12px/);
});

test("a wide result does not bring back the page scrollbar", () => {
  assert.match(js, /if \(!past\) scroller\.style\.paddingBottom = "0px"/);
  assert.match(js, /scroller\.style\.overflowY = extra <= 4 \? "hidden" : "auto"/);
  assert.match(js, /while \(extra > 4 && shrinks < 8\)/);
});

test("a long line can slide sideways without sliding the page", () => {
  assert.match(css, /#result\s*\{[^}]*overflow-x:\s*auto/);
  assert.match(css, /#result > div\s*\{[^}]*width:\s*max-content/);
  assert.match(css, /\.scroll\s*\{[^}]*overflow-x:\s*hidden/);
  assert.match(js, /Math\.abs\(event\.deltaX\) > Math\.abs\(event\.deltaY\)/);
  assert.match(js, /slideBy\(result, event\.deltaX\)/);
});

test("previous lines can scroll and the page scrollbar stays away", () => {
  assert.match(js, /function snapNearest/);
  assert.match(js, /if \(max - box\.scrollTop < step\)/);
  assert.match(js, /box\.scrollTop = max/);
  assert.match(js, /past\.__keepY != null/);
  assert.match(js, /snapNearest\(past\)/);
  assert.match(js, /scroller\.style\.overflowY = extra <= 4 \? "hidden" : "auto"/);
});

test("a short window keeps the task and the end button inside", () => {
  assert.match(js, /window\.addEventListener\("resize", \(\) => \{/);
  assert.match(js, /document\.body\.classList\.toggle\("tight", tight\)/);
  assert.match(js, /rows \* 20 \+ 2 \+ \(wide \? 12 : 0\)/);
  assert.match(js, /maxX - tail <= 48/);
  assert.match(js, /scroller\.style\.gap = "0px"/);
  assert.match(css, /body\.tight \.endbar\s*\{[^}]*height:\s*44px/);
  assert.match(css, /body\.tight #summary\s*\{[^}]*height:\s*30px/);
  assert.match(css, /body\.tight #command,\s*body\.tight #btn-propose\s*\{[^}]*height:\s*28px/);
});

test("a narrow window keeps the start button in view", () => {
  assert.match(css, /max-width:\s*759px[\s\S]*#left-actions\s*\{[^}]*position:\s*sticky/);
});

test("a narrow window can still reach connect after the record opens", () => {
  assert.match(css, /max-width:\s*759px[\s\S]*#zone-right\s*\{[^}]*z-index:\s*3/);
  assert.match(css, /#btn-connect\s*\{[^}]*scroll-margin-bottom:\s*16px/);
  assert.match(js, /home\.scrollTop = next/);
});

test("the home record keeps five rows", () => {
  assert.match(css, /#history, #audit\s*\{[^}]*5 \* 24px \+ 4 \* 6px/);
  assert.match(css, /#history\s*\{[^}]*flex:\s*0 0 auto/);
});

test("a finished change clears the command", () => {
  const link = fs.readFileSync(new URL("../src-tauri/src/link.rs", import.meta.url), "utf8");
  const doneAt = link.indexOf("copy::DONE");
  const next = link.slice(doneAt, doneAt + 180);
  assert.ok(doneAt > 0);
  assert.ok(next.includes("is_read"));
  assert.ok(next.includes("ClearPropose"));
  assert.ok(js.includes('$("command").value = ""'));
});

test("cancel tells the helper it did not connect", () => {
  assert.ok(blob.includes("已取消，没有连上。"));
});

test("older snapshots do not replace a newer one", () => {
  assert.ok(js.includes("__queqiaoSeq"));
  assert.ok(js.includes("s.seq"));
});

test("production boot does not render a demo connection", () => {
  assert.ok(js.includes("__QUEQIAO_SKIP_BOOT__"));
  assert.equal(js.includes("queqiaoRender("), false);
  assert.equal(js.includes("界面样例"), false);
  assert.equal(js.includes("QUEQIAO_SHOTS ="), false);
  assert.ok(js.includes("test/fixtures.js"));
  assert.match(html, /id="host"[^>]*hidden/);
  assert.match(html, /id="helper"[^>]*hidden/);
  assert.match(html, /id="settings"[^>]*hidden/);
  assert.match(html, /id="mask"[^>]*hidden/);
});
