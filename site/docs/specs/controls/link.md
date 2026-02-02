---
title: Link Specs
---

# Link Specs

This document specifies the current behavior and design of the `Link` control as implemented.

> [!NOTE]
> For end-user usage and examples, see [Link](../../controls/link.md).

## Goals

- Provide a clickable hyperlink visual that can emit OSC 8 hyperlinks when supported by the terminal backend.
- Support mouse and keyboard activation.
- Provide predictable single-line layout and optional trimming for long URLs/text.
- Keep rendering allocation-free in steady-state (aside from hyperlink registration).

## Non-goals

- Automatic URL detection in arbitrary text (use `Markup` or build `Link` explicitly).
- Multi-line wrapping (the link is always height `1`).
- Opening URLs directly from the library (the control raises an event; the app decides what to do).

## Public surface (v1)

- `Text : string?` (display text)
- `Uri : string?` (hyperlink target)
- `Trimming : TextTrimming` (currently only `EndEllipsis` has special handling)
- Event:
  - `Opened` (bubbling routed event), args: `LinkOpenedEventArgs.Uri`

## Defaults

- `Focusable = true`.
- If `Text` is null/empty, the rendered text falls back to `Uri`.

## Implementation map

- Control: `src/XenoAtom.Terminal.UI/Controls/Link.cs`
- Style: `src/XenoAtom.Terminal.UI/Styling/LinkStyle.cs`
- Tests: `src/XenoAtom.Terminal.UI.Tests/LinkTests.cs` (OSC 8 emission)
- Demo: `samples/ControlsDemo/Demos/LinkDemo.cs`

## Layout

### Measure

- Chooses the single-line text: `Text ?? Uri ?? ""`.
- Width is computed via `TerminalTextUtility.GetWidth(...)` and height is always `1`.
- Returns `SizeHints.Fixed`.

## Rendering

### OSC 8 hyperlink registration

- If `Uri` is non-empty, the renderer calls `buffer.RegisterHyperlink(uri)` and passes the returned token to `WriteText`/`SetCell`.
- If `Uri` is null/empty, no hyperlink token is used (the link renders as text only).

Whether OSC 8 escapes are emitted depends on the terminal backend and its capabilities.

### Trimming

- If the text width fits, the full text is written.
- If it does not fit:
  - With `Trimming = TextTrimming.EndEllipsis`, the link renders a prefix plus an ellipsis glyph (U+2026) so the result fits.
  - Otherwise, the text is clipped at the available cell width.

## Styling

`LinkStyle` resolves a `Style` based on:

- `enabled` (disabled uses a dimmed style and `theme.Disabled` foreground when available)
- `focused` / `hovered` (defaults to underline + accent foreground, with bold for focus/hover)

By default, links are underlined and use `theme.Accent` as foreground when available.

## Input handling

- Mouse:
  - Left press marks the link as pressed.
  - Left release triggers `Opened` if the pointer is released inside the bounds.
- Keyboard:
  - `Enter` or `Space` triggers `Opened` when enabled.

The control does not perform navigation; it only raises an event containing the URI.

## Tests and demos

- `LinkTests.Write_Emits_Osc8_When_Supported` verifies OSC 8 escape sequences are present in output when writing a link.
- `LinkDemo` demonstrates trimming and reminds users that OSC 8 support depends on the terminal.

## Future / v2 ideas

- Support additional trimming modes (start ellipsis, middle ellipsis).
- Optional visited state styling and hover/pressed style variants.
- Optional integration helpers at the app level (open URL in browser) while keeping the control itself platform-agnostic.
