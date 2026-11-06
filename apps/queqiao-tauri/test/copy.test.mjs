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
  assert.match(js, /result\.scrollTop = result\.scrollHeight/);
  assert.match(css, /\.result\s*\{[^}]*max-height:\s*168px/);
  assert.match(css, /\.result\s*\{[^}]*overflow:\s*auto/);
});

test("path view button stays on one line", () => {
  assert.match(css, /#btn-path-go\s*\{[^}]*white-space:\s*nowrap/);
  assert.match(css, /\.path-row #path\s*\{[^}]*min-width:\s*0/);
});

test("write confirm shows the command without an extra click", () => {
  assert.match(js, /show\(\$\("dlg-detail-toggle"\), false\)/);
  assert.match(js, /show\(\$\("dlg-detail"\), hasDetail\)/);
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
