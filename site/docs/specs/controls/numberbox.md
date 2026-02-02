---
title: NumberBox Specs
---

# NumberBox Specs

This document captures the design and implementation details of `NumberBox<T>`.

> [!NOTE]
> For end-user usage and examples, see [NumberBox](../../controls/numberbox.md).

## Overview

- **Status**: Implemented
- **Purpose**: A single-line numeric editor with validation and a bindable numeric `Value`.
- **Base**: Built on `TextEditorCore` via `TextEditorBase` (caret, selection, clipboard, undo/redo).
- **Key behavior**: `Value` only updates when the current text parses successfully (and optional validation succeeds).

## Goals

- Provide a rich editing experience for numeric input without reimplementing a text editor.
- Keep a strongly-typed bindable `Value` while allowing the user to temporarily enter invalid text.
- Display an inline validation message when parsing or validation fails.
- Avoid overwriting bound values before the user edits the control (important for binding scenarios).

## Non-goals

- Multi-line numeric input.
- Locale-independent parsing by default (defaults follow .NET behavior unless a `FormatProvider` is set).
- Arbitrary formatted editing (formatting is applied when synchronizing from `Value`, not while the user is typing).

## Public API

`NumberBox<T>` is defined as:

- `public partial class NumberBox<T> : TextEditorBase where T : struct, INumber<T>`

Bindable properties:

- `Value : T` - the numeric value (primary binding surface).
- `TextAlignment : TextAlignment` - alignment of the text inside the editor.
- `ShowValidationMessage : bool` - whether validation messages are shown.
- `InvalidNumberMessage : string?` - message used when parsing fails.
- `ValueFormatter : Delegator<Func<T, string>>` - optional formatter used when converting `Value` to displayed text.
- `ParseStyles : NumberStyles` - number styles used for parsing (default `NumberStyles.Number`).
- `FormatProvider : IFormatProvider?` - provider used for parsing/formatting (default `null`).
- `ValueValidator : Delegator<Func<T, string?>>` - optional semantic validator for parsed values.

Additional internal bindable property:

- `Text : string?` - backing text for the editor (used via a `DynamicTextDocument`). This is intentionally internal and is
  mainly for diagnostics/tests.

## Value and text synchronization

`NumberBox<T>` maintains two representations:

- `Value : T` (typed)
- `Text : string` (editable text)

### Editing direction (Text -> Value)

On text changes (user edits):

1. If the text is empty, no parse error is shown and `Value` is not updated.
2. Otherwise the control attempts `T.TryParse(text, ParseStyles, FormatProvider, out parsed)`.
3. If parsing fails, the validation message is set to `InvalidNumberMessage` and `Value` is not updated.
4. If parsing succeeds, `ValueValidator` (if set) runs:
   - `null` means valid
   - any non-empty string becomes the validation message and `Value` is not updated
5. If validation passes, the validation message is cleared and `Value` is updated to the parsed value.

### Display direction (Value -> Text)

When the control decides that `Value` is the source of truth, it formats `Value` into `Text` using:

1. `ValueFormatter` (if set), otherwise
2. `IFormattable.ToString(null, FormatProvider)` when supported, otherwise
3. `Value.ToString()`

Key rules:

- Before the user edits the control, `Value` is treated as the source of truth even if validation is triggered by
  unrelated property changes. This avoids overwriting a bound state.
- When not focused, `RenderOverride` synchronizes `Text` from `Value` to keep the display current even if `Value` changes
  through binding mechanisms that do not trigger the generated change callback.

## Layout

### Measure

`NumberBox<T>` currently requests a fixed width chosen as:

- `width = max(10, min(availableWidth, 24))`

Height is:

- the editor line height (1 + text box padding vertical)
- plus an optional validation area when a message is present and `ShowValidationMessage` is true
- plus an optional gap (`ValidationStyle.Gap`) between the editor and the validation message.

### Arrange

The final rectangle is split into:

- editor area at the top
- optional validation area below

The editor uses `TextBoxStyle.Padding` and calls `UpdateEditorLayout(...)` from the base editor infrastructure.

## Rendering

### Editor rendering

The editor portion uses `TextBoxStyle` (via `GetTextBoxStyle()`):

- background fill
- selection style
- placeholder style
- caret and text rendering (provided by `TextEditorBase`)

### Horizontal overflow indicators

`NumberBox<T>` supports "overflow indicators" (left/right glyphs) when the edited text is horizontally scrolled.

- Indicator glyphs and style are taken from `TextBoxStyle`.
- The editor content rectangle is shrunk by 1 cell on the left and/or right to make room for indicators.
- The control performs up to 3 passes during arrange to reach a stable combination of:
  - editor rect width
  - `Scroll.OffsetX` / `Scroll.ViewportWidth` / `Scroll.ExtentWidth`
  - indicator visibility

### Validation message rendering

Validation is rendered using an internal `ValidationMessageHost` child:

- The severity and message content is stored as `ValidationMessage`.
- Styling is resolved via `ValidationStyle` (not `TextBoxStyle`).
- The host can wrap message text when the message is a `TextBlock` or `Markup`.

## Input handling

`NumberBox<T>` inherits all text editor behaviors from `TextEditorBase`:

- caret movement, selection, mouse selection
- copy/cut/paste, undo/redo

The numeric-specific logic is implemented as validation on every text change.

## Styling

`NumberBox<T>` uses:

- `TextBoxStyle` for the editor portion (padding, background, selection, placeholder, overflow indicators).
- `ValidationStyle` for the validation message line (padding, glyphs, colors, gap).

Note: a `NumberBoxStyle` type exists in the codebase, but the current `NumberBox<T>` implementation does not consult it.
If `NumberBoxStyle` is intended to be the primary style surface for validation message customization, this should be wired
up in a future change (or the unused style should be removed).

## Tests & demos

Tests:

- `src/XenoAtom.Terminal.UI.Tests/NumberBoxInputTests.cs` (parsing, validation, binding updates)
- `src/XenoAtom.Terminal.UI.Tests/DataPresenterTests.cs` (default template uses `NumberBox<int>` for `int` editor role)

Demo:

- `samples/ControlsDemo/Demos/NumberBoxDemo.cs`

## Future / v2 ideas

- Allow the control width policy to be style-driven or content-driven (instead of fixed 10..24).
- Add an optional `Min`/`Max` value clamp mode (either strict or coercing) as a first-class feature.
- Provide a dedicated "display format while unfocused" vs "editing format while focused" mode for better UX.
- Wire `NumberBoxStyle` into the implementation (or remove it if obsolete) and add deterministic rendering tests for the
  validation line and overflow indicators.
