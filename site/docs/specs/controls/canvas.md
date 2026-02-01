---
title: Canvas Specs
---

# Canvas Specs

This document captures design and implementation notes for `Canvas`.

> [!NOTE]
> For end-user usage and examples, see [Canvas](../../controls/canvas.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: A lightweight immediate-mode drawing surface for cell-based terminal graphics.
- **Rendering model**: no backing buffer; the user-provided `Painter` draws directly into the current `CellBuffer` during `Render`.
- **Typical uses**: custom plots, mini-maps, sparklines, diagrams, debugging visuals.

## Public API surface

### Type

- `Canvas : Visual`

### Bindables

- `Painter : Delegator<Action<CanvasContext>>`
  - Callback invoked during rendering to draw into the current buffer.

### Defaults

- `HorizontalAlignment = Align.Stretch`
- `VerticalAlignment = Align.Stretch`

## Layout

`Canvas` does not have intrinsic content size. Its `MeasureCore` returns:

- `Min = (0, 0)`
- `Natural = (0, 0)`
- `Max = (∞, ∞)`
- `GrowX = 1` only when `HorizontalAlignment == Align.Stretch`
- `GrowY = 1` only when `VerticalAlignment == Align.Stretch`

This makes `Canvas` behave like a flexible “fills available space” visual by default, but it can also be constrained
via min/max size like any other visual.

## Rendering

During `RenderOverride`:

- If `Bounds` is empty, does nothing.
- If `Painter` is null, does nothing.
- Otherwise it resolves:
  - `Theme` via `GetTheme()`
  - `CanvasStyle` via `GetStyle<CanvasStyle>()`
  - the default drawing style via `CanvasStyle.ResolveDefaultStyle(theme)`
- Then it constructs a `CanvasContext` and calls the painter callback.

> [!IMPORTANT]
> `Painter` is executed inside the normal render tracking context.
> If the painter reads bindables/state, those reads will be tracked and changes will automatically schedule repaint.

### Painter coordinate system

`CanvasContext` draws with coordinates relative to the canvas origin:

- `(0,0)` is the top-left cell of the canvas bounds
- all drawing methods clip to the canvas bounds

The framework already establishes clipping to the canvas `Bounds` before calling `RenderOverride`, but `CanvasContext`
also performs explicit bounds checks to keep drawing helpers safe.

## CanvasContext drawing helpers

`CanvasContext` is a small helper API around the underlying `CellBuffer`, providing:

- `Bounds` / `Size`
- `Clear()` / `Clear(rune, style)`
- `SetPixel(x, y, rune, style)` (cell-level draw)
- line primitives:
  - horizontal / vertical lines
  - `DrawLine(...)` (diagonal line; Bresenham-style cell rasterization)
- rectangles:
  - `FillRect(...)`
  - `DrawBox(...)` with `LineGlyphs` (handles small widths/heights)
- circles:
  - `DrawCircle(...)` (midpoint circle algorithm; outlines only)
- text:
  - `WriteText(...)` (clipped to the canvas bounds)

The helper methods are intentionally simple, allocation-free, and optimized for small surfaces rather than large
framebuffers.

> [!TIP]
> Since there is no retained drawing state, a typical painter starts with `ctx.Clear(...)` and then redraws the full scene.

## Styling

### CanvasStyle

Resolved from the environment via `CanvasStyle.Key`:

- `DefaultRune` (default: `'█'`)
- `DefaultStyle : Style?`
  - When null, defaults to `theme.ForegroundTextStyle()` (draw using “ink” on terminal default background).

## Tests & demos

Tests that lock down current behavior:

- `src/XenoAtom.Terminal.UI.Tests/CanvasTests.cs`
  - line + box rendering
  - circle outline rendering

## Future / v2 ideas

- Add higher-level plot helpers (axes, ticks, labels) as opt-in helpers (likely outside the core `CanvasContext`).
- Consider an optional retained “drawing list” mode for very dynamic scenes (to reduce redraw code), while keeping the
  current immediate-mode `Painter` as the fast/low-overhead default.
