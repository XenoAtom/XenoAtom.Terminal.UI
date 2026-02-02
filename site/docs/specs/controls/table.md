---
title: Table Specs
---

# Table Specs

This document captures design and implementation notes for `Table`.

> [!NOTE]
> For end-user usage and examples, see [Table](../../controls/table.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Layout and render a grid of header cells and row cells, with optional borders/separators.
- **Content model**: Each cell is a `Visual` (not just text), so tables can embed composed UIs (e.g. `VStack`, `TextArea`, badges).
- **Sizing strategy**:
  - columns size to the max desired width across header + rows (then fit to available width)
  - rows size to the max desired height across cells **for the chosen column widths**

## Public API surface

### Type

- `Table : Visual` (sealed)

### Bindable properties

- `HeaderCells : VisualList<Visual>`
  - header row cells; can be empty (no header row)
- `RowCells : BindableList<VisualList<Visual>>`
  - a list of rows; each row is a `VisualList<Visual>` owned by the table
- `ShowHeaderSeparator : bool`
  - additional toggle layered on top of `TableStyle.ShowHeaderSeparator`

### Fluent helpers

Most call sites use `VisualExtensions` helpers:

- `Headers(params Visual[] headers)` / `AddHeader(Visual header)`
- `AddRow(params Visual[] cells)` / `Rows(params Visual[][] rows)`

These helpers create row lists with the correct owner (`new VisualList<Visual>(table, ...)`) so `RowCells` validation passes.

## Layout & rendering

### Measure

Measure computes the intrinsic “grid” size in three phases:

1) **Determine column count**: `max(HeaderCells.Count, max(RowCells[r].Count))`.
2) **Compute per-column desired widths**:
   - measure every header/row cell with `LayoutConstraints.Unbounded`
   - take `max(cell.DesiredSize.Width)` per column
3) **Fit widths to available**:
   - `FitColumnWidthsToWidth(widths, constraints.MaxWidth, ...)` with `expandToAvailable: false`
   - if required width exceeds available, columns are clamped to an equal “per-column” width

Then it computes row heights:

- cells are re-measured with `MaxWidth = columnWidth` (unbounded height)
- row height is `max(cell.DesiredSize.Height)` across columns plus `TableStyle.CellPadding.Vertical`
- header row height is computed similarly

The resulting fixed desired size includes:

- `TableStyle.CellPadding` and separator widths
- optional outer border
- optional header separator / row separators

### Arrange

Arrange re-fits the cached column widths to the final width:

- `FitColumnWidthsToWidth(..., expandToAvailable: true)`
  - if there is extra width, it is added to the **last column**

Cells are then arranged in row order:

- each cell is given a `Rectangle` that is the column content width and row content height (after padding)
- if vertical separators are enabled, an extra 1-cell column is reserved between columns
- if row separators are enabled, an extra 1-cell row is reserved between rows

### Render

Rendering draws the table “chrome” (backgrounds and separators); cells render themselves as children.

`Table` uses:

- `LineGlyphs` (from `TableStyle.Glyphs` or `Theme.Lines`) for borders/separators
- `TableStyle.ResolveBorderStyle(theme, focused)` for lines/borders (`focused = HasFocusWithin`)
- `TableStyle.ResolveCellStyle(theme)` for body background fill
- `TableStyle.ResolveHeaderStyle(theme)` for header background fill

Render order:

1) outer border top line (optional)
2) header row area fill (optional)
3) header separator line (optional)
4) each row area fill + row separators (optional)
5) outer border bottom line (optional)

## Validation & safety

- `RowCells` only accepts rows whose `VisualOwner` is the `Table` instance; otherwise it throws.
- Removing a row clears it to detach its children.

## Styling

### TableStyle

Key knobs:

- chrome toggles: `ShowOuterBorder`, `ShowVerticalLines`, `ShowRowSeparators`, `ShowHeaderSeparator`
- `CellPadding` (applied inside every cell)
- optional overrides:
  - `Glyphs` (single/rounded/double line sets)
  - `CellStyle`, `HeaderStyle`, `BorderStyle`

Provided presets:

- `TableStyle.Minimal`, `TableStyle.Grid`, `TableStyle.RoundedGrid`, `TableStyle.DoubleGrid`

## Tests & demos

- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/TableRenderingTests.cs` (including multi-line cell content)
- Demo:
  - ControlsDemo includes multiple table examples (including composed cells).

## Future / v2 ideas

- Support per-column alignment and per-column width policies (fixed, min/max, proportional).
- Support column spanning / row spanning for richer layouts.
