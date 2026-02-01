---
title: Button Specs
---

# Button Specs

This document captures design and implementation notes for `Button`.

> [!NOTE]
> For end-user usage and examples, see [Button](../../controls/button.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: A clickable, focusable control that raises a `Click` routed event when activated.
- **Core interactions**:
  - keyboard activation (`Enter`, `Space`)
  - mouse click (press/drag/release)
  - hover + pressed visual states
- **Content model**: `Button` is a `ContentVisual` and renders a single child (`Content`) centered in the button.

## Public API surface

### Type

- `Button : ContentVisual`

### Constructors

- `Button()` sets `Focusable = true` and defaults `Tone = ControlTone.Default`.
- `Button(Visual text)` assigns `Content` via the fluent `Content(...)` helper.

### Bindable properties

- `Tone : ControlTone`
  - Semantic tone used by the default style resolver (default/primary/success/warning/error).
- `IsPressed : bool`
  - Public pressed state (set by pointer events).

### Internal state

- `IsPressedInside : bool` (internal bindable)
  - Tracks whether the pointer is currently inside the bounds while pressed.

### Routed events

- `Click` (bubble routing)
  - Raised on keyboard invocation and on mouse release when the press started and ended inside the button bounds.

## Layout & rendering

### Measure

Measurement is driven by `ButtonStyle`:

- `Padding` is applied around the content.
- `ShowBorder` reserves 1 row at the top and bottom (see rendering details below).

The button measures its `Content` using inner constraints reduced by:

- `Padding.Horizontal` / `Padding.Vertical`
- and when `ShowBorder == true`, an additional `2` cells in both width and height (historically treated like a full border pad).

The resulting `SizeHints` are derived from the content hints plus the added padding/border cells.

> [!IMPORTANT]
> When `ShowBorder == true`, `Button` enforces a minimum of `3x3` cells so the top/bottom border and a content row can coexist.

### Arrange

Arrangement centers the content within the inner padded rectangle:

- Computes the content slot as:
  - `finalRect` minus padding
  - and when `ShowBorder == true`, minus a 1-cell pad on each side (see note above).
- Arranges the content at `(x, y)` so that it is centered and clipped by the slot.

The button does not currently expose a content alignment property; centering is the built-in behavior.

### Render

Rendering is split into three passes:

1) **Full-rect background fill**
   - Fills `Bounds` with spaces using the resolved style for the current state.

2) **Optional top/bottom border**
   - When `ButtonStyle.ShowBorder == true` and height allows, draws a horizontal line at:
     - `y = top` using `ButtonStyle.BorderTopGlyph`
     - `y = bottom` using `ButtonStyle.BorderBottomGlyph`
   - Border style comes from `theme.BorderStyle(focused: HasFocus)`.

3) **Content region style stamping**
   - The content rectangle is filled with spaces to apply *text attributes* (e.g. bold) without re-applying the background.

> [!IMPORTANT]
> Button backgrounds can be RGBA overlays (alpha blending). Writing the same RGBA background twice in the content region
> would blend twice and produce visible “bands”. The implementation avoids this by filling the full rect once, then stamping
> the content region with `style.ClearBackground()` so the background remains the one from the initial fill.

## Input & commands

`Button` does not currently expose dedicated `Command` instances for activation. It directly raises the `Click` routed event.

### Keyboard

- `Enter` or `Space` triggers `Click` and marks the event handled.

### Pointer

- Only left mouse button is handled.
- Press sets:
  - `IsPressed = true`
  - `IsPressedInside = Bounds.Contains(pointer)`
- Move while pressed updates `IsPressedInside` based on hit-testing.
- Release:
  - clears `IsPressed`
  - raises `Click` if the pointer is inside on release
  - otherwise, it clears `IsHovered` to avoid leaving a hover state behind after a drag-release outside.

## Styling

### ButtonStyle

Resolved from the environment via `ButtonStyle.Key`:

- `Padding` (default: `new Thickness(2, 0, 2, 0)`)
- `ShowBorder` (top/bottom only)
- `BorderTopGlyph` (default: `' '`)
- `BorderBottomGlyph` (default: `'▁'`)
- Optional state overrides:
  - `Normal`, `Hovered`, `Pressed`, `Focused`, `Disabled`

### Default style resolution

When state overrides are not provided, `ButtonStyle.Resolve(...)` derives styles from the `Theme`:

- **Normal**:
  - `Tone.Default`: uses `theme.ControlFill ?? theme.SurfaceAlt ?? theme.Surface ?? theme.Background`
  - `Tone.Primary/Success/Warning/Error`: uses tone color as background and the theme background/foreground as foreground
- **Hovered**:
  - For default tone, prefers `theme.ControlFillHover ?? theme.SurfaceAlt` as background
  - Otherwise uses `Bold`
- **Pressed**:
  - Uses `theme.ControlFillPressed ?? theme.Selection` background + `Bold`
- **Focused** (default tone only):
  - Uses `theme.FocusBorder` as foreground (when present) + `Underline`
  - Intentionally does not look like pressed (foreground + underline rather than background swap)
- **Disabled**:
  - Uses theme disabled foreground when available, and applies `Dim`

## Tests & demos

Tests that lock down current behavior:

- `src/XenoAtom.Terminal.UI.Tests/ButtonMeasureTests.cs`
- `src/XenoAtom.Terminal.UI.Tests/ButtonInteractionTests.cs`
- `src/XenoAtom.Terminal.UI.Tests/PointerHoverTests.cs`

## Future / v2 ideas

- Expose activation as a `Command` (in addition to the routed event) for discoverability in `CommandBar` / `CommandPalette`.
- Add optional content alignment (start/center/end) instead of always centering.
- Reconcile `ShowBorder` horizontal padding semantics (currently rendered as top/bottom lines only).
- Expand border rendering (optional left/right glyphs) if needed for theming.
