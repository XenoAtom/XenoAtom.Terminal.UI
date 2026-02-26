---
title: DocumentFlow (Virtualized Rich Document Feed) Specs
---

# DocumentFlow (Virtualized Rich Document Feed) Specs

This document specifies a new control for **XenoAtom.Terminal.UI**: **`DocumentFlow`**.

`DocumentFlow` is a **high-performance, virtualized, vertically scrollable feed** of *documents*.
Each document is a **flow of blocks** (paragraphs, headings, lists, tables, code blocks, …) intended to be produced by a
renderer such as a future Markdown integration.

Primary motivation:

- Efficiently render a **large number of “documents”** (e.g. a chat/conversation timeline) where documents are appended
  over time.
- Provide the infrastructure required to build a separate package
  `XenoAtom.Terminal.UI.Extensions.Markdown` (Markdig-based) later, without taking a Markdig dependency in the core UI
  library.

> [!NOTE]
> This is a contributor-facing spec. An end-user page (under `site/docs/controls`) will be added once the control exists.

---

## Goals

- **Virtualized rendering**: only render what is visible (plus minimal lookahead), even with thousands of documents.
- **Append-only first**:
  - the common case is *only adding elements* at the end (conversation/log-like),
  - layout caches can assume mostly-static content.
- **Stable scrolling experience**:
  - support “follow tail” (pinned to bottom) for live feeds,
  - preserve viewport content when capacity trimming removes old items.
- **Rich block flow** suitable for Markdown rendering:
  - mixed inline styles (bold/italic/code/link) via style runs,
  - block-level layout (tables, quotes, code blocks, rules),
  - hyperlinks via OSC 8 when available.
- **Conversation-friendly layout**:
  - per-document alignment (left/right/center/stretch),
  - bubble-like background/border/padding per document,
  - configurable spacing between documents.
- **Low allocations / high throughput**:
  - store content as data (not a Visual per line),
  - reuse buffers and precomputed prefix sums for fast row→item mapping.

---

## Non-goals (v1)

- In-place editing of document content (this is a viewer/feed).
- Efficient arbitrary insert/remove in the middle (append-only is the optimized path).
- Full HTML/CSS layout parity (terminal-first layout only).
- Implementing Markdown parsing in the core library (handled by a future extension package).
- Asynchronous event callbacks for per-inline interactions (links are rendered as hyperlinks, not interactive widgets).

---

## Naming

The control name should not imply “logging” and should remain generic for:

- chat/conversation timelines,
- documentation viewers,
- live streaming output that is richer than lines.

Proposed names considered:

- `RichLogControl`: rejected (too log-specific and implies line-oriented text).
- `MarkdownViewer`: rejected (too specific; Markdown support lives in an extension).
- `FlowDocument*`: possible, but risks confusion with WPF types and doesn’t communicate “feed of documents”.

Chosen name for this spec: **`DocumentFlow`** (feed of flow-documents).

---

## Architecture overview

`DocumentFlow` is a composite control similar in spirit to `LogControl`:

- A `ScrollViewer` hosts an internal content visual implementing `IScrollable`.
- The content visual owns:
  - a **scroll model** (`ScrollModel`) and an efficient mapping between scroll offsets and items,
  - document measurement/layout caches (per viewport width),
  - rendering of only the visible rows.

Key difference vs `LogControl`:

- `LogControl` is fundamentally **line-based**.
- `DocumentFlow` is **block-flow based** with optional per-document alignment and bubble styling, and may need nested
  virtualization inside large documents.

---

## Data model

`DocumentFlow` is driven by a list of document items.

### Document item

Each item represents a “document” in the feed (e.g. a message).

Required metadata per item:

- `Alignment` (Left/Right/Center/Stretch)
- `MaxWidth` behavior (optional; to get chat-like bubbles)
- `Padding` + background/border styling (bubble chrome)
- `Content` as a **flow of blocks**

### Blocks

A block is a vertically stacked unit with its own layout rules.

Examples for Markdown:

- Paragraph / heading
- List (ordered/bulleted)
- Quote
- Code block (monospace, optional background, optional horizontal scroll behavior)
- Table
- Rule

