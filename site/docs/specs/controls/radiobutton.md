---
title: RadioButton Specs
---

# RadioButton Specs

This document captures design and implementation notes for `RadioButton`.

> [!NOTE]
> For end-user usage and examples, see [RadioButton](../../controls/radiobutton.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: A single-choice selection control that can be grouped with other radio buttons.
- **Grouping model**: Uses an arbitrary `GroupBy` value; when a radio button becomes checked, it unchecks other checked radios in the same group.
- **Content model**: A label visual rendered to the right of the glyph.

## Public API surface

### Type

- `RadioButton : Visual` (sealed)

### Constructors

- `RadioButton()` sets `Focusable = true`.
- `RadioButton(string text, object? groupBy = null, bool isChecked = false)` convenience ctor:
  - sets `Text`, `GroupBy`, `IsChecked`

### Bindable properties

- `Text : Visual?`
  - label visual; common call sites pass a `string` which becomes a `TextBlock` via implicit conversions/extensions
- `IsChecked : bool`
- `GroupBy : object?`
  - group identifier used for mutual exclusion

## Layout & rendering

### Measure

- Measures the label with unbounded width and 1 line height.
- Desired size is:
  - `width = labelWidth + 4` (glyph + space + label)
  - `height = 1`

### Arrange

- Arranges the label at `x = finalRect.X + 2` with width clamped to available.

### Render

- Fills the row with the resolved style so the label can inherit background/foreground.
- Draws:
  - the checked/unchecked glyph at `Bounds.X`
  - a spacer cell at `Bounds.X + 1` (when width allows)
- Glyphs come from `RadioButtonStyle.CheckedGlyph` / `UncheckedGlyph`.

## Interaction

### Keyboard

- `Space` / `Enter`: checks the radio button (if not already checked).

### Pointer

- Left click:
  - press sets an internal `_isPressed` flag
  - release within bounds checks the radio button

### Group unchecking algorithm

When the radio becomes checked:

1) If `GroupBy` is `null`, no group behavior occurs.
2) Otherwise:
   - walk up to the visual tree root
   - enumerate visuals depth-first and uncheck other `RadioButton` instances where:
     - `IsChecked == true`
     - `Equals(other.GroupBy, this.GroupBy)`

This is a simple retained-mode approach that avoids requiring a global registry; it assumes grouped radios are within the same visual tree.

## Styling

### RadioButtonStyle

Key knobs:

- `CheckedGlyph` (default `◉`)
- `UncheckedGlyph` (default `◯`)
- optional styles: `Normal`, `Hovered`, `Focused`, `Disabled`

Default resolution uses theme colors:

- focused: bold + `Theme.FocusBorder` foreground (when available)
- hovered: uses `Theme.Accent` (when available)
- disabled: dim + `Theme.Disabled` (when available)

## Tests & demos

- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/RadioButtonTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/RadioButtonListTests.cs` (the list control, see also `RadioButtonList<T>`)
- Demo:
  - ControlsDemo includes radio button examples.

## Future / v2 ideas

- Add an explicit `RadioGroup` abstraction for cases where the visual-tree scan is too expensive or needs cross-tree grouping.
- Add a “nullable selection” option (allow unchecking or no selection in group) for specific UX needs.
