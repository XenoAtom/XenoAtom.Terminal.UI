---
title: Text Editor Architecture (Current Implementation + CodeEditor Roadmap)
---

# Text Editor Architecture (Current Implementation + CodeEditor Roadmap)

This document describes the **current** text editor architecture in XenoAtom.Terminal.UI and the **next steps** required to evolve it into a production-grade `CodeEditor`.

The goals are:

- Make the current behavior explicit (so we can test/maintain it).
- Preserve API compatibility for `TextBox` / `TextArea` while enabling future growth.
- Provide a concrete roadmap (performance, extensibility, usability).

## Current implementation: code map

### Controls

- `TextEditorBase` (`src/XenoAtom.Terminal.UI/Controls/TextEditorBase.cs`)
- `TextEditorCore` (`src/XenoAtom.Terminal.UI/Controls/TextEditorCore.cs`) (internal)
- `TextBox` (`src/XenoAtom.Terminal.UI/Controls/TextBox.cs`)
- `TextArea` (`src/XenoAtom.Terminal.UI/Controls/TextArea.cs`)
- `MaskedInput` (`src/XenoAtom.Terminal.UI/Controls/MaskedInput.cs`) (inherits `TextBox`)

### Text model

- `ITextDocument` (`src/XenoAtom.Terminal.UI/Text/ITextDocument.cs`)
- `ITextSnapshot` (`src/XenoAtom.Terminal.UI/Text/ITextSnapshot.cs`)
- `TextDocument` (`src/XenoAtom.Terminal.UI/Text/TextDocument.cs`) (string-backed)
- `DynamicTextDocument` (`src/XenoAtom.Terminal.UI/Text/DynamicTextDocument.cs`) (delegate-backed, bridges bindable `Text`)
- `TextSnapshot` (`src/XenoAtom.Terminal.UI/Text/TextSnapshot.cs`)
- `TextLine` (`src/XenoAtom.Terminal.UI/Text/TextLine.cs`)
- `TextPosition` / `TextRange` (`src/XenoAtom.Terminal.UI/Text/TextTypes.cs`)
- `TextDocumentChangedEventArgs` (`src/XenoAtom.Terminal.UI/Text/TextDocumentChangedEventArgs.cs`)

### Scrolling integration

- `ScrollModel` (`src/XenoAtom.Terminal.UI/Scrolling/ScrollModel.cs`)
- `IScrollable` (`src/XenoAtom.Terminal.UI/Scrolling/IScrollable.cs`)
- `ScrollViewer` (`src/XenoAtom.Terminal.UI/Controls/ScrollViewer.cs`)

## Goals (re-stated with current framework constraints)

### Functional scope (what exists now)

- Shared editing engine for single-line and multi-line editors.
- Caret + linear selection + clipboard integration.
- Mouse selection (drag) within the viewport.
- Multi-line soft wrapping (`WordWrap`).
- Horizontal scrolling for single-line (and for multi-line when wrapping is disabled).
- Terminal cursor integration via `ICursorProvider` (caret is not rendered in-buffer).

### Framework alignment requirements (non-negotiable)

- Layout uses `Measure(in LayoutConstraints)` / `Arrange(Rectangle)` and `SizeHints`.
- Binding dependency tracking relies on property usage; “hidden” backing-field reads bypass tracking.
- Rendering is done into a `CellBuffer` and clipped by the renderer (no “render all then clip” hacks).
- Input is routed events (`OnKeyDown`, `OnTextInput`, `OnPointer*`, `OnPaste`).
- Clipboard must go through `Terminal.Clipboard`.
- The caret should be a terminal cursor location (not reverse-video glyphs).

## Text model: contracts and current semantics

### `ITextDocument` and versioning

`ITextDocument` provides:

- `CurrentSnapshot` and `Version`
- `Insert`, `Remove`, `Replace`
- `BeginUpdate()` (currently does not batch change events; reserved for future batching)
- `Changed` event (`TextDocumentChangedEventArgs`)

Current contract assumptions:

- `Version` increments when text changes.
- The editor refreshes cached data when `Version` changes.
- The document stores user input as-is (no line-ending normalization).

### Snapshots and line model

`TextSnapshot` contains:

- the full `string` text
- `LineStarts[]` and per-line `LineBreakLength[]` (so CRLF vs LF is preserved)

`TextLine` exposes:

- `Start`, `Length`, `LineBreakLength`
- `End` and `EndIncludingBreak`

### Bridging bindable `Text` with `DynamicTextDocument`