Blocks SHOULD be represented as **data + rendering contract**, not as a `Visual` subtree by default.
A block MAY optionally wrap a `Visual` for complex cases, but that is not the fast path.

---

## Proposed public API surface (v1)

The API should match existing control patterns (`BindableList<T>`, `IScrollable`, follow-tail semantics similar to logs).

```csharp
namespace XenoAtom.Terminal.UI.Controls;

public enum DocumentFlowAlignment
{
    Left,
    Right,
    Center,
    Stretch,
}

public sealed class DocumentFlow : Visual, IScrollable
{
    public BindableList<DocumentFlowItem> Items { get; }

    public ScrollModel Scroll { get; }

    // Live feed behavior.
    public bool FollowTail { get; }
    public void ScrollToTail();

    // Capacity (optional, similar to LogControl).
    public int MaxCapacity { get; set; } // 0 disables trimming.

    // Default chrome for items (can be overridden per item).
    public Thickness ItemPadding { get; set; }
    public int ItemSpacing { get; set; }
}

public readonly record struct DocumentFlowItem
{
    public required IDocumentFlowContent Content { get; init; }
    public DocumentFlowAlignment Alignment { get; init; }
    public int? MaxWidth { get; init; } // optional bubble max width in cells
    public Thickness? Padding { get; init; } // per-item chrome override
    public Style? BackgroundStyle { get; init; } // supports colors/gradients via brushes
    public Style? BorderStyle { get; init; }
}
```

`IDocumentFlowContent` is the Markdig-independent content contract consumed by `DocumentFlow`.
It is expected to be produced by a future Markdown extension package, but it is also usable for non-Markdown rich feeds.

---

## Content model (`IDocumentFlowContent`)

This section is the critical part of the design: it defines how a “document” is represented and how blocks such as
paragraphs, lists, and tables fit into the rendering/virtualization pipeline.

Key constraints:

- `DocumentFlow` must compute **heights** for blocks to build prefix sums (for scroll extents and fast row→block mapping).
- Rendering must be **slice-based** (render only the visible rows of each block).
- The model must be **Markdig-independent** and allocation-conscious.
- It should reuse existing, proven behaviors where possible (e.g. `TextBlock` wrapping rules, `Table` layout).

### `IDocumentFlowContent`

At a minimum, `DocumentFlow` needs:

- **stable access** to blocks by index (avoid enumerator allocations),
- a **version** to detect changes (optional in v1, but required for dynamic content).

Proposed contract:

```csharp
namespace XenoAtom.Terminal.UI.Controls;

public interface IDocumentFlowContent
{
    /// <summary>
    /// Gets a monotonically increasing version. Increment when block structure or block content changes.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Gets the number of blocks in this document.
    /// </summary>
    int BlockCount { get; }

    /// <summary>
    /// Gets a block by index.
    /// </summary>
    DocumentFlowBlock GetBlock(int index);
}
```

Notes:

- `GetBlock(int)` should be **O(1)**.
- `Version` enables incremental re-layout of only the affected document item.
- The core library MAY ship a simple concrete implementation (e.g. `FlowDocument : IDocumentFlowContent`) that wraps an
  array/list of blocks.

### Block contract: `DocumentFlowBlock`

Blocks are the units that `DocumentFlow` measures and renders.

There are two supported rendering strategies:

1) **Native blocks** (fast path): block is stored as data and rendered directly into the `CellBuffer`.
2) **Visual blocks** (interop path): block is rendered by hosting an existing `Visual` (e.g. `Table`, `Rule`, custom
   composed visuals). This avoids reimplementing complex controls, at the cost of more overhead per block.

The block base type should be a small polymorphic surface:

```csharp
namespace XenoAtom.Terminal.UI.Controls;

public enum DocumentFlowBlockKind
{
    ParagraphText,
    PreformattedText,
    Rule,
    Visual,
}

public abstract class DocumentFlowBlock
{
    public abstract DocumentFlowBlockKind Kind { get; }

    /// <summary>
    /// Gets a monotonically increasing version for this block.
    /// </summary>
    public virtual int Version => 0;

    /// <summary>
    /// Optional spacing (in rows) added before/after this block.
    /// </summary>
    public virtual int MarginTop => 0;
    public virtual int MarginBottom => 0;
}
```

