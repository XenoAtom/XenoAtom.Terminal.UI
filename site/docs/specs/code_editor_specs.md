---
title: CodeEditor Control Specs
---

# CodeEditor Control Specs

This document specifies the `CodeEditor` control for **XenoAtom.Terminal.UI**.

`CodeEditor` is intended to be the code-oriented editor surface built into the core `XenoAtom.Terminal.UI` package,
implemented on top of the existing `TextEditorBase` / `TextEditorCore` infrastructure and explicitly designed to keep the
large-document / very-long-line optimizations already present in `TextEditorCore.LayoutCache`.

> [!NOTE]
> This spec is intentionally focused on the **core infrastructure** that must live in `XenoAtom.Terminal.UI`.
> Language-specific syntax packages, grammars, and richer IDE features can be shipped later from separate assemblies.

Related specifications:

- [Text Editor Architecture](text_editor_specs.md)
- [Undo/Redo](undo_redo_specs.md)
- [Layout Protocol](layout_protocol_specs.md)
- [Dirty Rendering](dirty_rendering_specs.md)

---

## Goals

- Provide a first-class `CodeEditor` control inside `XenoAtom.Terminal.UI`.
- Reuse the proven editor foundation:
  - `TextEditorBase`
  - `TextEditorCore`
  - `ITextDocument` / `ITextSnapshot`
  - `ScrollModel` / `IScrollable`
- Preserve the current large-file / very-long-line performance characteristics:
  - viewport-only rendering,
  - sparse wrapped-row checkpoints,
  - bounded wrapped-row block caches,
  - incremental layout updates on document edits.
- Add code-editor-specific chrome and extensibility:
  - line numbers (enabled by default),
  - left and right pluggable margins,
  - current-line and code-oriented styling,
  - pluggable syntax highlighting.
- Support syntax highlighting from external packages without forcing language logic into the core library.
- Support incremental / diff-aware syntax highlighting so edits in very large documents do **not** require re-highlighting
  the whole file on every change.
- Provide a simple syntax-highlighting hook, similar in spirit to `PromptEditor`, while also defining a more scalable
  provider model for serious code-editing workloads.

---

## Non-goals (v1)

- Full IDE functionality:
  - LSP integration,
  - symbol rename,
  - semantic navigation,
  - code actions,
  - inline diagnostics panels,
  - minimap.
- Multi-caret / rectangular selection / virtual space.
- Folding implementation in the first delivery, though the gutter design should keep room for it.
- Bundling many language grammars inside `XenoAtom.Terminal.UI`.
- Defining a universal parser / AST format in the core package.

---

## Design principles

### 1. `CodeEditor` is specialized chrome around the existing text editor engine

`CodeEditor` SHOULD derive from `TextEditorBase` rather than creating a completely new editing engine.

That means:

- caret, selection, scrolling, clipboard, undo/redo, search popup integration, and document ownership stay in the shared
  editor core;
- `CodeEditor` adds:
  - margin layout,
  - code-editor surface styling,
  - line-number rendering,
  - syntax-highlighting plumbing,
  - future code-oriented overlays.

### 2. Highlighting must be line-oriented, not wrapped-row-oriented

Syntax highlighting results MUST be expressed in terms of **logical document lines** and UTF-16 spans within those lines,
not in terms of wrapped rows.

Reason:

- wrapping is viewport-dependent;
- wrapping is already solved efficiently by `TextEditorCore.LayoutCache`;
- highlighting should survive viewport width changes without being recomputed from scratch;
- highlighting packages should not need to duplicate wrapping logic.

### 3. Scrolling must never force whole-document grapheme or highlight recomputation

Pure scrolling SHOULD only:

- map visible rows to logical lines via `TextEditorCore.LayoutCache`,
- obtain the visible wrapped segment(s) for those lines,
- intersect visible text segments with cached highlight runs.

Pure scrolling MUST NOT:

- re-tokenize the entire document,
- rebuild all line highlight runs,
- re-measure every line number width from total document size,
- walk grapheme boundaries outside the visible rows / active wrapped-row blocks.

### 4. The core package defines contracts, not languages

The core library SHOULD ship:

- the control,
- default line-number margin,
- margin infrastructure,
- syntax-highlighting interfaces / delegates / state contracts,
- styles.

The core library SHOULD NOT ship language-specific tokenizers beyond tiny samples/tests.

---

## Terminology

