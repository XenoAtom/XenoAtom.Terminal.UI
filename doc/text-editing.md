# Text Editing (TextBox, TextArea, MaskedInput)

XenoAtom.Terminal.UI includes a text subsystem designed to scale from a single-line TextBox to a future Code Editor.

## Architecture (v1)

The v1 foundation includes:

- `TextEditorBase` + `TextEditorCore` (shared editing behaviors)
- `ITextDocument` and document implementations
  - `DynamicTextDocument` (bridges a bindable `Text` property to a document)
  - `TextDocument` (simple document implementation)

Text controls use the terminal cursor as the caret (no fake reverse-video caret rendering).

## Wrapping

`TextArea` uses soft wrapping by default.

## Scroll integration

`TextArea` implements `IScrollable` so it can be wrapped in a `ScrollViewer`:

```csharp
new ScrollViewer(new TextArea(text))
```

## Specs and next steps

See the living design document:

- `doc/specs/text_editor_specs.md`

