---
title: Border Specs
---

# Border Specs

This document captures design and implementation notes for `Border`.

> [!NOTE]
> For end-user usage and examples, see [Border](../../controls/border.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Draw a 1-cell border around a single content visual.
- **Content model**: `Border` is a `Padder`, so it also supports user `Padding` around the content *inside* the border.
- **Focus feedback**: by default, the border highlights when focus is within the border subtree.

## Public API surface

### Type

- `Border : Padder`

### Constructors

- `Border()`
- `Border(Visual content)`
- `Border(Func<Visual> contentFactory)`
  - The factory is evaluated during dynamic updates to produce/rebuild content lazily.

### Inherited bindables

From `Padder` / `ContentVisual`:

- `Padding : Thickness` (bindable)
- `Content : Visual?`

## Layout & rendering

### Layout behavior

`Border` inherits all measurement/arrangement behavior from `Padder`, and reserves space for its chrome via:

- `Inset = new Thickness(1)`

Effective padding used by `Padder` is:

- `Padding + Inset`

This means:

- The border always consumes 1 cell on each side (left/top/right/bottom).
- Additional `Padding` is applied *inside* that border.

### Render

Rendering is purely visual (no input handling) and consists of:

- **Background fill**: the full `Bounds` (including border and interior) is filled with spaces using:
  - `BorderStyle.BackgroundStyle` when provided, otherwise `Style.None`.
- **Border glyphs**:
  - The border uses `BorderStyle.Glyphs` if set, otherwise `theme.Lines`.
  - Corners (`TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`) are drawn first.
  - Then:
    - top/bottom edges are filled with `Horizontal`
    - left/right edges are filled with `Vertical`
- **Border style selection**:
  - When `BorderStyle.HighlightOnFocusWithin == true` (default), the border uses a “focused” border style when `HasFocusWithin` is true.
  - Otherwise it always uses the non-focused style.

> [!NOTE]
> `Border` does not clip differently than other visuals. The framework already clips every visual to its bounds, so the
> content cannot draw over the border.

## Input & commands

`Border` does not handle input and does not expose commands. It is purely a visual chrome/layout primitive.

## Styling

### BorderStyle

Resolved from the environment via `BorderStyle.Key`:

- `Glyphs : LineGlyphs?`
  - When null, uses `Theme.Lines`.
- `HighlightOnFocusWithin : bool` (default: `true`)
- `BorderCellStyle : Style?`
- `FocusedBorderCellStyle : Style?`
- `BackgroundStyle : Style?`

Predefined convenience styles:

- `BorderStyle.Single`
- `BorderStyle.Rounded`
- `BorderStyle.Double`
- `BorderStyle.Heavy`
- `BorderStyle.Ascii`
- `BorderStyle.AsciiHeavy`
- `BorderStyle.Dashed`

### Theme integration

When explicit styles are not provided:

- non-focused borders use `theme.BorderStyle(focused: false)`
- focused-within borders use `theme.BorderStyle(focused: true)`

## Tests & demos

Tests that lock down current behavior:

- `src/XenoAtom.Terminal.UI.Tests/PadderMeasureArrangeTests.cs` (border inset sizing)
- `src/XenoAtom.Terminal.UI.Tests/BorderGroupGlyphStyleRenderTests.cs` (ASCII glyph set rendering)

## Future / v2 ideas

- Support “title” / caption variants directly (today this is handled by `Group`).
- Allow per-edge drawing (e.g. border only on top/bottom) as an opt-in style.
- Consider a “background only” chrome variant (fill + inset) without glyphs for lightweight panels.
