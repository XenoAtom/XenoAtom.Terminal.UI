---
title: CheckBox Specs
---

# CheckBox Specs

This document captures design and implementation notes for `CheckBox`.

> [!NOTE]
> For end-user usage and examples, see [CheckBox](../../controls/checkbox.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: A toggle control representing a boolean value with an optional label visual.
- **Interaction**: toggles on `Space`/`Enter` and left click.

## Public API surface

### Type

- `CheckBox : Visual` (sealed)

### Constructors

- `CheckBox()` sets `Focusable = true`.
- `CheckBox(string text, bool isChecked = false)` convenience ctor:
  - sets `Text` and `IsChecked`

### Bindable properties

- `Text : Visual?` (label)
- `IsChecked : bool`

## Layout & rendering

### Measure

Measure depends on:

- `CheckBoxStyle.CheckedGlyph` / `UncheckedGlyph` rune width
- `CheckBoxStyle.SpaceBetweenGlyphAndText`
- label desired width (measured unbounded width, 1 line height)

Desired size is a single row:

- `width = glyphWidth + gap + labelWidth`
- `height = 1`

### Arrange

- Arranges the label to the right of the glyph and gap:
  - `x = finalRect.X + glyphWidth + gap`
  - width clamped to available

### Render

- Fills the whole row with the resolved style so children inheriting `Style.None` get consistent colors.
- Draws:
  - checked/unchecked glyph at `Bounds.X` (bold)
  - gap spaces after the glyph (when a label exists)

## Interaction

### Keyboard

- `Space` / `Enter`: toggles `IsChecked`.

### Pointer

- Left click toggles `IsChecked` immediately on press.

## Styling

### CheckBoxStyle

Key knobs:

- `CheckedGlyph` / `UncheckedGlyph` (defaults `☑` / `☐`)
- `SpaceBetweenGlyphAndText` (default `2`)
- optional styles: `Normal`, `Hovered`, `Focused`, `Disabled`

Default style resolution:

- focused: bold + `Theme.FocusBorder` (when available)
- hovered: uses `Theme.Accent` (when available)
- disabled: dim + `Theme.Disabled` (when available)

## Tests & demos

- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/CheckBoxTests.cs`
- Demo:
  - ControlsDemo includes checkbox examples.

## Future / v2 ideas

- Add an indeterminate state (tri-state) if needed for hierarchical selection UIs.
