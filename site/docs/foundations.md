---
title: "Ecosystem & Foundations"
---

# Ecosystem & Foundations

XenoAtom.Terminal.UI is built on a small stack of focused libraries:

- **XenoAtom.Terminal.UI** — retained-mode UI (visual tree, binding, layout, controls, rendering)
- **XenoAtom.Terminal** — terminal I/O + hosting (safe output, input events, inline live regions, fullscreen apps)
- **XenoAtom.Ansi** — ANSI/VT building blocks (markup, styles, parsing, text utilities)

## How they fit together

| Library | Role | Depends on |
|---|---|---|
| <img src="../../img/xenoatom-logo.png" width="40" height="40" alt="XenoAtom.Terminal.UI"> | UI widgets + layout + rendering | XenoAtom.Terminal, XenoAtom.Ansi |
| <img src="../../img/xenoatom-terminal.svg" width="40" height="40" alt="XenoAtom.Terminal"> | Terminal API (output/input/scopes/backends) | XenoAtom.Ansi |
| <img src="../../img/xenoatom-ansi.svg" width="40" height="40" alt="XenoAtom.Ansi"> | ANSI/VT primitives (markup, SGR, parsing) | — |

> [!NOTE]
> There is no dedicated website for XenoAtom.Terminal and XenoAtom.Ansi, so this documentation includes the most relevant
> parts you typically need when building apps with Terminal.UI.

## What to read next

- [XenoAtom.Terminal](terminal.md) — the hosting and I/O foundation underneath Terminal.UI
- [XenoAtom.Ansi](ansi.md) — markup syntax and ANSI primitives used by Terminal.UI (including the `Markup` control)
