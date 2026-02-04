---
title: Banner
layout: base
sitemap: false
---

<!--
  This page is used to create screenshots for social cards, GitHub banners, etc.
  Keep it self-contained and "screenshot friendly" (no navigation chrome).
-->

<style>
  /* Hide site chrome for clean captures. */
  #xenoatom > nav.navbar,
  #xenoatom footer,
  #xenoatom hr {
    display: none !important;
  }

  /* Make the banner use the full width available. */
  #xenoatom.container {
    max-width: none;
    padding: 0;
  }

  .xenoatom-banner section {
    padding: 0 !important;
  }

  .banner-root {
    padding: 2rem 1.25rem 3rem;
  }

  .banner-canvas {
    width: min(1200px, 100%);
    aspect-ratio: 1200 / 630;

    margin: 0 auto;
    border-radius: 18px;

    background:
      linear-gradient(180deg, rgba(10, 9, 12, 0.65) 0%, rgba(10, 9, 12, 0.80) 100%),
      url('/img/theming.png');
    background-size: cover;
    background-position: center;

    box-shadow:
      0 24px 70px rgba(0, 0, 0, 0.55),
      0 2px 0 rgba(255, 255, 255, 0.06) inset;

    overflow: hidden;
    display: grid;
    align-items: center;
  }

  .banner-inner {
    padding: clamp(1.25rem, 3vw, 2.75rem);
    color: var(--xenoatom-color-foreground, #dcd8e4);
  }

  .banner-title {
    font-weight: 750;
    letter-spacing: -0.02em;
    margin: 0;
    line-height: 1.05;
    font-size: clamp(2.25rem, 4.2vw, 3.6rem);
  }

  .banner-subtitle {
    margin: 0.9rem 0 0;
    color: rgba(220, 216, 228, 0.86);
    font-size: clamp(1rem, 1.6vw, 1.35rem);
    max-width: 55ch;
  }

  .banner-top {
    display: flex;
    gap: 1rem;
    align-items: center;
  }

  .banner-logo {
    width: clamp(64px, 8vw, 96px);
    height: auto;
    flex: 0 0 auto;
  }

  .banner-pill-row {
    margin-top: 1.2rem;
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
  }

  .banner-pill {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    padding: 0.35rem 0.6rem;
    border-radius: 999px;

    background: rgba(255, 255, 255, 0.06);
    border: 1px solid rgba(255, 255, 255, 0.10);
    color: rgba(220, 216, 228, 0.92);
    font-size: 0.95rem;
    white-space: nowrap;
  }

  .banner-pill i.bi {
    opacity: 0.95;
  }

  .banner-code {
    margin-top: 1.15rem;
    display: inline-flex;
    align-items: center;
    gap: 0.65rem;

    padding: 0.6rem 0.8rem;
    border-radius: 12px;
    background: rgba(0, 0, 0, 0.30);
    border: 1px solid rgba(255, 255, 255, 0.10);
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
    font-size: 0.95rem;
  }

  .banner-code kbd {
    font-family: inherit;
    font-size: inherit;
    background: rgba(255, 255, 255, 0.08);
    border: 1px solid rgba(255, 255, 255, 0.12);
    padding: 0.05rem 0.35rem;
    border-radius: 8px;
  }

  .banner-links {
    margin-top: 1.25rem;
    display: flex;
    gap: 0.9rem;
    align-items: center;
    flex-wrap: wrap;
  }

  .banner-links a {
    text-decoration: none;
    font-weight: 600;
    color: rgba(220, 216, 228, 0.92);
    border-bottom: 1px solid rgba(220, 216, 228, 0.35);
  }

  .banner-links a:hover {
    color: rgba(220, 216, 228, 1);
    border-bottom-color: rgba(220, 216, 228, 0.75);
  }

  .banner-links i.bi {
    margin-right: 0.35rem;
  }
</style>

<div class="banner-root">
  <div class="banner-canvas" role="img" aria-label="XenoAtom.Terminal.UI branding banner">
    <div class="banner-inner">
      <div class="banner-top">
        <img class="banner-logo" src="/img/xenoatom-logo.png" alt="XenoAtom.Terminal.UI logo" width="96" height="96">
        <div>
          <h1 class="banner-title">XenoAtom.Terminal.UI</h1>
          <p class="banner-subtitle">A modern, reactive retained-mode terminal UI framework for .NET.</p>
        </div>
      </div>
      <div class="banner-pill-row" aria-hidden="true">
        <span class="banner-pill"><i class="bi bi-lightning-charge"></i>Reactive bindings</span>
        <span class="banner-pill"><i class="bi bi-layout-text-window"></i>Retained-mode visuals</span>
        <span class="banner-pill"><i class="bi bi-aspect-ratio"></i>Layout + rendering</span>
        <span class="banner-pill"><i class="bi bi-mouse"></i>Mouse + keyboard input</span>
        <span class="banner-pill"><i class="bi bi-palette"></i>Themes + alpha blending</span>
      </div>
      <div class="banner-code" aria-label="Install command">
        <span>Install:</span>
        <kbd>dotnet add package XenoAtom.Terminal.UI</kbd>
      </div>
      <div class="banner-links">
        <a href="/docs/"><i class="bi bi-book"></i>Docs</a>
        <a href="https://github.com/XenoAtom/XenoAtom.Terminal.UI"><i class="bi bi-github"></i>GitHub</a>
        <a href="/docs/controls/"><i class="bi bi-grid-3x3-gap"></i>Controls</a>
      </div>
    </div>
  </div>
</div>
