# TextArea

`TextArea` is a multi-line text editor with soft wrapping by default.

Screenshot placeholder:

![TextArea](../../img/screenshots/textarea.png)

## Basic usage

```csharp
new TextArea("Hello\nWorld");
```

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

