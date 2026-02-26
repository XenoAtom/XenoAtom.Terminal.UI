---
title: Paragraph Specs
---

# Paragraph Specs

This document specifies a new display-only control for **XenoAtom.Terminal.UI**: **`Paragraph`**.

`Paragraph` fills the gap between:

- `TextBlock` (plain text, no inline styles), and
- `Markup` (ANSI markup parsing).

`Paragraph` renders **plain text + style runs + hyperlinks** directly, with paragraph-like wrapping and optional
indentation/prefix features required by document renderers (Markdown, chat bubbles, etc.).

---

## Overview

- **Status**: Planned
- **Primary purpose**: Render a paragraph of rich text (style runs + hyperlinks) efficiently without markup parsing.
- **Key scenarios**:
  - Markdown rendering (paragraphs/headings/list items/quotes),
  - table cells with inline styles without allocating markup strings,
  - rich conversation feeds when hosted by `DocumentFlow`.
- **Non-goals**:
  - editing/caret/selection (use `TextBox` / `TextArea` / `TextEditorBase`),
  - interactive inline widgets (links are rendered as OSC 8 hyperlinks, not clickable visuals),
  - full layout engine (no nested inline elements beyond style runs).

---

## Public API surface (v1)

### Type

- `Paragraph : Visual` (sealed)

### Constructors

Follow the control constructor conventions:

- `new Paragraph()`
- `new Paragraph(string text)`
- `new Paragraph(Func<string> text)`
- `new Paragraph(Binding<string?> text)`

### Bindable properties

Text:

- `Text : string?` (bindable)

Text layout:

- `Wrap : bool` (bindable; default `true` for paragraph-like rendering)
- `TextAlignment : TextAlignment` (bindable; default `Left`)
- `Trimming : TextTrimming` (bindable; applies only when `Wrap == false`)

Inline styling:

- `Runs : StyledRun[]` (bindable or settable; default empty)
- `Hyperlinks : HyperlinkRun[]` (bindable or settable; default empty)

Indentation and prefixes:

- `Indent : int` (bindable; left padding in cells; default `0`)
- `HangingIndent : int` (bindable; extra indent on wrapped continuation lines; default `0`)
- `LinePrefix : string?` (bindable; first line prefix; default `null`)
- `ContinuationPrefix : string?` (bindable; continuation line prefix; default `null`)
- `PrefixStyle : Style` (bindable; applied when rendering prefixes; default `Style.None`)

> [!NOTE]
> `Runs` and `Hyperlinks` are modeled as arrays because the primary producer (Markdown renderer) naturally generates
> contiguous spans. The control should treat `Array.Empty<T>()` as the common case and avoid defensive copies.

### `HyperlinkRun`

Proposed span type:

```csharp
namespace XenoAtom.Terminal.UI.Controls;

public readonly record struct HyperlinkRun(int Start, int Length, string Uri);
```

Semantics:

- `Start`/`Length` are in UTF-16 indices within `Text` (same convention as `StyledRun`).
- Overlapping hyperlink runs are not supported in v1 (last-wins if produced).

---

## Layout & rendering

### Measurement

`Paragraph` measures similarly to `TextBlock`, but must account for:

- indentation/prefix widths,
- hanging indent on wrapped lines,
- hard line breaks (newlines),
- the chosen wrapping width.

Measurement rules:

- Width is clamped to constraints.
- Height is:
  - `1` when `Wrap == false`,
  - otherwise computed as the number of wrapped lines for the given width (at least `1`).

The wrap algorithm MUST be terminal-width correct (grapheme clusters and wide runes) and should reuse
`TerminalTextUtility` for width calculations and safe slice boundaries.

### Wrapping model

Wrapping behavior should match `TextBlock`’s paragraph-like wrapping rules, with additional indentation:

- First line:
  - left inset = `Indent` + `width(LinePrefix)`
  - available width = `Bounds.Width - leftInset`
- Continuation lines:
  - left inset = `Indent + HangingIndent` + `width(ContinuationPrefix)`
  - available width = `Bounds.Width - leftInset`

`LinePrefix` is rendered only on the first physical line. `ContinuationPrefix` is rendered on wrapped continuation lines.

### Alignment

Alignment applies to the **text portion** of each physical line within the available width (after indentation/prefix).
Prefixes are not aligned; they are always written starting at `Bounds.X + Indent` (first line) or
`Bounds.X + Indent + HangingIndent` (continuations).

### Trimming

When `Wrap == false`, trimming matches `TextBlock` semantics:

- `Clip`: render the prefix that fits.
- `EndEllipsis` / `StartEllipsis`: reserve 1 cell for U+2026 when trimming is required.

Trimming applies to the rendered text region (after indentation/prefix).

### Rendering model

Rendering writes directly into a `CellBuffer` using `CellBuffer.WriteText(...)`:

- Unstyled text segments are written using `Style.None` (transparent/inherit).
- Styled segments are written using the run style, optionally merged with a base style if provided in the future.
- Hyperlink segments are written with a hyperlink token obtained from `CellBuffer.RegisterHyperlink(uri)`.

Runs and hyperlinks must be applied without allocations:

- merge the style-run spans and hyperlink spans into a single stream of segments,
- write each segment once with a `(Style, hyperlinkToken)` pair.

> [!NOTE]
> `CellBuffer.WriteText` expands tabs using deterministic tab stops (`TerminalTextUtility.DefaultTabWidth`), so paragraph
> rendering remains consistent between text metrics and rendering.

---

## Interaction

`Paragraph` is display-only:

- no focus,
- no input handling,
- no commands.

---

## Relationship to other controls

- Use `TextBlock` for cheap plain text labels (no runs/hyperlinks).
- Use `Markup` when you already have markup strings and want parsing.
- Use `Paragraph` when you have structured rich text (plain text + `StyledRun[]` + links) and want:
  - deterministic layout,
  - no markup parsing,
  - indentation/prefix features for document rendering.

`DocumentFlow` is expected to use `Paragraph` as its primary building block for Markdown paragraphs and list items.

---

## Testing plan (when implemented)

Add rendering/measurement tests covering:

- wrapping with indentation and hanging indent (list alignment correctness),
- hard line breaks handling,
- trimming + ellipsis in the presence of prefixes,
- merging style runs + hyperlink spans (including boundary conditions),
- Unicode width correctness (wide runes / grapheme clusters).

