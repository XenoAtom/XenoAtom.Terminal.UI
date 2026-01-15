# Advanced Text Architecture Specification

## 1. Goals

### 1.1 Functional scope

The architecture must support:

**TextBox**

* Single-line editing
* Caret + selection + clipboard
* Undo/redo
* Optional “caret-keeping” horizontal shift without scrollbars

**TextArea**

* Multi-line editing
* Vertical/horizontal scrolling and/or word-wrap
* Page navigation, mouse wheel scrolling
* Mouse selection drag and autoscroll

**CodeEditor**

* Gutter: line numbers, breakpoints, diagnostics glyphs, folding markers
* Syntax highlighting (incremental)
* Diagnostics underlines (squiggles), search highlights
* Indentation rules, smart Enter, bracket matching
* Completion UI + signature help anchors
* Folding
* Optional multi-caret and rectangular selection
* Integration with LSP and/or Roslyn-style services

### 1.2 Performance

* Efficient mid-buffer edits for large documents (amortized near O(1) / O(log n)).
* Rendering is **viewport-only** (never render full document and clip).
* Low allocation rendering and tokenization.
* Incremental updates (layout, highlighting, diagnostics) with **version gating**.

### 1.3 Architectural properties

* Shared engine modules across TextBox/TextArea/CodeEditor.
* Clear separation of responsibilities:

  * **Storage** (document + line index + snapshots)
  * **Controller** (editing state + commands + undo)
  * **Presenter** (layout/mapping/render)
  * **Scrolling** (offsets/extents + bring-into-view)
  * **Chrome** (scrollbars/gutter)
  * **Services** (highlighting/diagnostics/completion/folding)

---

# 2. Module overview

1. **Text Storage**

* `ITextDocument`, `ITextSnapshot`, line index

2. **Editing Controller**

* `TextEditorController`: caret/selection/commands/undo/clipboard policy

3. **Presentation Layer**

* `ITextPresenter`: wrapping, tabs, cell widths, mapping, rendering

4. **Scrolling**

* `ScrollModel`, `IScrollable`, optional `ScrollViewer` chrome

5. **Decorations**

* highlights, squiggles, inlays, current-line, search results, etc.

6. **Editor Views / Shells**

* `EditorView` (shared internal component)
* `TextBox`, `TextArea`, `CodeEditor` compose around `EditorView`

7. **Language / Editor Services**

* tokenizer, diagnostics, completion, folding, formatting, etc. (snapshot-based)

---

# 3. Core data types

```csharp
public readonly record struct TextPosition(int Index); // UTF-16 index
public readonly record struct TextRange(int Start, int Length)
{
    public int End => Start + Length;
}

public enum SelectionKind { Linear, Rectangular }

public sealed class CaretState
{
    public TextPosition Position { get; set; }
    public int DesiredVisualX { get; set; } // preserve X across up/down
    public bool IsOverwriteMode { get; set; }
}

public sealed class SelectionState
{
    public SelectionKind Kind { get; set; } = SelectionKind.Linear;

    // Linear selection:
    public TextRange Range { get; set; }

    // Rectangular selection (optional):
    public RectSelectionState? Rect { get; set; }
}

public sealed class RectSelectionState
{
    public TextPosition Anchor { get; set; }
    public TextPosition Active { get; set; }
    // Additional derived info can be computed by presenter (visual space).
}
```

Indexing is UTF-16 for .NET ergonomics. The presenter is responsible for graphemes and terminal cell widths.

---

# 4. Text storage

## 4.1 Snapshot contract (read-only, immutable)

Snapshots are used by background services and presenter logic where stability matters.

```csharp
public interface ITextSnapshot
{
    int Version { get; }
    int Length { get; }
    int LineCount { get; }

    char this[int index] { get; }

    TextLine GetLine(int lineIndex);
    int GetLineIndexFromPosition(int position);

    void CopyTo(int start, Span<char> destination);

    // Optional high-performance path:
    SnapshotChunkEnumerator GetChunks(int start, int length);
}
```

### Line model

```csharp
public readonly struct TextLine
{
    public TextLine(int index, int start, int length, int lineBreakLength)
    { Index = index; Start = start; Length = length; LineBreakLength = lineBreakLength; }

    public int Index { get; }
    public int Start { get; }
    public int Length { get; }           // excluding line break
    public int LineBreakLength { get; }  // 0,1,2
    public int End => Start + Length;
    public int EndIncludingBreak => End + LineBreakLength;
}
```

## 4.2 Mutable document contract

