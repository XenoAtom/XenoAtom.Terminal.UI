---
title: Markup Specs
---

# Markup Specs

This document captures the design and implementation details of `Markup` (the control, not the markup language).

> [!NOTE]
> For end-user usage and examples, see [Markup](../../controls/markup.md).

## Overview

- **Status**: Implemented
- **Purpose**: Render ANSI markup (XenoAtom.Ansi) into styled text directly in a `CellBuffer`.
- **Good fit**: Inline styles (colors, underline, dim, etc.) without building a visual tree of `TextBlock` nodes.

## Goals

- Keep `Markup` as a small, fast, allocation-conscious visual that participates in the binding dirty model.
- Parse markup once and reuse parsed runs across measure and render until the input changes.
- Make measurement based on *visible* text width (markup tags do not contribute to size).

## Non-goals

- Editing, caret, selection, or any input handling (use `TextBox`/`TextArea`/`TextEditorBase`-derived controls).
- Full rich-text layout (no inline images, no bidirectional layout, no justification support).
- Perfect whitespace-preservation when wrapping (wrapping uses whitespace heuristics).

## Public API

`Markup` is a `Visual` with these bindable properties:

- `Text : string?` - Markup source text. `null` is treated as empty.
- `Wrap : bool` - When `true`, wraps hard lines to the available width.
- `TextAlignment : TextAlignment` - Left/Center/Right/Justify (see behavior notes; justify is not implemented).
- `Trimming : TextTrimming` - Clip/EndEllipsis/StartEllipsis (applies only when `Wrap` is `false`).

Constructors:

- `Markup()`
- `Markup(string markup)`
- `Markup(ref AnsiMarkupInterpolatedStringHandler handler)`
- `Markup(Func<string> markup)` (binds `Text`)
- `Markup(Func<AnsiMarkupInterpolatedStringHandler> handler)` (binds `Text`)

## Markup parsing model

`Markup` parses `Text` into:

- `_plainText` (markup stripped)
- `_runs : StyledRun[]` (spans in `_plainText` associated with a `Style`)

The parser is `MarkupTextParser` (`src/XenoAtom.Terminal.UI/Text/MarkupTextParser.cs`), which uses XenoAtom.Ansi (`AnsiMarkup`)
to process markup tags and convert ANSI styles into Terminal.UI `Style` values.

### Theme-provided style tokens

`Markup` uses `GetTheme().GetMarkupStyles()` as the dictionary of custom style tokens (e.g. `[primary]...[/]`).
This lets themes define additional tag names besides the built-in XenoAtom.Ansi set.

### Caching behavior

`Markup` caches parsing using reference equality:

- It re-parses when the `Text` instance reference changes, or when the `GetMarkupStyles()` dictionary instance reference changes.
- This is intentional and avoids string comparisons, but it means that producing a new string instance on every update will
  force a re-parse each time.

This cache is an internal detail and can change, but it is important to understand when analyzing performance.

## Layout

### Measure

`MeasureCore` computes desired size from `_plainText`:

- Width is computed from the maximum hard-line width and clamped to available width.
- When `Wrap` is `false`, height is the number of hard lines (clamped to available height).
- When `Wrap` is `true`, height is the number of wrapped lines (clamped), and width is the wrapping width chosen by measure.

Notes:

- Hard lines are split on `\n`, `\r\n`, and `\r`.
- Width computations use `TerminalTextUtility.GetWidth(...)` to account for terminal cell width.

### Arrange

`Markup` has no custom `ArrangeCore`; it renders in its `Bounds` and does not allocate child visuals.

## Rendering

### General behavior

- `Markup` does not fill its background. It only writes glyphs (with styles) into the `CellBuffer`.
- Unstyled segments (no foreground/background in markup) are written with `Style.None` (transparent, no override).
- Newlines create hard line breaks; there is no vertical scrolling in this control.

### Wrapping

When `Wrap` is `true`:

- Each hard line is wrapped into slices that fit `Bounds.Width`.
- The wrap algorithm prefers to break on whitespace before the width boundary, otherwise it breaks at the boundary.
- Leading whitespace is skipped when computing the next slice; trailing whitespace can effectively be collapsed at wrap points.

### Alignment

- `TextAlignment.Left`, `Center`, and `Right` align each rendered line within `Bounds.Width`.
- `TextAlignment.Justify` is treated as `Left` (there is no justification implementation).

### Trimming

Trimming applies only when `Wrap` is `false` and the hard line exceeds `Bounds.Width`:

- `Clip`: render only the prefix that fits.
- `EndEllipsis`: render the prefix and then an ellipsis in the last cell.
- `StartEllipsis`: render an ellipsis and then the suffix that fits.

Implementation note: the ellipsis cell is currently written using `Style.None` (not the style of the adjacent run).

## Input and commands

`Markup` is display-only and does not expose commands.

## Tests, demos, and docs

Tests:

- `src/XenoAtom.Terminal.UI.Tests/MarkupTextParserTests.cs` (plain text and style runs)
- `src/XenoAtom.Terminal.UI.Tests/MarkupMeasureTests.cs` (measurement and wrapping)
- `src/XenoAtom.Terminal.UI.Tests/MarkupRenderingTests.cs` (hard line breaks)

Demo:

- `samples/ControlsDemo/Demos/MarkupDemo.cs`

User documentation:

- `site/docs/controls/markup.md`

## Future ideas

- Optional: apply run style to the ellipsis cell during trimming (instead of `Style.None`).
- More wrapping modes (character wrapping, preserve indentation, preserve leading whitespace).
- Optional text justification (if a deterministic algorithm is desired for terminals).
- More deterministic rendering tests for trimming/alignment edge cases.
