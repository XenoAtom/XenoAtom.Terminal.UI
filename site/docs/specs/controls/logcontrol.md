---
title: LogControl Specs
---

# LogControl Specs

This document specifies the current behavior and design of the `LogControl` control as implemented.

> [!NOTE]
> For end-user usage and examples, see [LogControl](../../controls/logcontrol.md).

## Goals

- Provide a high-throughput scrolling log viewer suitable for "tail"-like workloads.
- Support efficient appending of plain text and markup lines.
- Provide built-in selection/copy and a built-in search UI.
- Stay performant by rendering only the visible rows while keeping entries as data (not a Visual per line).

## Non-goals (current implementation)

- Structured log columns (timestamp/severity/category) (not implemented).
- In-place editing or replace operations (the search UI is find-only for logs).
- Persisting logs to disk (apps should handle that separately).

## Related specifications

- Command system / discoverability:
  - [Command System & Key Hints](../command_specs.md)
- Search UI popup used by logs and editors:
  - [SearchReplacePopup Specs](searchreplacepopup.md)

## Public surface (v1)

### Append / clear

- `AppendLine(string message)` (splits newlines into multiple entries)
- `AppendMarkupLine(string markup)` (splits newlines into multiple entries)
- `AppendMarkupLine(ref AnsiMarkupInterpolatedStringHandler handler)`
- `Clear()`

### Scrolling

- `FollowTail : bool` (read/write; controls whether appended entries auto-follow the tail)
- `ScrollToTail()` (clears selection, enables follow-tail, and scrolls to the end)

### Search

- `SearchText : string?` (bindable)
- `Search(string searchText)`
- `GoToNextMatch()` / `GoToPreviousMatch()`
- Search flags:
  - `SearchCaseSensitive : bool`
  - `SearchWholeWord : bool`
  - `SearchRegex : bool`
  - `SearchOptions : LogSearchOptions` (flags convenience wrapper)
- Match state:
  - `MatchCount : int`
  - `ActiveMatchIndex : int` (or -1)

### Selection / copy

- `HasSelection : bool`
- Keyboard shortcuts:
  - Ctrl+A selects all
  - Ctrl+C copies selection to clipboard (plain text)

### Capacity and wrapping

- `MaxCapacity : int` (0 disables trimming)
- `WrapText : bool` (when true, horizontal scrolling is disabled and lines reflow to the viewport width)

## Styling

LogControl styling uses two style keys:

- `LogControlStyle` (surface/border/padding/selection)
- `LogControlSearchStyle` (match and active-match highlight, error style)

## Defaults

- `Focusable = true`
- `HorizontalAlignment = Align.Stretch`, `VerticalAlignment = Align.Stretch`
- `WrapText = true`
- `FollowTail = true`
- Search popup is find-only (no replace).

## Implementation map

- Control: `src/XenoAtom.Terminal.UI/Controls/LogControl.cs`
- Styles:
  - `src/XenoAtom.Terminal.UI/Styling/LogControlStyle.cs`
  - `src/XenoAtom.Terminal.UI/Styling/LogControlSearchStyle.cs`
- Tests: `src/XenoAtom.Terminal.UI.Tests/LogControlTests.cs`
- Demos:
  - `samples/ControlsDemo/Demos/LogControlDemo.cs`
  - `samples/FullscreenDemo/Program.cs` (log viewer usage)

## Composition

`LogControl` is a composite control:

- A `ScrollViewer` hosts a private `LogContentVisual` that renders the entries and provides scrolling extent.
- A `SearchReplacePopup` is anchored inside the padded viewport and shown in the app window layer (fullscreen only).

The `LogControl` itself owns focus and input handling; the internal `ScrollViewer` is created with `focusable: false`.

## Data model

### Entries

Entries are stored in a `BindableList<LogEntry>` where each entry holds:

- `Text` (plain text for the line)
- optional `StyledRun[]` (markup style runs relative to the entry start)

When appending markup, the control parses the markup to plain text + style runs at append time using `MarkupTextParser` and the current theme markup styles.

> [!NOTE]
> Because markup is parsed at append time, changing the theme later does not re-parse historical markup runs.

### Wrapping model

When `WrapText` is enabled, each entry can occupy multiple rendered rows. The content visual computes:

- `ExtentHeight` as the sum of per-entry row counts.
- `ExtentWidth` as the maximum measured cell width of any entry (used when wrapping is disabled).

The content visual maintains a prefix-sum array mapping content-row -> entry index (for fast row-to-entry mapping).

## Capacity trimming

`MaxCapacity` limits the number of retained entries.

