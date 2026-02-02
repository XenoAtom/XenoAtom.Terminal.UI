---
title: Switch Specs
---

# Switch Specs

This document captures design and implementation notes for `Switch`.

> [!NOTE]
> For end-user usage and examples, see [Switch](../../controls/switch.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: A compact toggle switch with optional inline content (label) rendered next to the track.
- **Content model**: `Switch : ContentVisual` (single child `Content`).
- **Interaction**:
  - toggles on `Space`/`Enter` and left click
  - supports `Left`/`Right` to force off/on
- **Rendering note**: Carefully handles RGBA track fills to avoid double-applied alpha blending when drawing the thumb over the track.

## Public API surface

### Type

- `Switch : ContentVisual` (sealed)

### Constructors

- `Switch()` sets `Focusable = true`.
- `Switch(Visual content)` assigns `Content`.

### Bindable properties

- `IsOn : bool`
  - raises the `Toggled` routed event when it changes
- `IsPressed : bool` (read-only public)
  - set during pointer press/release for pressed styling

### Routed events

- `Toggled` (bubble): raised with old/new boolean values.

## Layout & rendering

### Measure

- Track width is fixed at 4 cells (`TrackWidth = 4`).
- If there is content:
  - measures content with width reduced by `TrackWidth + SwitchStyle.SpaceBetweenGlyphAndText`
  - desired size is:
    - `width = TrackWidth + gap + contentWidth`
    - `height = max(1, contentHeight)`
- If no content: desired size is `(TrackWidth, 1)`.

### Arrange

- Content is arranged to the right of the track + gap within `finalRect`.
- The track itself is drawn in `Render` and is vertically centered within the control’s height.

### Render

Track rendering:

- Draws up to `min(TrackWidth, Bounds.Width)` cells for the track.
- Uses `SwitchStyle.TrackLeft`/`TrackRight` for the first/last cell glyphs, spaces otherwise.
- Resolves per-segment track style with `SwitchStyle.ResolveTrackPart(...)`:
  - segments are considered “active” based on `IsOn` and thumb position
  - hovered/pressed/focused variants are applied by the style resolver

Thumb rendering:

- Computes a `thumbIndex`:
  - for full 4-cell track: index is `1` (off) or `2` (on)
  - for smaller tracks, clamps to the available range
- Renders `SwitchStyle.ThumbGlyph` over the track.
- Avoids double alpha blending:
  - if the track background is RGBA, the thumb style is *not* merged with the track style
  - otherwise, merges unspecified style parts from the track into the thumb (`MergeUnspecified`)

## Interaction

### Keyboard

- `Space` / `Enter`: toggle
- `Left`: set `IsOn = false`
- `Right`: set `IsOn = true`

### Pointer

- left press sets `IsPressed = true`
- left release clears `IsPressed` and toggles `IsOn` if released within bounds

## Styling

### SwitchStyle

Key knobs:

- geometry:
  - `SpaceBetweenGlyphAndText`
  - `TrackLeft` / `TrackRight`
  - `ThumbGlyph` (default `⬤`; see `SwitchStyle.Round` preset)
- track styles:
  - `TrackOn*`, `TrackOff*` (active/inactive parts)
  - `TrackHovered`, `TrackPressed`, `TrackFocused`, `TrackDisabled`
- thumb styles:
  - `ThumbOn`, `ThumbOff`, `ThumbDisabled`

Default resolution uses theme colors like `Theme.Primary`, `Theme.Surface`, `Theme.SurfaceAlt`, `Theme.Selection`, `Theme.FocusBorder`.

## Tests & demos

- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/SwitchTests.cs`
- Demo:
  - ControlsDemo includes switch examples (with and without inline content).

## Future / v2 ideas

- Support configurable track width (while keeping a sensible default).
- Add optional “animated” toggling (if/when the framework supports lightweight animations).
