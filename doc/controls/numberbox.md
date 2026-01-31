# NumberBox

`NumberBox<T>` is a single-line numeric editor built on the `TextEditorCore` infrastructure. It provides caret/selection/clipboard support like `TextBox`, while keeping a bindable numeric `Value`.

> Screenshots: `docs/images/numberbox-basic.png` (placeholder)

## Basic usage

```csharp
var age = new State<int>(42);

var ui = new NumberBox<int>()
    .Value(age);
```

## Undo / redo

NumberBox supports undo/redo:

- `Ctrl+Z`: undo
- `Ctrl+R`: redo

See [Undo/Redo](../undo-redo.md).

## Validation

Validation runs on each text change:

- If the text parses successfully (as `T`) and `ValueValidator` returns `null`, the `Value` is updated.
- Otherwise `Value` is not updated and a validation message is shown below the editor.

```csharp
var port = new State<int>(8080);

var ui = new NumberBox<int>
{
    ValueValidator = v => v is >= 1 and <= 65535 ? null : "Port must be in [1..65535]",
}.Value(port);
```

## Parsing settings

You can control parsing via:

- `ParseStyles` (default: `NumberStyles.Number`)
- `FormatProvider` (default: `null` meaning current culture behavior for parsing/formatting)

```csharp
var ui = new NumberBox<double>()
    .ParseStyles(NumberStyles.Float)
    .FormatProvider(CultureInfo.InvariantCulture);
```

## Styling

The validation message is styled via `NumberBoxStyle` (`NumberBoxStyle.Key`).

```csharp
var ui = new NumberBox<int>()
    .Style(NumberBoxStyle.Default with
    {
        ValidationPrefix = "Error: ",
    });
```

## Related

- [Text Editing](../text-editing.md)
- [Binding](../binding.md)