- **Logical line**: a line from `ITextSnapshot.GetLine(lineIndex)`.
- **Wrapped row**: a viewport-dependent visual row produced from a logical line by `TextEditorCore.LayoutCache`.
- **Margin**: a non-text strip rendered on the left or right of the editor surface and vertically aligned with document rows.
- **Line-number margin**: the default left margin that renders 1-based logical line numbers.
- **Classification / highlight run**: a `StyledRun`-like span that applies a style to a range of text.
- **Line state**: highlighter-owned per-line state used to continue tokenization across lines.
- **Incremental highlighting**: re-highlighting only the affected line range after a text change, usually continuing until the
  end-of-line state stabilizes.

---

## High-level architecture

Conceptually, `CodeEditor` is:

```text
[ left margins ][ editor text surface ][ right margins ]
```

Where:

- the **editor text surface** is still powered by `TextEditorCore`;
- **left margins** and **right margins** are non-horizontally-scrolling chrome columns aligned to visible document rows;
- syntax highlighting decorates the text surface, not the margins.

### Responsibilities split

#### `TextEditorCore`

Remains responsible for:

- document editing,
- selection,
- caret movement,
- viewport mapping,
- extent computation,
- wrapped-row layout caching,
- search / replace primitives already supported by `TextEditorBase`.

#### `CodeEditor`

Adds:

- margin measurement and rendering,
- adaptive line-number width policy,
- current-line visual treatment,
- syntax-highlighting integration,
- future code-editor overlays.

#### Syntax-highlighting provider

Responsible for:

- tokenization / classification,
- incremental state updates after document changes,
- producing line-relative styled spans.

The provider MUST NOT be responsible for:

- wrapping,
- horizontal scrolling,
- margin rendering,
- caret positioning.

---

## Proposed public surface (v1/vNext direction)

The current API follows these concepts closely.

```csharp
namespace XenoAtom.Terminal.UI.Controls;

public delegate void CodeEditorLineHighlighter(
    in CodeEditorLineHighlightRequest request,
    List<StyledRun> runs);

public readonly record struct CodeEditorLineHighlightRequest(
    ITextSnapshot Snapshot,
    Theme Theme,
    int LineIndex,
    int LineStart,
    int LineLength,
    int CaretIndex,
    int SelectionStart,
    int SelectionLength);

public enum CodeEditorMarginSide
{
    Left,
    Right,
}

public readonly record struct CodeEditorVisibleLine(
    int LineIndex,
    int RowInLine,
    bool IsFirstRowOfLine,
    int ScreenY);

public abstract class CodeEditorMargin : IVisualElement
{
    public abstract CodeEditorMarginSide Side { get; }

    public abstract int MeasureWidth(in CodeEditorMarginMeasureContext context);

    public abstract void Render(in CodeEditorMarginRenderContext context);

    public virtual bool OnPointerPressed(in CodeEditorMarginPointerContext context) => false;
}

public abstract class CodeEditorSyntaxState
{
    public abstract int SnapshotVersion { get; }
}

public abstract class CodeEditorSyntaxHighlighter
{
    public abstract CodeEditorSyntaxState Build(in CodeEditorSyntaxBuildContext context);

    public abstract CodeEditorSyntaxState Update(
        CodeEditorSyntaxState previousState,
        in CodeEditorSyntaxUpdateContext context);

    public abstract void GetLineRuns(
        CodeEditorSyntaxState state,
        in CodeEditorLineSyntaxRequest request,
        List<StyledRun> runs);
}

public interface IAsyncCodeEditorSyntaxHighlighter
{
    ValueTask<CodeEditorSyntaxState> BuildAsync(
        in CodeEditorSyntaxBuildContext context,
        CancellationToken cancellationToken = default);

    ValueTask<CodeEditorSyntaxState> UpdateAsync(
        CodeEditorSyntaxState previousState,
        in CodeEditorSyntaxUpdateContext context,
        CancellationToken cancellationToken = default);
}

public sealed partial class CodeEditor : TextEditorBase
{
    [Bindable] public partial bool ShowLineNumbers { get; set; }
    [Bindable] public partial int MinLineNumberDigits { get; set; }
    [Bindable] public partial bool HighlightCurrentLine { get; set; }

    // Convenience hook, similar to PromptEditor.
    [Bindable] public partial Delegator<CodeEditorLineHighlighter> Highlighter { get; set; }

    // Advanced provider for large documents and incremental tokenization.
    [Bindable(NoVisualAttach = true)]
    public partial CodeEditorSyntaxHighlighter? SyntaxHighlighter { get; set; }

    public BindableList<CodeEditorMargin> LeftMargins { get; }
    public BindableList<CodeEditorMargin> RightMargins { get; }
}
```

