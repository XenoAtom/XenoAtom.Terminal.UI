---
title: ProgressBar Specs
---

# ProgressBar Specs

This document captures the design and implementation details of `ProgressBar`.

> [!NOTE]
> For end-user usage and examples, see [ProgressBar](../../controls/progressbar.md).

## Overview

- **Status**: Implemented
- **Purpose**: Render a single-line horizontal progress bar (0..100%) directly into the `CellBuffer`.
- **Rendering variants**: thin, solid, segmented, shaded, and bracketed (frame).
- **Input**: none (display-only).

## Goals

- Provide a lightweight, allocation-free control for frequently-updating progress values.
- Support multiple visual variants via style presets.
- Keep layout behavior simple and predictable in terminal cell units.

## Non-goals

- Vertical progress bars.
- Built-in labels, spinners, or multi-row layouts (use `ProgressTaskGroup` for composed progress rows).
- Accessibility semantics beyond what the host terminal provides.

## Public API

`ProgressBar` is a `Visual` with:

- `Value : double` (bindable) - progress value in range [0..1]. Values are clamped during rendering.

Defaults:

- `HorizontalAlignment = Align.Stretch` (set in constructor).

## Styling

`ProgressBar` uses `ProgressBarStyle` (`src/XenoAtom.Terminal.UI/Styling/ProgressBarStyle.cs`) to control:

- `Variant : ProgressBarVariant` (Thin, Solid, Segmented, Shaded, Bracketed)
- `ShowFrame : bool` and frame glyphs (`FrameLeftGlyph`, `FrameRightGlyph`)
- `FillGlyph` and `TrackGlyph`
- Optional styles: `Filled`, `Unfilled`, `Border`

The style resolves to theme-derived defaults when explicit styles are not provided:

- For thin/segmented variants, "filled" primarily uses a foreground color (theme Primary or FocusBorder).
- For solid/shaded variants, "filled" primarily uses a background color (theme Primary or Selection).
- "unfilled" typically uses a dim border-like style.

## Layout

### Measure

`MeasureCore` requests a single-row bar with a minimum width:

- minimum inner bar width is 10 cells
- if a frame is shown, 2 additional cells are required (left/right frame)

The returned size hints are:

- `Min` = required width x 1
- `Natural` = `Min`
- `Max` = infinite width x 1
- grows horizontally and shrinks horizontally

### Arrange

`ProgressBar` does not override `ArrangeCore`; it renders in its `Bounds`.

## Rendering

Rendering is single-line at `Bounds.Y`:

- If `ShowFrame` is true (or the variant is Bracketed), the left and right frame glyphs are written and the inner bar width is reduced by 2.
- The inner bar is filled based on `Value`:
  - Solid/Thin: fills by whole cells (`Round(width * value)`)
  - Segmented: fills by whole cells plus a partial cell using 1/8 block segments (based on the fractional part)
  - Shaded: uses fixed shaded glyphs for fill/track regardless of `FillGlyph`/`TrackGlyph`

Notes:

- Rendering writes all cells in the bar (either fill glyph or track glyph). It does not rely on background clear.
- Styles are applied per cell based on the resolved filled/unfilled/border styles.

## Input and commands

`ProgressBar` is display-only and does not handle keyboard/mouse input or expose commands.

## Tests, demos, and docs

Tests:

- `src/XenoAtom.Terminal.UI.Tests/InlineHostTests.cs` (used in a live region scenario)
- `src/XenoAtom.Terminal.UI.Tests/TerminalExtensionsTests.cs` (used in `Terminal.Live(...)` scenarios)

Demo:

- `samples/ControlsDemo/Demos/ProgressBarDemo.cs`

User documentation:

- `site/docs/controls/progressbar.md`

## Future / v2 ideas

- Add deterministic rendering tests per variant (including segmented partial cells and frame behavior).
- Consider optional vertical orientation or a separate `VerticalProgressBar` control.
- Consider adding a "pulse/indeterminate" mode (separate from value-based fill).
