# TextBox

`TextBox` is a single-line text editor.

Screenshot placeholder:

![TextBox](../../img/screenshots/textbox.png)

## Basic usage

```csharp
var name = new State<string>("Alex");
new TextBox().Text(name);
```

## Editing features

- cursor navigation
- selection (keyboard and mouse)
- clipboard shortcuts (Ctrl+C/X/V) when enabled by the control mode

## Overflow indicators

When content is wider than the viewport, the TextBox can show start/end indicators configured by `TextBoxStyle` (arrows/ellipsis variants).

## Styling

TextBox uses background on the text region while keeping borders visually compatible with the terminal background.

See also:

- `doc/text-editing.md`

