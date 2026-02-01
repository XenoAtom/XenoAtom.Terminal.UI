---
title: WrapStack controls specs (WrapHStack / WrapVStack)
---

# WrapStack controls specs (WrapHStack / WrapVStack)

This document specifies *wrapping stack* layout controls for XenoAtom.Terminal.UI:

- `WrapHStack` (horizontal flow layout, wraps into rows)
- `WrapVStack` (vertical flow layout, wraps into columns)

The goal is to generalize the “wrap row packing” logic currently used internally by `BreakdownChart` for its legend, and provide a reusable layout primitive.

Design goals:

- **Idiomatic to this framework**: retained visuals, `Panel` + `VisualList<Visual> Children`, `[Bindable]` properties, automatic dependency tracking.
- **Layout-protocol compliant**: `LayoutConstraints` + `SizeHints`, `LayoutConstants.Infinite` sentinel, no “infinite DesiredSize”.
- **Allocation-conscious**: compute run metadata without re-parenting child visuals or creating intermediate row/column visuals.

---

## Prerequisites (already in the codebase)

### Alignment

XenoAtom.Terminal.UI uses a single `Align` enum for both `HorizontalAlignment` and `VerticalAlignment` on `Visual`.

WrapStack controls MUST rely on child *self-alignment* being applied during `Visual.Arrange(...)` (i.e., children are arranged into a *slot rectangle* and `Align.Start/Center/End/Stretch` positions/sizes them within that slot).

### Clipping

All visuals are clipped to their `Bounds` automatically (`CellBuffer.PushClip(Bounds)` in `Visual.RenderTree`). Therefore WrapStack controls MUST NOT expose an overflow mode; children that don’t fit are clipped.

### Flex allocation helper

The codebase already provides `Layout.FlexAllocator.Allocate(...)` to distribute available main-axis size across items based on per-axis:

- `Min / Natural / Max`
- `FlexGrow / FlexShrink`

WrapStack SHOULD reuse `FlexAllocator` per run so “stretch” and “shrink” behave consistently with `HStack` / `VStack`.

---

## Public API

### Types

Provide two concrete panels (no `Orientation` switch, consistent with `HStack`/`VStack`, `HScrollBar`/`VScrollBar`, etc.):

```csharp
public sealed partial class WrapHStack : Panel;
public sealed partial class WrapVStack : Panel;
```

Implementation MAY use an internal shared base (e.g. `WrapStackBase : Panel`) but the public surface MUST remain two explicit controls.

### Bindable properties (shared)

All properties below MUST be `[Bindable]` so the source generator produces fluent extensions.

#### Spacing

- `int Spacing { get; set; }`  
  Spacing between items in the same run (row/column). Values < 0 are treated as 0.

- `int RunSpacing { get; set; }`  
  Spacing between runs. Values < 0 are treated as 0.

#### Justification (main axis within a run)

Expose a “CSS/Flutter-like” justification model, but keep it scoped to a run:

```csharp
public enum WrapJustify
{
    Start,
    Center,
    End,
    SpaceBetween,
    SpaceAround,
    SpaceEvenly,
}
```

- `WrapJustify Justify { get; set; } = WrapJustify.Start;`

Notes:

- Justification is applied **after** flex allocation in the run.
- Negative leftover space (run content wider than available) MUST behave like `Start` (no negative spacing).

#### Measure mode (main axis)

WrapStack needs to decide how children are measured on the *main axis*:

```csharp
public enum WrapMeasureMode
{
    ConstrainToRun,
    Unconstrained,
}
```

- `WrapMeasureMode MeasureMode { get; set; } = WrapMeasureMode.ConstrainToRun;`

`ConstrainToRun` is the default because it produces correct measurements for wrapping text (height depends on available width).

### Defaults

- `WrapHStack` constructor SHOULD set `HorizontalAlignment = Align.Start` (shrink-wrap by default, similar to `HStack`).
- `WrapVStack` constructor SHOULD set `VerticalAlignment = Align.Start` (shrink-wrap by default, similar to `VStack`).

---

## Layout terminology

### Axes

WrapHStack:

- `main = X` (width)
- `cross = Y` (height)

WrapVStack:

- `main = Y` (height)
- `cross = X` (width)

### Run

A run is a consecutive sequence of children placed along the main axis until the next child would exceed the available main-axis size.

