---
title: Slider Specs
---

# Slider Specs

This document captures design and implementation notes for `Slider<T>`.

> [!NOTE]
> For end-user usage and examples, see [Slider](../../controls/slider.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Select a numeric value within a range, with keyboard and mouse interaction.
- **Key goals**:
  - generic numeric support via `INumber<T>` (int, double, etc.)
  - step and large-step adjustments
  - optional snapping to the nearest step
  - horizontal and vertical orientations
  - allocation-free rendering (single pass fill into `CellBuffer`)
- **Non-goals**:
  - tick marks / ruler and editable numeric input in the same control (compose with `NumberBox<T>` if needed)
  - non-linear scales (log, exp) in v1 (possible future extension)

## Implementation notes

- Primary implementation:
  - `src/XenoAtom.Terminal.UI/Controls/Slider.cs`
  - `src/XenoAtom.Terminal.UI/Styling/SliderStyle.cs`
- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/SliderValueTests.cs`
- Demo:
  - `samples/ControlsDemo/Demos/SliderDemo.cs`

## Public API surface

### Type

- `Slider<T> : Visual where T : struct, INumber<T>` (sealed)

### Layout defaults

- `Focusable = true`
- `HorizontalAlignment = Align.Stretch`
- `VerticalAlignment` uses the `Visual` default (typically `Align.Start`)

### Properties

- `Orientation : Orientation` (bindable)
  - Horizontal is the default (`default(Orientation)`).
- `Minimum : T` (bindable)
- `Maximum : T` (bindable)
- `Value : T` (bindable)
- `Step : T` (bindable)
- `LargeStep : T` (bindable)
- `SnapToStep : bool` (bindable)
- `ShowValueLabel : bool` (bindable)
- `ValueFormatter : Delegator<Func<T, string>>` (bindable)
  - Used to format the value label when `ShowValueLabel` is true.
  - When not set, formatting falls back to the built-in `ToStringValue(value)` representation.

### Events

- `ValueChanged` routed event (bubble).

The event is raised only when the effective (clamped/snapped) value changes.

## Tests & demos

See:
- `src/XenoAtom.Terminal.UI.Tests/SliderValueTests.cs` for value coercion behavior.
- `samples/ControlsDemo/Demos/SliderDemo.cs` for interactive usage.

## Value coercion rules

### Range consistency

The control keeps the range consistent when a bound changes:
- When `Minimum` is set and `Maximum < Minimum`, `Maximum` is set to `Minimum`.
- When `Maximum` is set and `Maximum < Minimum`, `Minimum` is set to `Maximum`.

After either bound changes, `Value` is clamped (and snapped if enabled).

### Finite value enforcement

To avoid propagating invalid numeric values into layout/render code:
- `Minimum` and `Maximum` are coerced to finite values.
  - If non-finite, `Minimum` becomes `0` and `Maximum` becomes `1`.
- `Step` and `LargeStep` are coerced to non-negative finite values.
  - If invalid, they become `0`.

### Clamping and snapping

Whenever `Value` is set, it is transformed as follows:
1) clamp to `[min .. max]`
2) if `SnapToStep` is true and `Step > 0`, snap to the nearest step:
   - compute `t = (value - min) / step`
   - use `Math.Round(t) * step + min`
   - clamp again to `[min .. max]`

## Layout and rendering

### Measure

The slider measures to a small fixed size (a minimal track length) and relies on alignment to stretch in layout.

- Minimum track length is 6 cells.
- Horizontal orientation:
  - desired size: `min(MaxWidth, 6)` by `min(MaxHeight, 1)`
- Vertical orientation:
  - desired size: `min(MaxWidth, 1)` by `min(MaxHeight, 6)`

This ensures the control remains usable with small constraints while still expanding when placed in a stretching slot.

### Render

Rendering is performed directly into the `CellBuffer` within `Bounds`.

The control renders:
- an inactive track
- an active track (the portion from min up to the thumb position)
- a thumb glyph at the current value position

The thumb index is computed from the normalized ratio:
- `t = (Value - Minimum) / (Maximum - Minimum)`
- mapped to `[0 .. trackLength - 1]` using rounding

#### Horizontal rendering

- If `ShowValueLabel` is true, the control reserves space for a value label:
  - label cells are computed via `TerminalTextUtility.GetWidth(...)`
  - the track width becomes `Bounds.Width - labelCells - 1`
  - the label is written after the track with `TextStyle.Dim`
- Track fill rules for each cell `i` in `[0 .. trackWidth-1]`:
  - `i == thumbIndex`: write the thumb glyph with thumb style
  - `i < thumbIndex`: write active track glyph with active style
  - otherwise: write inactive track glyph with track style

#### Vertical rendering

Vertical orientation maps:
- bottom cell: `Minimum`
- top cell: `Maximum`

The thumb is placed using a bottom-based index so that increasing values move upward.

Active track fills below the thumb (toward the bottom), matching the "filled from min" mental model.

## Input behavior

`Slider<T>` currently implements its interactions directly in `OnKeyDown` and pointer handlers (it does not register
commands for them yet).

### Keyboard

When enabled:
- `Home`: set `Value = Minimum`
- `End`: set `Value = Maximum`
- `PageUp`: `Value -= LargeStep` (or `Step * 2` when `LargeStep <= 0`)
- `PageDown`: `Value += LargeStep` (or `Step * 2` when `LargeStep <= 0`)

Small-step adjustments:
- Horizontal: `Left` decreases, `Right` increases
- Vertical: `Down` decreases, `Up` increases

### Mouse

- Left press begins dragging and immediately updates the value based on the pointer position.
- While dragging, pointer move updates the value continuously.
- Left release ends dragging.

### Mouse wheel

When enabled, wheel adjusts by `Step` (or by `1` when `Step <= 0`):
- positive wheel delta decreases the value
- negative wheel delta increases the value

## Styling

### SliderStyle

`SliderStyle` controls:
- glyphs for track, active track, and thumb (these defaults use non-ASCII glyphs in code; override as needed)
- `TrackStyle`, `ActiveTrackStyle`, and several thumb state styles

Resolved styles:
- track uses `theme.BorderStyle(focused: false)` by default
- active track uses `theme.Accent` (or focus border / foreground fallback) and `TextStyle.Bold`
- thumb style depends on state:
  - disabled: dimmed theme border style
  - pressed: active track style with selection influence and bold
  - hovered: active track style with bold
  - focused: active track style with underline

## Future / v2 ideas

- Add optional tick marks, min/max labels, and formatting of the label for vertical sliders.
- Support alternative mappings (logarithmic scale) via a mapping function in style or a dedicated property.
- Commands for key actions (`Increase`, `Decrease`, `SetMinimum`, `SetMaximum`) for CommandBar discoverability.
