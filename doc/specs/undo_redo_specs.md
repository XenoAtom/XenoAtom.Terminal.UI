# Undo/Redo Architecture (TextEditorCore/Base + Search/Replace)

This document specifies a **lightweight**, **predictable**, and **extensible** undo/redo system for
`TextEditorCore` / `TextEditorBase` (and therefore `TextBox`, `TextArea`, `MaskedInput`, `NumberBox`, etc.).
It also specifies how the system integrates with **Search/Replace**.

The goal is to provide an implementation blueprint that fits the existing framework constraints:

- Retained-mode visuals, binding/dependency tracking.
- `ITextDocument` / `ITextSnapshot` as the text model.
- Integer/cell-based rendering.
- Terminal-oriented key input and platform differences (not every terminal emits the same modified keys).

---

## 1. Goals / non-goals

### 1.1 Goals

- Undo/redo of **document mutations** initiated by an editor:
  - typing, paste, delete/backspace, "kill" operations, replace/replace-all.
- Restore a sensible **editor state** per undo step:
  - caret index and selection range (minimum); scroll offset is optional but recommended.
- Support **coalescing** of successive edits into a single undo entry (typing runs, repeated backspace, etc.).
- Provide explicit **transaction/grouping** support for multi-step operations:
  - e.g. Search "Replace All" is one undo step even though it applies many replacements.
- Make the system usable from UI:
  - expose `CanUndo`, `CanRedo`, `Undo()`, `Redo()`, and `ClearUndoHistory()`.
- Handle **external document changes** safely:
  - if the document changes outside the editor's undo system, the undo history MUST be invalidated.
- Keep it efficient enough for typical terminal usage while remaining simple to implement.

### 1.2 Non-goals (v1)

- Persistent history across app runs.
- Branching undo trees (linear undo/redo only).
- Collaborative/merge-aware undo.
- Transforming history on external edits (v1 invalidates instead).
- Sophisticated semantic undo (e.g. "undo by word class", "undo by AST").

---

## 2. Code map / current architecture

Relevant existing types:

- Editors:
  - `TextEditorBase` (`src/XenoAtom.Terminal.UI/Controls/TextEditorBase.cs`)
  - `TextEditorCore` (`src/XenoAtom.Terminal.UI/Controls/TextEditorCore.cs`) (internal)
- Text model:
  - `ITextDocument` (`src/XenoAtom.Terminal.UI/Text/ITextDocument.cs`)
  - `TextDocument` (`src/XenoAtom.Terminal.UI/Text/TextDocument.cs`)
  - `DynamicTextDocument` (`src/XenoAtom.Terminal.UI/Text/DynamicTextDocument.cs`) (bindable `Text` bridge)
- Search/Replace integration:
  - `SearchReplacePopup` + `ISearchReplaceTarget` (`src/XenoAtom.Terminal.UI/Controls/SearchReplacePopup.cs`)
  - `TextEditorBase.CreateSearchReplaceTarget()` (internal)
  - `TextEditorCore.ReplaceCurrentSearchMatch(...)` / `ReplaceAllSearchMatches(...)`

---

## 3. Concepts and terminology

- **Undo stack**: sequence of performed undoable edits (oldest → newest).
- **Redo stack**: sequence of undone edits that can be redone (oldest → newest).
- **Undo entry**: one atomic undo step. It may represent:
  - a single document change (typical typing/backspace), or
  - a batch of changes applied as one semantic action (Replace All).
- **Transaction / group**: an explicit scope where multiple document changes are recorded as a single undo entry.
- **Coalescing**: merging consecutive compatible edits into the last undo entry.
- **External edit**: a document change not initiated through the editor's undo system.

---

## 4. Public API surface (v1)

### 4.1 `TextEditorBase` API

Expose undo/redo as editor-level behavior (owned by the editor by default):

- `public bool CanUndo { get; }` (bindable)
- `public bool CanRedo { get; }` (bindable)
- `public void Undo()`
- `public void Redo()`
- `public void ClearUndoHistory()`
- `public int MaxUndoEntries { get; set; }` (bindable, default e.g. 200)
- `public bool EnableUndo { get; set; }` (bindable, default true)

Notes:

- `CanUndo/CanRedo` MUST update when the stacks change. They should be implemented as bindable properties so UI can react.
- `ClearUndoHistory()` MUST clear both stacks and update `CanUndo/CanRedo`.

### 4.2 Optional future API (v2+)

- Share undo history across multiple editor views of the same document (document-owned manager).
- Undo scopes exposed as `IDisposable BeginUndoGroup(string? label = null)` for advanced callers.