Per run:

- `runMainAllocated = sum(itemMainAllocated) + Spacing*(n-1)`
- `runCrossNatural = max(itemCrossNatural)`

---

## Measure specification

WrapStack MUST implement the layout protocol by overriding `MeasureCore(in LayoutConstraints)` and returning `SizeHints`.

### Inputs

Let:

- `maxMain` be `constraints.MaxWidth` (WrapHStack) or `constraints.MaxHeight` (WrapVStack)
- `maxCross` be the other axis max

`LayoutConstants.Infinite` means “unbounded”.

### Child measurement

For each child in `Children` order:

1. Derive `childConstraints`:
   - main axis max:
     - `MeasureMode == ConstrainToRun` → `maxMain`
     - `MeasureMode == Unconstrained`  → `LayoutConstants.Infinite`
   - cross axis min/max passes through from `constraints` (same patterns as `HStack`/`VStack`).
   - concrete mapping:
     - WrapHStack: `new LayoutConstraints(0, childMaxWidth, constraints.MinHeight, constraints.MaxHeight)`
     - WrapVStack: `new LayoutConstraints(constraints.MinWidth, constraints.MaxWidth, 0, childMaxHeight)`
2. Call `child.Measure(childConstraints)` to obtain the child’s `SizeHints`.

### Run building

WrapStack MUST build runs greedily, based on children’s measured **natural** main-axis size:

- WrapHStack: `childMain = child.MeasureHints.Natural.Width`
- WrapVStack: `childMain = child.MeasureHints.Natural.Height`

When adding a child to the current run:

- `candidate = runMainNatural + (runCount > 0 ? Spacing : 0) + childMain`

Rules:

- If `maxMain` is unbounded (`LayoutConstants.Infinite`), wrapping is disabled → a single run is built.
- If `candidate > maxMain` and the current run already has at least one item, finalize the run and start a new run.
- A single oversized item MUST still be placed (never create an empty run).

### Panel size hints

The panel’s `Natural` size is derived from the runs built at the current `maxMain`:

- WrapHStack:
  - `Natural.Width  = max(runMainNatural)` (clamped to `constraints.MaxWidth` when bounded)
  - `Natural.Height = sum(runCrossNatural) + RunSpacing*(runCount - 1)`
- WrapVStack:
  - `Natural.Height = max(runMainNatural)` (clamped to `constraints.MaxHeight` when bounded)
  - `Natural.Width  = sum(runCrossNatural) + RunSpacing*(runCount - 1)`

`Min` / `Max`:

- WrapHStack:
  - `Min.Width` SHOULD be `max(child.MeasureHints.Min.Width)` clamped to `<= Natural.Width`.
  - `Min.Height` SHOULD be derived from runs:
    - per run `runCrossMin = max(child.MeasureHints.Min.Height)`
    - `Min.Height = sum(runCrossMin) + RunSpacing*(runCount - 1)` clamped to `<= Natural.Height`.
  - `Max.Width` MAY be `LayoutConstants.Infinite` (recommended for v1).
  - `Max.Height` MAY be `Natural.Height` (recommended for v1), unless any child max height is infinite, in which case it MAY be `LayoutConstants.Infinite`.
- WrapVStack:
  - `Min.Height` SHOULD be `max(child.MeasureHints.Min.Height)` clamped to `<= Natural.Height`.
  - `Min.Width` SHOULD be derived from runs:
    - per run `runCrossMin = max(child.MeasureHints.Min.Width)`
    - `Min.Width = sum(runCrossMin) + RunSpacing*(runCount - 1)` clamped to `<= Natural.Width`.
  - `Max.Height` MAY be `LayoutConstants.Infinite` (recommended for v1).
  - `Max.Width` MAY be `Natural.Width` (recommended for v1), unless any child max width is infinite, in which case it MAY be `LayoutConstants.Infinite`.

Flex:

WrapStack MUST represent “fill” at the panel level using its own alignment (same as base `Visual.MeasureCore` behavior):

- `growX = HorizontalAlignment == Align.Stretch ? 1 : 0`
- `growY = VerticalAlignment == Align.Stretch ? 1 : 0`
- `shrinkX/shrinkY` MAY be `1` when `Natural > Min` on that axis.

---

## Arrange specification

