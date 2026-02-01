---
title: Center Specs
---

# Center Specs

This document captures design and implementation notes for `Center`.

> [!NOTE]
> For end-user usage and examples, see [Center](../../controls/center.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Centers a single child within the available bounds.
- **Content model**: `Center` is a `ContentVisual` and arranges only `Content`.

## Public API surface

### Type

- `Center : ContentVisual`

### Inherited properties

`Center` uses `ContentVisual.Content` (and the usual `Visual` sizing/alignment properties), but it does not introduce
any new bindables of its own.

## Layout & rendering

`Center` is purely a layout helper; it does not render anything itself.

### Measure

- If `Content` is null: `DesiredSize = (0,0)`.
- Otherwise: returns `Content.Measure(constraints)`.

### Arrange

- If `Content` is null: no-op.
- Otherwise:
  - clamps arranged width/height to the smaller of:
    - available size (`finalRect`)
    - `Content.DesiredSize`
  - centers the resulting rectangle within `finalRect`:
    - `x = finalRect.X + (finalRect.Width - w) / 2`
    - `y = finalRect.Y + (finalRect.Height - h) / 2`

This means:

- the child is never stretched by `Center`
- oversized children are arranged to a clipped rectangle (normal visual clipping rules apply)

## Input & commands

`Center` does not handle input and does not expose commands.

## Styling

There is no `CenterStyle`. Visual appearance is determined entirely by the child.

## Tests & demos

There are no dedicated unit tests for `Center` at the time of writing.

## Future / v2 ideas

- Consider adding a small deterministic layout test if future changes introduce more behavior (e.g. margins or alignment overrides).
