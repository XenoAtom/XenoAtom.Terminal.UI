---
title: Padder Specs
---

# Padder Specs

This document captures design and implementation notes for `Padder`.

> [!NOTE]
> For end-user usage and examples, see [Padder](../../controls/padder.md).

## Overview

 - **Status**: Implemented
 - **Primary purpose**: Adds padding around a single content visual.
 - **Role in the framework**:
   - lightweight layout primitive for spacing/indent
   - base class for “chrome” controls that need reserved space via an internal inset (e.g. `Border`)

## Public API surface

### Type

- `Padder : ContentVisual`

### Constructors

- `Padder()`
- `Padder(Visual content)`
- `Padder(Func<Visual> contentFactory)`
  - The factory is evaluated during dynamic updates and can rebuild the child when its tracked dependencies change.

### Bindables

- `Padding : Thickness`
  - Applied around the content, in cells. Negative values are clamped to `0` per side when combined with `Inset`.

### Extensibility point

- `protected virtual Thickness Inset`
  - Additional internal padding reserved by derived controls (default: `Thickness.Zero`).

## Layout & rendering

`Padder` only affects layout; it does not render anything itself.

### Effective padding

Internally, `Padder` uses:

- `EffectivePadding = max(0, Padding + Inset)` per side

This allows derived controls to reserve internal chrome space while still allowing user padding.

### Measure

- Computes child constraints by subtracting `EffectivePadding.Horizontal/Vertical` from the parent max constraints.
- Measures `Content` with these inner constraints (or treats content as `SizeHints.Fixed(Size.Zero)` when null).
- Returns `SizeHints` as `ContentHints + EffectivePadding`, preserving the child’s flex grow/shrink factors.

### Arrange

- Computes an inner rectangle:
  - `x = finalRect.X + EffectivePadding.Left`
  - `y = finalRect.Y + EffectivePadding.Top`
  - `width = max(0, finalRect.Width - EffectivePadding.Horizontal)`
  - `height = max(0, finalRect.Height - EffectivePadding.Vertical)`
- Arranges the content into that rectangle (if non-null).

## Input & commands

`Padder` does not handle input and does not expose commands.

## Styling

There is no `PadderStyle`. Visual appearance is determined entirely by the child.

## Tests & demos

Tests that lock down current behavior:

- `src/XenoAtom.Terminal.UI.Tests/PadderMeasureArrangeTests.cs`

## Future / v2 ideas

- Consider a “background fill” variant (Padder + fill) as a dedicated control if it becomes a common pattern.
