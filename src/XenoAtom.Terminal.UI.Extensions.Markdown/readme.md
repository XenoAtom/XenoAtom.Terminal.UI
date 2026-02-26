# XenoAtom.Terminal.UI.Extensions.Markdown

Markdown rendering for **XenoAtom.Terminal.UI**, powered by [Markdig](https://github.com/xoofx/markdig).

This package adds a `MarkdownControl` (and supporting types) that renders Markdown into a `DocumentFlow` so large
documents remain smooth and efficient to scroll.

## Status

This package is currently specified but not yet implemented.

## Goals

- Render CommonMark-compatible Markdown to terminal visuals (`DocumentFlow` + existing controls).
- Keep rendering efficient (block virtualization, minimal allocations).
- Make Markdown fully styleable (theme/style keys for block roles and inline roles).

## Where to start

- `site/docs/specs/controls/documentflow.md`
- `site/docs/specs/controls/markdowncontrol.md`

