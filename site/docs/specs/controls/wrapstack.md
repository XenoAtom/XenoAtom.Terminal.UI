---
title: WrapStack Specs (WrapHStack / WrapVStack)
---

# WrapStack Specs (WrapHStack / WrapVStack)

This document captures design and implementation notes for the wrapping stack layout controls:

- `WrapHStack` (horizontal flow; wraps into rows)
- `WrapVStack` (vertical flow; wraps into columns)

> [!NOTE]
> For end-user usage and examples, see [WrapStack](../../controls/wrapstack.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: A simple “flow layout” primitive for terminal UIs (legends, tag lists, button groups, compact option rows).
- **Key characteristics**:
  - deterministic run building based on children natural sizes
  - optional justification within each run (`Start/Center/End/Space*`)
  - optional unconstrained child measurement on the main axis (`MeasureMode`)
  - allocation-conscious arrange via `ArrayPool<T>`

## Public API surface

### Types

- `WrapHStack : WrapStackBase`
- `WrapVStack : WrapStackBase`

`WrapStackBase` is an internal implementation base (`EditorBrowsable(Never)`); prefer the concrete controls.

### Shared bindables

All values below are `[Bindable]` on `WrapStackBase`:

- `Spacing : int`
  - Space between items in the same run. Negative values are clamped to `0`.
- `RunSpacing : int`
  - Space between runs. Negative values are clamped to `0`.
- `Justify : WrapJustify`
  - How leftover space is distributed along the main axis *within each run*.
- `MeasureMode : WrapMeasureMode`
  - How children are measured on the main axis.

### Enums

`WrapJustify`:

- `Start`
- `Center`
- `End`
- `SpaceBetween`
- `SpaceAround`
- `SpaceEvenly`

`WrapMeasureMode`:

- `ConstrainToRun` (default)
- `Unconstrained`

### Defaults

- `WrapHStack` defaults `HorizontalAlignment = Align.Start` (shrink-wrap on X unless parent stretches it).
- `WrapVStack` defaults `VerticalAlignment = Align.Start` (shrink-wrap on Y unless parent stretches it).
- `Justify = WrapJustify.Start`
- `MeasureMode = WrapMeasureMode.ConstrainToRun`
- `Spacing = 0`, `RunSpacing = 0`

## Layout terminology

Wrap stacks define:

- **main axis**: the direction items are placed within a run
- **cross axis**: the direction runs are stacked

For `WrapHStack`:

- main axis = X (width)
- cross axis = Y (height)

For `WrapVStack`:

- main axis = Y (height)
- cross axis = X (width)

## Measure

### Measuring children

Each child is measured before runs are built:

- If `MeasureMode == Unconstrained`, children are measured with `MaxMain = ∞`.
- If `MeasureMode == ConstrainToRun`, children are measured with `MaxMain = constraints.MaxMain`.

This exists to support text-like visuals where height depends on width:

- `ConstrainToRun`: lets a `TextBlock`/`Markup` wrap to the available width
- `Unconstrained`: lets items keep their intrinsic width and overflow is expected to be clipped

### Run building

Runs are built using **natural** main-axis sizes (`MeasureHints.Natural`):

- Items are appended to the current run while:
  - `runMain + Spacing + childMain <= availableMain`
- Otherwise a new run is started.

Special cases:

- `availableMain <= 0`: each child becomes its own run (deterministic ordering; everything overflows)
- `availableMain == ∞`: all children are placed into a single run

### Size hints computation

For each run, the implementation tracks:

- `MainNatural`, `MainMin`
- `CrossNatural`, `CrossMin`
- whether any child in the run has an infinite max on the cross axis (`MaxCrossInfinite`)

The wrap stack’s reported sizes:

- `NaturalMain = max(run.MainNatural)`
- `NaturalCross = sum(run.CrossNatural) + RunSpacing * (runs - 1)`
- `MinMain = max(run.MainMin)` (clamped to `NaturalMain`)
- `MinCross = sum(run.CrossMin) + RunSpacing * (runs - 1)` (clamped to `NaturalCross`)
- `MaxMain = ∞`
- `MaxCross = ∞` if any run has `MaxCrossInfinite`, otherwise `NaturalCross`

The result is converted back to `(Width, Height)` depending on orientation.

## Arrange

### Remeasuring for ConstrainToRun

`ConstrainToRun` supports a common terminal UI pattern:

- measure with unbounded width (e.g. inside a scroll viewer or when a parent doesn’t know its width yet)
- then arrange with a finite width

If `MeasureMode == ConstrainToRun` and the final main-axis size differs from the main-axis size used when runs were last computed,
children are re-measured with `MaxMain = finalMain` before run building.

### Per-run sizing via FlexAllocator

Within each run, the available main-axis size is:

- `available = finalMain - Spacing * (itemCount - 1)`

The run then allocates per-item sizes using `Layout.FlexAllocator.Allocate(...)` with each child’s:

- `Min`, `Natural`, `Max`
- `FlexGrow`, `FlexShrink`

This makes wrapping stacks consistent with other layout containers for “stretch/shrink” behavior.

The cross-axis size of a run is:

- `runCross = run.CrossNatural` (max natural cross of children in the run)

Children are arranged into slots of:

- `WrapHStack`: `(width = allocatedMain, height = runCross)`
- `WrapVStack`: `(width = runCross, height = allocatedMain)`

Child self-alignment then positions/sizes it inside its slot (`Align.Start/Center/End/Stretch`).

### Justification

After allocation, any remaining space on the main axis is distributed using `Justify`:

- `Start`: no offset; keep `Spacing`
- `Center`: leading offset = `leftover / 2`
- `End`: leading offset = `leftover`
- `SpaceBetween`: add extra to gaps between items (no leading/trailing padding)
- `SpaceEvenly`: distribute including start/end edges
- `SpaceAround`: distribute around items (start/end edges get half of the between-item space)

The implementation keeps a deterministic result without extra allocations by:

- computing a base offset and base gap
- distributing remainder (`leftover % gaps`) as `+1` to the first N gaps

### Allocation strategy

To keep `Arrange` allocation-conscious, the implementation:

- computes the max number of items in any run
- rents integer arrays from `ArrayPool<int>` for:
  - mins, naturals, maxs, grows, shrinks, results
- returns them after arranging all runs

## Performance characteristics

- Run building is `O(n)` over children and uses cached results keyed on:
  - children version (`Children.Version`)
  - main-axis size
  - `Spacing`, `RunSpacing`, `Justify`, `MeasureMode`
- Per-run allocation uses pooled arrays; no per-frame allocations on the hot path for typical sizes.

## Tests & demos

Tests that lock down current behavior:

- `src/XenoAtom.Terminal.UI.Tests/WrapHStackLayoutTests.cs`
- `src/XenoAtom.Terminal.UI.Tests/WrapVStackLayoutTests.cs`

Notable covered scenarios:

- wrapping into new runs with `Spacing` and `RunSpacing`
- justification offsets (`Center`, `End`)
- `MeasureMode.Unconstrained` allowing main-axis overflow

## Future / v2 ideas

- Add an optional “balanced” packing mode (bin packing / near-equal run widths) for some UIs (currently it’s greedy and deterministic).
- Consider exposing cross-axis alignment and/or per-run baseline alignment for text-heavy layouts.
- Consider an opt-in virtualization story for very large child collections (today it always measures and arranges all children).

