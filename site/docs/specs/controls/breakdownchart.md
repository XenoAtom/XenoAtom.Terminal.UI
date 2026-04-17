---
title: BreakdownChart Specs
---

# BreakdownChart Specs

This document captures design and implementation notes for `BreakdownChart`.

> [!NOTE]
> For end-user usage and examples, see [BreakdownChart](../../controls/breakdownchart.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: A segmented proportional bar (one row) with an optional legend. Useful for “parts of a whole” visualizations.
- **Composition**:
  - optional `Title` visual (top)
  - bar (always 1 row)
  - legend (above or below the bar)
- **Interaction**:
  - hover shows a tooltip per segment
  - click raises a routed event with the segment + index

## Public API surface

### Type

- `BreakdownChart : Visual`

### Bindables

- `Segments : BindableList<BreakdownSegment>` (read-only property)
  - The chart’s segment collection (bindable list with dependency tracking).
- `Title : Visual?`
- `LegendPlacement : BreakdownLegendPlacement` (`Above` / `Below`)
- `ShowPercentages : bool` (default: `true`)
- `ShowValues : bool` (default: `false`)

### Routed events

- `SegmentClicked` (bubble routing)
  - `BreakdownSegmentClickedEventArgs` carries:
    - `Index : int`
    - `Segment : BreakdownSegment`

### Segment model

`BreakdownSegment` is a state container (not a `Visual`) with bindables:

- `Value : double`
- `Label : Visual?`
- `Color : Color?`
- `Tooltip : Visual?`

Segments are attached/detached when added/removed from `Segments` so they can participate in UI element ownership:

- `BreakdownSegment : DispatcherObject, IVisualElement`

## Visual tree composition

Internally the chart hosts two child visuals (and an optional third):

- `BreakdownBar` (renders the segmented row and handles hover/click)
- `BreakdownLegend` (renders the legend, in either compact or expanded layout)
- `Title` is attached as a child when non-null

Child order depends on `LegendPlacement`:

- `Above`: `Title?`, `Legend`, `Bar`
- `Below`: `Title?`, `Bar`, `Legend`

## Layout

### Measure

The chart measures:

- `Title` with the full provided width and height constraints.
- `Bar` with height forced to `1`.
- `Legend` with the remaining height after `Title` + bar.

The natural size is:

- width = `max(TitleWidth, BarWidth, LegendWidth)`
- height = `TitleHeight + 1 + LegendHeight`

Size hints:

- `growX = 1` (the chart likes to stretch horizontally)
- `growY = 0` (height is content-driven)

### Arrange

Arranges:

- optional title at the top
- bar in a 1-row rectangle
- legend in the remaining rectangle

## Bar rendering and segment sizing

The bar draws a single line of segment cells.

### Inputs

From `BreakdownStyle`:

- `FillRune` (default: space) - the rune stamped in segment cells
- `SegmentGap` (default: `1`) - empty cells between segments
- `BarStyle` - base style used for the bar (defaults to `theme.ControlFillStyle()`)

From each `BreakdownSegment`:

- `Value` (negative values are treated as `0` for sizing)
- `Color` (optional override)

### Width distribution algorithm

Given:

- `W = Bounds.Width`
- `gap = max(0, SegmentGap)`
- `n = Segments.Count`

Usable bar width is:

- `usable = max(0, W - gap * max(0, n - 1))`

If `n == 0`, `usable == 0`, or total value is `<= 0`, the bar is filled with `FillRune` using the base style.

Otherwise:

- Compute total: `total = sum(max(0, segment.Value))`
- For each segment:
  - initial width = `floor((value / total) * usable)`
- Distribute remaining cells (due to flooring) **left-to-right**:
  - iterate segments in order, and for each segment with `Value > 0`, add `1` width until remaining is `0`

This guarantees:

- segment widths sum to `usable`
- earlier segments receive the “rounding remainder” first

### Colors

Segment cell background is chosen in order:

1) `segment.Color`
2) `BreakdownStyle.DefaultSegmentColors` (cycled)
3) fallback based on theme tones (Primary/Success/Warning/Error/Accent)

