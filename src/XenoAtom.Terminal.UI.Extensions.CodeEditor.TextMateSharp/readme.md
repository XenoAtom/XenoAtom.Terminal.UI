# XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp

TextMateSharp-powered syntax highlighting for `CodeEditor` plus a Markdown fenced-code renderer for `MarkdownControl`.

## Package

```bash
dotnet add package XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp
```

## CodeEditor usage

```csharp
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;

var editor = new CodeEditor(source)
{
    SyntaxHighlighter = new TextMateCodeEditorSyntaxHighlighter(new TextMateCodeEditorOptions
    {
        LanguageId = "csharp",
    }),
};
```

You can also resolve the grammar from a file name:

```csharp
var highlighter = new TextMateCodeEditorSyntaxHighlighter(new TextMateCodeEditorOptions
{
    FileName = "program.cs",
});
```

## Markdown fenced-code usage

```csharp
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.Markdown;
using XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;

var markdown = new MarkdownControl(source)
{
    Options = MarkdownRenderOptions.Default with
    {
        CodeBlockRenderer = new TextMateMarkdownCodeBlockRenderer(),
    },
};
```

The renderer automatically chooses a bundled light or dark TextMate theme based on the current terminal UI theme.
