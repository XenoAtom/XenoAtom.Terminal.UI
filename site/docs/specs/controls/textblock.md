---
title: TextBlock Specs
---

# TextBlock Specs

This document captures design and implementation notes for `TextBlock`.

> [!NOTE]
> For end-user usage and examples, see [TextBlock](../../controls/textblock.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Render read-only text with optional wrapping, alignment, and trimming.
- **Key goals**:
  - correct terminal cell-width handling for Unicode (multi-cell runes, grapheme clusters)
  - predictable single-row behavior by default (common label usage)
  - optional multi-line wrapping (paragraph-like rendering)
  - optional ellipsis trimming for single-line text
  - avoid per-render allocations
- **Non-goals**:
  - rich text / markup (use `Markup` for styled spans)
  - text editing / selection (use `TextBox`, `TextArea`, `TextEditorBase`)

## Implementation notes

- Primary implementation:
  - `src/XenoAtom.Terminal.UI/Controls/TextBlock.cs`
  - `src/XenoAtom.Terminal.UI/Styling/TextBlockStyle.cs`
- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/TextBlockRenderingTests.cs`
- Demo:
  - `samples/ControlsDemo/Demos/TextBlockDemo.cs`

## Public API surface

### Type

- `TextBlock : Visual` (sealed)

### Constructors

- `new TextBlock()`
- `new TextBlock(string text)`
- `new TextBlock(Func<string> textProvider)`
  - Uses binding to re-evaluate the string when dependencies change.

Strings are commonly used where a `Visual` is expected because `Visual` defines an implicit conversion:
- `string -> Visual` creates a `TextBlock` with that string.

### Properties

- `Text : string?` (bindable)
- `Wrap : bool` (bindable)
  - When false (default), the control renders as a single row (height 1).
- `TextAlignment : TextAlignment` (bindable)
  - Supported: Left, Center, Right, Justify.
- `Trimming : TextTrimming` (bindable)
  - Supported: Clip, EndEllipsis, StartEllipsis.
  - Trimming is applied only in single-line rendering.

There are no events and no input handling.

## Layout and rendering

### Measurement model

`TextBlock` measures to a fixed size based on:
- the text's cell width (not string length)
- the wrap flag and available width
- the available height (clamped)

Let:
- `text = Text ?? ""`
- `naturalWidth = TerminalTextUtility.GetWidth(text)`
- `width = min(constraints.MaxWidth, naturalWidth)`

When `Wrap` is false or `width == 0`:
- desired size is `(width, 1)` (height is clamped to 1)

When `Wrap` is true:
- height is computed as the number of wrapped lines for the given width (at least 1)
- desired height is clamped to `constraints.MaxHeight`

Notes:
- The measured width does not attempt to "stretch to fill" when there is more horizontal space than the text requires.
  Stretching is handled by layout via `HorizontalAlignment = Align.Stretch` if desired.

### Rendering model

Render uses:
- `TerminalTextUtility` to compute cell widths and safe slice boundaries
- `TextBlockStyle.ResolveTextStyle(theme)` to determine the style applied to written glyph cells

If `TextBlockStyle.FillBackground` is enabled and a `Background` is set:
- the control fills its entire `Bounds` rectangle with spaces using `ResolveFillStyle(theme)`
- then renders text on top

If `FillBackground` is false:
- background colors (when set) apply only to the glyph cells written by the text rendering operations

### Single-line rendering (no wrap or height == 1)

Single-line rendering supports trimming:

- `Clip`:
  - clip the text to `Bounds.Width` cells and write it
- `EndEllipsis` / `StartEllipsis`:
  - if the text does not fit and `Bounds.Width == 1`, render only an ellipsis glyph (U+2026)
  - otherwise reserve 1 cell for the ellipsis and clip the remainder

Alignment is applied after trimming/clipping:
- `TextAlignment.Left`: `x = Bounds.X`
- `TextAlignment.Center`: centered
- `TextAlignment.Right`: right aligned

Implementation details:
- clipping is performed with `TerminalTextUtility.TryGetIndexAtCell(...)` so the control does not split multi-cell runes
  or grapheme clusters incorrectly
- start-ellipsis uses `TerminalTextUtility.GetPreviousTextElementIndex(...)` to find a safe suffix boundary from the end

### Multi-line wrapping (Wrap == true and height > 1)

Wrapping logic is whitespace-based and "paragraph-like":
- leading whitespace on each new line is skipped
- the slice for a line is the largest prefix that fits within `Bounds.Width` cells
- if the slice does not reach the end of the string, the algorithm attempts to wrap at the last whitespace within the
  slice
- newline characters are treated as whitespace, so explicit newlines naturally force breaks
- after selecting a wrap slice, trailing whitespace is skipped for the next line start

In wrapped mode:
- trimming (ellipsis) is not applied; each line is clipped to width
- `TextAlignment.Justify` is supported for non-last lines only

### Justification

When `TextAlignment == TextAlignment.Justify` and the current line is not the last line of the wrapped text:
- the renderer attempts to distribute extra spaces between words so the line fills the entire width

Constraints and fallback:
- the justification algorithm supports up to 32 words per line (uses `stackalloc` for performance)
- if there are too many words, or if the base line already exceeds the width, the renderer falls back to normal clipped
  writing (left aligned)

## Styling

### TextBlockStyle

`TextBlockStyle` provides optional overrides for:
- `Foreground : Color?`
- `Background : Color?`
- `TextStyle : TextStyle` (decorations like bold/underline)
- `FillBackground : bool`

Important default behavior:
- `TextBlockStyle.Default` does not set any explicit colors, which means `TextBlock` commonly inherits from whatever the
  buffer or parent visuals wrote before it. This is intentional and enables patterns like:
  - filling a row background in a parent, then rendering text that inherits that baseline

## Tests

`TextBlockRenderingTests` validates:
- end-ellipsis trimming renders U+2026 for small widths
- start-ellipsis trimming renders U+2026 for small widths
- center alignment works when horizontally stretched
- background styling applies to text-only vs filled background depending on `FillBackground`

## Future / v2 ideas

- Optional explicit line break handling with preserved empty lines (current behavior treats whitespace uniformly).
- Optional trimming behavior for wrapped mode (e.g., show ellipsis on the last visible line when height is constrained).
- Optional hyphenation or more advanced line breaking (current behavior wraps at whitespace only).