v1 can keep the core manager internal but should keep the design compatible with a future document-owned implementation.

---

## 5. Internal design (recommended)

### 5.1 Ownership model (v1)

For v1, the simplest and safest model is:

- Each `TextEditorBase` instance owns a `TextUndoRedoManager`.
- All document mutations performed by `TextEditorCore` MUST go through that manager (or through core helpers that record).

If the editor's `TextDocument` is replaced (`TextEditorBase.TextDocument = ...`), the undo manager MUST:

- detach from the old document,
- attach to the new document,
- clear undo/redo stacks.

### 5.2 Data structures

#### 5.2.1 `TextChange`

Represents a replace operation at a position:

- `int Position`
- `string RemovedText` (may be empty)
- `string InsertedText` (may be empty)

The fundamental document operation is `Replace(Position, RemovedText.Length, InsertedText)`.

Undo applies the inverse:

- `Replace(Position, InsertedText.Length, RemovedText)`

Redo applies the forward operation again.

#### 5.2.2 `EditorStateSnapshot`

Undo should restore editor state meaningfully. Minimum fields:

- `int CaretIndex`
- `int SelectionAnchor` (or `-1` when none)
- `int SelectionEnd` (or `-1` when none)

Recommended fields:

- `int ScrollX`, `int ScrollY` (from `ScrollModel`)
- `int PreferredColumn` (if relevant to vertical caret movement)

Restoration rules:

- Restore selection and caret indices.
- Ensure they are aligned to a grapheme/text-element boundary (`TerminalTextUtility.GetPreviousTextElementIndex` / `GetNextTextElementIndex`).
- After restoration, call `EnsureCaretVisible(...)` to make the caret visible (or restore scroll offsets if you store them).

#### 5.2.3 `UndoEntry`

An undo entry contains:

- `TextChange[] Changes` (usually length 1; for Replace All can be N).
- `EditorStateSnapshot Before`
- `EditorStateSnapshot After`
- `UndoKind Kind` (Typing, Paste, Delete, Replace, ReplaceAll, etc.)
- `DateTime Timestamp` (or tick count) for coalescing windows
- Optional metadata for merging (see 5.4).

### 5.3 Applying undo/redo

Undo/redo MUST not create new undo entries while applying changes:

- The manager needs a suppression flag: `bool _isApplying`.
- While `_isApplying` is true:
  - recorders should no-op,
  - external-change detection should ignore document `Changed` events.

Apply rules:

- **Redo**: apply `Changes` in the order they were recorded.
- **Undo**: apply `Changes` in reverse order.

For multi-change entries (e.g. Replace All), this rule is critical when replacement lengths differ.

Implementation note:

- Use `using var _ = document.BeginUpdate();` around applying all changes in an entry to reduce intermediate work.

### 5.4 Coalescing rules

Coalescing should be supported, but conservative:

#### 5.4.1 Typing coalescing (default)

Typing edits (insertion with no selection) MAY be merged into the previous entry when:

- previous entry kind is Typing,
- time since previous entry ≤ `TypingMergeWindow` (e.g. 500ms),
- caret at end of previous insert and new insert is contiguous:
  - `new.Position == previous.Position + previous.InsertedText.Length`
- neither operation inserted a newline when in single-line mode,
- no intervening operation that should break typing runs (cursor moved, selection changed, paste, etc.).

If merged, the previous entry's `InsertedText` grows and the `After` snapshot updates.

#### 5.4.2 Backspace/delete coalescing

Repeated backspace/delete can coalesce similarly when:

- same kind (Backspace vs Delete),
- within time window,
- deletion positions are contiguous and direction-consistent.

#### 5.4.3 Non-coalescing operations

These should NOT coalesce by default:

- Paste
- Replace / ReplaceAll
- Kill operations (`Ctrl+K/U/W`) (unless you explicitly decide to treat them as delete coalescing)
- Any operation performed while selection exists (replace selection) unless explicitly grouped in a transaction

### 5.5 Transactions / grouping

Some operations apply multiple document changes but should be one undo step:

- Search "Replace All"
- Indent/outdent selection (future)

Provide an internal API such as:

- `BeginUndoGroup(UndoKind kind, EditorStateSnapshot before)`
- `RecordChange(TextChange change)` (accumulate)
- `CommitUndoGroup(EditorStateSnapshot after)`
- `RollbackUndoGroup()` (optional)

During an open group:

- coalescing SHOULD be disabled,
- the redo stack MUST be cleared on commit (like any new entry),
- group commit produces a single `UndoEntry`.

