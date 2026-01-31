---
title: TextBox
---

# TextBox

`TextBox` is a single-line text editor.

## Basic usage

```csharp
var name = new State<string>("Alex");
new TextBox().Text(name);
```

You can also pass initial text:

```csharp
new TextBox("Hello");
```

## Editing features

- cursor navigation
- selection (keyboard and mouse)
- clipboard shortcuts (Ctrl+C/X/V) when enabled by the control mode / clipboard settings
- overflow indicators when content is wider than the viewport

## Undo / redo

TextBox supports undo/redo:

- `Ctrl+Z`: undo
- `Ctrl+R`: redo

See [Undo/Redo](../undo-redo.md).

## Password mode

`TextBox` can mask its text to behave like a password input:

```csharp
new TextBox("hunter2")
    .IsPassword(true)
    .ClipboardMode(TextBoxClipboardMode.Disabled)
    .PasswordRevealMode(PasswordRevealMode.WhileFocused);
```

Masking uses the glyph configured by `TextBoxStyle.PasswordMaskGlyph`.

## Overflow indicators

When content is wider than the viewport, the TextBox can show start/end indicators configured by `TextBoxStyle` (arrows/ellipsis variants).

## Key properties

- `Text`: the current text (bindable, supports `State<string>` two-way binding via fluent API).
- `TextAlignment`: left/center/right alignment inside the editor.
- `IsPassword`: enables masking.
- `PasswordRevealMode`: controls when the real text is revealed.
- `ClipboardMode`: enables/disables copy/cut/paste behaviors (useful for secrets).

> [!NOTE]
> In password mode, copy/cut is typically disabled so the masked value doesn’t leak via clipboard.

## Defaults

- Default alignment: `HorizontalAlignment = Align.Start`, `VerticalAlignment = Align.Start` 

## Styling
TextBox uses background on the text region while keeping borders visually compatible with the terminal background.

See also:

- [Text Editing](../text-editing.md)
- [Binding](../binding.md)
- [Styling](../styling.md)
