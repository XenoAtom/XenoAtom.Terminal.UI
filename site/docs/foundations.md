---
title: "Ecosystem & Foundations"
---

# Ecosystem & Foundations

XenoAtom.Terminal.UI is built on a small stack of focused libraries. Together they provide:

- a modern terminal API (output, input events, hosting)
- ANSI/VT primitives (markup, styles, parsing)
- a reactive retained-mode UI framework (visual tree, binding, layout, rendering)

<div class="row g-3 mt-2">
  <div class="col-12">
    <div class="card h-100">
      <div class="card-body">
        <div class="d-flex align-items-center gap-3 mb-2">
          <img src="../../img/xenoatom-logo.png" width="56" height="56" alt="XenoAtom.Terminal.UI">
          <div>
            <div class="h5 mb-0">XenoAtom.Terminal.UI</div>
            <div class="text-muted small">Widgets, layout, rendering, and app model</div>
          </div>
        </div>
        <div class="small">
          Retained-mode UI framework for .NET terminal apps: visual tree, binding/state, layout, controls, scrolling, overlays.
        </div>
      </div>
    </div>
  </div>

  <div class="col-12">
    <div class="card h-100">
      <div class="card-body">
        <div class="d-flex align-items-center gap-3 mb-2">
          <img src="../../img/xenoatom-terminal.svg" width="56" height="56" alt="XenoAtom.Terminal">
          <div>
            <div class="h5 mb-0">XenoAtom.Terminal</div>
            <div class="text-muted small">Terminal I/O, hosting, and input events</div>
          </div>
        </div>
        <div class="small">
          Terminal API and hosting layer: safe output, unified input events, inline live regions, fullscreen apps, and test backends.
        </div>
        <div class="mt-2 small">
          <a href="../terminal/"><i class="bi bi-book" aria-hidden="true"></i> Docs</a>
          <span class="mx-2 text-muted">|</span>
          <a href="https://github.com/XenoAtom/XenoAtom.Terminal"><i class="bi bi-github" aria-hidden="true"></i> GitHub</a>
        </div>
      </div>
    </div>
  </div>

  <div class="col-12">
    <div class="card h-100">
      <div class="card-body">
        <div class="d-flex align-items-center gap-3 mb-2">
          <img src="../../img/xenoatom-ansi.svg" width="56" height="56" alt="XenoAtom.Ansi">
          <div>
            <div class="h5 mb-0">XenoAtom.Ansi</div>
            <div class="text-muted small">Markup, styles, and ANSI/VT parsing</div>
          </div>
        </div>
        <div class="small">
          ANSI/VT building blocks: style emission (SGR), markup parsing, tokenization, and ANSI-aware text utilities.
        </div>
        <div class="mt-2 small">
          <a href="../ansi/"><i class="bi bi-book" aria-hidden="true"></i> Docs</a>
          <span class="mx-2 text-muted">|</span>
          <a href="https://github.com/XenoAtom/XenoAtom.Ansi"><i class="bi bi-github" aria-hidden="true"></i> GitHub</a>
        </div>
      </div>
    </div>
  </div>
</div>

## Dependency chain (simplified)

At a high level:

- `XenoAtom.Terminal.UI` depends on `XenoAtom.Terminal` and `XenoAtom.Ansi`
- `XenoAtom.Terminal` depends on `XenoAtom.Ansi`

In other words:

`XenoAtom.Terminal.UI -> XenoAtom.Terminal -> XenoAtom.Ansi`

## How they fit together

| Library | Role | Depends on |
|---|---|---|
| **XenoAtom.Terminal.UI** | UI widgets + layout + rendering | XenoAtom.Terminal, XenoAtom.Ansi |
| **XenoAtom.Terminal** | Terminal API (output/input/scopes/backends) | XenoAtom.Ansi |
| **XenoAtom.Ansi** | ANSI/VT primitives (markup, SGR, parsing) | - |

> [!NOTE]
> There is no dedicated website for XenoAtom.Terminal and XenoAtom.Ansi, so this documentation includes the most relevant parts you typically need when building apps with Terminal.UI.

## What to read next

- [XenoAtom.Terminal](terminal.md) - the hosting and I/O foundation underneath Terminal.UI
- [XenoAtom.Ansi](ansi.md) - markup syntax and ANSI primitives used by Terminal.UI (including the `Markup` control)
- [Markup](markup.md) - markup syntax reference and Terminal.UI semantic markup tokens
