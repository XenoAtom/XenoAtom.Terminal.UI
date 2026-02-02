---
title: Home
layout: simple
og_type: website
---

<section class="text-center py-5 text-white hero-text">
  <div class="container">
    <h1 class="fw-bold display-6">
      <span class="c64-text">XenoAtom.Terminal.UI</span>
    </h1>
    <p class="lead mt-3 mb-4">
      A modern, <strong>reactive retained-mode</strong> terminal UI framework for .NET.<br>
      Compose visuals, bind to state, and let the framework handle layout + rendering efficiently.
    </p>
    <div class="d-flex justify-content-center gap-3 mt-4 flex-wrap">
      <a href="/docs/getting-started/" class="btn btn-primary btn-lg"><i class="bi bi-rocket-takeoff"></i> Get started</a>
      <a href="/docs/controls/" class="btn btn-outline-light btn-lg"><i class="bi bi-ui-checks-grid"></i> Browse controls</a>
      <a href="https://github.com/XenoAtom/XenoAtom.Terminal.UI/" class="btn btn-info btn-lg"><i class="bi bi-github"></i> GitHub</a>
    </div>
    <div class="mt-4 text-start mx-auto" style="max-width: 56rem;">
      <pre class="language-shell-session"><code>dotnet add package XenoAtom.Terminal.UI</code></pre>
    </div>
  </div>
</section>

<section class="container my-5">
  <div class="row row-cols-1 row-cols-lg-2 gx-5 gy-5">
    <div class="col">
      <div class="card h-100">
        <div class="card-header display-6"><i class="bi bi-link-45deg xenoatom-feature-icon xenoatom-icon--binding"></i> Binding-first UI</div>
        <div class="card-body">
          <p class="card-text">
            Bindable properties are tracked during update/layout/render. Change state and only the affected visuals are invalidated.
          </p>
          <a href="/docs/binding/">Binding &amp; State</a>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header display-6"><i class="bi bi-ui-checks-grid xenoatom-feature-icon xenoatom-icon--controls"></i> Composable controls</div>
        <div class="card-body">
          <p class="card-text">
            Inputs, layout containers, menus, overlays, charts, and more — all composable and styleable.
          </p>
          <a href="/docs/controls/">Controls Reference</a>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header display-6"><i class="bi bi-pencil-square xenoatom-feature-icon xenoatom-icon--editing"></i> Text editing</div>
        <div class="card-body">
          <p class="card-text">
            TextBox/TextArea with selection, scrolling, clipboard, and Find/Replace — powered by a shared text subsystem.
          </p>
          <a href="/docs/text-editing/">Text Editing</a>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header display-6"><i class="bi bi-table xenoatom-feature-icon xenoatom-icon--data"></i> DataGridControl</div>
        <div class="card-body">
          <p class="card-text">
            A virtualized data grid with selection, filtering, search, and in-place editing.
          </p>
          <a href="/docs/controls/datagrid/">DataGridControl</a>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header display-6"><i class="bi bi-palette2 xenoatom-feature-icon xenoatom-icon--themes"></i> Themes &amp; alpha blending</div>
        <div class="card-body">
          <p class="card-text">
            Themes are derived from palettes and color schemes, with built-in alpha blending for subtle, layered UIs.
          </p>
          <a href="/docs/styling/">Styling</a> &amp; <a href="/docs/rendering/">Rendering</a>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header display-6"><i class="bi bi-bug xenoatom-feature-icon xenoatom-icon--debug"></i> Debug overlay</div>
        <div class="card-body">
          <p class="card-text">
            Press <kbd>F12</kbd> in fullscreen apps to inspect FPS, dirty regions, diff stats, and per-pass timings.
          </p>
          <a href="/docs/debugging/">Debugging</a>
        </div>
      </div>
    </div>
  </div>
</section>

<section class="container my-5">
  <div class="card">
    <div class="card-header display-6">
      <i class="bi bi-diagram-3 xenoatom-feature-icon xenoatom-icon--data"></i> Built on a small stack
    </div>
    <div class="card-body">
      <p class="card-text">
        XenoAtom.Terminal.UI builds on <strong>XenoAtom.Terminal</strong> (hosting + I/O) and <strong>XenoAtom.Ansi</strong> (markup + ANSI/VT primitives).
      </p>
      <div class="d-flex flex-wrap align-items-center gap-4 mt-3 mb-3">
        <div class="d-flex align-items-center gap-2">
          <img src="/img/xenoatom-terminal.svg" alt="XenoAtom.Terminal" width="56" height="56" loading="lazy">
          <span class="fw-semibold">XenoAtom.Terminal</span>
        </div>
        <i class="bi bi-arrow-right text-secondary" aria-hidden="true"></i>
        <div class="d-flex align-items-center gap-2">
          <img src="/img/xenoatom-logo.png" alt="XenoAtom.Terminal.UI" width="56" height="56" loading="lazy">
          <span class="fw-semibold">XenoAtom.Terminal.UI</span>
        </div>
        <i class="bi bi-plus text-secondary" aria-hidden="true"></i>
        <div class="d-flex align-items-center gap-2">
          <img src="/img/xenoatom-ansi.svg" alt="XenoAtom.Ansi" width="56" height="56" loading="lazy">
          <span class="fw-semibold">XenoAtom.Ansi</span>
        </div>
      </div>
      <a href="/docs/foundations/" class="btn btn-outline-light"><i class="bi bi-journal-text"></i> Ecosystem &amp; foundations</a>
    </div>
  </div>
</section>
