---
title: DockLayout Specs
---

# DockLayout Specs

This document specifies the current behavior and design of the `DockLayout` control as implemented.

> [!NOTE]
> For end-user usage and examples, see [DockLayout](../../controls/docklayout.md).

## Goals

- Provide a simple vertical “app chrome” layout:
  - a `Top` region (header/toolbars)
  - a `Bottom` region (footer/status/command bar)
  - a `Content` region that fills the remaining height
- Keep behavior predictable and terminal-friendly (no complex docking rules).

## Non-goals

- Left/right docking (not implemented).
- Overlapping regions (all regions are laid out vertically and do not overlap).
- Custom splitters or resizable chrome (compose with other controls if needed).

## Public surface (v1)

- `Top : Visual?`
- `Bottom : Visual?`
- `Content : Visual?`

## Defaults

- Default alignment: `HorizontalAlignment = Align.Stretch`, `VerticalAlignment = Align.Stretch`.

## Implementation map

- Control: `src/XenoAtom.Terminal.UI/Controls/DockLayout.cs`
- Demo: `samples/ControlsDemo/Demos/DockLayoutDemo.cs`
- Tests (examples of typical usage as app root chrome):
  - `src/XenoAtom.Terminal.UI.Tests/AppChromeTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/CommandBarRenderingTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/StatusBarRenderingTests.cs`

## Layout

`DockLayout` is a purely layout-oriented control: it does not render anything itself and has no input handling.

### Child order

- Children are exposed in the order: `Top`, `Content`, `Bottom` (when present).

### Measure

Measure is performed in this order:

1. `Top` is measured with `(maxWidth, remainingMaxHeight)`.
   - If the parent max height is finite, the remaining height is reduced by `top.MeasureHints.Natural.Height`.
2. `Bottom` is measured with `(maxWidth, remainingMaxHeight)`.
   - Remaining height is reduced by `bottom.MeasureHints.Natural.Height`.
3. `Content` is measured with whatever height remains.

The resulting `SizeHints` are derived by combining children:

- Width:
  - `Min`/`Natural` are the max of the corresponding widths across regions.
  - `Max` is infinite if any child can grow infinitely, otherwise the max of the child max widths.
- Height:
  - `Min`/`Natural` are the sum of the region heights.
  - `Max` is infinite if any child max height is infinite, otherwise it is the sum of child max heights.
- Flex:
  - `growY` / `shrinkY` are taken from the `Content` region (top/bottom are considered fixed chrome in Y).
  - `growX` / `shrinkX` are the max across all regions.

> [!NOTE]
> The “remaining height” logic means `Top` and `Bottom` get measurement priority. This is intentional: app chrome should
> reserve space before measuring the main content.

### Arrange

Arrange is also prioritized for chrome:

1. `Top` is arranged at the top using `min(remainingHeight, top.DesiredSize.Height)`.
2. `Bottom` is arranged at the bottom using `min(remainingHeight, bottom.DesiredSize.Height)`.
3. `Content` is arranged in the remaining vertical space between `Top` and `Bottom` (may be `0` height).

All regions receive the full available width.

## Styling

`DockLayout` has no dedicated style. It relies on the child visuals to provide styling (borders, background fills, etc.).

## Input handling

None. Input is handled by the contained visuals.

## Tests and demos

- `DockLayoutDemo` shows the typical “Top/Content/Bottom” behavior with simple bordered regions.
- `AppChromeTests` uses `DockLayout` to ensure `Header`/`Footer` render around a content body.
- Other tests use `DockLayout` as the root container for `CommandBar`/`StatusBar` scenarios.

## Future ideas

- Optional `TopGap` / `BottomGap` convenience spacing (can already be achieved with wrappers).
- Optional “single-child” helpers for common app patterns (header + body, body + footer).
