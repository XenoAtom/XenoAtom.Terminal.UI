---
title: StatusBar Specs
---

# StatusBar Specs

This document captures design and implementation notes for `StatusBar`.

> [!NOTE]
> For end-user usage and examples, see [StatusBar](../../controls/statusbar.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Provide a single-row bar with left- and right-aligned content.
- **Key goals**:
  - simple layout for "status left, hints right" use cases
  - cheap to render (single row background fill)
  - composition-friendly: left and right slots accept any `Visual`
- **Non-goals**:
  - multi-row status area (compose with `VStack`, `Footer`, or `DockLayout`)
  - command routing and key handling (StatusBar is presentational)

StatusBar is a more "classic" primitive; new apps often prefer `Header`/`Footer` for richer layout.

## Implementation notes

- Primary implementation:
  - `src/XenoAtom.Terminal.UI/Controls/StatusBar.cs`
  - `src/XenoAtom.Terminal.UI/Styling/StatusBarStyle.cs`
- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/StatusBarRenderingTests.cs`
- Demo:
  - `samples/ControlsDemo/Demos/StatusBarDemo.cs`

## Public API surface

### Type

- `StatusBar : Visual` (sealed)

### Layout defaults

- `HorizontalAlignment = Align.Stretch`
- `VerticalAlignment` uses the `Visual` default (typically `Align.Start`)

### Properties

- `LeftText : Visual?` (bindable)
- `RightText : Visual?` (bindable)

The children are the two slot visuals when present.

There are no events and no input handling.

## Layout and rendering

### Measure

StatusBar is a single-row control (height 1).

Measurement rules:
- Both `LeftText` and `RightText` are measured with:
  - width: `[0 .. Infinite]`
  - height: `[0 .. 1]`
- The required width is the sum of both desired widths.
- The returned hints are:
  - `min = (0, 1)`
  - `natural = clamp(requiredWidth, 1)`
  - horizontal flex: grow 1 / shrink 1 (`SizeHints.FlexX(...)`)

### Arrange

StatusBar arranges each slot into a 1-row rectangle:
- Left:
  - `x = finalRect.X`
  - `width = min(finalRect.Width, left.DesiredSize.Width)`
- Right:
  - `width = min(finalRect.Width, right.DesiredSize.Width)`
  - `x = finalRect.X + (finalRect.Width - width)`

Important: there is no collision resolution. If the total content width exceeds the available width, the arranged
rectangles can overlap. The final visual result depends on rendering order (children render after the parent fill).

### Render

The StatusBar renders a single row background fill across `Bounds.Width` using `StatusBarStyle.Resolve(theme)`.

Children then render on top. Since the background fill writes a full row of styled spaces, children that render with
`Style.None` inherit the status bar baseline styling.

## Styling

### StatusBarStyle

`StatusBarStyle` exposes:
- `Background : Color?`
- `Foreground : Color?`

Resolve behavior:
- foreground defaults to `theme.Foreground` when not specified
- background is optional (when null, background is not set on the style)
- `TextStyle.Bold` is always applied

## Tests and demos

- `StatusBarRenderingTests.StatusBar_Renders_Left_And_Right` validates that both slots appear when hosted in a layout.
- ControlsDemo shows usage with `Markup` slots with `Wrap = false`, embedded in a bordered panel.

## Future / v2 ideas

- Optional slot spacing or padding (currently handled by providing padded slot visuals).
- Optional "collapse rules" (e.g., hide right slot first under tight widths) if overlap becomes problematic.