```csharp
public interface ITextDocument
{
    ITextSnapshot CurrentSnapshot { get; }
    int Version { get; }

    IDisposable BeginUpdate(); // batches edits; nests

    void Insert(int position, ReadOnlySpan<char> text);
    void Remove(int position, int length);
    void Replace(int position, int length, ReadOnlySpan<char> text);

    event EventHandler<TextDocumentChangedEventArgs> Changed;
}
```

### Change payload

```csharp
public sealed class TextDocumentChangedEventArgs : EventArgs
{
    public required int OldVersion { get; init; }
    public required int NewVersion { get; init; }
    public required int Position { get; init; }
    public required int RemovedLength { get; init; }
    public required int InsertedLength { get; init; }
    public required int OldLineCount { get; init; }
    public required int NewLineCount { get; init; }

    public string? InsertedTextHint { get; init; } // optional optimization
}
```

## 4.3 Implementation strategies (recommended default: piece table)

* **Piece table** (recommended)
* Rope
* Gap buffer

The interface must not leak implementation details.

## 4.4 Line index requirements

The document maintains a line-start index:

* `GetLine(i)` and `GetLineIndexFromPosition(pos)` must be efficient (O(log n) or better).
* Incremental updates must use change range + newline delta.

---

# 5. Editing controller: `TextEditorController`

## 5.1 Responsibilities

`TextEditorController` is **non-visual** and owns editing behavior/state:

* caret + selection (+ multi-caret later)
* command execution
* undo/redo transactions
* clipboard actions via abstraction
* policy options (single-line, read-only, tab acceptance, etc.)
* selection expansion (word/line) and navigation primitives
* raising events for view invalidation

```csharp
public sealed class TextEditorController
{
    public ITextDocument Document { get; }
    public CaretState Caret { get; }
    public SelectionState Selection { get; }
    public IUndoManager Undo { get; }
    public EditorOptions Options { get; set; }

    public event Action? CaretChanged;
    public event Action? SelectionChanged;
    public event Action? ControllerInvalidated; // general repaint/layout invalidation hint

    public void Execute(IEditorCommand command);
    public void SetCaret(TextPosition position, bool clearSelection);
    public void SetSelection(TextPosition anchor, TextPosition active, SelectionKind kind);
}
```

### Editor options

```csharp
public sealed class EditorOptions
{
    public bool IsReadOnly { get; set; }
    public bool AcceptsReturn { get; set; }   // false for TextBox
    public bool AcceptsTab { get; set; }
    public bool WordWrap { get; set; }
    public int TabSize { get; set; } = 4;
    public bool NormalizeLineEndingsToLf { get; set; } = true;

    // Code editor evolutions:
    public bool EnableMultiCaret { get; set; }
    public bool EnableRectSelection { get; set; }
    public bool VirtualSpace { get; set; } // caret beyond EOL (optional)
}
```

## 5.2 Undo/redo

Undo manager must:

* support transactions (`BeginTransaction(name)` / `EndTransaction()`)
* restore caret + selection as part of each undo step
* coalesce sequential text insertions with rules (typing merges; movement breaks merges)

---

# 6. Presenter: `ITextPresenter`

## 6.1 Responsibilities

The presenter is the authority for:

* mapping document to **visual lines** (wrap/no-wrap)
* tab expansion in cell units
* wide glyph / combining mark handling (cell width)
* mapping doc position ↔ (row, col) visual space
* hit-testing mouse cell → doc position
* computing extent (visual rows/cols)
* rendering only the visible viewport

## 6.2 Contract

```csharp
public interface ITextPresenter
{
    void SetViewport(int widthCells, int heightCells);
    void SetOrigin(int originXCells, int originYRows);

    TextExtent GetExtent(ITextSnapshot snapshot, EditorOptions options);

    VisualCaretInfo GetVisualCaret(ITextSnapshot snapshot, EditorOptions options, TextPosition caret);
    TextPosition HitTest(ITextSnapshot snapshot, EditorOptions options, int xCell, int yRow);

    void Render(
        ITerminalRenderer r,
        ITextSnapshot snapshot,
        EditorOptions options,
        EditorRenderState state,
        Rect viewportClipCells);
}
```

```csharp
public readonly record struct TextExtent(int WidthCells, int HeightRows);
public readonly record struct VisualCaretInfo(int XCell, int YRow, int HeightRows);
```

## 6.3 Layout caching

* Cache wrapping decisions per logical line (depends on viewport width + options).
* On edits, invalidate from affected line forward until wrap state stabilizes.
* On viewport width change, invalidate all wrap caches.

## 6.4 Terminal cell-width rules

Presenter must use a cell width function (wcwidth-like):

* ASCII: 1 cell
* CJK wide: 2 cells
* combining marks: 0 (compose with previous base glyph)

Rules:

* Never split a double-width glyph across the viewport edge.
* Combining marks must not render “alone” if base glyph is clipped.