Notes:

- `Highlighter` is the **simple** `PromptEditor`-style hook.
  - It is convenient for demos, toy languages, and tests.
  - It is not sufficient by itself for the best possible large-document behavior.
- `SyntaxHighlighter` is the **advanced** incremental provider.
  - This is the intended integration point for the future language package.
- When both are present, `SyntaxHighlighter` SHOULD take precedence.
- `CodeEditorMargin` instances SHOULD be attachable from other assemblies without requiring them to be `Visual`s.

---

## Inherited editing behavior

`CodeEditor` SHOULD inherit the baseline editing behavior already available through `TextEditorBase`:

- text insertion / delete / replace,
- selection and clipboard,
- caret / cursor integration,
- `ITextDocument` swapping,
- search popup integration,
- undo / redo integration,
- `IScrollable` / `ScrollModel` support.

The first version of `CodeEditor` SHOULD feel like `TextArea` with code-editor chrome and highlighting, not like a separate
editing product with incompatible editing semantics.

---

## Layout behavior

## Overall layout model

The arranged editor rectangle is split into three zones:

1. **Left margin strip**
2. **Text surface**
3. **Right margin strip**

Margins are analogous to the prompt column in `PromptEditor`:

- they consume dedicated width outside the main text surface,
- they do not horizontally scroll with the document text,
- they remain vertically synchronized with the visible rows.

### Width computation

At arrange time, `CodeEditor` MUST compute:

- total left margin width = sum of enabled left-margin widths,
- total right margin width = sum of enabled right-margin widths,
- text surface width = total bounds width - left - right - style padding.

The text surface width is then passed into `TextEditorCore.UpdateLayout(...)` so the existing wrapped-line cache remains the
single source of truth for row mapping.

### Important constraint

Margins MUST NOT independently measure wrapped content.

Instead, margins should consume row/line data derived from `TextEditorCore.LayoutCache`:

- visible wrapped rows,
- owning logical line,
- whether a wrapped row is the first row of that logical line.

This prevents duplicated text measurement and duplicated grapheme walking.

---

## Line-number margin

Line numbering is ON by default.

### Behavior

- Render 1-based logical line numbers.
- Numbers are shown only on the **first wrapped row** of a logical line.
- Continuation rows are blank by default.
- The current caret line MAY use a stronger style.

### Adaptive width policy

The line-number margin width MUST adapt to the **actual range currently covered by the viewport**, not to
`snapshot.LineCount`.

That means width should be based on:

- the maximum visible logical line number in the current viewport,
- optionally combined with a configurable minimum such as `MinLineNumberDigits`.

Examples:

- editing near line 1 in a million-line file should not reserve 7 digits just because the file is large;
- when scrolling into line 10_000, the margin may expand as needed.

### Stability rule

The width SHOULD only change when the visible line-number **digit bucket** changes.

Examples:

- scrolling from lines 12–40 to 41–70 should keep the same width;
- scrolling from 98–126 may grow the width from 2 digits to 3 digits.

This minimizes layout churn while still keeping the margin compact for early parts of large files.

### Performance implication

The line-number width change MUST be treated as a width-affecting chrome change, not as a reason to recompute syntax state.

When the gutter width changes:

- `TextEditorCore.LayoutCache` may need to refresh wrapping because the content viewport width changed;
- syntax-highlight results MUST remain reusable because they are stored line-relative, not wrap-relative.

---

## Pluggable margins

`CodeEditor` SHOULD support ordered collections of margins on both sides:

- `LeftMargins`
- `RightMargins`

Default configuration:

- left: line-number margin
- right: empty

### Intended use cases

Left margins:

- line numbers,
- git-diff added/modified/deleted markers,
- breakpoint glyphs,
- fold indicators,
- diagnostics severity markers.

Right margins:

- overview markers,
- line annotations,
- blame-like metadata,
- custom status columns.

### Margin contract

Margins SHOULD be:

- pluggable from other assemblies,
- line-aware rather than text-aware,
- cheap to render for visible rows only,
- optionally interactive (e.g. click to toggle marker/breakpoint).

Margins receive:

- visible row/line mapping,
- editor focus / current-line state,
- theme/style data,
- a render target clipped to the margin bounds.

