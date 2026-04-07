---
title: BarChart Specs
---

# BarChart Specs

This document specifies the current behavior and design of the `BarChart` control as implemented.

> [!NOTE]
> For end-user usage and examples, see [BarChart](../../controls/barchart.md).

## Goals

- Render a compact **horizontal** bar chart suitable for terminals (small height, wide width).
- Support labels and optional value text, with consistent placement rules.
- Support customization via visuals (`Title`, per-item `Label`, per-item `ValueLabel`) without requiring custom drawing.
- Keep runtime cost low (no per-frame allocations in steady-state) and rely on the library binding/dirty model.

## Non-goals

- Vertical bar charts (not implemented).
- Multi-series/stacked bars (not implemented).
- Interaction/selection within the chart (the control is display-only).

## Public surface (v1)

### `BarChart`

- `Items : BindableList<BarChartItem>`
- `Title : Visual?`
- `TitlePlacement : ChartTitlePlacement` (`Above` / `Below`)
- `Minimum : double?` (optional; defaults to `0` when unset)
- `Maximum : double?` (optional; when unset, computed from the max item value)
- `ShowValues : bool` (default `true`)
- `ShowPercentages : bool` (default `false`)

### `BarChartItem`

`BarChartItem` is **not** a `Visual`. It is a bindable state container (`DispatcherObject`, `IVisualElement`) that provides visuals and values used by the owning chart.

- `Label : Visual?` (left column)
- `Value : double` (used for normalization and default value text)
- `ValueLabel : Visual?` (optional; rendered over the bar near the filled end)
- `BarColor : Color?` (optional; per-row color override)

## Composition

- `BarChart` is composed from a `Grid` with **two columns**:
  - Column 0: label (`Auto` width)
  - Column 1: bar cell (`Star` width; stretches to fill remaining space)
- Each item becomes a grid row with:
  - `LabelHost`: a `ComputedVisual(() => item.Label)`
  - `Bar`: a custom `Visual` that renders the bar (internally also hosts `ComputedVisual(() => item.ValueLabel)`)
- Rows are rebuilt when `Items.Version` changes (add/remove operations).

## Layout

### Measure

- The chart measures `Title` (when non-null), then measures the internal grid with the remaining height.
- The returned size hints are “flex” in X:
  - `min` width is `max(title.Min.Width, grid.Min.Width)`
  - `natural` width is `max(title.Natural.Width, grid.Natural.Width)`
  - Under bounded measurement, the chart keeps its intrinsic minimum width and lets the natural width expand to the offered width when the internal star bar column stretches.
  - `max` width is infinite (allows horizontal stretching)
  - `min/natural/max` height is computed by stacking the title and grid height hints

### Arrange

- If `TitlePlacement` is `Above`, `Title` is arranged at the top and the grid below it.
- If `TitlePlacement` is `Below`, the grid is arranged above and `Title` at the bottom.
- If `Title` is `null`, the grid takes the whole rect.

## Rendering

### Range normalization

Normalization range is resolved as:

- `Min = Minimum ?? 0`
- `Max = Maximum ?? max(Items[i].Value)` over finite values
- If `Max <= Min`, `Max` is forced to `Min + 1` to avoid division by zero.
- `NaN`/`Infinity` values are treated as `Min`.

The normalized progress `t` is computed as `(value - Min) / (Max - Min)` and clamped to `[0..1]`.

### Bar drawing

Each bar row is rendered using `BarChartStyle.BarStyle` (a `ProgressBarStyle`):

- The bar uses a `TrackGlyph` for unfilled cells and `FillGlyph` for filled cells.
- `Variant` affects the fill algorithm:
  - `Solid`: filled cell count is `Round(width * t)`.
  - `Segmented`: uses 1/8 block glyphs for the fractional cell.
  - `Shaded`: uses shaded blocks (`░`/`▓`) for track/fill.
- Frames:
  - If `ShowFrame` is enabled (or the variant implies a frame), the bar consumes 1 cell on each side for frame glyphs.

### Value text

There are two ways to display value text:

- **Default value text**: when `item.ValueLabel` is `null` and either `ShowValues` or `ShowPercentages` is enabled.
  - Values are formatted via the chart’s internal formatting helper.
  - Percentages are computed from the normalized `t` and rendered as `NN%`.
  - When both are enabled, the default is `"<value>  <pct>"`.
- **Custom value visual**: when `item.ValueLabel` is non-null.
  - The value visual is arranged on the bar row near the end of the filled segment (similar to Rich/Spectre behavior).

The default value text is written directly into the `CellBuffer` and is positioned near the bar’s filled end, clamped to remain within the bar rect.

## Styling

### `BarChartStyle`

`BarChart` styling is controlled via `BarChartStyle`:

- `Padding` and `RowSpacing` are applied to the internal grid.
- `BarStyle : ProgressBarStyle` controls glyphs, frame, and track/fill styles for each row.
- `LabelTextStyle` (optional) is applied to the label host via `TextBlockStyle`.
- `ValueTextStyle` (optional) is applied to the value host / default value text.
  - When default value text is used and the style does not specify a foreground, the bar color is used as the text foreground.

### Row colors

The bar color is chosen per row as:

1. `item.BarColor` when set
2. `BarChartStyle.DefaultBarColors[index % n]` when provided
3. Theme-derived fallback tones (`Primary`/`Success`/`Warning`/`Error` rotation)

For “solid-like” variants, the chosen color is applied to the **background**. For other variants, it is applied to the **foreground**.

## Input handling

`BarChart` is display-only:

- No keyboard focus behavior.
- No pointer interactions.
- Any interactive behavior must be composed by providing interactive visuals as `Label` and/or `ValueLabel`.

## Tests and demos

- Rendering-focused tests live in `src/XenoAtom.Terminal.UI.Tests/VisualizationTests.cs` (bar fill + value placement).
- Interactive usage can be seen in `samples/ControlsDemo/Demos/BarChartDemo.cs`.

## Future / v2 ideas

- Vertical bar charts and axis labels (if needed for a terminal-friendly layout).
- Multi-series and stacked bars.
- Optional sorting/grouping and richer label/value layout (e.g. right-aligned labels).
- Optional legend composition as a separate control/visual.
