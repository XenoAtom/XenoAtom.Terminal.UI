# TextBlock

`TextBlock` renders read-only text with optional wrapping, alignment, and trimming.

## Basic usage

```csharp
new TextBlock("Hello, world!");
```

## Dynamic text (bindings)

`TextBlock` supports a dynamic text provider via a `Func<string>`:

```csharp
var count = new State<int>(0);

new TextBlock(() => $"Count: {count.Value}");
```

## Wrapping and trimming

```csharp
new TextBlock("This is a long line that can wrap.")
    .Wrap(true);

new TextBlock("This is a long single line that will be trimmed.")
    .Wrap(false)
    .Trimming(TextTrimming.EndEllipsis);
```

## Alignment

`TextAlignment` controls how the text is aligned inside the available width:

```csharp
new TextBlock("Centered")
    .TextAlignment(TextAlignment.Center)
    .HorizontalAlignment(Align.Stretch);
```

## Styling

Use `TextBlockStyle` to override colors and decorations for a subtree (or for a single `TextBlock`):

```csharp
new TextBlock("Accent")
    .Style(TextBlockStyle.Default with
    {
        Foreground = Colors.DeepSkyBlue,
        TextStyle = TextStyle.Bold,
    });
```

To apply a background only behind the glyphs, set `Background`:

```csharp
new TextBlock("Highlighted")
    .Style(TextBlockStyle.Default with
    {
        Background = Colors.Blue,
        Foreground = Colors.White,
    });
```

To fill the whole bounds with a background, enable `FillBackground`:

```csharp
new TextBlock("Banner")
    .HorizontalAlignment(Align.Stretch)
    .Style(TextBlockStyle.Default with
    {
        Background = Colors.Blue,
        Foreground = Colors.White,
        FillBackground = true,
    });
```

## Related

- `../binding.md`
- `../styling.md`