Margins SHOULD NOT need raw full-document text unless they opt into it.

### Wrapped-row semantics

Margins MUST be able to distinguish:

- first wrapped row of a logical line,
- continuation wrapped rows.

This is necessary because some margins:

- render only once per logical line (line numbers),
- while others may render on all wrapped rows (background bands, guides).

---

## Syntax-highlighting model

The spec defines **two integration levels**.

### Level 1: simple synchronous line highlighter

This is the convenience model, comparable to `PromptEditor.Highlighter`.

Characteristics:

- synchronous,
- easy to plug in,
- line-oriented request,
- returns `StyledRun`s relative to a single logical line.

This path is useful for:

- tests,
- small scripting languages,
- demos,
- consumers that do not need persistent incremental state.

### Level 2: advanced incremental syntax provider

This is the scalable model intended for large files and future language packages.

Characteristics:

- maintains provider-owned syntax state,
- supports incremental updates from a previous state,
- returns line-relative runs on demand,
- can optionally compute in the background.

The advanced provider is the preferred path for production syntax highlighting.

---

## Incremental highlighting requirements

### Provider state

The highlighter MUST be able to keep opaque, provider-owned state associated with a specific snapshot version.

Examples of state:

- tokenizer end-of-line states,
- per-line token tables,
- cached classifications,
- language-specific parser checkpoints.

### Update model

After a document change, the editor SHOULD call the provider with:

- the new snapshot,
- the prior syntax state,
- the document change,
- the affected line range.

The provider SHOULD be able to:

1. start re-tokenization at the first affected logical line,
2. continue forward until end-of-line state converges with the previous cached state,
3. stop early when downstream lines are unchanged.

This is the crucial mechanism that makes large-document highlighting feasible.

### Version gating

All syntax states and async results MUST be tied to a snapshot version.

If an async result arrives for an older snapshot version, the editor MUST discard it.

### Scroll behavior rule

Pure scrolling MUST NOT call `Build(...)` or `Update(...)` on the syntax provider.

Scrolling may only call:

- `GetLineRuns(...)` for visible logical lines,
- or use already cached visible line runs.

---

## Relationship with `TextEditorCore.LayoutCache`

This is a hard requirement.

`CodeEditor` syntax highlighting MUST integrate with the existing high-performance wrapped-line layout cache instead of
recreating wrapping logic.

### Required interaction model

For each visible wrapped row, the editor should:

1. Use `TextEditorCore.LayoutCache` to map row -> logical line + row-in-line.
2. Use the same cache to get the visible wrapped segment start/length within the logical line.
3. Query the highlight provider for runs for that logical line.
4. Intersect only the visible wrapped segment with the relevant highlight runs.
5. Render the resulting styled text.

### Consequence

The highlight pipeline MUST be line-relative and MUST NOT store pre-wrapped visual rows.

If viewport width changes:

- wrapping may refresh,
- highlight state stays valid,
- only row-to-segment mapping changes.

### Long-line rule

For very long single lines, the render path MUST avoid rescanning from the beginning of the line on every visible row.

That means:

- visible wrapped segments should come from `TextEditorCore.LayoutCache`,
- visible highlight runs SHOULD be found via binary search / indexed access within a per-line sorted run list,
- no per-scroll whole-line grapheme or token walk is acceptable.

---

## Rendering layers

The editor surface should conceptually render in this order:

1. Background / chrome
2. Current-line background (optional)
3. Plain text
4. Syntax highlight styles
5. Selection
6. Search-result overlays / other inherited editor overlays
7. Margin overlays (handled in their own strips)

### Style merging

Like `PromptEditor`, highlight runs should be additive/merge-friendly.

The system should support combinations such as:

- keyword foreground + current-line background,
- comment foreground + search match background,
- diagnostic underline + selection background.

The exact rendering mechanics can evolve, but the data model should support overlapping style layers.

---

## Current-line treatment

The core control SHOULD support a current-line visualization appropriate for code editing.

At minimum:

- current-line background on the text surface,
- current-line emphasis in the line-number margin.

This SHOULD be theme/style driven, not hard-coded.

---

## Async / background highlighting

Large language packages will likely want to compute tokenization off the UI thread.

The infrastructure SHOULD allow this through an optional async interface such as `IAsyncCodeEditorSyntaxHighlighter`.

Rules:

