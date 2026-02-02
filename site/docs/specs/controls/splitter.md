---
title: Splitter (HSplitter / VSplitter) Specs
---

# Splitter (HSplitter / VSplitter) Specs

This document captures design and implementation notes for `HSplitter` and `VSplitter`.

> [!NOTE]
> For end-user usage and examples, see [Splitter (HSplitter / VSplitter)](../../controls/splitter.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Arrange two panes separated by a draggable bar, and expose a ratio-based split point.
- **Key goals**:
  - simple retained-mode split layout (two children max)
  - interactive resizing via mouse drag and keyboard
  - stable integer layout (bar thickness in cells, deterministic rounding)
  - styleable bar glyphs and state styling (hover/focus/drag/disabled)
- **Non-goals**:
  - multi-split layouts (use nested splitters or `DockLayout`)
  - proportional measurement that perfectly reflects arbitrary child sizing (measure uses a simple heuristic)

## Implementation notes

- Primary implementation:
  - `src/XenoAtom.Terminal.UI/Controls/Splitter.cs` (base)
  - `src/XenoAtom.Terminal.UI/Controls/HSplitter.cs`
  - `src/XenoAtom.Terminal.UI/Controls/VSplitter.cs`
  - `src/XenoAtom.Terminal.UI/Styling/SplitterStyle.cs`
- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/SplitterLayoutTests.cs`
- Demo:
  - `samples/ControlsDemo/Demos/SplitterDemo.cs`

## Public API surface

### Types

- `Splitter : Visual` (abstract base)
- `HSplitter : Splitter` (sealed)
- `VSplitter : Splitter` (sealed)

### Layout defaults (Splitter base constructor)

- `Focusable = true`
- `HorizontalAlignment = Align.Stretch`
- `VerticalAlignment = Align.Stretch`
- `Ratio = 0.5`
- `BarSize = 1`

### Properties

- `First : Visual?` (bindable)
- `Second : Visual?` (bindable)
- `Ratio : double` (bindable)
  - Intended range: `[0..1]`.
  - Invalid values (NaN/Infinity) are treated as `0.5` during arrange.
  - The effective ratio is clamped during arrange.
- `BarSize : int` (bindable)
  - Bar thickness in cells.
  - When both panes are present, the effective bar size is `max(1, BarSize)`.
  - When one or both panes are missing, no bar is rendered and there is nothing to drag.
- `MinFirst : int` (bindable)
  - Minimum size of the first pane along the split axis, in cells.
- `MinSecond : int` (bindable)
  - Minimum size of the second pane along the split axis, in cells.

The base `Splitter` also defines internal bindables used for styling state:
- `IsDragging` (while mouse is dragging the bar)
- `IsBarHovered` (when the mouse is over the bar and not dragging)

## Layout and rendering

### Children model

The splitter has up to 2 children (First and Second). When only one pane is set, that pane is arranged to fill the
entire available rectangle and no bar is used.

### Measure

Measurement is heuristic:
- if the split axis constraint is finite, the splitter measures each child into half of the available space (minus bar)
- if the split axis constraint is infinite, each child is measured with an infinite max along that axis

This keeps measure simple and avoids needing to solve "best split" during measure.

The returned `SizeHints` uses:
- `Natural` and `Max` equal to the clamped combined child desired sizes (plus the bar)
- `Min` is `(0, 0)`
- `FlexGrowX/Y` follow the control alignments (defaults are stretch, so it grows)
- `FlexShrinkX/Y = 1`

### Arrange

Arrange computes:
- `bar = max(1, BarSize)` when both panes exist, otherwise `bar = 0`
- `available = totalAlongAxis - bar`, clamped to >= 0
- `firstSize = round(available * ratio)`
- `firstSize` is clamped so that:
  - `firstSize >= clamp(MinFirst, 0, available)`
  - `secondSize = available - firstSize >= clamp(MinSecond, 0, available)`

Then it arranges:
- `First` with `firstSize`
- a bar rectangle (`_barRect`) with thickness `bar`
- `Second` with `secondSize`

Rounding is done with `Math.Round`, so the split point is stable but can move by 1 cell as the container size changes.

### Render

The splitter renders only the bar (the panes render themselves).

Key details:
- the bar is rendered only when `_barRect` is non-empty (i.e., both panes are present)
- the bar glyph is chosen to match the bar direction:
  - `HSplitter` draws a vertical bar glyph (a column separating left/right panes)
  - `VSplitter` draws a horizontal bar glyph (a row separating top/bottom panes)
- the bar rectangle is filled cell-by-cell with the glyph and the resolved style

## Tests & demos

- `SplitterLayoutTests` validates that:
  - the bar consumes space
  - pane bounds are allocated consistently for horizontal and vertical splits
- ControlsDemo includes both orientations with a hint to drag the bar.

## Input behavior

Splitters do not use commands yet; interactions are implemented directly in pointer/key handlers.

### Mouse

- Hover: updates `IsBarHovered` when the pointer moves over/out of the bar (when not dragging).
- Drag:
  - left press on the bar starts dragging and captures the starting pointer position and current first pane size
  - pointer move while dragging updates `Ratio` based on pointer delta
  - left release stops dragging

Drag updates also obey `MinFirst`/`MinSecond` clamping, so the split point cannot be moved beyond those constraints.

### Keyboard

When enabled and focused:
- Horizontal (`HSplitter`):
  - `Left` decreases ratio, `Right` increases ratio
  - `Home` sets ratio to 0, `End` sets ratio to 1
- Vertical (`VSplitter`):
  - `Up` decreases ratio, `Down` increases ratio
  - `Home` sets ratio to 0, `End` sets ratio to 1

Step size is based on modifiers:
- no modifier: 1 cell
- Shift: 5 cells
- Ctrl: 10 cells

The ratio delta is computed as `stepCells / availableCells`, where `availableCells` is the current arranged size along
the split axis (minus the bar).

## Styling

### SplitterStyle

`SplitterStyle` controls:
- glyphs:
  - `HorizontalGlyph` (used when the bar is horizontal)
  - `VerticalGlyph` (used when the bar is vertical)
  - defaults are non-ASCII box drawing glyphs in code; override as needed
- styles:
  - `BarStyle`
  - `HoverStyle`
  - `FocusStyle`
  - `DragStyle`
  - `DisabledStyle`

`SplitterStyle.Resolve(theme, enabled, focused, hovered, dragging)` applies precedence:
- disabled
- dragging
- hovered
- focused
- normal

Defaults (when not overridden) use `theme.BorderStyle(...)`, with bold emphasis for hover/drag, and dimmed styling when
disabled.

## Future / v2 ideas

- Expose commands for discoverability (`Increase`, `Decrease`, `CollapseFirst`, `CollapseSecond`).
- Optional visual affordances (cursor changes or a thicker hover hit target) if needed for usability.
