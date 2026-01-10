// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class TreeView : Visual
{
    private readonly BindableList<TreeNode> _roots;
    private readonly VisualList<Visual> _headers;

    private int _scrollOffset;
    private readonly List<(TreeNode Node, int Depth)> _visible = new(64);
    private bool _visibleDirty = true;

    public TreeView()
    {
        Focusable = true;
        this.Height(8);

        _headers = new VisualList<Visual>(this, "TreeView.Headers");
        _roots = new BindableList<TreeNode>(
            owner: this,
            name: "TreeView.Roots",
            onAdding: AttachNode,
            onRemoving: DetachNode);
    }

    public BindableList<TreeNode> Roots => _roots;

    [Bindable]
    public partial int SelectedIndex { get; set; }

    [Bindable]
    public partial int Height { get; set; }

    protected override int ChildrenCount => _headers.Count;

    protected override Visual GetChild(int index) => _headers[index];

    internal void AttachNode(TreeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.Header.Parent is not null)
        {
            throw new InvalidOperationException("TreeNode header is already part of a UI tree.");
        }

        _headers.Add(node.Header);
        node.Attach(this);
        _visibleDirty = true;
        Invalidate();
    }

    internal void DetachNode(TreeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        node.Detach(this);
        _headers.Remove(node.Header);
        _visibleDirty = true;
        Invalidate();
    }

    private void EnsureVisibleList()
    {
        if (!_visibleDirty)
        {
            return;
        }

        _visible.Clear();
        for (var i = 0; i < _roots.Count; i++)
        {
            AddVisible(_roots[i], depth: 0);
        }

        _visibleDirty = false;

        // Toggle header visibility based on current visible list.
        var visibleSet = new HashSet<Visual>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < _visible.Count; i++)
        {
            visibleSet.Add(_visible[i].Node.Header);
        }

        for (var i = 0; i < _headers.Count; i++)
        {
            var h = _headers[i];
            h.IsVisible = visibleSet.Contains(h);
        }
    }

    private void AddVisible(TreeNode node, int depth)
    {
        _visible.Add((node, depth));
        if (!node.IsExpanded)
        {
            return;
        }

        for (var i = 0; i < node.Children.Count; i++)
        {
            AddVisible(node.Children[i], depth + 1);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        EnsureVisibleList();

        var style = Get<TreeViewStyle>();
        var showBorder = style.ShowBorder;
        var markerWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.FocusMarkerGlyph));
        var gapAfterIcon = Math.Max(0, style.SpaceBetweenGlyphAndText);

        var maxWidth = 0;
        for (var i = 0; i < _visible.Count; i++)
        {
            var (node, depth) = _visible[i];
            var expander = node.Children.Count > 0 ? (node.IsExpanded ? style.ExpandedGlyph : style.CollapsedGlyph) : new Rune(' ');
            var expanderWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(expander));
            var icon = style.ResolveIcon(node.Icon);
            var iconWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(icon));

            var prefix = depth * style.IndentSize + markerWidth + expanderWidth + 1 + iconWidth + gapAfterIcon;
            node.Header.Measure(new Size(int.MaxValue / 4, 1));
            maxWidth = Math.Max(maxWidth, prefix + node.Header.DesiredSize.Width);
        }

        var width = Math.Min(availableSize.Width, Math.Max(1, maxWidth));
        var desiredHeight = Math.Min(Math.Max(1, Height), availableSize.Height);
        if (showBorder)
        {
            width = Math.Min(availableSize.Width, width + 2);
            desiredHeight = Math.Min(availableSize.Height, desiredHeight + 2);
        }

        return new Size(width, desiredHeight);
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;
        EnsureVisibleList();

        var style = Get<TreeViewStyle>();
        var showBorder = style.ShowBorder;
        var markerWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.FocusMarkerGlyph));
        var gapAfterIcon = Math.Max(0, style.SpaceBetweenGlyphAndText);
        var innerLeft = finalRect.X + (showBorder ? 1 : 0);
        var innerTop = finalRect.Y + (showBorder ? 1 : 0);
        var innerWidth = Math.Max(0, finalRect.Width - (showBorder ? 2 : 0));
        var innerHeight = Math.Max(0, finalRect.Height - (showBorder ? 2 : 0));

        var count = _visible.Count;
        var selected = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));

        if (selected < _scrollOffset)
        {
            _scrollOffset = selected;
        }
        else if (selected >= _scrollOffset + Math.Max(1, innerHeight))
        {
            _scrollOffset = Math.Max(0, selected - Math.Max(1, innerHeight) + 1);
        }

        for (var i = 0; i < count; i++)
        {
            var (node, depth) = _visible[i];
            var expander = node.Children.Count > 0 ? (node.IsExpanded ? style.ExpandedGlyph : style.CollapsedGlyph) : new Rune(' ');
            var expanderWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(expander));
            var icon = style.ResolveIcon(node.Icon);
            var iconWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(icon));

            var prefix = depth * style.IndentSize + markerWidth + expanderWidth + 1 + iconWidth + gapAfterIcon;
            var y = innerTop + (i - _scrollOffset);
            node.Header.Arrange(new Rectangle(innerLeft + prefix, y, Math.Max(0, innerWidth - prefix), 1));
        }
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        EnsureVisibleList();

        var style = Get<TreeViewStyle>();
        var showBorder = style.ShowBorder;
        var innerLeft = rect.X + (showBorder ? 1 : 0);
        var innerTop = rect.Y + (showBorder ? 1 : 0);
        var innerWidth = Math.Max(0, rect.Width - (showBorder ? 2 : 0));
        var innerHeight = Math.Max(0, rect.Height - (showBorder ? 2 : 0));

        var count = _visible.Count;
        var selected = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));

        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();
        var borderStyle = theme.BorderStyle(isFocused);
        var glyphs = theme.Lines;

        // Fill background.
        var background = CellStyle.None;
        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), background);
            }
        }

        if (showBorder && rect.Width >= 2 && rect.Height >= 2)
        {
            var left = rect.X;
            var top = rect.Y;
            var right = rect.X + rect.Width - 1;
            var bottom = rect.Y + rect.Height - 1;

            buffer.SetCell(left, top, glyphs.TopLeft, borderStyle);
            buffer.SetCell(right, top, glyphs.TopRight, borderStyle);
            buffer.SetCell(left, bottom, glyphs.BottomLeft, borderStyle);
            buffer.SetCell(right, bottom, glyphs.BottomRight, borderStyle);

            for (var x = left + 1; x < right; x++)
            {
                buffer.SetCell(x, top, glyphs.Horizontal, borderStyle);
                buffer.SetCell(x, bottom, glyphs.Horizontal, borderStyle);
            }

            for (var y = top + 1; y < bottom; y++)
            {
                buffer.SetCell(left, y, glyphs.Vertical, borderStyle);
                buffer.SetCell(right, y, glyphs.Vertical, borderStyle);
            }
        }

        for (var row = 0; row < innerHeight; row++)
        {
            var index = _scrollOffset + row;
            if ((uint)index >= (uint)count)
            {
                continue;
            }

            var (node, depth) = _visible[index];
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

            var indent = depth * style.IndentSize;
            for (var i = 0; i < indent && xCursor < innerLeft + innerWidth; i++)
            {
                buffer.SetCell(xCursor, y, new Rune(' '), rowStyle);
                xCursor++;
            }

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

            var icon = style.ResolveIcon(node.Icon);
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

    protected override void OnKeyDown(KeyEventArgs e)
    {
        EnsureVisibleList();
        if (_visible.Count == 0)
        {
            return;
        }

        var style = Get<TreeViewStyle>();
        var showBorder = style.ShowBorder;
        var viewportHeight = Math.Max(1, Bounds.Height - (showBorder ? 2 : 0));

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

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        EnsureVisibleList();

        var style = Get<TreeViewStyle>();
        var showBorder = style.ShowBorder;

        var innerY = (e.UiY - Bounds.Y) - (showBorder ? 1 : 0);
        var innerHeight = Math.Max(0, Bounds.Height - (showBorder ? 2 : 0));
        if ((uint)innerY >= (uint)innerHeight)
        {
            return;
        }

        var index = _scrollOffset + innerY;
        if ((uint)index >= (uint)_visible.Count)
        {
            return;
        }

        SelectedIndex = index;

        // Click on the expander glyph toggles expansion.
        var depth = _visible[index].Depth;
        var markerWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.FocusMarkerGlyph));
        var expanderX = (showBorder ? 1 : 0) + markerWidth + depth * style.IndentSize; // marker + indent
        if (e.LocalX == expanderX)
        {
            ToggleExpand(index, expand: null);
        }

        e.Handled = true;
    }

    protected override void OnPointerWheel(PointerEventArgs e)
    {
        EnsureVisibleList();

        if (_visible.Count == 0 || e.WheelDelta == 0)
        {
            return;
        }

        var selected = Math.Clamp(SelectedIndex, 0, _visible.Count - 1);
        SelectedIndex = e.WheelDelta > 0 ? Math.Max(0, selected - 1) : Math.Min(_visible.Count - 1, selected + 1);
        e.Handled = true;
    }

    private void ToggleExpand(int visibleIndex, bool? expand)
    {
        EnsureVisibleList();
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
        _visibleDirty = true;
        Invalidate();
    }
}
