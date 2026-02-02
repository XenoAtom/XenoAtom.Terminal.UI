---
title: VStack Specs
---

# VStack Specs

This document captures design and implementation notes for `VStack`.

> [!NOTE]
> For end-user usage and examples, see [VStack](../../controls/vstack.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Arrange children vertically in a stack with optional spacing.
- **Layout model**:
  - measures height as sum of children heights + spacing
  - measures width as the max child width
  - allocates child heights using the flex allocator (respecting min/natural/max + grow/shrink)

## Public API surface

### Type

- `VStack : Panel` (sealed)

### Constructors

- `VStack()` sets `VerticalAlignment = Align.Start`.
- `VStack(params Visual[] children)` sets alignment and adds the children.

### Bindable properties

- `Spacing : int`
  - number of blank rows between children (clamped to `>= 0`)

## Layout behavior

### Measure

For `N` children and `spacing = max(0, Spacing)`:

- `totalSpacing = spacing * max(0, N - 1)`
- each child is measured with:
  - width constrained by the parent (`MinWidth/MaxWidth`)
  - height unbounded below (`MinHeight = 0`) and bounded above (`MaxHeight`)
- resulting stack hints:
  - `Min.Width` / `Natural.Width` / `Max.Width` are based on the **maximum** width across children
  - `Min.Height` / `Natural.Height` are the **sum** across children plus `totalSpacing`
  - `Max.Height` is the sum of max heights (or infinite if any child max height is infinite)
  - `FlexGrowY` / `FlexShrinkY` are the sum across children

The result is normalized via `SizeHints.Normalize()`.

### Arrange

Arrange allocates each child height using `FlexAllocator.Allocate(...)`:

- available height = `finalRect.Height - totalSpacing`
- allocator inputs are per-child `MeasureHints` (min/natural/max + grow/shrink on Y)
- each child is arranged with:
  - full stack width (`finalRect.Width`)
  - allocated height
  - stacked `y` offsets with `Spacing` rows between children

## Styling & rendering

- `VStack` has no dedicated style and does not render anything itself; children render normally.

## Tests & demos

- `VStack` is exercised broadly through layout protocol tests and ControlsDemo layouts.

## Future / v2 ideas

- Support per-child cross-axis alignment overrides (beyond what each child already supports via its own alignment).