---

# 7. Scrolling

## 7.1 Scroll model

```csharp
public sealed class ScrollModel
{
    public int OffsetX { get; private set; }   // in cells
    public int OffsetY { get; private set; }   // in visual rows

    public int ViewportWidth { get; private set; }
    public int ViewportHeight { get; private set; }

    public int ExtentWidth { get; private set; }
    public int ExtentHeight { get; private set; }

    public event Action? Changed;

    public void SetViewport(int w, int h);
    public void SetExtent(int w, int h);

    public void SetOffset(int x, int y);
    public void ScrollBy(int dx, int dy);

    public void ScrollToMakeVisible(int xCell, int yRow);
}
```

## 7.2 Integration interface

```csharp
public interface IScrollable
{
    ScrollModel Scroll { get; }
}
```

## 7.3 Bring-caret-into-view policy

After any caret move or edit, the shell/view must:

1. `GetVisualCaret(...)`
2. `Scroll.ScrollToMakeVisible(caretX, caretY)`
3. invalidate render

This must be driven by the editor view (not generic scroll chrome), because it is editor semantics.

---

# 8. Decorations & overlays

## 8.1 Decoration primitives

Decorations are versioned and snapshot-keyed.

```csharp
public enum DecorationLayer
{
    Background,
    Selection,
    TextStyle,
    Underline,
    Foreground,
    Caret,
    Overlay
}

public sealed record TextDecorationSpan(TextRange Range, DecorationStyle Style, DecorationLayer Layer);

public sealed record LineGlyph(int LineIndex, GlyphKind Kind);

public sealed record InlayHint(TextPosition Position, string Text);
```

## 8.2 Decoration manager

`DecorationManager` aggregates:

* selection highlight (from controller)
* syntax classification
* diagnostics squiggles
* bracket match highlight
* search highlights
* current-line highlight
* inlay hints

Requirements:

* deterministic ordering by `Layer`, then priority
* efficient query per rendered line: “spans intersecting [lineStart, lineEnd]”
* discard stale decoration sets if snapshot version differs

---

# 9. Shared internal component: `EditorView`

## 9.1 Purpose

`EditorView` is the reusable internal component used by TextBox/TextArea/CodeEditor. It:

* holds `TextEditorController`
* holds `ITextPresenter`
* holds `ScrollModel`
* owns hit-testing and input mapping
* computes viewport + extents
* triggers invalidation

```csharp
public sealed class EditorView : Visual, IScrollable
{
    public TextEditorController Controller { get; }
    public ITextPresenter Presenter { get; }
    public ScrollModel Scroll { get; }
    public DecorationManager Decorations { get; }

    // Input entry points
    public void OnKey(KeyEvent e);
    public void OnTextInput(TextInputEvent e);
    public void OnPointerDown(PointerEvent e);
    public void OnPointerMove(PointerEvent e);
    public void OnPointerUp(PointerEvent e);
    public void OnWheel(WheelEvent e);
}
```

## 9.2 Rendering flow

On render:

1. `snapshot = Document.CurrentSnapshot`
2. ensure viewport known: `Presenter.SetViewport(w,h)`
3. `extent = Presenter.GetExtent(snapshot, options)`
4. `Scroll.SetExtent(extent.WidthCells, extent.HeightRows)`
5. `Presenter.SetOrigin(Scroll.OffsetX, Scroll.OffsetY)`
6. build `EditorRenderState` (caret + selection + decorations)
7. `Presenter.Render(...)`

---

# 10. Shell controls

## 10.1 TextBox

Composes `EditorView` with policy:

* `AcceptsReturn = false`
* `ViewportHeight = 1` (layout constrains to one row)
* No scrollbars by default
* Optional internal horizontal offset to keep caret visible:

  * `ScrollModel` still exists but is not exposed as visible chrome

Expected UX:

* left/right navigation keeps caret visible by adjusting `OffsetX`
* vertical movement is disabled

## 10.2 TextArea

Composes `EditorView` with policy:

* `AcceptsReturn = true`
* vertical scroll enabled
* wrap toggles horizontal behavior:

  * wrap ON: horizontal extent follows viewport, horizontal scrolling typically disabled
  * wrap OFF: horizontal scrolling enabled, extent width is max visual line width

May expose scrollbars:

* either drawn directly by TextArea
* or wrapped by generic `ScrollViewer` that reads `IScrollable`

## 10.3 CodeEditor

Composes:

* `GutterView`
* `EditorView`
* optional overlays (completion popup, signature help, find widget, etc.)

Layout:

* gutter width derived from:

  * digits of line count
  * reserved glyph columns (breakpoint/diagnostic/folding)
