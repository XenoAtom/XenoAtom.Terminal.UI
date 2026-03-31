---
title: Command Palette (Specs)
---

# Command Palette (Specs)

This document specifies the **CommandPalette v1** for XenoAtom.Terminal.UI and how it integrates with the unified
`Command` system ([Command Specs](../command_specs.md)).

The goal is to make commands **discoverable**, **searchable**, and **easy to invoke**, while staying consistent with:

- retained-mode visuals + dependency tracking
- the existing popup/window model (fullscreen only)
- minimal allocations and good performance

---

## Goals / non-goals

### Goals (v1)

- Provide a first-class **command palette UX**:
  - open/close via command shortcut (typically `Ctrl+P`, configurable)
  - a search input + list of matching commands
  - keyboard navigation (`Up/Down`, `PageUp/PageDown`, `Home/End`, `Enter`)
  - mouse navigation (hover + click)
- Populate the palette from the **current command context**:
  - local commands for the focused visual and its ancestors
  - global commands from `TerminalApp.GlobalCommands`
  - filter to commands marked with `CommandPresentation.CommandPalette`
- Provide **good default search**:
  - case-insensitive substring match across label + optional search terms
  - ranking: prefix match > word match > substring match
  - show highlighted matches (optional for v1; but make it easy to add)
- Provide a clean **extensibility model**:
  - user can override palette item layout (template)
  - user can add custom command providers (optional in v1; make room for v2)
- Integrate with the binding engine:
  - `CanExecute` and `IsVisible` affect the list and automatically invalidate when their dependencies change

### Non-goals (v1)

- A full “omnibox” with modes (`>`, `:`) and pluggable non-command providers (files, symbols, etc.).
- Inline hosting (popups/windows are fullscreen-only today).

---

## Current state (code)

Existing controls:

- `Controls/CommandPalette.cs`:
  - `TextBox` search + `OptionList<CommandPaletteItem>` results
  - shows itself via a dialog-backed modal window (fullscreen only)
- `Controls/CommandPaletteItem.cs`:
  - content/shortcut/description factories + `Action`

The current implementation is **not integrated with `Command`** (it is a separate “item” model).

---

## Proposed v1 design

### Palette item model: use `Command` directly

The palette should operate on `XenoAtom.Terminal.UI.Commands.Command`.

For v1, we do **not** want a separate `CommandPaletteItem` abstraction:

- It duplicates metadata already present on `Command` (`LabelMarkup`, `DescriptionMarkup`, shortcut).
- It makes it harder to keep discoverability consistent across surfaces.

**Proposal:**

- Replace the palette item list with `BindableList<Command>`.
- Remove `CommandPaletteItem`.
- The palette is responsible for building a display row for each `Command`.

### How palette collects commands

Introduce a **single internal helper** that all command surfaces can use:

```csharp
internal static class CommandQuery
{
    public static void Collect(
        TerminalApp app,
        Visual? target,
        CommandPresentation presentation,
        List<Command> output);
}
```

Rules:

1. Walk `target` (usually `TerminalApp.FocusedElement`) up to the root, collecting commands from each visual:
   - include command if `command.Presentation.HasFlag(presentation)`
   - include only if visible: `command.IsVisibleFor(targetVisual)`
2. Append global commands with the same filter.
3. Deduplicate by `Command.Id` (first wins).
4. Keep stable ordering:
   - local-first, then global
   - within each group: `CommandImportance` then insertion order

This is the same philosophy as `CommandBar`, but centralized so `CommandPalette` and future `ContextMenu` use the same logic.

### Search text: how commands should be matched

Command palette needs a searchable string for each command.

#### Minimal v1 rule (no `Command` API changes)

Search text is computed as:

- `LabelMarkup` with markup tags ignored for matching (e.g. `[primary]Save[/]` matches “save”)
- plus `DescriptionMarkup` ignored for matching (optional)

Implementation note: **do not allocate** for search. Instead:

- scan `LabelMarkup` on the fly while skipping markup tokens (`[...]`)
- treat everything else as text

This is enough for v1 and avoids adding new API surface to `Command`.

