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

## Related specifications

- [MarkdownControl Specs](markdowncontrol.md) (Markdig-based extension package)

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
- `MaxWidth` / `MaxWidthPercent` behavior (optional; to get chat-like bubbles)
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
    public double? MaxWidthPercent { get; init; } // optional bubble max width in percent of viewport width
    public Thickness? Padding { get; init; } // per-item chrome override
    public Style? BackgroundStyle { get; init; } // supports colors/gradients via brushes
    public Style? BorderStyle { get; init; }
}
```

Width rule:

- `Stretch` alignment always uses the full available width.
- otherwise, bubble width starts from viewport width and is reduced by:
  - `MaxWidthPercent` (when in `(0, 100]`),
  - `MaxWidth` (when `> 0`),
  - with the **most restrictive** value winning.

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

#### Ergonomics: `FlowDocument` and common block types

`DocumentFlow` should be usable without forcing users to implement custom block classes for common scenarios.

Recommended approach:

- Ship a minimal `FlowDocument` implementation (`IDocumentFlowContent`) that stores blocks in a `List<DocumentFlowBlock>`.
- Provide helper block types and/or builder methods such as:
  - `AddParagraph(string text)` / `AddParagraph(string text, StyledRun[] runs, HyperlinkRun[] links)`
  - `Add(Visual visual)` (wrap into a visual-backed block)
  - `AddTable(Action<Table> build)` or `Add(Table table)`
  - `AddRule(...)`

This keeps the “happy path” simple (build a document from blocks) while preserving an extensible model for advanced/custom
blocks.

### Block contract: `DocumentFlowBlock`

Blocks are the units that `DocumentFlow` measures and renders.

The core design goal is to **avoid reimplementing existing controls** (tables, rules, rich text) while still keeping
excellent performance.

The primary strategy is therefore:

- Represent each block as a **recyclable `Visual`** (e.g. `Paragraph`, `Table`, `Rule`, a composed `VStack`).
- `DocumentFlow` **virtualizes by blocks**: it only attaches/measures/arranges/renders visuals that intersect the viewport.
- Offscreen block visuals are detached and returned to a small recycle pool (similar to `ListBox<T>` recycling).

This yields a “best of both worlds” approach:

- Reuse existing visual implementations (no duplicated layout/render logic).
- Keep high throughput by ensuring the visual tree stays small (only visible blocks are attached).

#### Proposed `DocumentFlowBlock` contract

Blocks are *descriptors* that know how to create/update/release their visual representation:

```csharp
namespace XenoAtom.Terminal.UI.Controls;

public abstract class DocumentFlowBlock
{
    /// <summary>
    /// Gets a monotonically increasing version for this block. Increment when the block's content changes.
    /// </summary>
    public virtual int Version => 0;

    /// <summary>
    /// Optional spacing (in rows) added before/after this block.
    /// </summary>
    public virtual int MarginTop => 0;
    public virtual int MarginBottom => 0;

    /// <summary>
    /// Gets a reuse key for recycling. Blocks with the same key can reuse visuals.
    /// </summary>
    public virtual object? ReuseKey => GetType();

    /// <summary>Create a visual instance for this block.</summary>
    public abstract Visual CreateVisual();

    /// <summary>
    /// Try to update a recycled visual instance to represent this block.
    /// Return <c>true</c> if the visual was updated successfully; otherwise <c>false</c>.
    /// </summary>
    public virtual bool TryUpdate(Visual visual) => false;