`TextEditorBase` is document-first. `TextBox` and `TextArea` each define their own bindable `Text` property and set:

```csharp
TextDocument = new DynamicTextDocument(
    getter: () => Text ?? string.Empty,
    setter: value => Text = value);
```

This provides a V1-friendly two-way binding story:

- Editing operations mutate the `ITextDocument`.
- `DynamicTextDocument` writes back into the bindable `Text` property.
- External changes to `Text` are picked up by `DynamicTextDocument` by comparing `_getter()` with the cached string and incrementing `Version` when it differs.

Important note:

- `DynamicTextDocument.Replace(...)` currently performs `text.ToString()` for the inserted span and rebuilds the entire string. This is acceptable for V1 but not for CodeEditor-scale documents.

## Editor control model: `TextEditorBase` + `TextEditorCore`

### `TextEditorBase` responsibilities

`TextEditorBase` is a `Visual` that:

- hosts a `TextEditorCore`
- owns a `ScrollModel` and exposes it via `IScrollable`
- forwards routed input events to `TextEditorCore`
- provides the terminal cursor location via `ICursorProvider`
- exposes configuration as bindable properties:
  - `Placeholder`
  - `AcceptTab`
  - `WordWrap`
- exposes `ITextDocument TextDocument` (public, swappable)

### `TextEditorCore` responsibilities

`TextEditorCore` is internal and currently owns:

- caret index (UTF-16 index)
- linear selection (`anchor` + `end`)
- preferred column for vertical movement
- a cached full text string by `document.Version`
- viewport mapping and viewport-only rendering
- scrolling behavior (“ensure caret visible”)
- keyboard/mouse command handling

### Rendering hook: segment writer

Rendering goes through a `TextSegmentWriter` delegate. `TextEditorBase` supplies a `WriteTextSegment(...)` virtual method used by the delegate.

Current examples:

- `MaskedInput` overrides `WriteTextSegment` to render masked glyphs (without duplicating `TextBox` logic).

For CodeEditor, this hook will need to grow into a layered, styled text pipeline (see section 8.4).

### Cursor policy

Caret rendering is out-of-band:

- `TextEditorCore.TryGetCursorCell(...)` computes (x,y) in terminal cells.
- The terminal cursor is moved by the host (`TerminalApp`) based on the focused `ICursorProvider`.

## Layout and scrolling behavior

### Internal scroll model (`ScrollModel`)

The editor sets:

- viewport size from the arranged editor rectangle
- extent based on text + wrap mode
- offsets updated by caret movement, or by hosting scroll viewers / scrollbars

`ScrollModel.ScrollToMakeVisible(...)` is used to keep the caret visible.

### `TextBox` overflow indicators (horizontal)

`TextBox` uses `ScrollModel.OffsetX` and supports optional overflow indicators via style:

- left indicator when `Scroll.OffsetX > 0`
- right indicator when `Scroll.OffsetX + ViewportWidth < ExtentWidth`

When indicators are visible, `TextBox` shrinks the editor rectangle to reserve one cell for each indicator.

### `TextArea` word-wrap

`TextArea` defaults to `WordWrap = true`.

Current behavior:

- when wrapping is enabled, horizontal offset is forced to `0`
- vertical scrolling still applies

Wrap behavior is cell-based and rune-based; it is not “word wrap” with whitespace-aware breakpoints.

### Integrating `TextArea` with `ScrollViewer` (concrete API)

`TextEditorBase` implements `IScrollable`, so `ScrollViewer` can bind to it.

Recommended composition:

```csharp
var editor = new TextArea(text);
var view = new ScrollViewer(editor);
```

When the content implements `IScrollable`:

- scrollbars update the editor’s `ScrollModel`
- `ScrollViewer.HorizontalOffset`/`VerticalOffset` remain in sync with the editor scroll offsets

## Input and command behavior (current)

### Text insertion

- `OnTextInput`: inserts `e.Text`
- `OnPaste`: inserts the pasted string
- `Enter`: inserts `\n` if `AcceptsReturn` is enabled (multi-line)
- `Tab`: inserts `\t` if `AcceptTab` is enabled

### Selection

- Shift + navigation extends selection.
- Mouse drag extends selection.
- Ctrl+A selects all.

Selection is linear only.

### Navigation

