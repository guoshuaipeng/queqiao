# Layouts — 鹊桥 Tauri 客户端

无独立 layout 组件；壳层写在 `apps/queqiao-tauri/src/index.html`。

## App shell

- `.client#app` 全高列布局
- `header.top#top`：品牌 / 返回 / 设置标题 + 右侧账号芯片与设置
- 主内容区按视图切换（home / host / helper / account / settings）
- `footer.foot#foot`：安全提醒（会话中隐藏）
- `footer.endbar#endbar`：结束本次协助（会话中显示）
- `#mask` 对话框蒙层

## Header (relevant extract)

```html
<header class="top" id="top">
  <div class="brand" id="brand-home">
    <div class="logo"><i></i>鹊桥</div>
    <p class="tagline" id="tagline">亲友之间，发凭证或粘贴连接。</p>
  </div>
  <button type="button" class="back" id="btn-back" hidden>← 返回</button>
  <h1 id="settings-title" hidden>设置</h1>
  <div class="top-right" id="top-right">
    <button type="button" class="acct-chip" id="btn-account">…</button>
    <button type="button" class="gear" id="btn-settings">设置</button>
  </div>
</header>
```

## Session end bar

```html
<footer class="endbar" id="endbar" hidden>
  <button type="button" class="btn danger" id="btn-end">结束本次协助</button>
</footer>
```

Full file: `apps/queqiao-tauri/src/index.html`.
