# Undo/Redo (Text Editors)

XenoAtom.Terminal.UI provides a lightweight undo/redo system for text editors built on `TextEditorBase` / `TextEditorCore`.

## Supported controls

Undo/redo is available in:

- `TextBox`
- `TextArea`
- `MaskedInput`
- `NumberBox<T>`
- Custom controls deriving from `TextEditorBase`

## Key bindings

- **Undo**: `Ctrl+Z`
- **Redo**: `Ctrl+R`

Note: some terminals remap or intercept certain key combinations. If a key binding does not reach the app, remap it in your terminal.

## Behavior

- Undo/redo records **document edits** (insert, delete, newline, backspace join, paste, selection replace).
- Undo/redo also restores the editor selection/caret state for a consistent experience.
- Find/Replace integration:
  - `Replace` is undoable.
  - `Replace All` is recorded as a **single** undo step.

## Coalescing (typing runs)

Typing is coalesced into larger undo steps when possible (so you can undo a “typing run” instead of one character at a time).
Coalescing is reset by operations like paste, selection replacement, and some cursor/selection changes.

## Configuration

`TextEditorBase` exposes:

- `EnableUndo` (default: `true`)
- `MaxUndoEntries` (default: `200`)
- `CanUndo` / `CanRedo` (bindable state)
- `Undo()` / `Redo()`
- `ClearUndoHistory()`

Example:

```csharp
new TextArea("Hello\nWorld")
    .EnableUndo(true)
    .MaxUndoEntries(500);
```

## When history is cleared

Undo history is cleared when:

- `EnableUndo` is set to `false`
- the editor is bound to a different document
- the document is edited outside of the editor’s edit pipeline (for example, by calling `ITextDocument` methods directly)

If you perform external document updates and still want undo/redo, route changes through the editor control (or provide a higher-level edit API that records undo entries).

