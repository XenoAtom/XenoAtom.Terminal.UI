---
title: TextFiglet Specs
---

# TextFiglet Specs

This document captures design and implementation notes for `TextFiglet`.

> [!NOTE]
> For end-user usage and examples, see [TextFiglet](../../controls/textfiglet.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Render big banner text using FIGlet fonts (multi-line ASCII-art-like glyphs).
- **Key goals**:
  - cache rendered FIGlet lines so normal layout/render is cheap
  - support dynamic text via bindings (`Func<string>`)
  - allow choosing fonts (built-in block font + embedded predefined fonts + loaded `.flf` fonts)
  - allow simple spacing and missing-glyph behavior
  - allow horizontal alignment inside the arranged bounds
- **Non-goals**:
  - rich styling inside the banner (FIGlet output is plain text; use `Markup` or custom visuals for richer output)
  - paragraph handling inside a single TextFiglet (newlines are intentionally treated as spaces; stack multiple TextFiglet
    instances for multi-paragraph banners)

## Implementation notes

- Primary implementation:
  - `src/XenoAtom.Terminal.UI/Controls/TextFiglet.cs`
  - `src/XenoAtom.Terminal.UI/Styling/TextFigletStyle.cs`
- Supporting FIGlet implementation:
  - `src/XenoAtom.Terminal.UI/Figlet/FigletFont.cs`
  - `src/XenoAtom.Terminal.UI/Figlet/FigletRenderOptions.cs`
  - `src/XenoAtom.Terminal.UI/Figlet/FigletPredefinedFont*.g.cs` (embedded fonts, generated)
- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/FigletTests.cs`
- Demo:
  - `samples/ControlsDemo/Demos/TextFigletDemo.cs`

## Public API surface

### Type

- `TextFiglet : Visual` (sealed)

### Constructors

- `new TextFiglet()`
- `new TextFiglet(string text)`
- `new TextFiglet(Func<string> textProvider)`
  - Uses binding to re-evaluate the string when dependencies change.

### Properties

- `Text : string?` (bindable)
- `Font : FigletFont?` (bindable)
- `LetterSpacing : int` (bindable)
  - Coerced to `>= 0`.
- `TrimTrailingSpaces : bool` (bindable)
- `MissingGlyph : char` (bindable)
- `TextAlignment : TextAlignment` (bindable)
  - Effective support: Left, Center, Right.
  - Justify is treated as left (there is no special justify logic in TextFiglet).

Defaults (constructor):
- `Font = FigletFont.Block`
- `LetterSpacing = 1`
- `TrimTrailingSpaces = true`
- `MissingGlyph = '?'`
- `TextAlignment = TextAlignment.Left`

There are no events and no input handling.

## FIGlet font model (FigletFont)

`FigletFont` represents a FIGlet font (typically from a `.flf` file):
- fixed `Height` (number of rows per glyph)
- glyphs primarily for ASCII code points 32..126, with optional code-tagged glyphs for additional characters

Font sources:
- `FigletFont.Block` (built-in lazy-loaded block font)
- embedded fonts via `FigletPredefinedFont.*` (generated, lazy-loaded)
- external fonts via `FigletFont.Load(path)` or `FigletFont.Load(stream)`

`FigletFont.RenderLines(...)` behavior:
- returns an array of `Height` strings (one string per row), or empty if input is empty
- carriage returns (`\r`) are ignored
- newlines (`\n`) are currently treated as spaces (v1 choice); users can stack multiple TextFiglet controls to render
  multiple banner paragraphs
- if a glyph is missing, it falls back to `MissingGlyph` (default `?`)
- `LetterSpacing` inserts spaces between characters after the first rendered character
- `TrimTrailingSpaces` trims spaces on each output line after render

## Caching and invalidation

TextFiglet caches the rendered lines and computed width.

Cache key:
- `Font` is compared by reference (`ReferenceEquals`)
- `Text` is compared ordinally
- render options are compared by value (`FigletRenderOptions` record struct)

When the cache is invalidated, TextFiglet calls `font.RenderLines(text, options)` and then computes:
- `_cachedWidth` as the maximum cell width across all rendered lines (`TerminalTextUtility.GetWidth(...)`)

This makes measure/render cheap when the inputs are stable.

## Layout and rendering

### Measure

Measure forces the cache to be up-to-date, then returns a fixed desired size:
- width: `_cachedWidth`
- height: `_cachedLines.Length`

The size is clamped to the provided constraints.

### Render

Render:
- forces the cache to be up-to-date
- writes up to `min(Bounds.Height, _cachedLines.Length)` lines
- applies horizontal alignment per line when there is extra space:
  - Left: `x = Bounds.X`
  - Center: `x = Bounds.X + ((Bounds.Width - lineWidth) / 2)`
  - Right: `x = Bounds.X + (Bounds.Width - lineWidth)`

Important details:
- The line width is computed in terminal cells (`TerminalTextUtility.GetWidth`).
- There is no explicit clipping in TextFiglet; `CellBuffer.WriteText(...)` performs the final clipping to the buffer.
- TextFiglet does not fill background. If a background is desired, wrap it (e.g. in `Border` or `Padder`) or apply a
  background style in an ancestor that fills the area.

## Styling

### TextFigletStyle

`TextFigletStyle` currently controls only the style used to draw the banner text:
- `TextStyle : Style?`
  - when null, it resolves to `theme.ForegroundTextStyle()`

Note: despite the property name, `TextFigletStyle.TextStyle` is a `Style` (foreground/background/decorations), not a
`TextStyle` enum value.

## Tests

`FigletTests` cover:
- parsing minimal `.flf` fonts
- `FigletFont.RenderLines` producing multi-line output for a block font
- `TextFiglet` rendering with a custom block font (ensures expected characters appear)

## Future / v2 ideas

- Optional multi-paragraph rendering (treat `\n` as paragraph breaks) behind a flag or separate control.
- Optional trimming/clipping controls (currently it relies on buffer clipping).
- More style options (e.g., background fill, per-character colorization) as separate composition helpers.