- When the capacity is exceeded, the oldest entries are removed.
- If `FollowTail` is false, the control keeps the viewport stable by subtracting the removed row count from `ScrollViewer.VerticalOffset` before deleting entries.
- Trimming clears any selection and rebuilds matches.

## Follow-tail behavior

`FollowTail` is the user-controlled auto-follow mode, not just a reflection of whether the viewport currently happens to sit at the tail.

- Appending while `FollowTail` is true and there is no selection schedules a scroll-to-bottom.
- Setting `FollowTail = false` disables auto-follow even if the viewport is already at the maximum vertical offset (bottom).
- The control applies a pending follow-tail scroll during arrange (and may re-arrange the ScrollViewer in the same pass so the view updates immediately).
- Manual navigation away from the tail disables follow-tail; returning to the bottom by paging does not silently re-enable it.
- Creating a selection disables follow-tail.
- `ScrollToTail()` and `FollowTail = true` clear selection and re-enable follow-tail.

## Selection and copy

Selection is tracked as two logical positions: an anchor and an active end position, both expressed as `(EntryIndex, TextIndex)`.

- Mouse:
  - Left click sets anchor/active to the clicked position.
  - Drag updates the active position (selection extends).
  - Shift+click extends from the existing anchor.
- Keyboard:
  - Ctrl+A selects from the start of the first entry to the end of the last entry.

Copy exports plain text (markup stripped):

- Ctrl+C copies the selected text to `Terminal.Clipboard`.
- When the selection spans multiple entries, newline characters are inserted between entry fragments.

## Search

### Query state

Search is controlled by bindable properties (`SearchText` and flags). There is also a `SearchOptions` flags wrapper for convenience.

### Match building

Matches are computed over the plain entry text and stored as:

- a flat list of `(EntryIndex, Start, Length)`
- and an optional per-entry list for fast rendering of highlights

Search modes:

- Regex:
  - Uses `RegexOptions.CultureInvariant` and `IgnoreCase` when case-insensitive.
  - Whole-word wraps the pattern as `\\b(?:pattern)\\b`.
  - Invalid regex errors are captured and surfaced via the search UI (no exception escapes).
- Plain text:
  - Uses ordinal/ordinal-ignore-case search.
  - Whole-word uses `WordBoundaryUtility.IsWordBoundary(...)`.

### Navigating matches

- `GoToNextMatch()` / `GoToPreviousMatch()` wrap around when reaching the end/beginning.
- Navigating to a match disables follow-tail and scrolls the viewport so the match row is visible.

### Search UI

The built-in UI is a `SearchReplacePopup` anchored to the top-right of the padded viewport:

- Ctrl+F opens the popup (also exposed as a `Command` with `CommandPresentation.CommandBar`).
- The popup displays status text (e.g. "3/10") and error text (e.g. invalid regex) via the `ISearchReplaceTarget` integration.
- LogControl is find-only: replace actions are no-ops.

## Rendering

### Surface fill

The outer control fills the padded viewport background using `LogControlStyle.BackgroundStyle(theme, focused)`.

### Content rendering and virtualization

`LogContentVisual` renders only the visible content rows:

- It maps screen rows to entries (and wrapped row slices when `WrapText` is true).
- It writes either plain text or markup runs.
- It overlays selection and match highlights without mutating the stored markup runs.

Highlight styles:

- Selection uses `LogControlStyle.SelectionStyle(theme)`.
- Matches use `LogControlSearchStyle.ResolveMatchStyle(theme)`.
- Active match uses `LogControlSearchStyle.ResolveActiveMatchStyle(theme)`.

The control uses an internal bindable `InteractionVersion` to invalidate rendering when selection/match state changes.

## Input handling summary

- Ctrl+F: open search popup
- Ctrl+A: select all
- Ctrl+C: copy selection
- Up/Down/PageUp/PageDown/Home/End: scroll (clears selection unless Shift is held)
- Mouse wheel: disables follow-tail
- Mouse left press/drag: selection in the content area

## Tests and demos

`LogControlTests` covers:

- follow-tail auto-scroll on append
- programmatic follow-tail restore (`ScrollToTail`)
- max capacity trimming
- select-all and clipboard copy
- Ctrl+F opening the search popup
- searching and navigating matches scrolls to results (wrap-around navigation)

## Future ideas

- Richer entry model (severity, timestamp, structured columns) while keeping virtualization.
- Additional keyboard selection/navigation behaviors (extend selection with Shift + arrows).
- Optional "follow tail" toggle indicator in the UI.
- Persisting search state and/or exposing search commands beyond Ctrl+F (e.g. F3 next match) at the LogControl level.
