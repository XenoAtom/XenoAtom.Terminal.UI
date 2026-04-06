---
title: MarkdownControl Specs
---

# MarkdownControl Specs

This document specifies the **Markdown rendering integration** for **XenoAtom.Terminal.UI** as a separate NuGet package:

- **Package**: `XenoAtom.Terminal.UI.Extensions.Markdown`
- **Implementation dependency**: Markdig (`Markdig` NuGet package, `1.0.*`)
- **Rendering target**: [`DocumentFlow`](documentflow.md) (virtualized block feed)

The core UI library MUST NOT take a Markdig dependency; Markdown support lives entirely in the extension package.

> [!NOTE]
> This is a contributor-facing spec. End-user documentation (under `site/docs/controls`) will be added with the first
> implementation and demo screenshots.

---

## Goals

- **CommonMark coverage**: render all CommonMark block and inline constructs using Markdig parsing.
- **Selected extensions** (v1):
  - **Tables** (pipe tables at minimum; see “Tables”).
  - **Alert blocks** (GitHub-style `[!NOTE]`, `[!TIP]`, …; see “Alert blocks”).
- **Efficient scrolling**: large documents should remain smooth by relying on `DocumentFlow` block virtualization.
- **Reuse existing controls**:
  - `Paragraph` for rich wrapped text,
  - `Table` for tables (no new table implementation),
  - `LogControl` for large preformatted/code blocks (with optional height caps),
  - `Rule`, `Group`, `Padder`, `VStack`, etc. as building blocks.
- **Fully styleable**: all block roles (heading/code/quote/alert/…) and inline roles (emphasis/link/code/…) can be
  styled via theme/environment style keys without forking the renderer.
- **Easy integration**:
  - render a single Markdown document via `MarkdownControl`,
  - render many Markdown “documents/messages” by producing `IDocumentFlowContent` for `DocumentFlow`.

---

## Non-goals (v1)

- Markdown **editing** (use `TextArea`/editors).
- Full HTML/CSS layout parity (terminal-first layout only).
- Arbitrary interactive inline widgets (links are OSC 8 hyperlinks).
- Syntax highlighting for code blocks (can be layered later).

---

## Public API surface (proposed)

### Control: `MarkdownControl`

The primary “easy” entry point to render a **single Markdown document**.

Namespace: `XenoAtom.Terminal.UI.Controls` (control discoverability stays consistent with other controls).

```csharp
namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class MarkdownControl : Visual, IScrollable
{
    public MarkdownControl();
    public MarkdownControl(string markdown);
    public MarkdownControl(Func<string> markdown);
    public MarkdownControl(Binding<string?> markdown);

    [Bindable] public partial string? Markdown { get; set; }

    // Optional: provide a custom Markdig pipeline; when null, a default pipeline is used.
    [Bindable] public partial Markdig.MarkdownPipeline? Pipeline { get; set; }

    // Optional: used to resolve relative links (e.g. "docs/readme.md") to absolute URIs.
    [Bindable] public partial Uri? BaseUri { get; set; }

    // Optional knobs for block rendering decisions.
    [Bindable] public partial MarkdownRenderOptions Options { get; set; }

    // Expose scrolling (forwarded to the internal DocumentFlow).
    public ScrollModel Scroll { get; }
}
```

Design notes:

- `MarkdownControl` is a **composite** control that internally hosts a `DocumentFlow` with a single item.
- When `Markdown` changes, it re-parses (or re-renders from a cached AST if available) and updates the underlying
  `IDocumentFlowContent`.
- The control SHOULD NOT allocate a new Markdig pipeline per update. Pipelines are immutable and thread-safe; build once
  and reuse.

### Reusable content: `MarkdownDocumentContent`

To support conversation feeds (many Markdown “documents”), the extension package SHOULD provide a reusable
`IDocumentFlowContent` implementation that can be appended to `DocumentFlow.Items`.

```csharp
namespace XenoAtom.Terminal.UI.Extensions.Markdown;

public sealed class MarkdownDocumentContent : IDocumentFlowContent
{
    public MarkdownDocumentContent(
        string markdown,
        Markdig.MarkdownPipeline? pipeline = null,
        Uri? baseUri = null,
        MarkdownRenderOptions? options = null);

    public int Version { get; }
    public int BlockCount { get; }
    public DocumentFlowBlock GetBlock(int index);
}
```

This type enables:

- creating one `MarkdownDocumentContent` per chat message,
- keeping `DocumentFlow` append-only semantics,
- reusing caches (parsed AST, computed runs) per document instance.

---

## Default Markdig pipeline

The extension package MUST provide a default pipeline that:

- Parses **CommonMark** correctly.
- Enables **pipe tables** and **alert blocks**.

