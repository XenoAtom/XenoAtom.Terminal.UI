---
title: TextArea
---

# TextArea

`TextArea` is a multi-line text editor with soft wrapping by default.


![TextArea](../../img/controls/textarea.svg){.terminal}

## Basic usage

```csharp
new TextArea("Hello\nWorld");
```

To two-way bind to a `State<string>`:

```csharp
var text = new State<string>("Hello\nWorld");
new TextArea().Text(text);
```

## Find / Replace

`TextArea` includes a built-in Find / Replace popup:

- `Ctrl+F`: Find
- `Ctrl+H`: Replace

See also [SearchReplacePopup](searchreplacepopup.md).

## Undo / redo

TextArea supports undo/redo:

- `Ctrl+Z`: undo
- `Ctrl+R`: redo

Replace operations are undoable. `Replace All` is recorded as a single undo step.

See [Undo/Redo](../undo-redo.md).

## Scroll integration

TextArea implements `IScrollable`, so it integrates with `ScrollViewer`:

```csharp
new ScrollViewer(new TextArea(longText));
```

When you bound the control (e.g. with `.MaxHeight(...)`) the scroll model provides an extent larger than the viewport,
and the viewer can render scrollbars and synchronize offsets.

## Defaults

- Default alignment: `HorizontalAlignment = Align.Start`, `VerticalAlignment = Align.Start` 

## Styling
`TextAreaStyle` controls colors, padding, and selection rendering.

See also:

- [Text Editing](../text-editing.md)
- [Binding](../binding.md)
- [ScrollViewer](scrollviewer.md)

## Related

- [TextArea Specs](../specs/controls/textarea.md)