    /// <summary>Called when a visual instance is being returned to a recycle pool.</summary>
    public virtual void Release(Visual visual) { }
}
```

`DocumentFlow` maintains per-block cached layout results keyed by:

- viewport width,
- block identity (document index + block index),
- and the block’s `Version` (when non-zero).

> [!NOTE]
> This contract deliberately avoids a fixed `DocumentFlowBlockKind` enum. `DocumentFlow` can special-case known visual
> types for optional fast paths, but the public content model stays open-ended and extension-friendly.

---

## Reusable block visuals

To avoid reimplementing text/table rendering logic inside `DocumentFlow`, the core library should provide (or reuse)
small visuals that cover the common Markdown building blocks.

### `Paragraph` (new control; reusable outside DocumentFlow)

`TextBlock` is plain text; `Markup` is markup parsing. Markdown rendering needs a third option:

- **plain text + style runs** (no markup parsing at render time),
- plus common document layout behaviors (indentation, hanging indent, list prefixes).

Introduce a reusable `Paragraph` control that renders (see [Paragraph Specs](paragraph.md)):

- `string Text` (plain text)
- `StyledRun[] Runs` (optional)
- optional hyperlink spans (URI ranges)
- `Wrap`, `Trimming`, `TextAlignment` (similar semantics to `TextBlock`/`Markup`)
- indentation/prefix:
  - `Indent` (left padding in cells)
  - `HangingIndent` (extra indent for wrapped continuation lines)
  - `LinePrefix` (first line only, e.g. `• ` or `1. `)
  - `ContinuationPrefix` (wrapped lines, e.g. spaces aligning after the bullet)

This allows Markdown paragraphs, headings, list items, and blockquotes to be represented as a single cheap visual.

Hyperlinks:

- Links are expressed as spans similar to `StyledRun`, but carrying a URI string.
- During rendering, the visual registers URIs via `CellBuffer.RegisterHyperlink(uri)` and writes hyperlink tokens into
  the buffer for the covered cells.

Suggested span type:

```csharp
public readonly record struct HyperlinkRun(int Start, int Length, string Uri);
```

Performance intent:

- `Paragraph` should follow the same allocation-conscious philosophy as `TextBlock`/`Markup`
  (cache wrapping state by width, avoid per-render string allocations).

### Preformatted/code blocks: reuse `LogControl` (with optional height limiting)

Markdown code blocks can be very large. Reusing `LogControl` is attractive because it already provides:

- fast append-only storage,
- line virtualization (render only visible rows),
- markup parsing support when needed (syntax highlighting can be expressed as markup or style runs).

In a document feed, code blocks should typically be **read-only** and optionally **height-limited**:

- small blocks: render at natural height.
- large blocks: clamp to a maximum height (e.g. `MaxHeight`) and show an expandable footer (e.g. “Show more…”).

This can be done without adding a new core control by composing existing visuals inside a `DocumentFlowBlock`:

- `Border`/`Group` for chrome
- a `LogControl` configured for code display (`WrapText` often `false`, monospace theme style, no follow-tail)
- an optional `Link`/`Button` line to expand/collapse

This avoids nested scrolling in v1: expanding increases the block height, and the outer `DocumentFlow` scroll handles the
navigation.

### Tables and other complex blocks: host existing controls

Blocks like tables should be implemented by hosting existing visuals (e.g. `Table`, `Rule`), not by reimplementing them.
`DocumentFlow` stays responsible for block virtualization; the hosted control stays responsible for its own layout/render.

---

## Collapsible blocks and sections

Collapsing is valuable for large documents and Markdown outlines (headers).

Recommended v1 approach (keeps `DocumentFlow` generic and append-only optimized):

- Collapsing is a **content concern**:
  - a header block visual toggles a state in the content model,
  - the content model updates its block list (or block visibility) and increments `IDocumentFlowContent.Version`,
  - `DocumentFlow` re-layouts that document and updates prefix sums.

This makes headers and other blocks collapsible without requiring `DocumentFlow` to understand Markdown structure.

Future enhancement (optional):

- Provide a helper content implementation (e.g. `FlowDocumentOutline`) that understands heading levels and can hide/show
  blocks between headings efficiently, while still exposing a flat block list to `DocumentFlow`.

---

## How Markdown elements map to blocks

The Markdown extension (`XenoAtom.Terminal.UI.Extensions.Markdown`) should translate Markdig nodes into a **flat list of
blocks** for each document. The flattening is intentional: it avoids creating a deep visual tree and keeps virtualization
simple.

Suggested mapping (v1):

- Paragraph → `Paragraph` visual (wrap enabled).
- Headings (`#`, `##`, …) → `Paragraph` visual with:
  - distinct base style (bold/underline or theme-derived),
  - `MarginTop/MarginBottom` to match Markdown spacing,
  - optional “collapsible section” toggle behavior (see previous section).
