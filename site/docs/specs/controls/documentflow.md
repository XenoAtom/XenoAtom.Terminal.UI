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

`IDocumentFlowContent` is an internal-facing contract for rendering/layout that the Markdown extension will implement.
The public API may expose a concrete `FlowDocument` type instead, as long as it remains Markdig-independent.

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

