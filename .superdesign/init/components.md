# Components — 鹊桥 Tauri 客户端

无 React/Vue 组件库；UI 原语是 `index.html` + `styles.css` 里的 class。

## Button `.btn`

Variants: default, `.primary`, `.danger`, `.block`, `.sm`, `.dark`, ops `.sel`.

```css
.btn {
  height: 40px;
  border-radius: 12px;
  border: 1px solid var(--line);
  background: #fff;
  padding: 0 16px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  color: var(--ink);
}
.btn.primary {
  background: #2563eb;
  border-color: #2563eb;
  color: #fff;
  box-shadow: 0 1px 2px rgba(37, 99, 235, 0.18);
}
.btn.danger { color: var(--red); border-color: #fecaca; }
```

## Status `.status` + `.dot`

Connected green / wait amber / gray offline.

## Ops toolbar `.ops`

```html
<div class="ops">
  <button type="button" class="btn" data-tool="ListProcesses">查看正在运行的程序</button>
  <button type="button" class="btn" data-tool="ListServices">查看服务状态</button>
  <button type="button" class="btn" data-tool="ListDir">查看文件夹</button>
  <button type="button" class="btn" data-tool="ReadLogTail">查看日志</button>
  <button type="button" class="btn" data-tool="ReadFile">查看文件</button>
  <button type="button" class="btn" data-tool="RunCommand">提议一项改动</button>
</div>
```

## Result pane

`pre.result#result` + `#past` 历史，等行高 20px 的等宽列表滚动区。

## Dialog

`#mask` > `.dlg`：标题、正文、详细、拒绝/允许。

Source of truth: `apps/queqiao-tauri/src/index.html`, `apps/queqiao-tauri/src/styles.css`.
