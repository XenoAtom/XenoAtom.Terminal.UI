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

## Fine pixel mode (thin drawing)

Some scenarios benefit from drawing at a higher resolution than the cell grid (e.g. thinner lines, smoother diagonals,
sparklines). Terminals support a family of Unicode **8-dot pattern glyphs** (`U+2800..U+28FF`) where each terminal cell
can encode an internal **2×4 dot grid** (8 sub-pixels). By rasterizing shapes into this dot grid and emitting one rune
per cell, Canvas can render “thin” strokes while staying in the normal cell rendering pipeline.

This feature is enabled via a Canvas-level boolean so it is easy to switch on/off without plumbing style records.

### Public API (additive)

Add a bindable boolean directly on `Canvas`:

- `UseFinePixels : bool` (default: `false`)

Naming rationale:

- “Fine pixels” describes the user-visible behavior (higher effective resolution) without naming a specific Unicode
  block or any external library.

### Scope: primitives supported

When `UseFinePixels = true`, **all existing `CanvasContext` drawing primitives should continue to work**, but rasterize
using the fine 2×4 dot grid where relevant:

- `SetPixel(...)`: sets a single dot at the center of the target cell (instead of painting a full block rune).
- `DrawLine(...)`: runs the line rasterizer on the dot grid (produces smoother diagonals).
- `DrawCircle(...)`: rasterizes into dots (higher detail than coarse cell rasterization).
- `DrawBox(...)` / rectangle outlines: can use dots for thinner borders (implementation-defined, but should be
  consistent).
- `FillRect(...)`, `WriteText(...)`: remain cell-based (they are already “full cell” operations).

The intent is that users can toggle `UseFinePixels` without rewriting their painter code.

### Coordinate system (unchanged)

All existing drawing methods keep **cell coordinates**:

- `(0,0)` remains the top-left cell.
- Integers continue to address logical cells.

Internally, fine rasterization maps a cell coordinate `(x, y)` to the dot coordinate at the *center* of the cell:

- `dotX = x * 2 + 1`
- `dotY = y * 4 + 2`

Line endpoints, circle centers, and other primitive inputs use this mapping so the overall layout stays stable across
modes.

### Rendering model

Fine pixel mode can be implemented as a small dot-mask layer inside `Canvas` / `CanvasContext`:

- Maintain a per-cell bitmask (8 bits) representing which dots are lit.
- Convert each non-empty mask to a rune (`0x2800 + mask`) and write it to the `CellBuffer`.

Because terminal cells can only carry a **single** foreground/background style:

- Dot colors within the same cell cannot be represented independently.
- The simplest and most predictable rule: the final style for a cell is the last `Style` written to that cell (or the
  resolved `CanvasStyle` default when omitted).

### Interaction with regular cell drawing

Canvas is immediate-mode and draws directly into the `CellBuffer`. Fine pixel mode should have deterministic ordering:

- Default: cell drawing happens first, and the fine dot mask is flushed at the end of the painter callback (thin strokes
  appear “on top”).
- If a painter needs text on top of thin strokes, it draws the text *after* the primitives that produce thin strokes.

### Compatibility notes

- The 8-dot pattern glyph block is Unicode; it requires a font/terminal that renders these glyphs as single-cell width.
- If a runtime capability check is needed, the behavior should degrade gracefully to coarse cell drawing.

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
- Consider extending fine pixel mode to support:
  - explicit flush ordering (if “flush at end of painter” is insufficient for advanced compositing),
  - configurable per-cell style resolution when multiple dots use different styles,
  - anti-aliasing/dithering helpers for charts (careful: can increase CPU and reduce clarity in terminals).