`DocumentFlow` maintains per-block cached layout results keyed by:

- viewport width,
- block identity (document index + block index),
- and the block’s `Version` (when non-zero).

### Native text blocks

Native text blocks are the **primary performance mechanism** for Markdown paragraphs and most inline-heavy content.
They are deliberately similar to `TextBlock` / `Markup` behavior, but they avoid any parsing step by consuming plain text
plus style runs.

#### `DocumentFlowTextBlock` (paragraph-like)

Represents a paragraph/heading/list item line flow.

Inputs:

- `string Text` (plain text; UTF-16)
- `StyledRun[] Runs` (optional; spans into `Text`)
- optional hyperlink spans (URI ranges; see below)
- wrapping + alignment settings (defaults should match `TextBlock`/`Markup` semantics)
- indentation and prefix metadata for lists/quotes

Layout behavior should match existing controls:

- **Wrapping**: whitespace-based, “paragraph-like” wrapping (same rules as `TextBlock` wrapping; see [TextBlock Specs](textblock.md)).
- **Trimming**: optional, for single-line blocks; multi-line blocks clip per line.
- **Unicode correctness**: width and slicing based on `TerminalTextUtility` (no splitting of wide runes/graphemes).

To support lists and quotes without introducing nested block trees in v1, the text block supports:

- `Indent` (left padding in cells),
- `HangingIndent` (extra indent applied to wrapped continuation lines),
- `LinePrefix` (rendered at the start of the first line only; e.g. bullet `• `),
- `ContinuationPrefix` (rendered at the start of wrapped lines; e.g. spaces aligning to the text after the bullet),
- optional “quote bars” can be expressed as a prefix repeated per line (e.g. `│ `) plus an indent.

This keeps the virtualization model simple (document → flat list of blocks), while still covering Markdown structures.

#### `DocumentFlowPreformattedTextBlock` (code blocks)

Code blocks and other “pre” content require different rules than `TextBlock`:

- preserve whitespace and indentation,
- do not skip leading whitespace on wrapped lines,
- typically do **not** wrap (clip instead), though wrapping could be an option.

This block kind should:

- treat newlines as hard line breaks,
- render each line clipped to the available width,
- support a distinct background style (often a subtle fill).

#### Inline styles and hyperlinks

Style runs:

- Use `StyledRun` from `XenoAtom.Terminal.UI.Text` (`Start`, `Length`, `Style`).
- Runs are applied during rendering by writing text segments with their associated `Style`.
- Block-level base style (e.g. paragraph default style, heading style) is applied under runs using the normal style
  composition rules (`Style.MergeUnspecified` / `Style | Style` behavior).

Hyperlinks:

- Markdown links should be expressed as spans similar to `StyledRun`, but carrying a URI string.
- During rendering, the renderer registers URIs via `CellBuffer.RegisterHyperlink(uri)` and writes the hyperlink token
  into the buffer for the covered cells.

The exact hyperlink span type is TBD, but it should be a small value type:

```csharp
public readonly record struct HyperlinkRun(int Start, int Length, string Uri);
```

### Visual blocks (interop with existing controls)

Some blocks are best expressed by reusing an existing control rather than implementing a dedicated renderer.
This is especially true for **tables**, where the library already has a flexible `Table` control.

`DocumentFlow` therefore supports a `Visual` block kind that hosts a child visual **only when needed**.

#### `DocumentFlowVisualBlock`

Proposed contract:

```csharp
public abstract class DocumentFlowVisualBlock : DocumentFlowBlock
{
    public sealed override DocumentFlowBlockKind Kind => DocumentFlowBlockKind.Visual;

    /// <summary>Create a visual instance for this block.</summary>
    public abstract Visual CreateVisual();

    /// <summary>
    /// Try to update a recycled visual instance to represent this block.
    /// Return false to request recreation.
    /// </summary>
    public virtual bool TryUpdate(Visual visual) => true;

    /// <summary>Called when a visual instance is being returned to a recycle pool.</summary>
    public virtual void Release(Visual visual) { }
}
```

Hosting rules:

- `DocumentFlow` attaches visuals for visible visual-blocks as children of its internal content visual.
- Offscreen visual-block visuals are detached and returned to a small recycle pool (similar in spirit to `ListBox<T>`
  recycling).
