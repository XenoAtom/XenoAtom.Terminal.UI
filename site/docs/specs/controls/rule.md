---
title: Rule Specs
---

# Rule Specs

This document captures the design and implementation details of `Rule`.

> [!NOTE]
> For end-user usage and examples, see [Rule](../../controls/rule.md).

## Overview

- **Status**: Implemented
- **Purpose**: Render a single-line horizontal separator with optional labels (start, center, end).
- **Base**: Display-only `Visual` that draws into the `CellBuffer` and arranges optional label visuals.
- **Orientation**: Horizontal only (there is no vertical rule control in v1, though `RuleGlyphs` contains a vertical glyph).

## Goals

- Provide a simple separator that composes with the layout system (stretch/flex width).
- Support labels without requiring custom markup composition:
  - left label
  - centered label (clamped to avoid overlapping left/right labels)
  - right label
- Keep rendering and layout deterministic in terminal cell units.

## Non-goals

- Multi-line rules.
- Vertical rules (use `Border`, `Grid`, or custom drawing in `Canvas` if needed).
- Automatic label truncation or ellipsis behavior (labels are arranged with the width available; if there is no room they are arranged at width 0).

## Public API

`Rule` is a `Visual` with:

- `StartLabel : Visual?` (bindable)
- `CenterLabel : Visual?` (bindable)
- `EndLabel : Visual?` (bindable)

Defaults:

- `HorizontalAlignment = Align.Stretch` (set in constructor)
- Height is always 1 row.

Labels are regular visuals and can be any composable content (for example, `TextBlock`, `Markup`, or a small `HStack`).

## Layout

### Measure

`MeasureCore`:

- Measures labels with unbounded width and height 1.
- Computes a "total width" for each label as:
  - `label.DesiredSize.Width + (LabelPadding * 2)` (clamped to the available width)
- The minimum required width is the sum of the totals for start + center + end, clamped to at least 1.
- Height is always 1.

### Arrange

`ArrangeCore` arranges the labels on a single row:

- Start label is arranged at the left edge.
- End label is arranged at the right edge.
- Center label is arranged near the midpoint but clamped so it does not overlap the reserved space for start/end labels.
- If there is no space for a label (for example, not enough width after padding), it is arranged with width 0.

All arranged label rectangles use the label padding on both sides.

## Rendering

`RenderOverride` draws the rule line and clears a gap behind each arranged label:

1. The full rule width is filled with the horizontal line glyph.
2. For each label with a non-zero arranged width, the rule clears a region that includes the label bounds plus padding.
   This prevents the line glyph from rendering "through" the label content.

Important detail:

- The label gap is filled with space glyphs using `Style.None | TextStyle.Bold`. Labels render on top afterwards.
  The gap fill is intentionally not derived from theme border style, so it behaves like a transparent "erase" of the line.

## Tests & demos

Tests:

- `src/XenoAtom.Terminal.UI.Tests/RuleLayoutTests.cs` (basic label arrangement)

Demo:

- `samples/ControlsDemo/Demos/RuleDemo.cs`

User documentation:

- `site/docs/controls/rule.md`

## Styling

`Rule` uses `RuleStyle` (`src/XenoAtom.Terminal.UI/Styling/RuleStyle.cs`):

- `Glyphs : RuleGlyphs?` (optional override)
- `LineStyle : Style?` (optional override)
- `LabelPadding : int` (defaults to 1)

If `Glyphs` is not specified, `RuleStyle.ResolveGlyphs(theme)` defaults to `theme.Lines.Horizontal`/`theme.Lines.Vertical`.
The vertical glyph is part of the style surface but is not used by `Rule` (horizontal-only control).

`RuleGlyphs` (`src/XenoAtom.Terminal.UI/Styling/RuleGlyphs.cs`) provides predefined glyph sets (Ascii, Single, Double, Heavy, Dashed, Dotted, Block).

## Known limitations

- There is no built-in vertical rule control.
- Labels are arranged with height 1. Multi-line label visuals are not supported.
- Center label does not attempt advanced collision resolution; it is simply clamped between start and end reservations.

## Future ideas

- Add deterministic rendering tests that verify line clearing around labels.
- Consider a `VerticalRule` control or extending `Rule` with an orientation flag (requires layout and rendering changes).
- Add optional background/style control for label gap fill (currently fixed to `Style.None | TextStyle.Bold`).