#### Optional v1 enhancement (recommended): add `Name` / `SearchText`

If we want command palette and future “command prompt” (`/help`) to be first-class, consider extending `Command` with:

- `string? Name` (a stable textual name, no markup, e.g. `save`, `open`, `theme.set`)
- `string? SearchText` (additional terms, synonyms, tags)

Matching then uses:

1. `Name` (if present)
2. `LabelMarkup` (markup-stripped)
3. `SearchText` (if present)
4. `DescriptionMarkup` (optional, and only for low-priority matches)

**Note:** this is safe for NativeAOT and minimal overhead because it is static metadata per command.

### Ranking rules

Given query `q` and candidate text `t`, rank as:

1. Exact match (case-insensitive)
2. Prefix match on a word boundary
3. Substring match

Ties:

- higher `CommandImportance` first
- then stable insertion order

The v1 implementation can be simple and deterministic (no fuzzy scoring required).

### Executing a command from the palette

When user activates a selected item:

1. Determine `target`:
   - default: `TerminalApp.FocusedElement ?? root`
2. If `CanExecuteFor(target)` is true:
   - call `Execute(target)`
   - close palette
3. If `CanExecuteFor(target)` is false:
   - keep palette open (optional: show disabled style)

---

## UI behavior

### Opening / closing

- Palette opens as a modal window hosted by the app window layer (fullscreen only).
- Palette closes when:
  - `Esc` pressed
  - focus moves outside palette (click outside)
  - command executed successfully

### Focus management

When opened:

- the search box is focused
- the results list is always present; it can be navigated using arrow keys

While open:

- `Tab` stays within the palette (search box ↔ results list)
- `Enter` in the search box executes the currently highlighted result
- `Down` in the search box moves focus into the results and advances from the default highlighted first item
- global commands that conflict should not execute (palette should capture key events)

### List behavior

- results are virtualized using existing list control (`OptionList<Command>`)
- the list shows:
  - primary line: label (markup)
  - right-aligned shortcut: `Gesture` or `Sequence` formatted as text
  - optional secondary line: description (if present and enabled in style)

---

## Styling and templating

### Palette style

Introduce `CommandPaletteStyle` (or update existing one) with:

- `PopupTemplateFactory` (already used elsewhere)
- `ItemTemplate` (`DataTemplate<Command>`) used by the results list
- `PopupIsResizable` to control dialog-like resize behavior
- optional `DescriptionVisible` / `ShowDescription`
- optional `MaxWidth`, `ResultsHeight`, padding, etc.

### Default item template

Default row visual (single line):

```
<LabelMarkup>          <ShortcutText>
```

If description enabled:

```
<LabelMarkup>          <ShortcutText>
<DescriptionMarkup>
```

Shortcut formatting:

- `command.Sequence?.ToString() ?? command.Gesture?.ToString()`

---

## Context integration

### Palette command(s)

The palette itself should be opened by a command registered by the app:

- `Id = "App.CommandPalette"`
- `Presentation` includes at least `CommandPresentation.CommandPalette` and probably `CommandBar`
- Default shortcut:
  - either `Ctrl+P` (recommended)
  - or a sequence such as `Ctrl+K Ctrl+P` (common in editors)

### Updating list as context changes

While the palette is open:

- if focus changes (e.g. a modal opens), the palette command list should be recomputed
- changes in `CanExecute` / `IsVisible` should update the list automatically (dependency tracking)

---

## Tests

- Collect logic:
  - local overrides global (same `Id`)
  - presentation filtering works
  - `IsVisible` exclusion works
- Search ranking:
  - prefix ranked above substring
  - stable ordering maintained
- UI behavior:
  - opening focuses the search box
  - tab cycles within the palette
  - enter executes + closes

---

## Implementation steps

1. Introduce `CommandQuery` helper (internal) used by command surfaces.
2. Refactor `CommandPalette` to operate on `Command` directly.
3. Remove `CommandPaletteItem` and update demos/docs/tests.
4. Add optional `Command.Name` / `Command.SearchText` if we agree it’s needed for the future “command prompt” feature.