WrapStack MUST override `ArrangeCore(in Rectangle finalRect)` and position children without creating intermediate row/column visuals.

### Reflow on arrange

The panel MUST be able to reflow based on the arranged size, not just the measured constraints (same principle as `BreakdownLegend.EnsureRows`).

Specifically:

- If the last run layout was computed at a different main-axis size than the current final rect:
  - WrapHStack: `finalRect.Width`
  - WrapVStack: `finalRect.Height`
  the panel MUST rebuild its run list in `ArrangeCore` using that main-axis size.

This ensures correct behavior when a parent measured unbounded (extent discovery) but arranges bounded (viewport).

### Per-run flex allocation (main axis)

For each run, determine:

- WrapHStack: `slotMain = finalRect.Width`
- WrapVStack: `slotMain = finalRect.Height`
- `availableForItems = max(0, slotMain - Spacing*(n-1))`

Allocate `itemMainAllocated[i]` for run items using `FlexAllocator` on the main axis:

- WrapHStack:
  - `min[i] = child.MeasureHints.Min.Width`
  - `natural[i] = child.MeasureHints.Natural.Width`
  - `max[i] = child.MeasureHints.Max.Width`
  - `grow[i] = child.MeasureHints.FlexGrowX`
  - `shrink[i] = child.MeasureHints.FlexShrinkX`
- WrapVStack:
  - `min[i] = child.MeasureHints.Min.Height`
  - `natural[i] = child.MeasureHints.Natural.Height`
  - `max[i] = child.MeasureHints.Max.Height`
  - `grow[i] = child.MeasureHints.FlexGrowY`
  - `shrink[i] = child.MeasureHints.FlexShrinkY`

This MUST be performed per run (not globally) so “stretch” items can fill the remaining space of their row/column.

### Justification (main axis)

After allocation, compute leftover:

- `leftover = slotMain - (sum(itemMainAllocated) + Spacing*(n-1))`

If `leftover <= 0`, behave like `WrapJustify.Start`.

Otherwise adjust start offset and/or spacing:

- `Start`: `offset=0`, `gap=Spacing`
- `Center`: `offset=leftover/2`, `gap=Spacing`
- `End`: `offset=leftover`, `gap=Spacing`
- `SpaceBetween` (n>1): `offset=0`, `gap=Spacing + leftover/(n-1)`
- `SpaceAround` (n>0): `gap=Spacing + leftover/n`, `offset=gap/2`
- `SpaceEvenly` (n>0): `gap=Spacing + leftover/(n+1)`, `offset=gap`

Remainder handling SHOULD be deterministic and stable (e.g. distribute +1 to gaps left-to-right).

### Child slots and self alignment

Run cross size is the maximum natural cross size of its children:

- WrapHStack: `runCross = max(child.MeasureHints.Natural.Height)`
- WrapVStack: `runCross = max(child.MeasureHints.Natural.Width)`

Each child receives a slot rectangle:

- main size = `itemMainAllocated`
- cross size = `runCross`

The panel MUST call `child.Arrange(slot)` for each item so the child’s own alignment (`HorizontalAlignment` / `VerticalAlignment`) can apply inside the slot.

Finally advance:

- main cursor += `itemMainAllocated + gap`
- cross cursor += `runCross + RunSpacing` after each run

---

## ScrollViewer considerations

ScrollViewer uses unbounded constraints in scroll directions to discover extent.

This implies:

- WrapHStack measured with `MaxWidth = LayoutConstants.Infinite` will not wrap horizontally (it will produce a single long row), which is correct when horizontal scrolling is enabled.
- WrapHStack measured with bounded width and unbounded height (vertical scrolling) will still wrap normally.

If an app needs wrapping even under an unbounded main axis, it SHOULD set a finite `MaxWidth/MaxHeight` on the WrapStack instance (or wrap it in a container that bounds the axis).

---

## Implementation notes (for this repo)

- Use `Children.Version` (from `VisualList`) plus the relevant bindable properties (`Spacing`, `RunSpacing`, `Justify`, `MeasureMode`) to invalidate cached run metadata.
- Run metadata SHOULD be stored as indices into `Children` (avoid re-parenting or duplicating visuals).
- Temporary arrays needed for `FlexAllocator` SHOULD be stack-allocated for small runs or rented from `ArrayPool<int>` for larger runs (implementation detail).
