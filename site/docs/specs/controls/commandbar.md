---
title: CommandBar Specs
---

# CommandBar Specs

This document specifies the `CommandBar` control.

> [!NOTE]
> For end-user usage and examples, see [CommandBar](../../controls/commandbar.md).

## Related specifications

- The unified command system, routing rules, and key-hints UX are specified in:
  - [Command System & Key Hints](../command_specs.md)

This file intentionally focuses on *implementation-specific* notes for `CommandBar` and avoids duplicating the broader command specs.

## Goals

- Provide a lightweight, single-row “key hints” surface intended for app chrome (footer/header).
- Surface commands relevant to the current focus context and app/global commands.
- Stay allocation-conscious and compatible with the binding dirty model.

## Non-goals

- Acting as a command palette or menu (use `CommandPalette` / `MenuBar` instead).
- Handling input directly (it renders hints; the command router executes shortcuts).

## Implementation map

- Control: `src/XenoAtom.Terminal.UI/Controls/CommandBar.cs`
- Style: `src/XenoAtom.Terminal.UI/Styling/CommandBarStyle.cs`
- Tests: `src/XenoAtom.Terminal.UI.Tests/CommandBarRenderingTests.cs`
- Demo: `samples/ControlsDemo/Demos/CommandBarDemo.cs`

## Layout & rendering (current behavior)

- The control measures to a single row (`Height = 1`) and to the *current* content width (focused context), while allowing clipping when the available width is smaller.
- Render always clears the entire bar row using `CommandBarStyle.Resolve(theme).BarStyle` (chrome must not “inherit” background/attributes from content behind it).
- When a key sequence prefix is active (`TerminalApp.PendingCommandSequenceCount > 0`), `CommandBar` renders the pending prefix + `…` before regular entries.

## Command collection

- The bar collects **local** commands from the focused visual chain and **global** commands from `TerminalApp`.
- Commands are filtered by:
  - `Presentation` flags (default is `CommandPresentation.CommandBar`)
  - enabled/visible state (`Command.IsVisibleFor(target)`, `Command.CanExecuteFor(target)`)
  - de-duplication by `Command.Id` (local wins over global).
- When a command sequence prefix is active, the bar can restrict displayed sequence commands to those matching the prefix.

## Styling

- `CommandBarStyle` controls:
  - bar background/foreground
  - keycap glyphs and styles
  - label style and disabled label style
  - separator text
- Labels are rendered from `Command.LabelMarkup` via `MarkupTextParser` using the current theme markup styles.

## Future ideas

- Optional “More…” entry that opens a help/palette surface when there is insufficient space.
- Optional grouping (local vs global) or ordering by importance/category.