- Thematic break (`---`) → host the existing `Rule` control.
- Lists (ordered/bulleted) → multiple `Paragraph` visuals with:
  - `LinePrefix` = bullet/number prefix on the first line,
  - `ContinuationPrefix`/`HangingIndent` so wrapped lines align correctly,
  - nested lists increase indentation.
- Blockquotes (`>`) → `Paragraph` visuals with a quote prefix (`│ `) and indentation.
- Code blocks → a `LogControl`-based block (optionally height-limited with expand/collapse chrome).
- Tables → host the existing `Table` control (details below).

Inline formatting (emphasis/strong/inline code) maps to `StyledRun[]` on the relevant `Paragraph`.
Links map to hyperlink runs.

### Tables: reuse `Table` (no reimplementation)

Markdown tables should be rendered by **hosting the existing `Table` control**.
`DocumentFlow` is responsible for virtualization; `Table` is responsible for table layout and rendering.

Why this works well:

- `Table` already implements the hard parts: column sizing, row height computation, chrome (grid/rounded/double), and
  arbitrary cell visuals (see [Table Specs](table.md)).
- `DocumentFlow` only attaches/measures/arranges/renders the `Table` visual when the block is visible.
- Rendering is clipped by the framework: `Visual.RenderTree` early-outs when a visual’s bounds do not intersect the
  current clip, so offscreen table rows/cells will be skipped even though the `Table` owns them.

Suggested Markdown→`Table` mapping (v1):

- Markdown table header → `Table.HeaderCells`
  - use `Paragraph` (single-line) or `TextBlock` with bold style (theme-derived).
- Markdown table rows → `Table.RowCells`
  - use `Paragraph` (single-line) or `TextBlock` with `Wrap = false` and `Trimming = EndEllipsis` by default to keep tables readable in a feed.
  - allow a “wrap cells” option for users who prefer multi-line table rows (trades height for completeness).
- Table style defaults (reasonable for chat bubbles):
  - `TableStyle.Minimal` or `TableStyle.Grid` depending on how “structured” the feed should look.
  - avoid outer borders if the document bubble already has a border.

Inline formatting inside table cells:

- Preferred approach: use `Paragraph` inside each cell so the extension can pass **plain text + `StyledRun[]`** without
  allocating markup strings or parsing markup in the control.
- If the extension already has markup strings readily available, it can use `Markup` inside cells (simple but less
  efficient due to parsing).

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

- `IDocumentFlowContent.Version` enables re-layout of only the affected document when its blocks change.
- `DocumentFlowBlock.Version` enables re-measurement of only the affected blocks when their content changes.
- **Dynamic visuals** are supported:
  - blocks can host visuals that update via bindings (e.g. the last “streaming” block containing a spinner),
  - `DocumentFlow` should not recreate those visuals; it should only update cached heights when the measured height
    actually changes.
- Collapsing/expanding sections is treated as a normal content update:
  - toggling increments content version and changes `BlockCount` / `GetBlock` results,
  - `DocumentFlow` recomputes prefix sums for that document.
- When a document’s height changes while the user is not in follow-tail mode, the control SHOULD preserve the viewport
  stable (similar to `LogControl` trimming logic).

This keeps streaming scenarios feasible (e.g. a message being updated while it is still “in flight”), and optimizes for
the common case where only the tail block changes frequently.

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
