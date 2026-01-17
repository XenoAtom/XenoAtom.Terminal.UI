# TextFiglet

`TextFiglet` renders large banner text using a FIGlet font.

Screenshot placeholder:

![TextFiglet](../../img/screenshots/textfiglet.png)

## Basic usage

```csharp
new TextFiglet("Hello")
{
    Font = FigletFont.Block,
};
```

## Fonts

FIGlet fonts are represented by `FigletFont` (namespace `XenoAtom.Terminal.UI.Figlet`).

- Use `FigletFont.Block` for a built-in demo font.
- Load a `.flf` font from a stream with `FigletFont.Load(...)`.

## Styling

Use `TextFigletStyle` to change the foreground/background and decorations:

```csharp
new TextFiglet("XenoAtom")
    .Style(TextFigletStyle.Default with { TextStyle = CellStyle.None | TextStyle.Bold });
```

