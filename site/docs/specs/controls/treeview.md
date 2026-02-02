---
title: TreeView Specs
---

# TreeView Specs

This document captures design and implementation notes for `TreeView` and `TreeNode`.

> [!NOTE]
> For end-user usage and examples, see [TreeView](../../controls/treeview.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Display hierarchical nodes with selection and expand/collapse interaction.
- **Key goals**:
  - composable node headers (`Visual`)
  - expandable/collapsible nodes with a visible expander glyph
  - selection as a visible-row index (`SelectedIndex`)
  - keyboard navigation and mouse selection
  - scroll integration via `IScrollable` + `ScrollModel` (vertical and horizontal offsets)
  - optional hierarchy guide lines (tree connectors)
- **Non-goals**:
  - built-in data model or reflection-based tree construction (users build `TreeNode` structures)
  - full virtualization (TreeView tracks all expanded nodes; offscreen nodes are still part of the render list)
  - per-node enabled/disabled state in v1 (enable/disable is at the control level)

## Implementation notes

- Primary implementation:
  - `src/XenoAtom.Terminal.UI/Controls/TreeView.cs`
  - `src/XenoAtom.Terminal.UI/Controls/TreeNode.cs`
  - `src/XenoAtom.Terminal.UI/Styling/TreeViewStyle.cs`
- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/TreeViewTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/TreeViewScrollViewerTests.cs`
- Demo:
  - `samples/ControlsDemo/Demos/TreeViewDemo.cs`

## Public API surface

### TreeView

- `TreeView : Visual, IScrollable` (sealed)

Layout defaults:
- `Focusable = true`
- `HorizontalAlignment = Align.Stretch`
- `VerticalAlignment = Align.Stretch`

Properties:
- `Roots : BindableList<TreeNode>` (bindable getter)
  - Owned by the TreeView and uses `onAdding`/`onRemoving` hooks to attach/detach nodes.
- `SelectedIndex : int` (bindable)
  - Index into the current "visible row" list.
  - Coerced to `-1` when the tree has no visible rows, otherwise clamped to `[0 .. VisibleCount - 1]`.
- `Scroll : ScrollModel` (non-bindable property from `IScrollable`)
  - Exposes scroll state for `ScrollViewer` integration.

### TreeNode

- `TreeNode : IVisualElement` (sealed)

Properties:
- `Header : Visual` (required)
- `Children : BindableList<TreeNode>` (bindable getter)
  - When the node is attached to a TreeView, adding/removing children automatically attaches/detaches their headers.
- `Parent : TreeNode?` (set automatically when added as a child)
- `IsExpanded : bool` (bindable)
- `Icon : Rune?` (bindable)
  - Optional node-provided icon. The final icon is resolved by `TreeViewStyle.ResolveIcon(...)`.
- `Data : object?` (bindable)
  - Optional data context used by `TreeViewStyle.IconResolver`.

Notes:
- TreeNode is an `IVisualElement` because it owns a `BindableList<TreeNode>`, but it is not itself a `Visual`. The
  renderable surface is the `Header` visual.

## Visible rows and child attachment model

TreeView maintains:
- `_headers`: a `VisualList<Visual>` containing the header visuals for all nodes currently in the tree
- `_visible`: a list of visible rows derived from expanded nodes

Node headers are attached once and reused:
- when a node is added (directly or via child list changes), its header is attached to the TreeView visual tree
- when a node is removed, its header is detached

The visible row list is recomputed during `PrepareChildren`:
- it traverses `Roots` recursively and includes children only when `IsExpanded` is true
- it also computes hierarchy line metadata (continuation mask and last-sibling flag)

To avoid allocating temporary sets when toggling visibility, `PrepareChildren`:
- sets all headers `IsVisible = false`
- then sets `IsVisible = true` for headers that appear in `_visible`

Important: this is not viewport virtualization. All expanded nodes are part of `_visible`, even if scrolled out of
view. Viewport virtualization could be a future improvement.

## Scrolling integration

TreeView exposes a `ScrollModel` via `IScrollable.Scroll`.

TreeView updates scroll state during arrange:
- `Scroll.SetViewport(viewportWidth, viewportHeight)`
- `Scroll.SetExtent(extentWidth, extentHeight)`

Offsets:
- `Scroll.OffsetY` selects which visible row is rendered at the top.
- `Scroll.OffsetX` is used to horizontally shift the row chrome and headers.

Selection and scroll:
- when `SelectedIndex` changes, TreeView schedules "ensure selected visible"
- if a viewport is already known (`Scroll.ViewportHeight > 0`), it updates the scroll offset immediately
- otherwise it performs the ensure-visible step during the next arrange

Integration with ScrollViewer:
- ScrollViewer can drive the offsets by setting its own offsets; TreeView reflects those through its scroll model.
- TreeView handles mouse wheel events itself (scroll by 1 row), and this works when hosted in a ScrollViewer because the
  ScrollViewer reads offsets from the scroll model.

### Binding model loop avoidance (ScrollVersion)

`ScrollModel.Version` is a bindable property that changes when viewport/extent/offset change.

TreeView both:
- reads scroll state during layout/render
- and writes scroll state during arrange (SetViewport/SetExtent/SetOffset)

To avoid "read then write the same bindable in one tracking context" errors, TreeView uses the standard pattern:
- in `PrepareChildren`, copy `Scroll.Version` into a private bindable `ScrollVersion`
- in `ArrangeCore` and `RenderOverride`, read `ScrollVersion` (not `Scroll.Version`)

This decouples the binding dependency read from the writes performed during arrange.

TreeView also stores `MeasuredContentWidth` as a private bindable so arrange can re-run when content width changes.

## Layout and rendering

### Measure

TreeView measures based on the visible row list and header desired widths:
- each visible node header is measured with infinite max width and height 1
- the control computes a "prefix width" per row:
  - selection marker glyph width (`FocusMarkerGlyph`)
  - indentation (depends on depth and whether hierarchy lines are enabled)
  - expander glyph width (or a space when the node has no children)
  - a fixed 1-cell gap
  - icon glyph width
  - `SpaceBetweenGlyphAndText` gap
- the max row width is `prefix + header.DesiredSize.Width`

The measured content width is stored in `MeasuredContentWidth`.

Height:
- desired height is `max(1, VisibleCount)` (one row per visible node)

Vertical scrollbar width reservation:
- if the height is bounded and the desired height exceeds the available height, TreeView adds a "reserved width" equal
  to `ScrollViewerStyle.ScrollBarThickness`.
- This is intended to reduce layout shifts when a TreeView is hosted in a ScrollViewer that will display a vertical
  scrollbar.

Measure returns flexible hints (grow/shrink enabled) so it can be used as a resizable panel.

### Arrange

Arrange:
- sets the scroll viewport to the arranged size
- sets the scroll extent to:
  - height: visible row count
  - width: max(arranged width, MeasuredContentWidth)
- clamps offsets to valid range
- applies ensure-selected-visible if needed

Each visible header is arranged to a 1-row rectangle at:
- `y = Bounds.Y + (rowIndex - Scroll.OffsetY)`
- `x = Bounds.X + prefixWidth - Scroll.OffsetX`

### Render

TreeView renders the row chrome (not the header content):
- fills the whole bounds with spaces (Style.None baseline)
- then for each viewport row:
  - fills the entire row width with the resolved row style
  - draws the selection marker glyph in the first cell
  - optionally draws hierarchy guide lines using `HierarchyLines` (when enabled and indent size >= 2)
  - draws the expander glyph and icon glyph
  - leaves remaining row cells for the header visuals to render (children render after the parent)

Hierarchy line rendering uses a continuation mask to decide which ancestor levels draw a vertical connector.

## Input behavior

TreeView does not register commands yet; interactions are implemented directly in key/pointer handlers.

### Keyboard

- Navigation: Up/Down, Home/End, PageUp/PageDown
- Expansion:
  - Left: collapse selected node (if it has children)
  - Right: expand selected node (if it has children)
  - Enter/Space: toggle expand/collapse (if it has children)

There is no type-to-search in v1.

### Mouse

- Left click selects the clicked row.
- Clicking on the expander glyph cell toggles expand/collapse.
  - The expander hit test is based on the computed prefix position and includes horizontal scrolling (`OffsetX`).

### Wheel

Mouse wheel scrolls the tree by 1 row without changing selection.

## Styling

### TreeViewStyle

TreeViewStyle controls:
- indentation and spacing:
  - `IndentSize`
  - `SpaceBetweenGlyphAndText`
- hierarchy lines:
  - `HierarchyLines : LineGlyphs?` (default: Single; null disables)
  - `HierarchyLineStyle : Style?` (default: theme border style with dim)
  - convenience variants: `NoLines`, `HeavyLines`, `DoubleLines`
- glyphs:
  - `ExpandedGlyph`, `CollapsedGlyph` (non-ASCII defaults; override if needed)
  - `FocusMarkerGlyph` (non-ASCII default; this is the selection marker)
- icons:
  - `IconResolver : Func<object?, Rune?, Rune>?`
  - node icon fallback uses `TreeNodeIcons.DocumentGlyph`
    - defaults are emoji glyphs (e.g. folder/file/document); see `TreeNodeIcons` for the code points
- row styles:
  - `Item`, `SelectedFocused`, `SelectedUnfocused`, `Disabled`
  - `ResolveItemStyle(theme, enabled, selected, focused)` provides theme-driven defaults when overrides are not set.

## Tests and demos

- `TreeViewTests` cover:
  - expanding nodes and showing children
  - hierarchy line rendering (default and disabled)
  - root sibling connector rendering
  - scrolling to keep selection visible when hosted in a ScrollViewer
- `TreeViewScrollViewerTests` cover:
  - ScrollViewer driving the tree scroll offsets
  - mouse wheel scrolling
  - horizontal extent reporting for long content (horizontal scrolling)
- ControlsDemo shows:
  - style variants for hierarchy lines
  - a larger tree hosted in a ScrollViewer

## Future / v2 ideas

- Viewport virtualization (only arrange/measure headers in the viewport plus overscan).
- Type-to-search or incremental search on node labels.
- Per-node enabled state and callbacks (activate node, selection changed events).
- Lazy child loading (async-friendly) as a separate model layer or helper API.
