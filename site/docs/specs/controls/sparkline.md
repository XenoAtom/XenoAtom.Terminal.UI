---
title: Sparkline Specs
---

# Sparkline Specs

This document captures design and implementation notes for `Sparkline`.

> [!NOTE]
> For end-user usage and examples, see [Sparkline](../../controls/sparkline.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Render a compact, one-row trend visualization from a numeric series.
- **Key goals**:
  - be cheap to render (single row, no child visuals)
  - scale automatically by default, with optional explicit min/max
  - preserve spikes when downsampling to a smaller width
  - avoid per-render allocations
- **Non-goals**:
  - multi-row sparklines (use `LineChart` / `Canvas` for richer charts)
  - interactive input/hover markers in v1

## Implementation notes

- Primary implementation:
  - `src/XenoAtom.Terminal.UI/Controls/Sparkline.cs`
  - `src/XenoAtom.Terminal.UI/Styling/SparklineStyle.cs`
- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/VisualizationTests.cs` (sparkline rendering)
- Demo:
  - `samples/ControlsDemo/Demos/SparklineDemo.cs`

## Public API surface

### Type

- `Sparkline : Visual` (sealed)

### Properties

- `Values : BindableList<double>`
  - The source series.
  - The list instance is created in the constructor and owned by the control.
- `Minimum : double?` (bindable)
  - Optional explicit minimum used for scaling.
- `Maximum : double?` (bindable)
  - Optional explicit maximum used for scaling.

There are no events and no input handling.

## Layout and rendering

### Measure

The sparkline measures as a one-row fixed-size visual:
- height is always 1
- natural width is based on `Values.Count`

Implementation details:
- `MeasureCore` returns `SizeHints.Fixed(constraints.Clamp(new Size(max(0, Values.Count), 1)))`.
- In practice, the arranged width can be smaller or larger depending on parent layout constraints and alignment.

### Render

The sparkline renders into `Bounds` (single row) and uses `SparklineStyle` for:
- glyph selection (`SparklineStyle.Glyphs`)
- cell style (`SparklineStyle.Resolve(theme)`)

#### Scaling

Scaling uses a min/max range:
- if `Minimum` and/or `Maximum` are not provided, they are computed from `Values` by scanning the list once
- `NaN` and infinities are ignored during min/max computation and sampling
- if no finite values exist, the sparkline renders nothing
- if `max <= min`, the code forces `max = min + 1.0` to avoid division by zero

#### Downsampling and spike preservation

When `Bounds.Width` is smaller than `Values.Count`, the sparkline down-samples values into buckets.

For each output column `x`:
- compute the input bucket range:
  - `start = (x * count) / width`
  - `end = ((x + 1) * count) / width`
  - if `end <= start`, clamp `end = min(count, start + 1)`
- take the maximum value in that bucket (ignoring non-finite values)

Using the bucket maximum preserves spikes: a single high sample will still show up in the downsampled output.

When `Bounds.Width` is larger than `Values.Count`, the integer mapping causes some buckets to repeat the same input
indices, effectively stretching the series horizontally.

#### Level mapping

Each sampled value is normalized to a [0..1] range and mapped to 8 levels:
- `t = (sample - min) / (max - min)` clamped to [0..1]
- `level = round(t * 7)` (0..7)
- `rune = glyphs.GetLevel(level)`

Note: the default glyph set (`Blocks8`) uses non-ASCII block glyphs in code. Specs and docs should avoid embedding those
glyph characters directly; refer to glyph set names instead.

## Styling

### SparklineStyle

`SparklineStyle` controls:
- `Glyphs : SparklineGlyphs` (default: `Blocks8`)
- `Style : Style?` override

`SparklineStyle.Resolve(theme)` returns:
- `Style` if explicitly provided
- otherwise, `theme.ForegroundTextStyle()` with `theme.Accent` as the foreground when available

The sparkline does not fill background cells; it only sets the single rune per output column with the resolved style.

## Tests

`VisualizationTests.Sparkline_Renders_Block_Glyphs` validates that rendering produces block glyph output by checking for
the presence of the expected code points (e.g., U+2581..U+2588) when using the default glyph set.

## Future / v2 ideas

- Optional alternative downsampling modes (min/avg) and/or dual sampling to show min/max ranges.
- Optional per-level coloring (thresholds or gradients) as part of `SparklineStyle`.
- Multi-row sparkline variants (at that point, likely a separate control rather than extending `Sparkline`).
