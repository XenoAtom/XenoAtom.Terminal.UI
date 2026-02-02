---
title: TextArea Specs
---

# TextArea Specs

This document captures design and implementation notes for `TextArea`.

> [!NOTE]
> For end-user usage and examples, see [TextArea](../../controls/textarea.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: A multi-line text editor built on the shared text editing infrastructure (`TextEditorBase`).
- **Default behavior**:
  - word wrapping enabled
  - accepts `Tab` and `Enter`
  - stretches in both directions (`Align.Stretch`)
- **Integrated feature**: an embedded `SearchReplacePopup` (find/replace) arranged inside the editor surface.

## Public API surface

### Type

- `TextArea : TextEditorBase` (sealed)

### Constructors

- `TextArea()`:
  - sets `AcceptTab = true`, `WordWrap = true`
  - sets `HorizontalAlignment/VerticalAlignment = Align.Stretch`
  - sets `TextDocument` to a `DynamicTextDocument` bound to `Text`
  - attaches an internal `SearchReplacePopup`
  - registers commands:
    - `TextEditor.Find` (`Ctrl+F`)
    - `TextEditor.Replace` (`Ctrl+H`)
- `TextArea(string? text)` sets initial `Text`.

### Bindable properties (TextArea)

- `Text : string?`

Most editing behavior is configured through inherited bindables on `TextEditorBase` (e.g. `WordWrap`, `AcceptTab`, `Placeholder`, selection/caret state, etc.).

## Layout & rendering

### Measure

`TextArea` uses a fixed default size (clamped by constraints):

- `width = 32`
- `height = 10`
- returns `SizeHints.Fixed(constraints.Clamp(...))`

### Arrange

Arrange computes a padded content rectangle:

- `contentRect = finalRect` minus `TextAreaStyle.Padding`
- `UpdateEditorLayout(contentRect)` updates the editor’s internal scroll model and layout (caret, wrapping, viewport)
- `_searchPopup.ArrangeWithin(contentRect)` places the popup inside the editor surface

### Render

- Fills the padded content rectangle with `TextAreaStyle.BackgroundStyle(theme, focused)`.
- Calls `RenderEditor(...)` into `contentRect` using:
  - `TextAreaStyle.SelectionStyle(theme)`
  - `TextAreaStyle.PlaceholderStyle(theme, focused)`
- The `SearchReplacePopup` renders as a child overlay within the same content region.

## Text editing & scrolling integration

Because `TextEditorBase` implements `IScrollable`, a `TextArea` provides a `ScrollModel` to containers like `ScrollViewer`.

Important implication:

- When placed inside a `ScrollViewer`, the viewer will prefer the content-owned scroll model mode (it will not call `ScrollModel.SetViewport` and will derive bar visibility from the model’s extent/viewport).

## Commands & discoverability

`TextArea` registers commands so that they are discoverable through UI surfaces like `CommandBar` / `CommandPalette`:

- `Ctrl+F`: open find
- `Ctrl+H`: open replace

The popup open logic is implemented via `TryOpenSearchReplacePopup(...)` and delegates to the internal `_searchPopup`.

## Styling

### TextAreaStyle

`TextAreaStyle` controls:

- `Padding` (default `Thickness(1,0,1,0)`)
- optional color overrides: `Border`, `FocusBorder`, `Selection`, `Background`, `Placeholder`
- theme-based resolution:
  - selection is bold + selection background
  - background uses `Theme.InputFill` / `Theme.InputFillFocused` and surface colors as fallbacks

## Tests & demos

- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/TextAreaTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/TextAreaSearchReplaceTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/TextEditorAutoScrollTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/TextEditorUndoRedoTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/GraphemeEditingTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/ScrollViewerTextAreaInteractionTests.cs`
- Demo:
  - ControlsDemo includes a TextArea page (including find/replace UI).

## Future / v2 ideas

- Add an opt-in visible border/chrome variant (TextArea currently paints only the padded background surface).
- Provide built-in gutters (line numbers, diagnostics) as optional layered visuals.
