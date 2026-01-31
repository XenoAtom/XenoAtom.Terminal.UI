---
title: "Rendering & Performance"
---

# Rendering & Performance

Rendering is done through an intermediate cell buffer and a diff renderer:

- visuals render into a `CellBuffer`
- a diff renderer computes minimal terminal updates
- output is written in a single batched write per frame where possible

This makes rendering:

- deterministic (a frame is always a complete buffer),
- fast (diffing avoids rewriting unchanged areas),
- compatible with both fullscreen and inline/live hosting.

## Render pipeline (high level)

In a typical frame, the app performs:

1. **Dynamic update** (optional): controls may rebuild children based on state.
2. **Prepare**: controls compute internal state needed for layout/render (e.g. scroll versions).
3. **Measure**: controls compute `SizeHints` given `LayoutConstraints`.
4. **Arrange**: controls receive a final `Rectangle` and position children.
5. **Render**: controls paint into the `CellBuffer`.
6. **Host render**: the diff renderer writes ANSI output to the terminal.

> [!IMPORTANT]
> Rendering must be side-effect free. Don’t mutate bindable state in `RenderOverride`.

## Synchronized output (DEC private mode 2026)

Fullscreen and inline rendering use synchronized output to reduce tearing:

- “begin synchronized output” is emitted at the start of a frame
- “end synchronized output” is emitted at the end

## Cursor handling

The framework uses the terminal cursor as the caret for text controls:

- only one cursor is visible at a time
- controls report desired cursor position during rendering

## CellBuffer basics

`CellBuffer` is a 2D grid of cells. Each cell is a tuple of:

- a `Rune` (glyph),
- a `Style` (foreground/background/text decorations),
- optional metadata (e.g. hyperlink token).

Controls render by calling:

- `buffer.SetCell(x, y, rune, style)`
- `buffer.WriteText(x, y, span, style)`

The buffer is clipped by the visual tree while rendering. For performance, many controls start with a quick clip check:

```csharp
if (!buffer.ClipIntersects(rect))
{
    return;
}
```

## Diff renderer

After rendering a frame, the diff renderer compares the new `CellBuffer` to the previous frame and emits only the
necessary updates.

This is why many “live” UIs can run at 60 FPS without flicker: unchanged areas produce no output.

> [!NOTE]
> Some situations force a full repaint (for example after a resize, or when the host can’t safely preserve previous state).

## Alpha-aware colors (RGB + RGBA)

Unlike many terminal UI libraries, XenoAtom.Terminal.UI supports **alpha-aware colors** via `Color.RgbA(r,g,b,a)`.

This enables modern UI effects such as:

- subtle hover overlays,
- soft surfaces,
- dimmed backdrops behind modal dialogs,
- “lifted” panels using translucent highlights.

Internally, alpha colors are **blended** during rendering so that stacked overlays produce stable results. The final
terminal output is still a concrete color per cell (terminals don’t have real alpha), but the blending makes layered UIs
look consistent.

> [!TIP]
> Prefer alpha overlays for “soft” elevation, but avoid stacking many translucent layers over large areas if your UI is
> extremely dynamic; it increases per-cell work.

## Avoiding allocations

The rendering path reuses internal buffers where possible to minimize per-frame allocations.

## Writing fast custom controls

Guidelines:

- Prefer measuring text using `TerminalTextUtility` (handles grapheme width).
- Fill backgrounds explicitly when you want predictable inheritance (`Style.None` can inherit from what you wrote before).
- Use `Span<T>`/`ReadOnlySpan<T>` APIs for text and avoid allocating substrings in render loops.

## Related

- [Binding](./binding.md)
- [Layout](./layout.md)
- [Styling](./styling.md)
