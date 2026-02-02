---
title: Spinner Specs
---

# Spinner Specs

This document captures design and implementation notes for `Spinner`.

> [!NOTE]
> For end-user usage and examples, see [Spinner](../../controls/spinner.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Render an animated spinner indicator, optionally with a label, to show ongoing activity.
- **Key goals**:
  - animation driven by `TerminalApp` (no user input required)
  - support multi-cell frames (e.g., wide "bar" spinners)
  - allow semantic coloring via `ControlTone`
  - be very cheap to measure/render (1 row, no allocations in the hot path)
- **Non-goals**:
  - interactive control (no focus, no commands, no pointer handling)
  - multi-line labels (spinner is a single-row control)

## Implementation notes

- Primary implementation:
  - `src/XenoAtom.Terminal.UI/Controls/Spinner.cs`
  - `src/XenoAtom.Terminal.UI/Styling/SpinnerStyle.cs`
  - `src/XenoAtom.Terminal.UI/Styling/SpinnerStyles.cs` (built-in catalog)
- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/SpinnerTests.cs`
- Demo:
  - `samples/ControlsDemo/Demos/SpinnerDemo.cs`
- Integration:
  - `ProgressTaskColumns.Spinner(...)` creates a spinner cell for `ProgressTaskGroup`.

## Public API surface

### Type

- `Spinner : Visual, IAnimatedVisual` (sealed)

### Constructors

- `new Spinner()`
  - Defaults: `IsActive = true`, `Tone = ControlTone.Primary`.
- `new Spinner(string label)`
  - Sets `Label` to the provided value. (There is an implicit conversion convenience in the UI layer that allows using
    strings as visuals in many places.)

### Properties

- `Label : Visual?` (bindable)
  - Optional label visual rendered to the right of the spinner frame.
  - Participates in the visual tree as a single child when present.
- `IsActive : bool` (bindable)
  - When false, the spinner stops animating and always displays frame 0.
- `Tone : ControlTone` (bindable)
  - Determines which theme color to use (primary/success/warning/error/muted).

The control itself does not define a `Style` property; it uses the environment `SpinnerStyle`.

## Animation model

`Spinner` participates in the application animation loop via `IAnimatedVisual`:
- `NextAnimationTick` returns a timestamp (stopwatch ticks) for the next frame change
- `AdvanceAnimation(timestamp)` advances the frame when enough time has elapsed and returns whether the visual changed

Animation is enabled only when all conditions are true:
- the spinner is attached to an app (`App` is not null)
- `IsActive` is true
- `IsVisible` is true

Otherwise:
- `_nextTick` is set to `long.MaxValue`
- no frames are advanced

### Style changes reset animation

The spinner caches the resolved `SpinnerStyle` instance by reference. When `GetStyle<SpinnerStyle>()` returns a
different object reference, the spinner:
- resets `_frameIndex` to 0
- recomputes the tick interval
- schedules the next tick relative to the current timestamp

This makes style swaps deterministic and immediate.

## Layout and rendering

### Measure

Spinner is a single-row control (height 1).

Let `frameWidth = max(1, SpinnerStyle.FrameWidth)`.

- When `Label` is null:
  - desired size is `(frameWidth, 1)` clamped by constraints.
- When `Label` is present:
  - the label is measured with:
    - width: `[0 .. Infinite]` (unbounded)
    - height: `[0 .. 1]` (single row)
  - desired width is `frameWidth + 1 + label.DesiredSize.Width` (1 cell spacing)
  - desired size is clamped by constraints

### Arrange

If a label is present, it is arranged to the right of the spinner frame:
- label X = `finalRect.X + frameWidth + 1`
- label width = min(available, label.DesiredSize.Width)
- label height = 1

If there is no space for the label, it is not arranged.

### Render

Render writes:
1) The spinner frame at `(Bounds.X, Bounds.Y)` using `SpinnerStyle.Resolve(theme, IsEnabled, Tone)`.
2) If a label exists and there is space:
   - a single space separator (the gap cell) is written in the label style
   - the label area is pre-filled with the label style so that label visuals inheriting `Style.None` render with a
     stable baseline.

#### Frame truncation for multi-cell frames

Spinner frames can be multi-rune strings, but they must have a stable cell width (`SpinnerStyle.FrameWidth`).

At render time, the spinner truncates the frame string to the number of cells it can display:
- target cells = `min(frameWidth, Bounds.Width)`
- truncation is performed via `TerminalTextUtility.TryGetIndexAtCell(...)` to avoid splitting grapheme clusters or
  multi-cell runes incorrectly.

## Styling

### SpinnerStyle

`SpinnerStyle` defines:
- `Name : string`
- `Interval : TimeSpan` (frame rate)
- `Frames : ReadOnlySpan<string>`
- `FrameWidth : int` (cell width of each frame, validated at construction time)
- `TextStyle : TextStyle` (default: bold)
- `Foreground : Color?` optional override

Construction rules:
- `interval` must be >= 0
- `frames` must be non-empty
- all frames must have the same cell width as measured by `TerminalTextUtility.GetWidth(...)`
- `FrameWidth` must be > 0

`SpinnerStyle.Resolve(theme, enabled, tone)`:
- chooses a foreground based on `Foreground` override or `tone` and theme colors
- applies `TextStyle`
- applies `TextStyle.Dim` when the control is disabled

### SpinnerStyles catalog

`SpinnerStyles` provides many built-in styles (single-cell and multi-cell). Many frames use non-ASCII glyphs and some
use emoji. Specs and docs should avoid embedding those glyph characters directly; refer to style names instead.

## Tests

- `Spinner_Animates_Without_User_Input` validates that frames advance over time under `TerminalAppTestDriver`.
- `SpinnerStyle_Rejects_Different_FrameWidths` validates frame width enforcement at style construction.

## Future / v2 ideas

- Optional pause/resume policies for host controls (e.g., progress views that stop animating when off-screen).
- Optional background styling for the label area (currently label uses `theme.ForegroundTextStyle()`).
- Optional "speed multiplier" as a property (in addition to style interval).