The bar fills:

- each segment with `FillRune` and the chosen background color
- gaps with spaces and base style
- any “tail” cells (if widths don’t cover `Bounds.Width`) with `FillRune` and base style

## Legend rendering

Legend layout is configured by `BreakdownStyle`:

- `LegendLayout : BreakdownLegendLayout`
  - `Compact`: uses `WrapHStack`
  - `Expanded`: uses `VStack` (one item per row)
- `LegendItemSpacing : int` (compact spacing, default `4`)
- `LegendJustify : WrapJustify` (compact justification, default `SpaceBetween`)
- `LegendStyle : Style?` (applied to legend text)
- `LegendMutedStyle : Style?` (applied to suffix text: percentages/values)

### Legend items

Each segment produces a `LegendItem` visual composed of:

- a 1x1 swatch (`■`) in the segment color
- the segment `Label` (via a `ComputedVisual` so changes are tracked)
- an optional suffix `TextBlock` containing:
  - `(NN%)` when `ShowPercentages == true`
  - the formatted raw value when `ShowValues == true`

Formatting uses `Visual.ToStringValue(...)`, which is culture-aware (see `CultureStyle`).

### Legend reflow and recycling

Legend items are **reused**:

- `_items` list grows/shrinks with `Segments.Count`
- switching `LegendLayout` moves the same legend item instances between the wrap layout and the expanded layout

This is important because `BreakdownSegment.Label` is a `Visual` instance: re-creating legend items while reusing the
same label instances would trigger “visual already has a parent” failures.

## Interaction: hover tooltips and segment click

### Hover tooltips

Hover is handled by the bar (`BreakdownBar`):

- pointer movement performs a segment hit test based on the same width distribution algorithm used for rendering
- when hovered segment changes, a tooltip window is updated/shown

Tooltip content selection:

1) `BreakdownSegment.Tooltip` if provided
2) otherwise a default tooltip is created:
   - `Label` (if non-null)
   - a line with `Value (Percent%)`

> [!IMPORTANT]
> The default tooltip currently *reuses* `BreakdownSegment.Label` (a `Visual`) inside the tooltip tree.
> A visual cannot have two parents, so if you need the legend label and the tooltip label simultaneously, provide
> an explicit `BreakdownSegment.Tooltip` instead of relying on the default tooltip.

Tooltips are shown via `TooltipWindow`:

- anchored at the last pointer UI cell (`AnchorRect = 1x1`)
- placed above the pointer with a 1-cell offset

### Animation hook (tooltip lifetime)

While a tooltip is visible, the bar registers as an `IAnimatedVisual` and requests animation ticks so it can close the
tooltip when hover is lost (even if no further mouse events arrive).

### Segment click

Click selection is press/release-based:

- on left press inside the bar: record the pressed segment index
- on left release:
  - if released inside and hit-tests to the same segment index, raise `SegmentClicked`
  - otherwise do nothing

## Styling

`BreakdownStyle` is resolved from the environment via `BreakdownStyle.Key`.

Defaults:

- `FillRune = ' '` (segments are primarily background-color blocks)
- `SegmentGap = 1`
- `LegendLayout = Compact`
- `LegendItemSpacing = 4`
- `LegendJustify = SpaceBetween`

## Tests & demos

Tests that lock down current behavior:

- `src/XenoAtom.Terminal.UI.Tests/BreakdownTests.cs`
  - legend reflow stability (no reparent exceptions)
  - left-to-right remainder distribution
  - `SegmentClicked` routed event

## Future / v2 ideas

- Avoid reusing `BreakdownSegment.Label` for the default tooltip (clone or render as text) to prevent parent conflicts.
- Add keyboard navigation and activation (segments as focusable targets) for non-mouse terminals.
- Add an optional “selected segment” state (with style override) for dashboard use-cases.
