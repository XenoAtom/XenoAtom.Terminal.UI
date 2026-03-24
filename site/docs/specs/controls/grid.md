---
title: Grid Specs
---

# Grid Specs

This document specifies the current behavior and design of the `Grid` control as implemented.

> [!NOTE]
> For end-user usage and examples, see [Grid](../../controls/grid.md).

## Goals

- Provide a retained-mode rows/columns layout container with deterministic integer sizing.
- Support common track sizing modes: `Fixed(n)`, `Auto`, `Star(weight)`, `FlexStar(weight)`.
- Support per-track `Min`/`Max` constraints.
- Support padding and row/column gaps.
- Support cell placement with row/column indices and spans.
- Support optional implicit growth (auto-add rows/columns as needed).

## Non-goals (current implementation)

- Named lines/areas (not implemented).
- Shared size groups across multiple grids (not implemented).
- Cell spanning influencing track sizing (spans are supported for placement, but span > 1 does not participate in track measurement).
- Special focus navigation ordering (children are enumerated in `Cells` insertion order).

## Public surface (v1)

### `Grid`

- `RowDefinitions : BindableList<RowDefinition>`
- `ColumnDefinitions : BindableList<ColumnDefinition>`
- `Cells : BindableList<GridCell>`
- `Padding : Thickness`
- `RowGap : int`
- `ColumnGap : int`
- `AutoGrowRows : bool` (default `true`)
- `AutoGrowColumns : bool` (default `true`)

### `GridCell`

`GridCell` is a `ContentVisual` that carries placement information.

- `Row : int` (clamped to `>= 0`)
- `Column : int` (clamped to `>= 0`)
- `RowSpan : int` (clamped to `>= 1`)
- `ColumnSpan : int` (clamped to `>= 1`)
- `Content : Visual?` (inherited from `ContentVisual`)

## Defaults

- Default alignment: `HorizontalAlignment = Align.Stretch`, `VerticalAlignment = Align.Stretch`.
- `AutoGrowRows = true`, `AutoGrowColumns = true`.
- When `RowDefinitions` or `ColumnDefinitions` are empty, tracks default to `Star(1)`.

## Implementation map

- Control: `src/XenoAtom.Terminal.UI/Controls/Grid.cs`
- Cell type: `src/XenoAtom.Terminal.UI/Controls/GridCell.cs`
- Allocator used for track sizing: `src/XenoAtom.Terminal.UI/Layout/FlexAllocator.cs`
- Tests: `src/XenoAtom.Terminal.UI.Tests/GridLayoutTests.cs`
- Demo: `samples/ControlsDemo/Demos/GridDemo.cs`

## Track definitions

### Grid length types

`RowDefinition.Height` and `ColumnDefinition.Width` use `GridLength` with:

- `Fixed(n)`: track size is exactly `n` (after min/max clamping).
- `Auto`: track size is derived from the maximum child size in that track (span == 1), with no extra growth.
- `Star(weight)`: weighted remaining-space sizing. On bounded axes it starts from the track min and divides the remaining space by weight.
- `FlexStar(weight)`: content-aware weighted sizing. It tracks intrinsic child size first, then participates in growth when extra space is available.

### Min/Max constraints

- `ColumnDefinition.MinWidth` / `MaxWidth`
- `RowDefinition.MinHeight` / `MaxHeight`

Min is always respected; Max is enforced when finite.

## Effective grid size (implicit growth)

The effective row/column counts are derived from:

- at least `max(1, RowDefinitions.Count)` and `max(1, ColumnDefinitions.Count)`
- plus the maximum `(Row + RowSpan)` / `(Column + ColumnSpan)` requested by any cell

If `AutoGrowRows` is `false`, the grid uses only the definition count (minimum 1) even if cells address rows beyond it.
The same applies to `AutoGrowColumns`.

If definitions exist but are fewer than the effective count, the missing tracks are implicitly `Star(1)`.

## Layout

`Grid` is a layout-only container:

- No rendering (it does not draw backgrounds, lines, or borders).
- No input handling of its own.

### Padding and gaps

