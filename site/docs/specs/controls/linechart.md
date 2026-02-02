---
title: LineChart Specs
---

# LineChart Specs

This document specifies the current behavior and design of the `LineChart` control as implemented.

> [!NOTE]
> For end-user usage and examples, see [LineChart](../../controls/linechart.md).

## Goals

- Provide a simple, terminal-friendly trend visualization for a single series of numeric values.
- Support optional fixed scale (`Minimum`/`Maximum`) or auto scaling from data.
- Keep rendering deterministic and lightweight (no axes/labels in v1).

## Non-goals (current implementation)

- Multi-series charts, legends, gridlines, axis labels (not implemented).
- Interpolated lines between points (the current renderer plots one point per column).
- Interaction/selection (display-only).

## Public surface (v1)

- `Values : BindableList<double>`
- `Minimum : double?`
- `Maximum : double?`

## Defaults

- Default height preference is 4 rows in measure when unconstrained (see below).
- Default point glyph is `LineChartStyle.PointGlyph` (U+2022 BULLET).

## Implementation map

- Control: `src/XenoAtom.Terminal.UI/Controls/LineChart.cs`
- Style: `src/XenoAtom.Terminal.UI/Styling/LineChartStyle.cs`
- Demo: `samples/ControlsDemo/Demos/LineChartDemo.cs`
- Tests: `src/XenoAtom.Terminal.UI.Tests/VisualizationTests.cs` (basic point rendering)

## Layout

`LineChart` is a render-only control (it does not have children).

### Measure

- If `Values.Count == 0`, desired size is `Size.Zero`.
- Otherwise:
  - `width = min(constraints.MaxWidth, max(1, Values.Count))`
  - `height = min(constraints.MaxHeight, max(1, 4))`

This means:

- The chart naturally wants to be at least 4 rows tall (when possible).
- When the available width is smaller than the number of values, multiple values are aggregated per column during rendering.

## Rendering

### Scale resolution

- If `Minimum` and/or `Maximum` are not set, the chart scans `Values` and computes min/max from finite values.
- `NaN` and `Infinity` values are ignored for min/max computation.
- If all values are non-finite, the chart renders nothing.
- If `max <= min`, `max` is forced to `min + 1` to avoid division by zero.

### Column sampling

For each output column `x` in the arranged width:

- Determine the covered source indices:
  - `start = (x * count) / width`
  - `end = ((x + 1) * count) / width` (at least `start + 1`)
- Compute a column sample as the maximum finite value in `[start..end)`.
  - If the range contains no finite values, the sample falls back to `min`.

The sample is normalized:

- `t = clamp((sample - min) / (max - min), 0..1)`

Then mapped to a row:

- `y = rect.Y + round((1 - t) * (height - 1))`

The point is drawn with `LineChartStyle.PointGlyph` and `LineChartStyle.ResolvePointStyle(theme)`.

> [!NOTE]
> The sampling uses max-per-bucket. This is a simple way to preserve peaks when downsampling, but it means the control
> is closer to a compact "trend/peak" plot than a mathematically interpolated line.

## Styling

`LineChartStyle` provides:

- `PointGlyph` (defaults to U+2022 BULLET)
- `PointStyle` (optional; otherwise uses theme foreground and `theme.Accent` when available)

## Input handling

None.

## Tests and demos

- `LineChartDemo` shows a basic series rendered at a fixed height.
- `VisualizationTests.LineChart_Renders_Points` asserts that the point glyph is present in output.

## Future ideas

- Render connecting segments (line interpolation) and optionally fill under the curve.
- Add optional axes/labels and support for multiple series.
- Add alternate sampling modes (min/max/avg) for downsampling.