- Arrow keys move by rune (`TerminalTextUtility.GetNextRuneIndex/GetPreviousRuneIndex`).
- Ctrl+Left / Ctrl+Right move by “word” category (letters/digits/underscore vs whitespace vs other).
- Home / End move to line boundary.
- Ctrl+Home / Ctrl+End move to start/end of document in multi-line.
- PageUp / PageDown move by viewport height in multi-line.

### Delete operations

- Backspace/Delete remove selection if present.
- Otherwise remove rune (or word in certain Ctrl-modified cases in single-line paths).

### Clipboard

- Ctrl+C copies selection to `Terminal.Clipboard`.
- Ctrl+X cuts selection to `Terminal.Clipboard`.
- Ctrl+V pastes from `Terminal.Clipboard.Text`.

### Emacs-like kill/yank parity

Implemented:

- Ctrl+K: kill to end
- Ctrl+U: kill to start
- Ctrl+W: kill previous word
- Ctrl+Y: yank (insert kill buffer)

## Known limitations (intentional for V1)

The current foundation is intentionally simple. Missing pieces include:

- Undo/redo and edit transactions.
- Multi-caret, rectangular selection, virtual space.
- Auto-scroll while selecting outside the viewport.
- Token-aware wrapping and richer classification-aware layout beyond the current cached per-line wrap metadata / sparse wrapped-row checkpoints / bounded reusable wrapped-row block cache.
- IME/composition support (only basic text input events).
- A classification + decoration pipeline (syntax highlighting, diagnostics, search highlights, etc.).
- Background services with snapshot version gating.

## CodeEditor roadmap (what we need next)

### Performance: document storage and incremental line index

For CodeEditor-scale workloads, introduce a new `ITextDocument` implementation:

- piece table / rope / gap buffer (implementation choice)
- incremental updates of the line index (update only impacted line range)
- snapshots that do not require copying the entire text on every edit
- a richer change event model (batched changes via `BeginUpdate()`)

Compatibility constraint:

- `TextBox`/`TextArea` must keep working with `DynamicTextDocument`.

### Undo/redo

Add an undo manager (likely an internal controller layered on top of the document operations):

- transaction grouping (typing merges, paste merges)
- undo/redo commands (Ctrl+Z / Ctrl+Y or Ctrl+Shift+Z)

This must work with any `ITextDocument` including `DynamicTextDocument`.

### Scrolling policy: caret-follow vs user scrolling

Today the editor keeps the caret visible aggressively.

For CodeEditor we need a configurable policy:

- allow the user to scroll without “snap back to caret”
- optionally re-follow caret on typing / navigation
- support autoscroll while selecting outside viewport

This likely requires separating:

- the scroll offsets controlled by user gesture
- the “bring caret into view” behavior as a mode (or threshold-based)

### Rendering extensibility: classification and layers

The current segment-writer hook is sufficient for masking, but CodeEditor requires:

- per-token styling (classification spans -> `CellStyle`)
- layered rendering (selection, current line, highlights, squiggles/underlines, etc.)
- gutter rendering (line numbers, glyphs)
- low allocation (cache layouts per line; recompute only when width/version changes)

Proposed direction:

- keep `TextEditorCore` as the caret/selection/viewport engine
- add a pluggable “classifier” and “renderer” abstraction used during render:
  - input: snapshot + viewport (rows) + wrap settings
  - output: styled segments + decoration overlays

### CodeEditor chrome and UI composition

Implement `CodeEditor` as a control composed around the editor surface:

- left gutter for line numbers + glyphs
- folding controls
- diagnostics markers
- completion popup anchored to caret cell coordinates

Likely composition:

- `Grid`/`DockLayout` combining:
  - gutter visual
  - editor surface visual
  - optional minimap / overlays

### Background services and version gating

Add snapshot-based services runnable off-thread:

- tokenizer / highlighter
- diagnostics engine
- search index

All service results must be version-gated:

- include snapshot version in the result
- discard stale results on the UI thread if the document version advanced

## Checklist

### Implemented

- Shared engine (`TextEditorCore`) for `TextBox` and `TextArea`
- `ITextDocument` + snapshots + line model (CRLF preserved)
- Delegate-backed document for bindable `Text`
- Mouse selection inside viewport
- Soft wrap in multi-line editor
- ScrollViewer integration via `IScrollable` / `ScrollModel`
- Masked input implemented as a `TextBox` specialization via segment writer override

### Next for CodeEditor

- efficient document (piece table/rope) + incremental line index
- undo/redo
- classification + layered rendering pipeline + gutter
- improved scrolling policy and selection autoscroll
- background services with snapshot version gating
