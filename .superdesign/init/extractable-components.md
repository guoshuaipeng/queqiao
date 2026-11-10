# Extractable components

Vanilla HTML/CSS SPA — no reusable React layout components to extract as `<sd-component>`.

## Layout (markup patterns, not extractable DraftComponents)
- TopHeader — Source: `apps/queqiao-tauri/src/index.html` `header.top`
- SessionStatusBar — `#helper .sbar` / `#host .sbar`
- EndBar — `footer.endbar`

## Basic patterns
- OpsButtonRow — `.ops` tool buttons
- ResultConsole — `#result` / `#past`
- PathRow / ProposeForm — conditional tool params

**Skip Step 2.5 component extraction** for this target (no framework components; extractable-components lists markup patterns only).
