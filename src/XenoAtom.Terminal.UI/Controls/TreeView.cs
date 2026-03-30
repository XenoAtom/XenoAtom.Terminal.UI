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
    private readonly VisualList<Visual> _rightVisuals;

    private readonly ScrollModel _scroll;
    private readonly List<VisibleRow> _visible = new(64);
    private bool _ensureSelectedVisible;
    private TreeNode? _selectedNodeCache;

    private readonly record struct VisibleRow(TreeNode Node, int Depth, ulong ContinuationMask, bool IsLastSibling);

    /// <summary>
    /// Initializes a new instance of the <see cref="TreeView"/> class.
    /// </summary>
    public TreeView()
    {
        Focusable = true;
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;
        HoveredIndex = -1;

        _headers = new VisualList<Visual>(this, "TreeView.Headers");
        _rightVisuals = new VisualList<Visual>(this, "TreeView.RightVisuals");
        _scroll = new ScrollModel(this);
        _roots = new BindableList<TreeNode>(
            owner: this,
            name: "TreeView.Roots",
            onAdding: AttachNode,
            onRemoving: DetachNode);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TreeView"/> class with root nodes.
    /// </summary>
    /// <param name="roots">The root nodes.</param>
    public TreeView(IEnumerable<TreeNode> roots) : this()
    {
        ArgumentNullException.ThrowIfNull(roots);
        foreach (var root in roots)
        {
            Roots.Add(root);
        }
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
    /// Gets the currently selected visible node.
    /// </summary>
    [Bindable]
    public partial TreeNode? SelectedNode { get; private set; }

    partial void OnSelectedIndexChanging(ref int value)
    {
        var count = _visible.Count;
        value = count == 0 ? -1 : Math.Clamp(value, 0, count - 1);
    }

    partial void OnSelectedIndexChanged(int value)
    {
        UpdateSelectedNodeFromSelectedIndex(value);
        _ensureSelectedVisible = true;

        // If we already have a viewport (i.e. the control was arranged at least once), update the scroll model
        // immediately so parents like ScrollViewer can update scroll bars during the same frame.
        if (_scroll.ViewportHeight > 0)
        {
            EnsureSelectedVisible(value);
            _ensureSelectedVisible = false;
        }
    }

    /// <summary>
    /// Gets the scroll model for this tree view.
    /// </summary>
    public ScrollModel Scroll => _scroll;

    [Bindable]
    private partial int ScrollVersion { get; set; }

    [Bindable]
    private partial int MeasuredContentWidth { get; set; }

    /// <inheritdoc />
    protected override int ChildrenCount => _headers.Count + _rightVisuals.Count;

    /// <inheritdoc />
    protected override Visual GetChild(int index) => index < _headers.Count ? _headers[index] : _rightVisuals[index - _headers.Count];

    [Bindable]
    internal partial int HoveredIndex { get; set; }

    /// <inheritdoc />
    protected override void PrepareChildren()
    {
        ScrollVersion = _scroll.Version;
        if (!IsHovered)
        {
            HoveredIndex = -1;
        }

        var previouslySelectedNode = _selectedNodeCache;

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

        for (var i = 0; i < _rightVisuals.Count; i++)
        {
            _rightVisuals[i].IsVisible = false;
        }

        for (var i = 0; i < _visible.Count; i++)
        {
            var node = _visible[i].Node;
            node.Header.IsVisible = true;
            for (var j = 0; j < node.RightVisuals.Count; j++)
            {
                node.RightVisuals[j].Visual.IsVisible = true;
            }
        }

        SynchronizeSelection(previouslySelectedNode);
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

    internal void AttachNodeRightVisual(TreeNodeRightVisual rightVisual)
    {
        ArgumentNullException.ThrowIfNull(rightVisual);
        _rightVisuals.Add(rightVisual.Visual);
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

    internal void DetachNodeRightVisual(TreeNodeRightVisual rightVisual)
    {
        ArgumentNullException.ThrowIfNull(rightVisual);
        _rightVisuals.Remove(rightVisual.Visual);
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
        var maxWidth = 0;
        var headerConstraints = new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1);
        for (var i = 0; i < _visible.Count; i++)
        {
            var (node, depth, _, _) = _visible[i];
            var prefix = GetPrefixWidth(style, node, depth);
            var rightVisualWidth = MeasureRightVisuals(node, headerConstraints);
            node.Header.Measure(headerConstraints);
            maxWidth = Math.Max(maxWidth, prefix + node.Header.DesiredSize.Width + rightVisualWidth);
        }

        var width = Math.Max(1, maxWidth);
        MeasuredContentWidth = width;
        var desiredHeight = Math.Max(1, _visible.Count);

        var scrollBarThickness = Math.Max(1, GetStyle<ScrollViewerStyle>().ScrollBarThickness);
        var reserveVerticalBar = constraints.IsHeightBounded && desiredHeight > constraints.MaxHeight;
        var reservedWidth = width + (reserveVerticalBar ? scrollBarThickness : 0);

        var min = new Size(1, 1);
        var natural = new Size(Math.Max(min.Width, reservedWidth), Math.Max(min.Height, desiredHeight));
        var max = new Size(LayoutConstants.Infinite, LayoutConstants.Infinite);
        return SizeHints.Flex(min, natural, max, growX: 1, growY: 1, shrinkX: 1, shrinkY: 1);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        _ = ScrollVersion;

        var style = GetStyle<TreeViewStyle>();
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
        var extentWidth = Math.Max(innerWidth, Math.Max(0, MeasuredContentWidth));

        _scroll.SetViewport(innerWidth, innerHeight);
        _scroll.SetExtent(extentWidth, count);

        if (_ensureSelectedVisible)
        {
            EnsureSelectedVisible(selected);
            _ensureSelectedVisible = false;
        }

        var maxOffsetY = Math.Max(0, _scroll.ExtentHeight - _scroll.ViewportHeight);
        if (_scroll.OffsetY > maxOffsetY)
        {
            _scroll.SetOffset(_scroll.OffsetX, maxOffsetY);
        }

        var scrollOffset = _scroll.OffsetY;
        var offsetX = _scroll.OffsetX;
        for (var i = 0; i < count; i++)
        {
            var (node, depth, _, _) = _visible[i];
            var prefix = GetPrefixWidth(style, node, depth);
            var isRowHovered = i == HoveredIndex;
            var rightVisualWidth = GetActiveRightVisualWidth(node, isRowHovered);
            var y = innerTop + (i - scrollOffset);
            node.Header.Arrange(new Rectangle(innerLeft + prefix - offsetX, y, Math.Max(0, extentWidth - prefix - rightVisualWidth), 1));
            ArrangeRightVisuals(node, innerLeft, innerWidth, y, isRowHovered);
        }
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;

        _ = ScrollVersion;

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

        var isFocused = HasFocus;
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

            var xCursor = innerLeft - _scroll.OffsetX;

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
                buffer.SetCell(xCursor, y, icon, style.ResolveIconStyle(rowStyle, node.IconStyle));
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

        if (e.RoutingPhase != RoutingPhase.Bubble)
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
        var contentX = e.LocalX + _scroll.OffsetX;
        if (contentX == expanderX)
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

        // Scroll the viewport without changing selection.
        _scroll.ScrollBy(0, e.WheelDelta > 0 ? -1 : 1);
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

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!Bounds.Contains(e.UiX, e.UiY))
        {
            HoveredIndex = -1;
            return;
        }

        var innerY = e.UiY - Bounds.Y;
        var index = _scroll.OffsetY + innerY;
        HoveredIndex = (uint)index < (uint)_visible.Count ? index : -1;
    }

    /// <summary>
    /// Attempts to select a currently visible node.
    /// </summary>
    /// <param name="node">The node to select.</param>
    /// <returns><see langword="true"/> if the node is currently visible and was selected; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/>.</exception>
    public bool TrySelectNode(TreeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var visibleIndex = FindVisibleIndex(node);
        if (visibleIndex < 0)
        {
            return false;
        }

        SelectedIndex = visibleIndex;
        return true;
    }

    /// <summary>
    /// Gets the visible-row index for a node.
    /// </summary>
    /// <param name="node">The node to locate.</param>
    /// <returns>The visible-row index, or <c>-1</c> when the node is not currently visible.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/>.</exception>
    public int IndexOfVisibleNode(TreeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return FindVisibleIndex(node);
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

    private void SynchronizeSelection(TreeNode? preferredNode)
    {
        if (_visible.Count == 0)
        {
            _selectedNodeCache = null;
            SelectedIndex = -1;
            SelectedNode = null;
            return;
        }

        var selectedIndex = ResolveSelectionIndex(preferredNode);
        SelectedIndex = selectedIndex;
        UpdateSelectedNodeFromSelectedIndex(selectedIndex);
    }

    private int ResolveSelectionIndex(TreeNode? preferredNode)
    {
        var candidate = preferredNode;
        while (candidate is not null)
        {
            var visibleIndex = FindVisibleIndex(candidate);
            if (visibleIndex >= 0)
            {
                return visibleIndex;
            }

            candidate = candidate.Parent;
        }

        // Use the generated backing field directly so PrepareChildren can reconcile selection
        // without recording a read dependency on SelectedIndex before writing it back.
        return Math.Clamp(_selectedIndex, 0, _visible.Count - 1);
    }

    private void UpdateSelectedNodeFromSelectedIndex(int selectedIndex)
    {
        var node = (uint)selectedIndex < (uint)_visible.Count ? _visible[selectedIndex].Node : null;
        _selectedNodeCache = node;
        SelectedNode = node;
    }

    private int FindVisibleIndex(TreeNode node)
    {
        for (var i = 0; i < _visible.Count; i++)
        {
            if (ReferenceEquals(_visible[i].Node, node))
            {
                return i;
            }
        }

        return -1;
    }

    private static int GetIndentSize(TreeViewStyle style) => style.HierarchyLines is null ? style.IndentSize : Math.Max(2, style.IndentSize);

    private static int GetMarkerWidth(TreeViewStyle style) => Math.Max(1, TerminalTextUtility.GetRuneWidth(style.FocusMarkerGlyph));

    private static int GetExpanderWidth(TreeViewStyle style, TreeNode node)
    {
        var expander = node.Children.Count > 0 ? (node.IsExpanded ? style.ExpandedGlyph : style.CollapsedGlyph) : new Rune(' ');
        return Math.Max(1, TerminalTextUtility.GetRuneWidth(expander));
    }

    private static int GetIconWidth(TreeViewStyle style, TreeNode node)
    {
        var icon = style.ResolveIcon(node.Data, node.Icon);
        return Math.Max(1, TerminalTextUtility.GetRuneWidth(icon));
    }

    private static int GetPrefixWidth(TreeViewStyle style, TreeNode node, int depth)
    {
        var visualDepth = style.HierarchyLines is null ? depth : depth + 1;
        var gapAfterIcon = Math.Max(0, style.SpaceBetweenGlyphAndText);
        return visualDepth * GetIndentSize(style) + GetMarkerWidth(style) + GetExpanderWidth(style, node) + 1 + GetIconWidth(style, node) + gapAfterIcon;
    }

    private static bool IsRightVisualVisible(TreeNodeRightVisual rightVisual, bool isRowHovered)
        => rightVisual.Visibility == TreeNodeRightVisualVisibility.Always || (isRowHovered && rightVisual.Visibility == TreeNodeRightVisualVisibility.Hover);

    private static int MeasureRightVisuals(TreeNode node, in LayoutConstraints constraints)
    {
        var totalWidth = 0;
        for (var i = 0; i < node.RightVisuals.Count; i++)
        {
            var visual = node.RightVisuals[i].Visual;
            visual.Measure(constraints);
            totalWidth += visual.DesiredSize.Width;
        }

        return totalWidth;
    }

    private static int GetActiveRightVisualWidth(TreeNode node, bool isRowHovered)
    {
        var totalWidth = 0;
        for (var i = 0; i < node.RightVisuals.Count; i++)
        {
            var rightVisual = node.RightVisuals[i];
            if (IsRightVisualVisible(rightVisual, isRowHovered))
            {
                totalWidth += rightVisual.Visual.DesiredSize.Width;
            }
        }

        return totalWidth;
    }

    private static void ArrangeRightVisuals(TreeNode node, int innerLeft, int innerWidth, int y, bool isRowHovered)
    {
        if (innerWidth <= 0 || node.RightVisuals.Count == 0)
        {
            return;
        }

        var totalWidth = GetActiveRightVisualWidth(node, isRowHovered);
        var collapsedX = innerLeft + innerWidth;

        var x = collapsedX - totalWidth;
        ArrangeRightVisualGroup(node, TreeNodeRightVisualVisibility.Hover, isRowHovered, y, ref x);
        ArrangeRightVisualGroup(node, TreeNodeRightVisualVisibility.Always, isRowHovered, y, ref x);
        CollapseInactiveRightVisuals(node, collapsedX, y, isRowHovered);
    }

    private static void ArrangeRightVisualGroup(TreeNode node, TreeNodeRightVisualVisibility visibility, bool isRowHovered, int y, ref int x)
    {
        for (var i = 0; i < node.RightVisuals.Count; i++)
        {
            var rightVisual = node.RightVisuals[i];
            if (rightVisual.Visibility != visibility || !IsRightVisualVisible(rightVisual, isRowHovered))
            {
                continue;
            }

            var visual = rightVisual.Visual;
            var width = Math.Max(0, visual.DesiredSize.Width);
            visual.Arrange(new Rectangle(x, y, width, 1));
            x += width;
        }
    }

    private static void CollapseInactiveRightVisuals(TreeNode node, int x, int y, bool isRowHovered)
    {
        for (var i = 0; i < node.RightVisuals.Count; i++)
        {
            var rightVisual = node.RightVisuals[i];
            if (IsRightVisualVisible(rightVisual, isRowHovered))
            {
                continue;
            }

            rightVisual.Visual.Arrange(new Rectangle(x, y, 0, 1));
        }
    }
}