### 5.6 External changes and invalidation

Undo history is only valid if the document hasn't changed unexpectedly.

The system MUST detect and handle external edits by **invalidating** both stacks:

- Track a `KnownDocumentVersion` in the undo manager.
- On any attempt to record a change:
  - if `document.Version != KnownDocumentVersion`, clear undo/redo and set `KnownDocumentVersion = document.Version`.
- Subscribe to `document.Changed`:
  - if a `Changed` event is received while not recording and not applying, treat as external and clear history.

Notes:

- `DynamicTextDocument` can change its `Version` due to a bound `Text` property changing without raising `Changed`.
  Version checks are therefore required; event-based invalidation alone is insufficient.
- Clearing history on external change is acceptable for v1. More advanced reconciliation can be a v2 feature.

---

## 6. Integration in `TextEditorCore`

### 6.1 Central rule: mutate through a single path

All document mutation code paths in `TextEditorCore` MUST route through one helper that:

1. Ensures the undo manager is in sync with the current document/version.
2. Captures "before" editor state.
3. Captures the removed text span (from the cached text).
4. Applies `_document.Replace(...)`.
5. Captures "after" editor state.
6. Records the undo entry (with coalescing/transactions as appropriate).
7. Clears redo stack if a new entry is committed.

This prevents missing cases and keeps behavior consistent.

### 6.2 Mutations that must be captured

At minimum:

- `InsertText(...)` (typing and paste)
- `Backspace(...)`, `Delete(...)`
- `DeleteSelection()`
- Kill operations (`Ctrl+K`, `Ctrl+U`, `Ctrl+W`)
- Search/Replace:
  - `ReplaceCurrentSearchMatch(...)`
  - `ReplaceAllSearchMatches(...)` (transaction)

### 6.3 Search/Replace integration points

- "Find" does not affect undo.
- "Replace current" records one Replace entry.
- "Replace all" records one ReplaceAll entry containing N changes.

Because `TextEditorCore.ReplaceAllSearchMatches(...)` currently replaces matches from bottom → top,
the undo transaction MUST record changes in the **same execution order**, and undo MUST apply them in reverse.

---

## 7. Keyboard shortcuts and conflicts

### 7.1 Terminal realities

Terminal key input differs per terminal and OS:

- Some terminals do not transmit a distinct "Ctrl+Shift+Z" vs "Ctrl+Z".
- Some control characters map to ASCII control codes (`Ctrl+A`..`Ctrl+Z`) regardless of Shift.

Therefore:

- Defaults must be conservative and predictable.
- Multiple gestures for redo should be supported.

### 7.2 Default bindings (recommended)

**Undo**:

- `Ctrl+Z` (`TerminalChar.CtrlZ`)

**Redo**:

- `Ctrl+R` (`TerminalChar.CtrlR`)

Important: the current editor supports Emacs-like kill/yank (`Ctrl+K/U/W/Y`). In that mode:

- `Ctrl+Y` is already used for yank and MUST remain yank by default.
- redo should default to `Ctrl+Shift+Z` (and could also support `Ctrl+R` if you want an alternative that's more likely to be emitted).

### 7.3 Configurability

Expose a configuration mechanism for undo/redo gestures in `TextEditorBase` (or a derived options object),
so applications can choose Windows-like vs Emacs-like behavior.

---

## 8. Testing requirements

Add unit tests for:

- Basic undo/redo:
  - type "abc", undo → empty, redo → "abc"
- Coalescing:
  - type "a" then "b" within merge window, undo removes "ab" in one step
  - backspace coalescing (optional)
- Replace selection:
  - select "bc" in "abcd", type "X", undo restores "abcd" and selection/caret state
- ReplaceAll transaction:
  - replace all "a" → "xx", undo restores original (verify both content and caret)
- External invalidation:
  - mutate document directly (not through editor), ensure `CanUndo` becomes false and history clears
- Search/Replace:
  - Replace current is undoable
  - Replace all is one undo step

For time-based coalescing tests:

- Avoid real time sleeps. Use an injected clock/tick provider inside the undo manager (test-only).

---

## 9. Future roadmap (CodeEditor)

When introducing a piece-table/rope based document (CodeEditor v2), consider moving undo/redo closer to the document:

- Document-owned undo keeps history consistent across multiple editor views.
- Changes can reference immutable buffers instead of allocating strings for every entry.
- Large ReplaceAll operations can be represented as batched spans referencing shared storage.

The v1 design should keep the entry model (a list of `TextChange`s + before/after snapshot) compatible with that future direction.

