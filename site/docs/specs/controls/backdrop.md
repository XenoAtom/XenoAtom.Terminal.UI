---
title: Backdrop Specs
---

# Backdrop Specs

This document captures design and implementation notes for `Backdrop`.

> [!NOTE]
> For end-user usage and examples, see [Backdrop](../../controls/backdrop.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Fill the viewport with a dimmed surface behind modal UI (dialogs, popups, command palette, etc.).
- **Key goals**:
  - improve readability of overlays by reducing contrast/noise in the underlay
  - be extremely cheap to measure and render (simple fill loop)
  - avoid “style leaks” from the underlay (foreground/decorations bleeding into overlays)
- **Non-goals**:
  - Backdrop does not handle input or focus by itself (it is a visual surface)
  - Backdrop does not provide blurring; it’s a dim overlay only

## Public API surface

### Type

- `Backdrop : Visual` (sealed)

### Layout defaults

- `HorizontalAlignment = Align.Stretch`
- `VerticalAlignment = Align.Stretch`

There are no additional public bindable properties on `Backdrop` itself; customization is done via `BackdropStyle`.

## Layout & rendering

### Measure

`Backdrop` measures as a fully flexible surface:

- `SizeHints.Flex(min: 0, natural: 0, max: Infinite, growX: 1, growY: 1, shrinkX: 0, shrinkY: 0)`

This makes it suitable as the bottom layer of a fullscreen overlay stack (e.g. `ZStack` in a `DialogHost`).

### Arrange

No custom arrangement logic. The bounds are determined by parent layout and alignment.

### Render

Rendering is a straightforward fill of the `Bounds` rectangle:

- resolve style via `BackdropStyle.Resolve(theme)`
- fill every cell with `BackdropStyle.FillRune` (default space) and the resolved style

Backdrop intentionally writes **both background and foreground** so that subsequent visuals that render with
`Style.None` (inheriting from the buffer) get a stable baseline.

## Styling

### BackdropStyle

Backdrop behavior is controlled via `BackdropStyle`:

- `FillRune` (default `' '`)
- `Dim` (default `true`): applies `TextStyle.Dim`
- `UseThemeBackground`:
  - `false` (default): uses `Theme.Disabled` as the fill background (typical “dimming” behavior)
  - `true`: uses `Theme.Background` as the fill background (useful for theme previews / “reset surface” fills)
- `Background` / `Foreground` overrides:
  - when provided, override theme-derived defaults

#### Preventing style leakage (important)

Backdrops are usually placed under dialogs/popups. Historically, if the backdrop only set a background, any underlay
foreground (and sometimes text decorations) could leak into overlay content that inherits styles from the buffer.

To prevent this, `BackdropStyle.Resolve(...)`:

- always sets an explicit foreground (`Foreground ?? theme.Foreground ?? Color.Default`)
- explicitly returns `style.WithTextStyle(style.TextStyle)` so the written cell has a concrete text style (avoiding
  inheriting decorations like underline from an underlay)

This is especially important because many overlay controls render text using defaults that rely on inheritance.

## Input

Backdrop has no built-in input handling.

Input behavior (e.g. click outside to close, `Esc` to dismiss) is provided by higher-level overlay hosts such as dialogs,
popups, command palette, etc.

## Tests & demos

- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/BackdropStyleTests.cs` verifies that the backdrop writes a baseline foreground to
    prevent underlay color leaking into overlays.
- Demo:
  - ControlsDemo “Backdrop” shows a modal dialog rendered above a backdrop layer.

## Future / v2 ideas

- Support gradient backdrops (e.g. top-to-bottom dim) for more depth.
- Optional “click to dismiss” helper wrapper (as a separate control/host) so Backdrop remains purely visual.
