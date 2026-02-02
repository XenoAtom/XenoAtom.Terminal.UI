---
title: HStack Specs
---

# HStack Specs

This document captures design and implementation notes for `HStack`.

> [!NOTE]
> For end-user usage and examples, see [HStack](../../controls/hstack.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Arrange children horizontally in a stack with optional spacing.
- **Layout model**:
  - measures width as sum of children widths + spacing
  - measures height as the max child height
  - allocates child widths using the flex allocator (respecting min/natural/max + grow/shrink)

## Public API surface

### Type

- `HStack : Panel` (sealed)

### Constructors

- `HStack()` sets `HorizontalAlignment = Align.Start`.
- `HStack(params Visual[] children)` sets alignment and adds the children.

### Bindable properties

- `Spacing : int`
  - number of blank columns between children (clamped to `>= 0`)

## Layout behavior

### Measure

For `N` children and `spacing = max(0, Spacing)`:

- `totalSpacing = spacing * max(0, N - 1)`
- each child is measured with:
  - height constrained by the parent (`MinHeight/MaxHeight`)
  - width unbounded below (`MinWidth = 0`) and bounded above (`MaxWidth`)
- resulting stack hints:
  - `Min.Width` / `Natural.Width` are the **sum** across children plus `totalSpacing`
  - `Max.Width` is the sum of max widths (or infinite if any child max width is infinite)
  - `Min.Height` / `Natural.Height` / `Max.Height` are based on the **maximum** height across children
  - `FlexGrowX` / `FlexShrinkX` are the sum across children

The result is normalized via `SizeHints.Normalize()`.

### Arrange

Arrange allocates each child width using `FlexAllocator.Allocate(...)`:

- available width = `finalRect.Width - totalSpacing`
- allocator inputs are per-child `MeasureHints` (min/natural/max + grow/shrink on X)
- each child is arranged with:
  - full stack height (`finalRect.Height`)
  - allocated width
  - stacked `x` offsets with `Spacing` columns between children

## Styling & rendering

- `HStack` has no dedicated style and does not render anything itself; children render normally.

## Tests & demos

- `HStack` is exercised broadly through layout protocol tests and ControlsDemo layouts.

## Future / v2 ideas

- Add optional “baseline alignment” for text-heavy stacks (if the text system exposes baselines).
