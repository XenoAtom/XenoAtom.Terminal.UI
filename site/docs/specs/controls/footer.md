---
title: Footer Specs
---

# Footer Specs

This document specifies the current behavior and design of the `Footer` control as implemented.

> [!NOTE]
> For end-user usage and examples, see [Footer](../../controls/footer.md).

## Goals

- Provide a lightweight, single-row footer bar for app chrome.
- Offer three slots (`Left`, `Center`, `Right`) with predictable placement and clipping.
- Always clear/paint its background so content behind it cannot bleed into the footer row.
- Remain allocation-conscious and compatible with the binding dirty model (slot visuals can be dynamic).

## Non-goals

- Multi-line footer (the footer is always height `1`).
- Automatic wrapping/ellipsis behavior inside slots (delegate to the contained visuals).
- Input handling (the footer itself is non-interactive; slots may be interactive).

## Public surface (v1)

- `Left : Visual?`
- `Center : Visual?`
- `Right : Visual?`

## Defaults

- Default alignment: `HorizontalAlignment = Align.Stretch`.
- Default height: `1` (fixed).

## Implementation map

- Control: `src/XenoAtom.Terminal.UI/Controls/Footer.cs`
- Style: `src/XenoAtom.Terminal.UI/Styling/FooterStyle.cs`
- Demo: `samples/ControlsDemo/Demos/FooterDemo.cs`
- Tests (usage as app chrome root):
  - `src/XenoAtom.Terminal.UI.Tests/AppChromeTests.cs`

## Layout

### Child order

Children are exposed in order: `Left`, `Center`, `Right` (when present).

### Measure

- Each slot is measured with `maxWidth = infinite` and `maxHeight = 1` (single-line).
- The natural width is computed as:
  - `leftWidth + rightWidth` (when `Center` is null)
  - otherwise `max(leftWidth + rightWidth, leftWidth + centerWidth + rightWidth)`
- Returns `SizeHints.FlexX` with:
  - `min = (0, 1)`
  - `natural = (requiredWidth, 1)`
  - `growX = 1`, `shrinkX = 1`

This means `Footer` is happy to stretch horizontally but never requests extra height.

### Arrange

Given a final rect (width `W`):

- `Left` is arranged at `X` with width `min(W, leftDesiredWidth)`.
- `Right` is arranged at `Right - min(W, rightDesiredWidth)`.
- `Center` (if present) gets at most `max(0, W - leftW - rightW)` cells.
  - It is arranged centered within that remaining space.

`Center` will clip naturally if it is wider than the remaining space.

## Rendering

- Footer is app chrome: it always clears its entire row to spaces using the resolved footer style, ensuring a stable background.
- Slot visuals then render on top through normal visual tree rendering.

## Styling

`FooterStyle` resolves to a single cell `Style`:

- `Foreground`: `FooterStyle.Foreground ?? theme.Foreground`
- `Background`: `FooterStyle.Background ?? theme.SurfaceAlt`
- Always includes `TextStyle.Bold`.

## Input handling

`Footer` itself does not handle input. If a slot contains a focusable visual, that visual behaves normally.

## Tests and demos

- `FooterDemo` shows typical hint/status usage with non-wrapping markup visuals.
- `AppChromeTests` verifies header/footer integration when used in a `DockLayout` root.

## Future ideas

- Optional slot spacing/padding knobs (can be achieved today by wrapping slot visuals).
- Optional "separator" rendering between slots.
- A "compact" style preset (less bold, different background) as a theme convenience.
