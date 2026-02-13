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
        <div class="mt-2 small">
          <a href="../"><i class="bi bi-book" aria-hidden="true"></i> Docs</a>
          <span class="mx-2 text-muted">|</span>
          <a href="https://github.com/XenoAtom/XenoAtom.Terminal.UI"><i class="bi bi-github" aria-hidden="true"></i> GitHub</a>
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

  <div class="col-12">
    <div class="card h-100">
      <div class="card-body">
        <div class="d-flex align-items-center gap-3 mb-2">
          <img src="../../img/xenoatom-logging.png" width="56" height="56" alt="XenoAtom.Logging">
          <div>
            <div class="h5 mb-0">XenoAtom.Logging</div>
            <div class="text-muted small">High-performance logging + LogControl sink</div>
          </div>
        </div>
        <div class="small">
          Structured logging runtime with a Terminal.UI integration that can write directly into <code>LogControl</code>
          (including markup and rich formatting support).
        </div>
        <div class="mt-2 small">
          <a href="https://xenoatom.github.io/logging/docs/"><i class="bi bi-book" aria-hidden="true"></i> Docs</a>
          <span class="mx-2 text-muted">|</span>
          <a href="https://github.com/XenoAtom/XenoAtom.Logging"><i class="bi bi-github" aria-hidden="true"></i> GitHub</a>
        </div>
      </div>
    </div>
  </div>

  <div class="col-12">
    <div class="card h-100">
      <div class="card-body">
        <div class="d-flex align-items-center gap-3 mb-2">
          <img src="../../img/xenoatom-commandline.png" width="56" height="56" alt="XenoAtom.CommandLine">
          <div>
            <div class="h5 mb-0">XenoAtom.CommandLine</div>
            <div class="text-muted small">Composition-first CLI parser + Terminal.UI help visuals</div>
          </div>
        </div>
        <div class="small">
          Companion command-line library with an optional <code>XenoAtom.CommandLine.Terminal</code> package that can render
          help/errors via Terminal.UI visuals and embed command help into fullscreen apps.
        </div>
        <div class="mt-2 small">
          <a href="../commandline/"><i class="bi bi-book" aria-hidden="true"></i> Docs</a>
          <span class="mx-2 text-muted">|</span>
          <a href="https://github.com/XenoAtom/XenoAtom.CommandLine"><i class="bi bi-github" aria-hidden="true"></i> GitHub</a>
        </div>
      </div>
    </div>
  </div>
</div>

## Dependency chain (simplified)

At a high level:

- `XenoAtom.Terminal.UI` depends on `XenoAtom.Terminal` and `XenoAtom.Ansi`
- `XenoAtom.Terminal` depends on `XenoAtom.Ansi`
- optional companion: `XenoAtom.CommandLine.Terminal` depends on `XenoAtom.CommandLine` and `XenoAtom.Terminal.UI`

In other words:

`XenoAtom.Terminal.UI -> XenoAtom.Terminal -> XenoAtom.Ansi`

and for CLI visual help integration:

`XenoAtom.CommandLine.Terminal -> XenoAtom.Terminal.UI -> XenoAtom.Terminal -> XenoAtom.Ansi`

## How they fit together

{.table}
| Library | Role | Depends on |
|---|---|---|
| **XenoAtom.Terminal.UI** | UI widgets + layout + rendering | XenoAtom.Terminal, XenoAtom.Ansi |
| **XenoAtom.Terminal** | Terminal API (output/input/scopes/backends) | XenoAtom.Ansi |
| **XenoAtom.Ansi** | ANSI/VT primitives (markup, SGR, parsing) | - |
| **XenoAtom.Logging** | Structured logging + Terminal.UI `LogControl` sink | XenoAtom.Terminal.UI (integration package) |
| **XenoAtom.CommandLine** | Command-line parser and command model | - |
| **XenoAtom.CommandLine.Terminal** | Terminal markup/visual help renderers | XenoAtom.CommandLine, XenoAtom.Terminal.UI |

> [!NOTE]
> There is no dedicated website for XenoAtom.Terminal and XenoAtom.Ansi, so this documentation includes the most relevant parts you typically need when building apps with Terminal.UI.

## What to read next

- [XenoAtom.Terminal](terminal.md) - the hosting and I/O foundation underneath Terminal.UI
- [XenoAtom.Ansi](ansi.md) - markup syntax and ANSI primitives used by Terminal.UI (including the `Markup` control)
- [Markup](markup.md) - markup syntax reference and Terminal.UI semantic markup tokens
- [Logging Integration](logging.md) - integrate XenoAtom.Logging with LogControl for fullscreen log-viewer apps
- [XenoAtom.Logging Docs](https://xenoatom.github.io/logging/docs/) - full logging framework documentation
- [CommandLine](commandline.md) - integrate XenoAtom.CommandLine.Terminal for rich help visuals and CLI UX
