---
title: Validation Specs
---

# Validation Specs

This document captures design and implementation notes for the validation infrastructure.

> [!NOTE]
> For end-user usage and examples, see [Validation](../../controls/validation.md).

## Overview

- Status: implemented
- Primary purpose: show a status/validation message (info/warning/error) above or below another visual.
- Composition first: this is a wrapper control, not an attached-property system.
- Used both by end users and internally by controls/prompts that need to surface validation feedback.
- Designed for the library binding/dirty model: message updates must trigger the correct layout/render invalidation without imperative "request render" calls.

## Key types

### `ValidationSeverity`

Three severities:

- `Info`
- `Warning`
- `Error`

Severity affects the glyph, the message text style, and the default foreground color.

### `ValidationPlacement`

Controls where the message is placed relative to the wrapped content:

- `Above`
- `Below` (default)

### `ValidationMessage`

`ValidationMessage` is a small value type:

- `ValidationSeverity Severity`
- `Visual Content`

The message content is a `Visual` to allow rich composition (text, markup, links, layout containers).

### `ValidationPresenter`

`ValidationPresenter : ContentVisual` wraps a single `Content` visual and optionally shows a message.

Bindable properties:

- `ValidationMessage? Message`
- `ValidationPlacement Placement`

`ValidationPresenter` is not focusable (`Focusable = false`). The wrapped `Content` keeps its own input behavior.

### `ValidationExtensions`

Convenience methods in `ValidationExtensions` make the feature easy to use without explicit presenter construction:

- `content.Validation(ValidationMessage?)`
- `content.Validation(Binding<ValidationMessage?>)`
- `content.Validation(State<ValidationMessage?>)`
- `content.Validate(valueBinding, validator, placement)`

`Validate(...)` binds `ValidationPresenter.Message` to a computed binding that calls the validator on demand:

- The validator is evaluated as part of dependency tracking, so it re-runs when the bound value changes.
- The validator returns `null` when valid.

## Layout behavior

### Child structure

`ValidationPresenter` always has a `ValidationMessageHost` child attached, even when no message is displayed. The host collapses itself to `Size.Zero` when `Message` is null.

This keeps style inheritance stable and avoids frequent attach/detach of the host itself.

### Measure

`ValidationPresenter.MeasureCore`:

- Measures `Content` (if any) with the incoming constraints.
- Measures the message host with the same constraints.
- Computes its width as the max of content and message widths.
- Computes its height as `contentHeight + messageHeight + Gap`, where `Gap` comes from `ValidationStyle` and is only applied when a message is present.
- Uses `SizeHints.Flex(...)` and normalizes the result.

### Arrange

`ValidationPresenter.ArrangeCore`:

- Uses `FlexAllocator.Allocate` to distribute height between content and message based on their min/natural/max hints.
- Applies `ValidationStyle.Gap` between content and message only when a message is present.
- Honors `Placement`:
  - `Above`: message first, then content.
  - `Below`: content first, then message.

## Message synchronization and the binding model

`ValidationPresenter` maintains a cached `_resolvedMessage` and calls `EnsureMessageApplied()` during layout.

This exists for a binding-system detail:

- When `Message` is updated through a generated binding (including two-way bindings), updates may bypass the C# property setter.
- `ValidationPresenter` must still detect the new value so it can update the internal host (attach/detach the message content and re-measure/re-arrange it).

`EnsureMessageApplied()` actively reads `Message` during layout and pushes it to the host when it changes. This ensures message changes participate in dependency tracking and invalidation.

## Styling

Validation is styled by `ValidationStyle`:

- `Padding`: padding around the entire line/block.
- `GlyphSpacing`: number of spaces after the severity glyph.
- `Gap`: vertical spacing between the wrapped content and the message.
- `InfoGlyph`, `WarningGlyph`, `ErrorGlyph`: optional glyphs (nullable `Rune`).
  - Defaults are code points `U+2139` (info), `U+26A0` (warning), `U+26D4` (error).
- `InfoStyle`, `WarningStyle`, `ErrorStyle`: optional style overrides for the message line.

If no override style is provided, the line foreground is derived from the theme:

- Info: `Theme.Muted` (or fallback `Theme.Foreground`)
- Warning: `Theme.Warning` (or fallback `Theme.Foreground`)
- Error: `Theme.Error` (or fallback `Theme.Foreground`)

### `ValidationMessageHost` rendering details

`ValidationMessageHost` is an internal `Visual` that implements the line/block rendering:

- In `PrepareChildren`, it attaches/detaches the message `Content` visual based on `Message`.
- It enforces wrapping for common text controls:
  - If the message content is a `TextBlock` or `Markup`, it sets `Wrap(true)` so messages wrap by default.
- In `MeasureCore`, it subtracts `Padding` and the prefix width (glyph + spacing) from the available width before measuring the message content.
- In `ArrangeCore`, it offsets the message content rectangle by `Padding.Left` and prefix width.
- In `RenderOverride`, it fills the entire host bounds with spaces using the resolved line style, then writes the prefix at the top-left (after padding).
- It sets `StyleEnvironment[TextBlockStyle.Key]` to a severity-appropriate `TextBlockStyle` so `TextBlock` children inherit the correct foreground by default.

## Input behavior

- `ValidationPresenter` and `ValidationMessageHost` are not focusable.
- They do not define built-in commands or keyboard shortcuts.
- If the message content contains interactive visuals (for example, a `Link`), those visuals can still participate in hit testing normally.

## Tests and demos

- Unit tests: `src/XenoAtom.Terminal.UI.Tests/ValidationPresenterTests.cs` verifies that `Validate(...)` reevaluates the validator when the bound value changes.
- Demo: `samples/ControlsDemo/Demos/ValidationDemo.cs` shows "below" vs "above" placement and a custom `ValidationStyle`.
- Internal usage example: `NumberBox<T>` composes `ValidationMessageHost` directly to surface parse/validation errors without requiring external wrappers.

## Related specifications

- [Binding and Dirty Model](../binding_dirty_model_specs.md)
- [Rendering](../dirty_rendering_specs.md)

## Future ideas

- Support multiple messages (stacked lines) and per-message placement.
- Provide built-in severity icons for environments that do not support the default glyphs well.
- Add optional "reserved space" mode to avoid layout shift when messages appear/disappear.
- Add more deterministic rendering tests (including prefix/padding sizing and style inheritance).
