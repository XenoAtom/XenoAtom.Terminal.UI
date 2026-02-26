# XenoAtom.Terminal.UI.Extensions.Markdown

Markdown rendering for **XenoAtom.Terminal.UI**, powered by [Markdig](https://github.com/xoofx/markdig).

This package adds `MarkdownControl` and `MarkdownDocumentContent` to render Markdown into `DocumentFlow` blocks,
including CommonMark support plus table and alert block extensions.

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

## Features

- CommonMark block and inline rendering.
- Extensions enabled by default: pipe tables and alert blocks.
- `MarkdownRenderOptions` for code block wrapping/height and HTML/image fallbacks.
- `MarkdownStyle` for heading/link/emphasis/alert style customization.
- `MarkdownDocumentContent` for direct usage with `DocumentFlow` feeds.

## Docs

- User docs: `site/docs/controls/markdowncontrol.md`
- Specs: `site/docs/specs/controls/markdowncontrol.md`
