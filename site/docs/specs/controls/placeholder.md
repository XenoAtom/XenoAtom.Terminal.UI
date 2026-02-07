---
title: Placeholder Specs
---

# Placeholder Specs

This document captures the design proposal for a new `Placeholder` control.

> [!NOTE]
> This is a planned control spec (not implemented yet).

## Overview

- **Status**: Planned
- **Purpose**: A simple visual block used for mockups, empty states, and layout placeholders.
- **Primary goals**:
  - extremely easy to configure (`new Placeholder()`, optional text)
  - useful with text or without text
  - supports solid colors and the brush/gradient model for both background and text
  - low rendering overhead and no per-frame allocations

## Goals

- Provide a first-class lightweight placeholder surface (similar spirit to Textual screenshot placeholders).
- Allow rendering with:
  - background only (no text),
  - text only,
  - both text and background.
- Support gradients through the new brush APIs:
  - foreground text brush
  - background brush
- Keep API fluent and minimal.

## Non-goals

- Not a rich text editor/viewer.
- Not a data visualization control.
- No interaction model beyond standard visual behavior (no built-in input/events needed).

## Public API (proposed)

### Type

- `Placeholder : Visual` (sealed)

### Constructors

- `new Placeholder()`
- `new Placeholder(string text)`
- `new Placeholder(Func<string?> text)`
- `new Placeholder(Binding<string?> text)`

These match the current constructor patterns used by controls such as `TextBlock`.

### Bindable properties

- `Text : string?`
- `Wrap : bool` (default: `true`)
- `TextAlignment : TextAlignment` (default: `Center`)
- `VerticalTextAlignment : Align` (default: `Center`)
- `Trimming : TextTrimming` (default: `Clip`)

### Styling

Use a dedicated `PlaceholderStyle` (environment style):

- `Foreground : Color?`
- `Background : Color?`
- `ForegroundBrush : Brush?`
- `BackgroundBrush : Brush?`
- `TextStyle : TextStyle`
- `FillBackground : bool` (default: `true`)
- `Padding : Thickness` (default: `0`)

#### Precedence rules

- `ForegroundBrush` overrides `Foreground` for text cells.
- `BackgroundBrush` overrides `Background` for filled background cells.
- If a brush/color is not specified, values fall back to inherited/theme-resolved style.

## Layout behavior (proposed)

- If `Text` is null/empty:
  - measure to at least `1x1` so the control remains visible when explicitly sized by container.
- If text is provided:
  - measurement follows `TextBlock`-like width/height computation (Unicode width aware).
- Wrapping and trimming behavior align with existing `TextBlock` semantics.

## Rendering behavior (proposed)

1. Resolve `PlaceholderStyle` + theme.
2. If `FillBackground` is true and either background color or background brush is set:
   - fill entire bounds (or content bounds if padding is used).
   - for gradient background: sample across full bounds.
3. If `Text` is not null/empty:
   - render aligned text inside content bounds.
   - for gradient text:
     - sample `ForegroundBrush` using text rendering coordinates.
     - for wrapped text, follow `TextBlock` per-line restart behavior by default.

### No-text background usage

The control must support:

```csharp
new Placeholder()
    .Style(PlaceholderStyle.Default with
    {
        Background = Colors.SlateBlue
    });
```

Or with gradients:

```csharp
new Placeholder()
    .Style(PlaceholderStyle.Default with
    {
        BackgroundBrush = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            [
                new GradientStop(0f, Colors.MidnightBlue),
                new GradientStop(1f, Colors.SlateBlue),
            ])
    });
```

## Performance notes

- Reuse existing `CellBuffer` brush helpers (`FillRectWithBrush`, `WriteTextWithBrush`).
- Avoid allocations in `RenderOverride`:
  - no per-frame string splitting
  - no temporary collections for line layout when avoidable
- Use existing text measurement/wrap primitives already optimized in current controls.

## Planned implementation files

- `src/XenoAtom.Terminal.UI/Controls/Placeholder.cs`
- `src/XenoAtom.Terminal.UI/Styling/PlaceholderStyle.cs`
- `src/XenoAtom.Terminal.UI.Tests/PlaceholderTests.cs`
- `samples/ControlsDemo/Demos/PlaceholderDemo.cs`
- `site/docs/controls/placeholder.md`

## Planned tests

- Background-only placeholder fills bounds.
- Text-only placeholder renders aligned text.
- Foreground gradient changes text cell colors.
- Background gradient changes background cell colors.
- Wrapped multiline text uses deterministic alignment/line behavior.
- Empty text does not throw and still renders background when configured.

## Open decisions

- Whether `VerticalTextAlignment` should be in v1 or deferred.
- Whether default `FillBackground` should be `true` (recommended) or `false`.
- Whether to include a built-in helper mode for automatic dimension labels (e.g. `40 x 6`) used in mockups.

## Recommendation

Implement `Placeholder` as a thin composition over proven text rendering logic (`TextBlock`-style behavior) and
the new brush infrastructure. This keeps behavior consistent, reduces implementation risk, and gives immediate value
for demos/mockups.