- `RowGap` and `ColumnGap` are clamped to `>= 0`.
- Total gaps are:
  - `(rows - 1) * RowGap` and `(cols - 1) * ColumnGap` (when there are at least 2 tracks).
- The inner available space for tracks is the outer constraint minus padding and gaps.

### Measure (high level)

`Grid.Measure` runs a two-pass measure to support width-dependent heights (wrapping):

1. Measure all cells against the available inner size to collect intrinsic requirements for tracks.
2. Allocate column widths, then re-measure cells using the allocated column widths to refine row heights.

Only non-fixed tracks are influenced by cell measurement hints, and only for cells with span == 1.

### Track sizing inputs (per axis)

For each row/column, the grid constructs arrays:

- `min[]`, `natural[]`, `max[]`
- `grow[]`, `shrink[]`

The arrays are initialized from the track definitions:

- `Fixed`: min/natural/max are set to the fixed size (clamped to min/max); grow/shrink are 0.
- `Auto` / `Star` / `FlexStar`:
  - initial `min` and `natural` are the track min (MinWidth/MinHeight)
  - `max` is MaxWidth/MaxHeight (or infinite), normalized to be at least min
  - `grow` is 0 for Auto, and `>= 1` for Star/FlexStar (from the weight with a default of 1)
  - `shrink` is 1

Then, after measuring cells:

- For each cell with `ColumnSpan == 1`, the column's `min` is updated to the max of the cell min hint.
- For each cell with `RowSpan == 1`, the row's `min` is updated to the max of the cell min hint.
- `Auto` and `FlexStar` also update `natural` from child natural hints.
- `Star` updates `natural` from child natural hints only when the measured axis is unbounded; on bounded axes it keeps `natural == min` so child natural widths/heights do not bias the ratio.

The grid then normalizes tracks so `natural` is clamped to `[min..max]` when max is finite.

### Allocation (Arrange)

During arrange, `FlexAllocator.Allocate(...)` computes the final integer sizes for each track:

- If there is extra space, it is distributed to Star and FlexStar tracks by their weights.
- If space is constrained, tracks shrink toward their `min` according to `shrink` weights.

After allocation, the grid computes offsets using padding + gaps, then arranges each cell into its spanned rect.

### Star tracks in unbounded measure

When measuring with an unbounded width (max width is infinite), the grid treats Star columns as intrinsic:

- It allocates columns using `availableForColumns = Sum(colNat)` instead of infinite.

This makes the grid report a meaningful desired width in unbounded measure scenarios (see `GridLayoutTests.Unbounded_Measure_Treats_Star_Columns_As_Intrinsic`).

`FlexStar` tracks are also intrinsic on unbounded axes, because they always track child natural size.

## Spanning behavior (current)

Spans affect placement (the arranged rect size), but span > 1 does not contribute to track sizing:

- A spanning cell does not increase `min/natural` of the spanned tracks during measure.
- Track sizing is influenced only by span == 1 cells.

This is a deliberate simplification in the current implementation and is a key limitation to keep in mind when authoring complex grids.

## Styling

There is no `GridStyle`. Styling is achieved by wrapping the grid or individual cells (e.g., `Border`, `Group`, `Rule`), and by styling the cell content visuals.

## Input handling

None. Input routing and focus are handled by child visuals.

## Tests and demos

- `GridLayoutTests` covers:
  - fixed + star allocation
  - FlexStar allocation
  - auto track sizing from child desired sizes
  - implicit track growth when addressing columns beyond the definitions
  - unbounded measure behavior for star/FlexStar columns
  - constrained layouts where auto tracks shrink to preserve star track min width
- `GridDemo` shows typical form-like layouts using the fluent `.Rows(...)`, `.Columns(...)`, and `.Cell(...)` helpers.

## Future / v2 ideas

- Add span-aware sizing so spanning children can influence Auto/Star tracks (multi-pass or constraint solving).
- Add named lines/areas for readability in larger layouts.
- Add shared size groups (useful for property panels and consistent label columns across pages).
- Consider exposing predictable focus navigation ordering (row-major) as an option.