* editor viewport width = total width − gutter width − scrollbar gutter (if used)

---

# 11. Mouse and pointer interaction specification

## 11.1 Pointer coordinate space

Pointer events must provide:

* cell-based position `(xCell, yRow)` relative to the editor viewport
* modifier keys (Shift/Ctrl/Alt)
* button state
* click count (single/double/triple) or timestamp-based multi-click detection

If raw events are pixel-based, conversion to cells is required before hit-testing.

## 11.2 Capture / drag model

EditorView must support a “capture” concept:

* on pointer down inside editor viewport: capture pointer until up
* during capture: pointer moves update selection/caret even if pointer leaves viewport
* support autoscroll while dragging outside viewport bounds

## 11.3 Selection behaviors

### Single click (left)

* places caret at hit-tested position
* clears selection unless Shift is held

### Shift + click

* extends selection from existing anchor to clicked position (linear)
* for rectangular selection mode (optional), modifier can switch modes (recommended: Alt toggles rectangular)

### Click-and-drag

* updates active selection endpoint continuously
* if cursor leaves viewport edge, autoscroll in that direction and continue updating selection

### Double click

* selects word under pointer (word boundary rules configurable)

### Triple click

* selects logical line (or visual line; recommended: logical line)

### Right click

* optional: move caret to click location (common in editors)
* optional: context menu event emitted; selection preserved by default

## 11.4 Mouse wheel

* wheel scrolls vertically by N lines per notch (configurable)
* Ctrl+wheel optionally changes zoom (terminal UI might ignore or implement “font scale” concept)
* when the editor cannot scroll further, wheel event may bubble to parent containers

## 11.5 Gutter mouse interactions (CodeEditor)

GutterView hit-testing zones:

* breakpoint column: click toggles breakpoint marker
* folding column: click toggles fold
* line number column: click selects line; drag selects multiple lines

Gutter interactions must map to document lines, not visual wrapped rows.

---

# 12. Code editor services

## 12.1 Snapshot-based service contract

Services run on snapshots and publish results keyed by `snapshot.Version`. Stale results are discarded.

```csharp
public interface IEditorService
{
    string Name { get; }
    void OnDocumentChanged(ITextSnapshot snapshot, TextDocumentChangedEventArgs change);
}
```

## 12.2 Incremental syntax highlighting (required for code editor)

Maintain per logical line:

* token list
* lexer state at line start (for multi-line constructs)

Update algorithm:

1. find first affected logical line
2. re-tokenize forward, carrying state
3. stop when resulting tokens + end state match previous cached values (stabilization)
4. publish `TextDecorationSpan` (classification spans) for affected ranges

## 12.3 Diagnostics

Diagnostics publish:

* underline spans (style by severity)
* gutter glyphs per line
* optional hover text (tooltip-like UI)

## 12.4 Completion + signature help

Completion engine:

* requests data using caret position + snapshot
* supports cancellation and version gating
  UI:
* anchor rect derived from caret visual position
* supports mouse selection within completion list

## 12.5 Folding

Folding model:

* fold ranges defined on logical line intervals
  Presenter must support folded mapping:
* folded block renders as a single placeholder line
* hit-testing and navigation must treat folded regions as atomic unless expanded

---

# 13. Threading and version gating

* All document edits occur on UI thread.
* Background services read immutable snapshots.
* Every published result includes snapshot version; mismatches are discarded.
* Debounce service recomputation (tokenize/diagnose) to avoid thrash while typing.

---

# 14. Rendering and invalidation

## 14.1 Damage model (minimum)

* caret move: redraw minimal cells/rows (caret old/new + affected selection)
* selection drag: redraw affected region (ideally line-based)
* text edit: redraw impacted lines and possibly reflowed wrap region

## 14.2 Layering order (typical)

1. background (theme/current line)
2. selection background
3. glyphs (with classification styles)
4. underlines/squiggles
5. inlay hints
6. caret
7. overlay UI (search boxes, completion popups, etc.)

---

# 15. Testing requirements

* Random edit sequences maintain invariants (line index correctness, snapshot stability).
* Golden render tests: snapshot + options + viewport + origin ⇒ expected cell buffer.
* Performance tests:

  * long lines, heavy wrapping, large paste
  * selection drag with autoscroll
* Version gating tests:

  * stale service results never apply to newer versions

---

# 16. Recommended default configuration

* Storage: **piece table**
* Presenter: viewport-only renderer with per-line wrap cache
* EditorView: `TextEditorController + ITextPresenter + ScrollModel + DecorationManager`
* TextBox/TextArea/CodeEditor: thin shells configuring options and adding chrome
* Services: background tokenizer + diagnostics with snapshot version gating
