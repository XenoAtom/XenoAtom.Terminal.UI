---
title: MaskedInput Specs
---

# MaskedInput Specs

This document captures the design and implementation details of `MaskedInput`.

> [!NOTE]
> For end-user usage and examples, see [MaskedInput](../../controls/maskedinput.md).

## Overview

- **Status**: Implemented
- **Purpose**: A template-based single-line input control for structured values (credit card numbers, dates, identifiers, etc.).
- **Key feature**: It reuses the `TextEditorCore` infrastructure (caret, selection, clipboard) while enforcing per-slot constraints.

## Goals

- Always show the mask: literal separators are visible, empty slots show placeholder glyphs.
- Reject invalid input on insertion/paste instead of allowing it and marking invalid later.
- Provide a slot-based `Value` representation suitable for binding to models.
- Keep the control compatible with the binding dirty model (no manual render requests).

## Non-goals

- Multi-line input (this control is always single-line).
- Per-character variable-width layout (mask is a fixed-length template in terminal cells).
- Full Unicode grapheme support per slot (see limitations).

## Public API

`MaskedInput` derives from `TextEditorBase` and exposes:

- `Template : string?` (bindable) - the template mask.
- `Value : string?` (bindable) - the slot string (separators are not included).
- `CompactValue : string` - `Value` with empty slots removed.
- `IsValid : bool` - `true` when all required slots are filled and all filled slots match constraints.

## Template format

The template is a string composed of:

- Token characters describing editable slots with constraints.
- Literal separators (any character that is not a token or directive).

Escaping and directives:

- `\\` escapes the next character so it is treated as a literal.
- `>` enables uppercasing for subsequent alphabetic tokens.
- `<` enables lowercasing for subsequent alphabetic tokens.
- `!` disables automatic case conversion.
- A template can end with `;c` where `c` is the placeholder glyph for all empty slots.

Template notes:

- Case directives are not rendered as part of the mask.
- The placeholder terminator is only recognized at the end of the template (last two characters are `;` and `c`).

### Token characters

Token characters map to internal token kinds:

- `A` / `a`: alphabetic (required / optional)
- `N` / `n`: alphanumeric (required / optional)
- `X` / `x`: non-space (required / optional)
- `9` / `0`: digit (required / optional)
- `D` / `d`: digit 1-9 (required / optional)
- `#`: digit or sign (+/-) (optional)
- `H` / `h`: hexadecimal digit (required / optional)
- `B` / `b`: binary digit (required / optional)

## Value model

### `Value`

`Value` stores only the slot characters, not the separators. It is positional:

- Slot index 0 maps to the first editable position in the template.
- Empty slots are represented by a space character.
- Trailing empty slots are trimmed from the returned string.

Example:

- Template: `99-99;_`
- Display: `12-34`
- Value: `1234`

### `CompactValue`

`CompactValue` is derived from `Value` by removing all space characters. This is a convenience API for masks where empty slots
should be ignored by consumers.

### `IsValid`

`IsValid` checks the current masked document:

- Required slots must not be empty.
- Filled slots must satisfy the token constraints.

## Binding and internal synchronization

Internally, `MaskedInput` uses a private `ITextDocument` implementation (MaskedInputDocument) that:

- Stores a single line of text that matches the template length (literals plus slot characters).
- Applies edits by clearing/editing editable positions only and skipping over literals.

Synchronization:

- `Template` is parsed into token metadata (cached by value equality on the template string).
- `PrepareChildren` synchronizes the internal document from `Template` and `Value`.
- When the document changes due to user edits, `MaskedInput` recomputes `Value` and updates the bindable property.

Implementation detail:

- `RenderOverride` reads `Template` and `Value` to participate in dependency tracking even if the current render path does not
  otherwise need the values. This is intentional for the binding dirty model.

## Layout

### Measure

`MeasureCore` requests a fixed size derived from the template length and `MaskedInputStyle.Padding`:

- Width: `tokens.Length + padding.Horizontal`, clamped to the available width.
- Height: `1 + padding.Vertical`, clamped to the available height (minimum 1).

### Arrange

`ArrangeCore` computes a content rectangle from padding and calls `UpdateEditorLayout(...)` from the text editor base class.

## Rendering

Rendering is delegated to the text editor base class (`RenderEditor`), but `MaskedInput` overrides `WriteTextSegment` to render:

- Literal separators with `MaskedInputStyle.SeparatorForeground` (or theme muted foreground).
- Empty slots as placeholders using either the template placeholder (`;c`) or style defaults:
  - Digit-like tokens: `MaskedInputStyle.DigitPlaceholderChar`
  - Alpha/alphanumeric tokens: `MaskedInputStyle.AlphaPlaceholderChar`
  - Fallback: `MaskedInputStyle.DefaultPlaceholderChar`
- Placeholder glyphs are rendered with the placeholder style and `TextStyle.Dim`.

The control fills its padded background using `MaskedInputStyle.BackgroundStyle(...)`.

## Input behavior

Because `MaskedInput` derives from `TextEditorBase`, it supports:

- Caret movement (arrows, home/end, etc.)
- Selection (keyboard and mouse)
- Clipboard (copy/cut/paste)
- Undo/redo (as provided by the editor infrastructure)

MaskedInput-specific behavior:

- Typed/pasted characters that do not match the token constraint are ignored.
- Insertion skips separators and fills the next editable slot.
- After successful insert/paste/cut operations, the caret snaps to the next empty editable slot.

## Styling

`MaskedInput` uses `MaskedInputStyle` (`src/XenoAtom.Terminal.UI/Styling/MaskedInputStyle.cs`):

- Inherits from `TextBoxStyle` to reuse padding, focused background fill, and selection visuals.
- Adds placeholder glyph defaults (`DefaultPlaceholderChar`, `DigitPlaceholderChar`, `AlphaPlaceholderChar`).
- Adds `SeparatorForeground` and a specialized `PlaceholderCellStyle`.

## Tests, demos, and docs

Tests:

- `src/XenoAtom.Terminal.UI.Tests/MaskedInputTests.cs` (placeholders, token filtering, case directives, selection/copy)

Demo:

- `samples/ControlsDemo/Demos/MaskedInputDemo.cs`

User documentation:

- `site/docs/controls/maskedinput.md`

## Known limitations

- Each slot expects a single `char`. Multi-char grapheme clusters and surrogate pairs are currently ignored during insertion.
- There is no per-slot placeholder; placeholders are chosen from `;c` or style defaults.

## Future ideas

- Surface editor commands explicitly on `MaskedInput` for discoverability (command palette / command bar integration).
- Allow per-token placeholder override and/or per-token styles.
- Improve Unicode input handling per slot (support a `Rune` per slot instead of `char`).