- UI thread editing always updates the document immediately.
- The editor keeps rendering with the last valid syntax state while a new state is being computed.
- When a new syntax state arrives:
  - if the snapshot version still matches, it is applied;
  - otherwise it is discarded.
- Async highlighting MUST NOT block scrolling or typing.

---

## Margin and highlighting independence

Margins and syntax highlighting are separate extension points.

This separation is important because:

- git diff markers may come from source control state rather than syntax state,
- breakpoints come from debugger state,
- diagnostics may come from analyzers,
- right-side metadata may come from unrelated services.

The editor MUST allow these features to coexist without forcing everything through the syntax highlighter.

---

## Styling requirements

`CodeEditor` SHOULD have a dedicated style record, likely `CodeEditorStyle`, covering at least:

- background,
- text style,
- selection style,
- current-line style,
- margin background,
- line-number style,
- current-line number style,
- margin separators,
- optional guide styles.

Margin-specific implementations may also expose their own style records where needed.

---

## Performance requirements

The following are explicit performance requirements, not optional nice-to-haves.

### Large document scrolling

Scrolling in a very large document MUST:

- render only visible rows,
- avoid full-document line walks,
- avoid full-document highlight recomputation,
- avoid repeated grapheme segmentation for offscreen content.

### Very long wrapped lines

For a single extremely long line:

- wrapping MUST continue using sparse wrapped-row checkpoints and bounded reusable block caches,
- syntax highlighting MUST operate on logical-line runs and visible wrapped segments,
- intersection of highlight runs with a visible segment SHOULD be `O(log n + k)` per visible segment, where `n` is the
  run count for the line and `k` the number of overlapping runs actually rendered.

### Gutter width changes

Line-number width changes SHOULD be rare and bucket-based.

Crossing from 2 digits to 3 digits may trigger a layout refresh because content width changes; this is acceptable.
However, ordinary scrolling within the same digit bucket MUST NOT cause repeated whole-document recomputation.

### Allocation expectations

The render path SHOULD avoid per-frame allocations beyond small temporary buffers for visible rows.

Persistent state should be cached by:

- snapshot version,
- wrap width,
- line index,
- syntax-state version.

---

## Phased implementation plan

### Phase 1 - code-editor shell

- Add `CodeEditor : TextEditorBase`.
- Add `CodeEditorStyle`.
- Add default left line-number margin.
- Add left/right margin collections.
- Keep behavior otherwise close to `TextArea`.

### Phase 2 - simple highlighting parity

- Add a `PromptEditor`-style simple highlighter delegate.
- Support line-relative `StyledRun`s.
- Render visible highlighted text without breaking current text-editor performance work.

### Phase 3 - advanced incremental highlighting

- Add persistent syntax-state abstraction.
- Add diff-aware update contexts.
- Add version-gated async support.
- Add tests proving no full-document re-highlight on scroll.

### Phase 4 - ecosystem package

- Create a separate language/highlighting package using the public contracts defined here.
- Add reusable tokenizers / adapters there, not in the core UI package.

---

## Testing requirements

When implementation begins, add tests for at least:

### Layout / chrome

- line numbers shown by default,
- line numbers render only on first wrapped row,
- line-number width adapts to the visible range,
- left/right margins stay aligned to wrapped rows during scrolling.

### Highlighting correctness

- simple highlighter styles the correct visible text,
- highlight runs intersect correctly with wrapped segments,
- selection and search overlays compose correctly with syntax styles.

### Incremental highlighting behavior

- edit near top of large document does not re-highlight every line,
- provider updates stop once line state stabilizes,
- scrolling does not trigger full rebuilds,
- stale async results are discarded.

### Performance regressions

- deep scrolling in a huge file touches only visible lines plus bounded cache windows,
- very long wrapped line rendering remains bounded,
- line-number width changes only when digit bucket changes.

---

## Summary

`CodeEditor` should be a **core** Terminal.UI control built on the existing text-editor engine, with:

- default line numbers,
- pluggable left/right margins,
- a simple `PromptEditor`-style highlighter hook,
- an advanced incremental syntax-highlighting provider model,
- and a strict requirement to reuse `TextEditorCore.LayoutCache` rather than duplicating wrap logic.

The most important architectural constraint is this:

> syntax highlighting is **line-relative and incremental**; wrapping is **viewport-relative and owned by `TextEditorCore.LayoutCache`**.

That separation is what allows the future `CodeEditor` to support very large documents and very long lines without
regressing the performance work already done in the text subsystem.
