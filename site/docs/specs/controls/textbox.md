---
title: TextBox Specs
---

# TextBox Specs

This document captures design and implementation notes for `TextBox`.

> [!NOTE]
> For end-user usage and examples, see [TextBox](../../controls/textbox.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: A single-line, focusable text input built on the shared text editing infrastructure (`TextEditorBase`).
- **Key features**:
  - selection, clipboard, undo/redo, word navigation (via `TextEditorBase` / `TextEditorCore`)
  - horizontal scrolling with optional overflow indicators
  - password mode with reveal policies
  - placeholder rendering (when empty)

## Public API surface

### Type

- `TextBox : TextEditorBase`

### Constructors

- `TextBox()`:
  - sets `HorizontalAlignment = Align.Stretch`
  - initializes a `DynamicTextDocument` bound to the `Text` property
  - sets defaults: `IsPassword = false`, `PasswordRevealMode = Never`, `ClipboardMode = CopyText`
- `TextBox(string? text)` sets initial `Text`.

### Bindable properties (TextBox)

- `Text : string?`
- `IsPassword : bool`
- `PasswordRevealMode : PasswordRevealMode` (`Never`, `WhileFocused`, `Always`)
- `ClipboardMode : TextBoxClipboardMode`
  - `CopyText`: allows standard copy/cut behavior
  - other modes: prevent `Ctrl+C`/`Ctrl+X` (for sensitive inputs)
- `TextAlignment : TextAlignment`

### Key behavior overrides

- `IsSingleLine = true`
- `AcceptsReturn = false`
- `Alignment => TextAlignment`
- `ShowPlaceholderWhenUnfocusedOnly = false`
- `GetTextBoxStyle()` resolves `TextBoxStyle` from the style environment.

## Layout & rendering

### Measure

`TextBox` intentionally measures to a “reasonable default” single-line size:

- `width = clamp(availableWidth, min: 10, max: 24)`
- `height = 1 + TextBoxStyle.Padding.Vertical` (clamped to available height)
- returns `SizeHints.Fixed(...)`

This is a *default sizing policy*; parent layout and constraints (e.g., `MinWidth`, `MaxWidth`, containers) still control the final size.

### Arrange

`ArrangeCore` computes a padded “base rect” and then allocates space for overflow indicators:

1) `baseRect = finalRect` minus `TextBoxStyle.Padding`
2) `UpdateEditorLayoutForOverflowIndicators(baseRect, style)`:
   - calls `UpdateEditorLayout(editorRect)` (from `TextEditorBase`) to update scroll viewport/extents
   - decides whether to show left/right indicators based on:
     - `Scroll.OffsetX > 0` (left)
     - `Scroll.OffsetX + Scroll.ViewportWidth < Scroll.ExtentWidth` (right)
   - if indicators are visible, shrinks the editor rect by 1 cell on the corresponding side(s)
   - repeats up to 3 passes to converge (because `UpdateEditorLayout` can change `Scroll.*` values)

### Render

Rendering is split into two parts:

- **Background fill**: fills the padded base rect with `TextBoxStyle.BackgroundStyle(theme, focused)`
- **Text editor**: calls `RenderEditor(...)` into the computed editor rect
- **Overflow indicators**: when active, draws glyphs in the reserved cells adjacent to the editor rect (inside the padded base rect), using `TextBoxStyle.OverflowIndicatorStyle(theme)`

## Text editing & document binding

- `TextBox` uses a `DynamicTextDocument` so the editor reads/writes through the `Text` bindable property.
- Placeholder rendering comes from `TextEditorBase.Placeholder` and is styled with `TextBoxStyle.PlaceholderStyle(...)`. The placeholder stays visible while the editor is empty, including when focused, and renders with `TextStyle.Dim`.
- Selection rendering uses `TextBoxStyle.SelectionStyle(theme)` (currently bold + selection background color).

## Password mode

- When `IsPassword` is enabled and `PasswordRevealMode` does not allow reveal, `TextBox` overrides `WriteTextSegment(...)` to render a mask glyph (`TextBoxStyle.PasswordMaskGlyph`) for the text region.
- The mask glyph must be single-cell wide; otherwise the control falls back to `*` to preserve layout.

## Styling

### TextBoxStyle

`TextBoxStyle` controls:

- `Padding` (default `Thickness(1,0,1,0)`)
- `PasswordMaskGlyph` (default `•`)
- `OverflowIndicatorLeft` / `OverflowIndicatorRight` (defaults `←` / `→`, can be disabled by setting to `null`)
- optional color overrides (`Border`, `FocusBorder`, `Selection`, `Background`, `Placeholder`)

The default style resolution uses theme colors like `Theme.InputFill`, `Theme.InputFillFocused`, `Theme.SurfaceAlt`, and `Theme.Muted`.

## Tests & demos

- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/TextBoxInputTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/TextBoxOverflowIndicatorTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/TextBoxPasswordTests.cs`
- Demo:
  - See the ControlsDemo “TextBox” page.

## Future / v2 ideas

- Expose a configurable default measure policy (e.g., desired width) without requiring custom subclasses.
- Add a dedicated “secure input” mode that disables both copy and cut, and optionally disables selection.