- Measurement and rendering are performed by the normal layout pipeline (`Measure`/`Arrange`/`RenderTree`), and clipping
  ensures that offscreen parts do not write into the `CellBuffer`.

This design keeps `DocumentFlow` fast for text-heavy feeds while still allowing “escape hatches” for complex blocks.

---

## How Markdown elements map to blocks

The Markdown extension (`XenoAtom.Terminal.UI.Extensions.Markdown`) should translate Markdig nodes into a **flat list of
blocks** for each document. The flattening is intentional: it avoids creating a deep visual tree and keeps virtualization
simple.

Suggested mapping (v1):

- Paragraph → `DocumentFlowTextBlock` (wrap enabled).
- Headings (`#`, `##`, …) → `DocumentFlowTextBlock` with:
  - distinct base style (bold/underline or theme-derived),
  - `MarginTop/MarginBottom` to match Markdown spacing.
- Thematic break (`---`) → `DocumentFlowVisualBlock` hosting `Rule` (or a dedicated `Rule`-kind native block).
- Lists (ordered/bulleted) → multiple `DocumentFlowTextBlock` blocks with:
  - `LinePrefix` = bullet/number prefix on the first line,
  - `ContinuationPrefix`/`HangingIndent` so wrapped lines align correctly,
  - nested lists increase indentation.
- Blockquotes (`>`) → text blocks with a quote prefix (`│ `) and indentation.
- Code blocks → `DocumentFlowPreformattedTextBlock` (clip; optional wrap).
- Tables → `DocumentFlowVisualBlock` hosting the existing `Table` control (details below).

Inline formatting (emphasis/strong/inline code) maps to `StyledRun[]` on the relevant text block.
Links map to hyperlink runs.

### Tables: reuse `Table` (no reimplementation)

Markdown tables should be rendered by **hosting the existing `Table` control** as a `DocumentFlowVisualBlock`.
`DocumentFlow` is responsible for virtualization; `Table` is responsible for table layout and rendering.

Why this works well:

- `Table` already implements the hard parts: column sizing, row height computation, chrome (grid/rounded/double), and
  arbitrary cell visuals (see [Table Specs](table.md)).
- `DocumentFlow` only attaches/measures/arranges/renders the `Table` visual when the block is visible.
- Rendering is clipped by the framework: `Visual.RenderTree` early-outs when a visual’s bounds do not intersect the
  current clip, so offscreen table rows/cells will be skipped even though the `Table` owns them.

Suggested Markdown→`Table` mapping (v1):

- Markdown table header → `Table.HeaderCells`
  - use `TextBlock` (or a future `StyledTextBlock`) with bold style (theme-derived).
- Markdown table rows → `Table.RowCells`
  - use `TextBlock` with `Wrap = false` and `Trimming = EndEllipsis` by default to keep tables readable in a feed.
  - allow a “wrap cells” option for users who prefer multi-line table rows (trades height for completeness).
- Table style defaults (reasonable for chat bubbles):
  - `TableStyle.Minimal` or `TableStyle.Grid` depending on how “structured” the feed should look.
  - avoid outer borders if the document bubble already has a border.

Inline formatting inside table cells:

- If the Markdown extension can easily produce ANSI markup strings, it can use the existing `Markup` control inside each
  cell (simple, but allocates markup strings and requires parsing).
- Preferred long-term approach: use a small visual (or shared renderer) that renders **plain text + `StyledRun[]`**
  directly (same data model as the `DocumentFlow` native text blocks). This avoids re-parsing and keeps inline rendering
  consistent across paragraphs and table cells.

Non-goal note:

- Markdown tables can be arbitrarily large, but the common case is small (a few rows). For very large tables, users
  should prefer `DataGridControl` or a future specialized virtualized table block.

---

## Layout, measurement, and virtualization

### Two-level virtualization

`DocumentFlow` MUST avoid work at two levels:

1) **Document-level**: do not render documents fully offscreen.
2) **Block-level** (within a visible document): do not render blocks that are offscreen inside a very tall document.

### Cached measurements

For a given viewport width, `DocumentFlow` maintains:

