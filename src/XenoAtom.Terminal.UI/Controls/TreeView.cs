// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Scrolling;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a hierarchical tree view with expandable nodes.
/// </summary>
public sealed partial class TreeView : Visual, IScrollable
{
    private readonly BindableList<TreeNode> _roots;
    private readonly VisualList<Visual> _headers;

    private readonly ScrollModel _scroll;
    private readonly List<VisibleRow> _visible = new(64);

    private readonly record struct VisibleRow(TreeNode Node, int Depth, ulong ContinuationMask, bool IsLastSibling);

    /// <summary>
    /// Initializes a new instance of the <see cref="TreeView"/> class.
    /// </summary>
    public TreeView()
    {
        Focusable = true;
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;

        _headers = new VisualList<Visual>(this, "TreeView.Headers");
        _scroll = new ScrollModel();
        _roots = new BindableList<TreeNode>(
            owner: this,
            name: "TreeView.Roots",
            onAdding: AttachNode,
            onRemoving: DetachNode);
    }

    /// <summary>
    /// Gets the collection of root nodes.
    /// </summary>
    [Bindable]
    public BindableList<TreeNode> Roots => _roots;

    /// <summary>
    /// Gets or sets the selected visible node index.
    /// </summary>
    [Bindable]
    public partial int SelectedIndex { get; set; }

    /// <summary>
    /// Gets the scroll model for this tree view.
    /// </summary>
    public ScrollModel Scroll => _scroll;

    /// <inheritdoc />
    protected override int ChildrenCount => _headers.Count;

    /// <inheritdoc />
    protected override Visual GetChild(int index) => _headers[index];

    /// <inheritdoc />
    protected override void PrepareChildren()
    {
        _visible.Clear();
        for (var i = 0; i < _roots.Count; i++)
        {
            AddVisible(_roots[i], depth: 0, continuationMask: 0, isLastSibling: i == _roots.Count - 1);
        }

        // Toggle header visibility based on the current visible list.
        // This avoids allocating temporary sets and keeps tree nodes attached for fast reuse.
        for (var i = 0; i < _headers.Count; i++)
        {
            _headers[i].IsVisible = false;
        }

        for (var i = 0; i < _visible.Count; i++)
        {
            _visible[i].Node.Header.IsVisible = true;
        }
    }

    /// <summary>
    /// Attaches a node and its header to the visual tree.
    /// </summary>
    internal void AttachNode(TreeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.Header.Parent is not null)
        {
            throw new InvalidOperationException("TreeNode header is already part of a UI tree.");
        }

