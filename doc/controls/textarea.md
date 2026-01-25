# TextArea

`TextArea` is a multi-line text editor with soft wrapping by default.

Screenshot placeholder:

![TextArea](../../img/screenshots/textarea.png)

## Basic usage

```csharp
new TextArea("Hello\nWorld");
```

## Find / Replace

`TextArea` includes a built-in Find / Replace popup:

- `Ctrl+F`: Find
- `Ctrl+H`: Replace

See also `doc/controls/searchreplacepopup.md`.

## Undo / redo

TextArea supports undo/redo:

- `Ctrl+Z`: undo
- `Ctrl+R`: redo

Replace operations are undoable. `Replace All` is recorded as a single undo step.

See `doc/undo-redo.md`.

## Scroll integration

TextArea implements `IScrollable`, so it integrates with `ScrollViewer`:

```csharp
new ScrollViewer(new TextArea(longText));
```

## Styling

`TextAreaStyle` controls colors, padding, and selection rendering.

See also:

- `doc/text-editing.md`
- `doc/controls/scrollviewer.md`
