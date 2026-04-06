# XenoAtom.Terminal.UI.Extensions.Markdown

Markdown rendering for **XenoAtom.Terminal.UI**, powered by [Markdig](https://github.com/xoofx/markdig).

This package adds:

- `MarkdownControl` and `MarkdownDocumentContent` to render Markdown into `DocumentFlow` blocks.
- `MarkdownMarkupConverter` to convert Markdown into ANSI markup text for the `Markup` control.

It includes CommonMark support plus table and alert block extensions.

## Quick start

```csharp
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.Markdown;

var markdown = """
# Hello Markdown

Paragraph with **bold** text and [a link](https://example.com).
""";

var control = new MarkdownControl(markdown);
```

Resolve relative file links locally while still supporting standard web `BaseUri` resolution:

```csharp
var control = new MarkdownControl(markdown)
{
    Options = MarkdownRenderOptions.Default with
    {
        LocalFileRootPath = Environment.CurrentDirectory,
    },
};
```

Convert interpreted markdown into markup:

```csharp
var converter = new MarkdownMarkupConverter();
var markupText = converter.Convert(markdown);
var preview = new Markup(markupText).Wrap(true);
```

Preserve original markdown source (PromptEditor/syntax highlight scenarios):

```csharp
var converter = new MarkdownMarkupConverter();
var sourceMarkup = converter.ConvertPreservingSource(markdown);
var runs = converter.Highlight(markdown); // StyledRun[] over the original markdown text
```

## Features

- CommonMark block and inline rendering.
- Extensions enabled by default: pipe tables and alert blocks.
- `MarkdownRenderOptions` for code block wrapping/height, compact spacing, HTML/image fallbacks, and local file-link resolution.
- Theme-aware pleasant defaults (bright-yellow headings, accent strong text, bright-red inline code, semantic alerts).
- `MarkdownStyle` for heading/link/emphasis/alert style customization.
- `MarkdownDocumentContent` for direct usage with `DocumentFlow` feeds.
- `MarkdownMarkupConverter` for both interpreted rendering and source-preserving markdown highlighting (`PromptEditor`-ready).

## Docs

- User docs: `site/docs/controls/markdowncontrol.md`
- User docs: `site/docs/controls/markdownmarkupconverter.md`
- Specs: `site/docs/specs/controls/markdowncontrol.md`