Suggested approach (implementation detail):

- Use `MarkdownPipelineBuilder.Configure(...)` so it is explicit and easy to audit.
- Recommended token string (v1 baseline):
  - `"common+pipetables+alerts"`

Optional tokens (if deemed useful in v1, but not required by this spec):

- `autolinks` (turns bare URLs into links),
- `tasklists` (GitHub task list items),
- `emphasisextras` (strikethrough/sub/sup),
- `smartypants` (typographic punctuation).

The control MUST accept a user-provided `MarkdownPipeline` to enable/disable extensions as needed.

---

## Styling model

Markdown rendering MUST be styleable via a single style key so callers can theme Markdown without custom renderers.

### `MarkdownStyle`

Proposed style record:

```csharp
namespace XenoAtom.Terminal.UI.Extensions.Markdown.Styling;

public sealed record MarkdownStyle : IStyle<MarkdownStyle>
{
    public static MarkdownStyle Default { get; }
    public static StyleKey<MarkdownStyle> Key { get; }

    // Block roles
    public MarkdownHeadingStyle Heading1 { get; init; }
    public MarkdownHeadingStyle Heading2 { get; init; }
    public MarkdownHeadingStyle Heading3 { get; init; }
    public MarkdownHeadingStyle Heading4 { get; init; }
    public MarkdownHeadingStyle Heading5 { get; init; }
    public MarkdownHeadingStyle Heading6 { get; init; }

    public MarkdownParagraphStyle Paragraph { get; init; }
    public MarkdownQuoteStyle Quote { get; init; }
    public MarkdownCodeBlockStyle CodeBlock { get; init; }
    public MarkdownTableStyle Table { get; init; }
    public MarkdownListStyle List { get; init; }
    public MarkdownAlertStyles Alerts { get; init; }

    public Style ResolveLinkStyle(Theme theme);
    public Style ResolveInlineCodeStyle(Theme theme);
}
```

Guidance:

- Defaults SHOULD be readable without requiring user configuration:
  - headings are bold (and optionally accented),
  - links are underlined (and optionally accented),
  - inline code uses a subtle background,
  - quotes/alerts have an accent bar or border.
- The style API MUST allow overriding:
  - margins (top/bottom spacing between blocks),
  - prefixes (quote bar glyph, list bullet glyph),
  - table chrome (`TableStyle` preset selection),
  - code block max height and wrapping.

### Block-level chrome

Markdown alert blocks and (optionally) quotes SHOULD use existing chrome controls (`Group`, `Border`, `Padder`) to avoid
new bespoke rendering code:

- Alert blocks: `Group` with a label (e.g. `NOTE`) + background style, containing a `VStack` of rendered child blocks.
- Quotes: either:
  - prefix-based rendering (preferred for performance: `Paragraph.LinePrefix`/`ContinuationPrefix`), or
  - a light container (`Border`/`Padder`) when hosting non-paragraph child blocks.

---

## Mapping Markdig AST to DocumentFlow blocks

Markdown is rendered into a **flat list of `DocumentFlowBlock` descriptors** (one list per “document”).

General rule:

- Prefer one `DocumentFlowBlock` per top-level Markdown block.
- Nested blocks (lists/quotes/alerts) should be flattened when possible by using indentation + prefixes, so `DocumentFlow`
  can virtualize at block granularity without deep visual trees.

### Paragraphs

- Markdown paragraph → `ParagraphDocumentFlowBlock`
  - `Wrap = true`
  - `Runs` and `Hyperlinks` populated from inline rendering

### Headings

- `HeadingBlock` level 1–6 → `ParagraphDocumentFlowBlock` (or `VisualDocumentFlowBlock` hosting `Paragraph`)
  - Apply heading style (bold, underline, accent color, etc.).
  - Suggested default margins:
    - top margin for non-first heading,
    - bottom margin after heading.

### Emphasis / strong / strikethrough

Inline spans map to `StyledRun` segments:

- emphasis → `TextStyle.Italic`
- strong → `TextStyle.Bold`
- strikethrough (when enabled by pipeline) → `TextStyle.Strikethrough`

Runs MUST be merged when adjacent and identical to reduce allocations.

### Inline code

- Render as plain text (no markup parsing) and apply an inline-code style run.
- The inline-code style SHOULD include a subtle background and/or dimmed foreground (theme-driven).

### Links

- Use `HyperlinkRun(start, length, uri)` on the rendered text span.
- Apply a link style run (underline + optional accent).
- Resolve relative URIs against `BaseUri` when provided.
- Absolute local file paths SHOULD be normalized to `file://` URIs.
- Relative local file links MAY be resolved against `MarkdownRenderOptions.LocalFileRootPath` when provided. This local file root takes precedence over `BaseUri` for non-absolute, non-fragment links.