        _headers.Add(node.Header);
        node.Attach(this);
    }

    /// <summary>
    /// Detaches a node and its header from the visual tree.
    /// </summary>
    internal void DetachNode(TreeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        node.Detach(this);
        _headers.Remove(node.Header);
    }

    /// <summary>
    /// Adds a node and its expanded descendants to the visible list.
    /// </summary>
    private void AddVisible(TreeNode node, int depth)
    {
        AddVisible(node, depth, continuationMask: 0, isLastSibling: true);
    }

    /// <summary>
    /// Adds a node and its expanded descendants to the visible list while tracking hierarchy line information.
    /// </summary>
    private void AddVisible(TreeNode node, int depth, ulong continuationMask, bool isLastSibling)
    {
        _visible.Add(new VisibleRow(node, depth, continuationMask, isLastSibling));
        if (!node.IsExpanded)
        {
            return;
        }

        var childContinuationMask = continuationMask;
        if (!isLastSibling && (uint)depth < 64)
        {
            childContinuationMask |= 1ul << depth;
        }

        for (var i = 0; i < node.Children.Count; i++)
        {
            AddVisible(node.Children[i], depth + 1, childContinuationMask, isLastSibling: i == node.Children.Count - 1);
        }
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = GetStyle<TreeViewStyle>();
        var indentSize = style.HierarchyLines is null ? style.IndentSize : Math.Max(2, style.IndentSize);
        var markerWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.FocusMarkerGlyph));
        var gapAfterIcon = Math.Max(0, style.SpaceBetweenGlyphAndText);

        var maxWidth = 0;
        var headerConstraints = new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1);
        for (var i = 0; i < _visible.Count; i++)
        {
            var (node, depth, _, _) = _visible[i];
            var visualDepth = style.HierarchyLines is null ? depth : depth + 1;
            var expander = node.Children.Count > 0 ? (node.IsExpanded ? style.ExpandedGlyph : style.CollapsedGlyph) : new Rune(' ');
            var expanderWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(expander));
            var icon = style.ResolveIcon(node.Data, node.Icon);
            var iconWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(icon));

            var prefix = visualDepth * indentSize + markerWidth + expanderWidth + 1 + iconWidth + gapAfterIcon;
            node.Header.Measure(headerConstraints);
            maxWidth = Math.Max(maxWidth, prefix + node.Header.DesiredSize.Width);
        }

        var width = Math.Max(1, maxWidth);
        var desiredHeight = Math.Max(1, _visible.Count);

        var min = new Size(1, 1);
        var natural = new Size(Math.Max(min.Width, width), Math.Max(min.Height, desiredHeight));
        var max = new Size(LayoutConstants.Infinite, LayoutConstants.Infinite);
        return SizeHints.Flex(min, natural, max, growX: 1, growY: 1, shrinkX: 1, shrinkY: 1);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var style = GetStyle<TreeViewStyle>();
        var indentSize = style.HierarchyLines is null ? style.IndentSize : Math.Max(2, style.IndentSize);
        var markerWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.FocusMarkerGlyph));
        var gapAfterIcon = Math.Max(0, style.SpaceBetweenGlyphAndText);
        var innerLeft = finalRect.X;
        var innerTop = finalRect.Y;
        var innerWidth = Math.Max(0, finalRect.Width);
        var innerHeight = Math.Max(0, finalRect.Height);

        var count = _visible.Count;

        if (innerWidth <= 0 || innerHeight <= 0 || count == 0)
        {
            _scroll.SetViewport(0, 0);
            _scroll.SetExtent(0, 0);
            return;
        }

        var selected = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));

        _scroll.SetViewport(innerWidth, innerHeight);
        _scroll.SetExtent(innerWidth, count);
        EnsureSelectedVisible(selected);

        var maxOffsetY = Math.Max(0, _scroll.ExtentHeight - _scroll.ViewportHeight);
        if (_scroll.OffsetY > maxOffsetY)
        {
            _scroll.SetOffset(_scroll.OffsetX, maxOffsetY);
        }

        var scrollOffset = _scroll.OffsetY;
        for (var i = 0; i < count; i++)
        {
            var (node, depth, _, _) = _visible[i];
            var visualDepth = style.HierarchyLines is null ? depth : depth + 1;
            var expander = node.Children.Count > 0 ? (node.IsExpanded ? style.ExpandedGlyph : style.CollapsedGlyph) : new Rune(' ');
            var expanderWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(expander));
            var icon = style.ResolveIcon(node.Data, node.Icon);
            var iconWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(icon));

            var prefix = visualDepth * indentSize + markerWidth + expanderWidth + 1 + iconWidth + gapAfterIcon;
            var y = innerTop + (i - scrollOffset);
            node.Header.Arrange(new Rectangle(innerLeft + prefix, y, Math.Max(0, innerWidth - prefix), 1));
        }
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var style = GetStyle<TreeViewStyle>();
        var indentSize = style.HierarchyLines is null ? style.IndentSize : Math.Max(2, style.IndentSize);
        var innerLeft = rect.X;
        var innerTop = rect.Y;
        var innerWidth = Math.Max(0, rect.Width);
        var innerHeight = Math.Max(0, rect.Height);

        var count = _visible.Count;
        var selected = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));

        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();

        // Fill background.
        var background = Style.None;
        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), background);
            }
        }

        for (var row = 0; row < innerHeight; row++)
        {
            var index = _scroll.OffsetY + row;
            if ((uint)index >= (uint)count)
            {
                continue;
            }

            var (node, depth, continuationMask, isLastSibling) = _visible[index];
            var isSelected = index == selected;
            var rowStyle = style.ResolveItemStyle(theme, IsEnabled, isSelected, isFocused);

            var y = innerTop + row;
            for (var x = 0; x < innerWidth; x++)
            {
                buffer.SetCell(innerLeft + x, y, new Rune(' '), rowStyle);
            }

            var xCursor = innerLeft;

            var markerWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.FocusMarkerGlyph));
            buffer.SetCell(xCursor, y, isSelected ? style.FocusMarkerGlyph : new Rune(' '), rowStyle);
            xCursor += markerWidth;

            var indentStart = xCursor;
            var visualDepth = style.HierarchyLines is null ? depth : depth + 1;
            var indent = visualDepth * indentSize;

            if (style.HierarchyLines is { } lines && visualDepth > 0 && indentSize >= 2)
            {
                var lineBaseStyle = style.HierarchyLineStyle ?? (theme.BorderStyle(focused: false) | TextStyle.Dim);
                var lineStyle = rowStyle | lineBaseStyle;

                var parentLevel = visualDepth - 1;
                for (var level = 0; level < parentLevel; level++)
                {
                    var x = indentStart + level * indentSize;
                    if (x >= innerLeft + innerWidth)
                    {
                        break;
                    }

                    var hasContinuation = (continuationMask & (1ul << level)) != 0;
                    buffer.SetCell(x, y, hasContinuation ? lines.Vertical : new Rune(' '), lineStyle);
                }

                var branchX = indentStart + parentLevel * indentSize;
                if (branchX < innerLeft + innerWidth)
                {
                    buffer.SetCell(branchX, y, isLastSibling ? lines.BottomLeft : lines.TeeLeft, lineStyle);
                }

                var branchHorizontalX = branchX + 1;
                if (branchHorizontalX < innerLeft + innerWidth)
                {
                    buffer.SetCell(branchHorizontalX, y, lines.Horizontal, lineStyle);
                }
            }

            var indentAdvance = Math.Max(0, Math.Min(indent, innerLeft + innerWidth - xCursor));
            xCursor += indentAdvance;

            var expander = node.Children.Count > 0 ? (node.IsExpanded ? style.ExpandedGlyph : style.CollapsedGlyph) : new Rune(' ');
            if (xCursor < innerLeft + innerWidth)
            {
                buffer.SetCell(xCursor, y, expander, rowStyle);
            }
            xCursor += Math.Max(1, TerminalTextUtility.GetRuneWidth(expander));

            if (xCursor < innerLeft + innerWidth)
            {
                buffer.SetCell(xCursor, y, new Rune(' '), rowStyle);
            }
            xCursor++;

            var icon = style.ResolveIcon(node.Data, node.Icon);
            if (xCursor < innerLeft + innerWidth)
            {
                buffer.SetCell(xCursor, y, icon, rowStyle);
            }
            xCursor += Math.Max(1, TerminalTextUtility.GetRuneWidth(icon));

            var gapAfterIcon = Math.Max(0, style.SpaceBetweenGlyphAndText);
            for (var i = 0; i < gapAfterIcon && xCursor < innerLeft + innerWidth; i++)
            {
                buffer.SetCell(xCursor, y, new Rune(' '), rowStyle);
                xCursor++;
            }
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_visible.Count == 0)
        {
            return;
        }

        var style = GetStyle<TreeViewStyle>();
        var viewportHeight = Math.Max(1, Bounds.Height);

        var count = _visible.Count;
        var selected = Math.Clamp(SelectedIndex, 0, count - 1);

        switch (e.Key)
        {
            case TerminalKey.Up:
                SelectedIndex = Math.Max(0, selected - 1);
                e.Handled = true;
                return;
            case TerminalKey.Down:
                SelectedIndex = Math.Min(count - 1, selected + 1);
                e.Handled = true;
                return;
            case TerminalKey.Home:
                SelectedIndex = 0;
                e.Handled = true;
                return;
            case TerminalKey.End:
                SelectedIndex = count - 1;
                e.Handled = true;
                return;
            case TerminalKey.PageUp:
                SelectedIndex = Math.Max(0, selected - viewportHeight);
                e.Handled = true;
                return;
            case TerminalKey.PageDown:
                SelectedIndex = Math.Min(count - 1, selected + viewportHeight);
                e.Handled = true;
                return;
            case TerminalKey.Left:
                ToggleExpand(selected, expand: false);
                e.Handled = true;
                return;
            case TerminalKey.Right:
                ToggleExpand(selected, expand: true);
                e.Handled = true;
                return;
            case TerminalKey.Enter:
            case TerminalKey.Space:
                ToggleExpand(selected, expand: null);
                e.Handled = true;
                return;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var style = GetStyle<TreeViewStyle>();

        var innerY = e.UiY - Bounds.Y;
        var innerHeight = Math.Max(0, Bounds.Height);
        if ((uint)innerY >= (uint)innerHeight)
        {
            return;
        }

        var index = _scroll.OffsetY + innerY;
        if ((uint)index >= (uint)_visible.Count)
        {
            return;
        }

        SelectedIndex = index;

        // Click on the expander glyph toggles expansion.
        var indentSize = style.HierarchyLines is null ? style.IndentSize : Math.Max(2, style.IndentSize);
        var depth = _visible[index].Depth;
        var markerWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.FocusMarkerGlyph));
        var visualDepth = style.HierarchyLines is null ? depth : depth + 1;
        var expanderX = markerWidth + visualDepth * indentSize; // marker + indent
        if (e.LocalX == expanderX)
        {
            ToggleExpand(index, expand: null);
        }

        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerWheel(PointerEventArgs e)
    {
        if (_visible.Count == 0 || e.WheelDelta == 0)
        {
            return;
        }

        var selected = Math.Clamp(SelectedIndex, 0, _visible.Count - 1);
        SelectedIndex = e.WheelDelta > 0 ? Math.Max(0, selected - 1) : Math.Min(_visible.Count - 1, selected + 1);
        e.Handled = true;
    }

    private void EnsureSelectedVisible(int selectedIndex)
    {
        var count = _visible.Count;
        if (count == 0 || selectedIndex < 0)
        {
            return;
        }

        var viewportHeight = Math.Max(1, _scroll.ViewportHeight);
        var offsetY = _scroll.OffsetY;

        if (selectedIndex < offsetY)
        {
            _scroll.SetOffset(_scroll.OffsetX, selectedIndex);
        }
        else if (selectedIndex >= offsetY + viewportHeight)
        {
            _scroll.SetOffset(_scroll.OffsetX, selectedIndex - viewportHeight + 1);
        }
    }

    /// <summary>
    /// Toggles expansion for a visible node index.
    /// </summary>
    private void ToggleExpand(int visibleIndex, bool? expand)
    {
        if ((uint)visibleIndex >= (uint)_visible.Count)
        {
            return;
        }

        var node = _visible[visibleIndex].Node;
        if (node.Children.Count == 0)
        {
            return;
        }

        var newValue = expand ?? !node.IsExpanded;
        if (node.IsExpanded == newValue)
        {
            return;
        }

        node.IsExpanded = newValue;
    }
}