- per-document measured height (in rows),
- per-document prefix sums (document start row offsets),
- per-document per-block heights and prefix sums (block start row offsets within the document).

Append-only enables incremental updates:

- append a document → layout its blocks once → append heights to prefix sums.

On viewport width changes (e.g. terminal resize):

- invalidate cached layouts and recompute heights/prefix sums.
  This is an O(N) operation, but resizes are rare; correctness and scroll stability are more important than incremental
  width-change updates in v1.

### Mapping offsets to visible items

The core operation for rendering is:

- Given `VerticalOffset` and `ViewportHeight`, compute `[firstDocIndex..lastDocIndex]` using binary search over the
  document prefix sum array.
- For each visible document, compute the visible row slice and then repeat the same mapping within the document blocks.

This is identical in spirit to `LogControl`’s “row → entry” mapping, extended to nested blocks.

---

## Rendering model

`DocumentFlow` renders into the normal retained-mode pipeline (to `CellBuffer`), but it SHOULD:

- render only the visible viewport rows,
- avoid allocating strings during rendering,
- rely on cached line-break/layout data.

Inline styles SHOULD be represented as style runs (similar to markup parsing output) so the Markdown extension can
produce:

- plain text content,
- style runs relative to a block/text span,
- optional hyperlink spans (URI) that map to `CellBuffer` hyperlink ids.

---

## Updates and invalidation

While the optimized path is append-only and mostly static, the design should accommodate occasional updates:

- `DocumentFlowItem.Content` MAY expose a `Version` (or `Changed` event) so the control can re-layout only the affected
  document when content changes.
- When a document’s height changes while the user is not in follow-tail mode, the control SHOULD preserve the viewport
  stable (similar to `LogControl` trimming logic).

This keeps streaming scenarios feasible (e.g. a message being updated while it is still “in flight”).

---

## Styling

`DocumentFlow` should be styleable at two levels:

- **control-level defaults**: spacing, default bubble padding, default background/border, selection style (if selection is
  implemented).
- **item-level overrides**: per document bubble chrome and alignment.

Markdown-specific role styling (heading/list/code/link) is expected to live in the Markdown extension package and/or be
provided via theme style keys, but `DocumentFlow` must support:

- per-cell foreground/background brushes (including gradients),
- decorations (underline, bold, etc.),
- hyperlinks.

---

## Relationship to existing controls

- `ScrollViewer` remains the generic scrolling container.
- `LogControl` remains the high-throughput line-based control.
- `DocumentFlow` is the “rich document feed” counterpart optimized for block flow and conversation-like layout.

Implementation should reuse patterns already proven in `LogControl`:

- prefix sums for fast mapping,
- avoid creating a Visual per logical row,
- explicit “follow tail” handling.

---

## Implementation map (planned)

Suggested components:

- `DocumentFlow` control: `src/XenoAtom.Terminal.UI/Controls/DocumentFlow.cs`
- Internal content visual: `DocumentFlowContentVisual` (private nested type)
- Styling record: `DocumentFlowStyle` (if needed; keep v1 minimal)
- Tests: `src/XenoAtom.Terminal.UI.Tests/DocumentFlowTests.cs`
- Demo: `samples/ControlsDemo/Demos/DocumentFlowDemo.cs` (added when implemented)

---

## Testing plan

Tests should focus on determinism and virtualization behavior:

- **Virtualization**:
  - appending many documents does not create/measures/renders offscreen blocks (assert via metrics or a test renderer hook).
- **Follow tail**:
  - append while pinned scrolls to bottom,
  - manual scroll disables follow-tail,
  - `ScrollToTail()` restores it.
- **Alignment**:
  - left/right aligned bubbles are arranged within the viewport width and do not overlap.
- **Width change**:
  - resizing triggers relayout and correct extent changes.
- **Capacity trimming** (if included in v1):
  - removing old documents preserves viewport stability when not pinned.

---

## Future ideas

- Selection/copy across documents with word/line navigation (like LogControl).
- Search (find-only) integrated with `SearchReplacePopup`, operating on the plain-text projection.
- Optional “recycling” of block layout objects to reduce allocations further (pooling line-break arrays).
- Richer item chrome helpers (speech-bubble tails, avatars, timestamps) via a lightweight template system.