Images:

- Images SHOULD be rendered as a textual placeholder (e.g. `![alt](url)`), with the URL as a hyperlink.
- No raster rendering in v1.

### Soft/hard line breaks

- Soft break → single space (CommonMark behavior).
- Hard break → newline (`\n`) so `Paragraph` creates a hard line break.

### Block quotes

Markdown quote blocks can contain nested blocks. The renderer should:

- Prefer prefix + indentation for paragraph-like children:
  - `LinePrefix = "│ "`, `ContinuationPrefix = "│ "`
  - `Indent` increased for nested quote depth
- For non-paragraph children (tables/code blocks), wrap the produced visual in `Padder(left: quoteIndent)` and optionally
  render an accent bar via an adjacent `VStack`/`Grid` pattern if needed.

### Lists (ordered/unordered)

Lists MUST render without creating a new dedicated list control.

Preferred rendering strategy:

- Each list item becomes one or more document blocks.
- The first paragraph-like block of the item uses:
  - `LinePrefix = bulletOrNumber + " "`
  - `HangingIndent` so wrapped lines align under the item content.
- Subsequent paragraph blocks within the same item use:
  - `LinePrefix = new string(' ', bulletWidth + 1)` (continuation alignment)

Nested lists increase `Indent` accordingly.

Task lists (optional extension):

- Render as `"[ ] "` / `"[x] "` prefixes (styleable).

### Code blocks (fenced/indented)

Code blocks can be large; they should not expand the overall document height unboundedly by default.

Default rendering:

- Use a `LogControl` hosted as a `VisualDocumentFlowBlock`:
  - `WrapText = false` by default (preserve lines),
  - `Focusable = false` by default (outer scroll remains primary),
  - `MaxHeight` configurable (e.g. 12–20 rows) so code blocks remain scannable.
- Optionally show a small header/label (language) above the code block as a `Paragraph` block.

### Tables

Markdown tables MUST be rendered by hosting the existing `Table` control.

Baseline requirement:

- Support Markdig pipe tables (`UsePipeTables`).

Mapping (v1):

- Table header row → `Table.HeaderCells`
- Body rows → `Table.RowCells`
- Each cell content:
  - Prefer `Paragraph` so inline formatting (emphasis/code/links) is preserved.

Column alignment:

- Map Markdig column alignment to `Paragraph.TextAlignment` for the cell visual.

Grid tables:

- Markdig grid tables (`UseGridTables`) may include spanning and multi-block cell content.
- v1 MAY treat grid tables as:
  - pipe-table-compatible subset (when possible), or
  - a fallback text block explaining unsupported constructs.

### Thematic breaks

- Markdown `---` / `***` → `Rule` control.

### HTML blocks/inlines

CommonMark includes raw HTML. Terminal rendering MUST be safe:

- Render HTML as plain text (dim style), OR
- Skip HTML entirely (configurable via `MarkdownRenderOptions`).

No HTML execution exists; this is purely a display choice.

---

## Performance and allocation guidance

The extension MUST be efficient under repeated updates and large feeds:

- **Reuse pipelines** (no per-update pipeline creation).
- **Avoid per-inline string allocations**:
  - build paragraph text with a reusable buffer,
  - accumulate runs/hyperlinks using pooled lists or pre-sized arrays.
- Prefer flattening (indent/prefix) over nested visual trees for lists/quotes.
- `DocumentFlowBlock.ReuseKey` SHOULD be stable per block (e.g. based on Markdig source span) so `DocumentFlow` can recycle
  visuals when the document re-renders with small changes.

---

## Testing plan (when implemented)

Add tests in a dedicated test project (or in `XenoAtom.Terminal.UI.Tests` with a project reference) covering:

- Inline runs:
  - emphasis/strong/code/link spans produce correct `StyledRun[]` and `HyperlinkRun[]` boundaries.
- Block structure:
  - headings/paragraphs/lists/quotes render to the expected number of blocks.
- Tables:
  - pipe tables render using `Table` (header + rows) and preserve inline styles in cells.
- Alert blocks:
  - `[!NOTE]`, `[!WARNING]`, etc. render with correct title + styles and nested content.
- Code blocks:
  - height capping behaves correctly and does not explode the `DocumentFlow` extent.

---

## Demo plan (when implemented)

Add a `ControlsDemo` page:

- Render a “kitchen sink” Markdown document:
  - headings, paragraphs, links, quotes, lists, code blocks, tables, alerts.
- Show style customization examples:
  - different heading styles,
  - link styling,
  - different table presets,
  - alert styles.
