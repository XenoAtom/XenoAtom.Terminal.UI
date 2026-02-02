---
title: ColorPicker Specs
---

# ColorPicker Specs

This document specifies the current behavior and design of the `ColorPicker` control as implemented.

> [!NOTE]
> For end-user usage and examples, see [ColorPicker](../../controls/colorpicker.md).

## Goals

- Provide a simple, terminal-friendly UI to pick a `Color` via:
  - RGB(A) channel sliders
  - hex input (`#RRGGBB` / optional `#RRGGBBAA`)
  - palette swatches (theme-derived by default)
- Provide a reliable preview for alpha colors using the library’s RGBA blending pipeline.
- Keep state synchronization predictable (no “fighting” the user while typing in the hex field).

## Non-goals

- Advanced pickers (hue wheel/field, OKLCH editor, gradients) (not implemented).
- Arbitrary input formats beyond hex (e.g. `rgb(…)`, named colors) (not implemented).

## Public surface (v1)

- `Value : Color`
- `AllowAlpha : bool`
- `ShowPalette : bool`
- `Palette : IReadOnlyList<Color?>?` (optional override; otherwise derived from `Theme.Scheme`)

## Composition

`ColorPicker` is a composite control built from existing visuals:

- Left: a `SwatchVisual` preview that renders a border, a checkerboard background, and the selected color over it.
- Middle: a channels `Grid` with 4 rows (`R`, `G`, `B`, `A`) and a `Slider<int>` per channel.
  - The alpha slider row is hidden when `AllowAlpha` is `false`.
- Hex editing: a `TextBox` wrapped by a `ValidationPresenter` to display parsing errors.
- Optional palette: a `ComputedVisual(BuildPalette)` that renders a grid of clickable swatches.

## Layout

### Measure / Arrange

- The control delegates measure/arrange to its `_root` visual.
- Default alignments:
  - `HorizontalAlignment = Align.Stretch`
  - `VerticalAlignment = Align.Start`
  - The control itself is not focusable; focus is handled by child controls.

### Internal structure

- The top row is an `HStack` with the swatch on the left and a `VStack` containing channels + hex row on the right.
- The palette (if enabled) is placed below that top row.

## Value synchronization

The control maintains two data flows:

### `Value` → UI controls

- Performed in `PrepareChildren()` via `UpdateControlsFromValue()`.
- Updates:
  - `R/G/B` sliders from `Value.ToRgb()`
  - `A` slider from `Value.A` when `AllowAlpha` is enabled; otherwise forced to `255`
  - hex placeholder and hex text (`#RRGGBB` or `#RRGGBBAA`)

> [!IMPORTANT]
> The hex text is **not** overwritten while the user is editing it: when the hex `TextBox` has focus, updates to `Value`
> do not replace the current hex text.

### UI controls → `Value`

- Slider changes call `UpdateValueFromSliders()`.
- Hex edits are observed through `TextBox.TextDocument.Changed` and parsed by `UpdateValueFromHex()`.

A private guard (`_updatingFromValue`) prevents internal feedback loops.

## Normalization and alpha mode

`Value` is normalized so that:

- When `AllowAlpha` is `false`, the value is forced to `ColorKind.Rgb` (alpha effectively becomes 255).
- When `AllowAlpha` is `true`, the value is represented as `ColorKind.RgbA` so alpha blending can be previewed.
- `Color.Default` is preserved and treated as “no color” in rendering; hex formatting uses `#000000` as the placeholder output.

## Styling

### `ColorPickerStyle`

- Swatch sizing and border glyphs:
  - `SwatchWidth`, `SwatchHeight`, `SwatchGlyphs`, `SwatchBorderStyle`
- Checkerboard colors (alpha preview):
  - `CheckerLight`, `CheckerDark`
  - When not explicitly provided, the checkerboard is derived from the theme background using OKLab mixing.
- Palette configuration:
  - `PaletteColumns`, `PaletteSwatchWidth`, `PaletteSwatchHeight`, `PaletteGap`
  - `PaletteSelectionGlyphs`, `PaletteSelectionStyle`
- Hex formatting:
  - `UppercaseHex`

> [!NOTE]
> `ColorPickerStyle.Padding` exists but is currently not applied by the `ColorPicker` implementation (reserved for future layout customization).

## Rendering details

### Swatch preview

- The swatch draws a border (configurable glyph set + style).
- The interior is filled with a checkerboard pattern (two theme-derived colors).
- The selected `Value` is then drawn as an overlay background on top of the checkerboard.
  - If `Value` is `RgbA`, the overlay relies on the CellBuffer’s alpha blending behavior to show the checkerboard through the color.

### Palette swatches

- Each swatch is rendered as a rectangular background fill (or “empty” if the palette value is `null`).
- The selected swatch draws a border when `swatch.ToRgb()` equals `Value.ToRgb()` (selection ignores alpha).

## Input handling

- `ColorPicker` itself is not focusable; it is interacted with via children:
  - sliders (keyboard + mouse per `Slider<T>` behavior)
  - hex textbox (keyboard per `TextBox` behavior)
  - palette swatches (mouse click sets `Value`)

## Tests and demos

- Tests live in `src/XenoAtom.Terminal.UI.Tests/ColorPickerTests.cs`:
  - hex parsing updates `Value`
  - invalid hex does not change `Value` and shows validation
  - slider updates update `Value`
  - palette click sets `Value`
- Demo: `samples/ControlsDemo/Demos/ColorPickerDemo.cs` shows RGB and RGBA variants side-by-side.

## Future / v2 ideas

- Add alternative color spaces (HSL/HSV/OKLab/OKLCH) with explicit editors.
- Improve palette interactions (keyboard navigation, recent colors, custom palette editing).
- Add “copy/paste” helpers for hex and a dedicated command surface for discoverability (CommandPalette/CommandBar integration).
