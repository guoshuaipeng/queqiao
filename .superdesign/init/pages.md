# Pages — dependency trees

## Helper session（连上远程后 · 协助端）— PRIMARY TARGET
Entry: `apps/queqiao-tauri/src/index.html` → `#helper`
Dependencies:
- `apps/queqiao-tauri/src/index.html` (shell + `#helper` section + `#endbar` + `#mask`)
- `apps/queqiao-tauri/src/styles.css` (session / ops / result / endbar tokens)
- `apps/queqiao-tauri/src/main.js` (tool wiring, view toggles — logic, not visual)

## Host session（被协助端）
Entry: `#host` in same `index.html`
Dependencies: same CSS/JS; audit log UI

## Home
Entry: `#home.zones`
Dependencies: same HTML/CSS/JS

## Account / Settings
Entry: `#account`, `#settings`
Dependencies: same files
