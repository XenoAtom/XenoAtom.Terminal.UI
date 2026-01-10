// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class TabControl : Visual
{
    private readonly List<TabPage> _tabs = new();
    private readonly List<TabHitRange> _hitRanges = new();
    private int _hoveredIndex = -1;
    private int _headerHeight = 1;

    public TabControl()
    {
        Focusable = true;
    }

    public TabControl(params TabPage[] tabs) : this()
    {
        ArgumentNullException.ThrowIfNull(tabs);
        for (var i = 0; i < tabs.Length; i++)
        {
            AddTab(tabs[i]);
        }
    }

    [Bindable]
    public partial int SelectedIndex { get; set; }

    partial void OnSelectedIndexChanging(ref int value)
    {
        value = _tabs.Count == 0 ? 0 : Math.Clamp(value, 0, _tabs.Count - 1);
    }

    partial void OnSelectedIndexChanged(int value)
    {
        _ = value;
        UpdateTabVisibility();
    }

    public IReadOnlyList<TabPage> Tabs => _tabs;

    public void AddTab(string header, Visual content)
    {
        ArgumentException.ThrowIfNullOrEmpty(header);
        AddTab(new TextBlock(header), content);
    }

    public void AddTab(Visual header, Visual content)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(content);

        if (header.Parent is not null)
        {
            throw new InvalidOperationException("A visual that is already in the UI tree cannot be used as a tab header.");
        }

        if (content.Parent is not null)
        {
            throw new InvalidOperationException("A visual that is already in the UI tree cannot be used as a tab content.");
        }

        var index = _tabs.Count;
        var page = new TabPage(header, content);
        _tabs.Add(page);

        AttachChild(header);
        AttachChild(content);

        // Avoid capturing SelectedIndex as an initializer dependency when AddTab is called from an initializer.
        content.IsVisible = index == _selectedIndex;
        if (index == 0)
        {
            UpdateTabVisibility();
        }
    }

    public void AddTab(TabPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        AddTab(page.Header, page.Content);
    }

    protected override int ChildrenCount => _tabs.Count * 2;

    protected override Visual GetChild(int index)
    {
        if ((uint)index >= (uint)ChildrenCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var tabIndex = index / 2;
        var page = _tabs[tabIndex];
        return (index % 2) == 0 ? page.Header : page.Content;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var style = Get<TabControlStyle>();
        var pad = style.TabPadding;
        var showBorder = style.ShowBorder;

        var headerHeight = 1;
        var headerTotalWidth = 0;

        for (var i = 0; i < _tabs.Count; i++)
        {
            var header = _tabs[i].Header;
            header.Measure(new Size(availableSize.Width, availableSize.Height));
            headerHeight = Math.Max(headerHeight, header.DesiredSize.Height);

            var tabWidth = header.DesiredSize.Width + pad.Horizontal;
            headerTotalWidth += tabWidth;
            if (i + 1 < _tabs.Count)
            {
                headerTotalWidth += 1;
            }
        }

        headerHeight = Math.Min(headerHeight, availableSize.Height);

        var contentSlot = new Size(
            Math.Max(0, availableSize.Width - (showBorder ? 2 : 0)),
            Math.Max(0, availableSize.Height - headerHeight - (showBorder ? 2 : 0)));

        var contentWidth = 0;
        var contentHeight = 0;
        for (var i = 0; i < _tabs.Count; i++)
        {
            var content = _tabs[i].Content;
            content.Measure(contentSlot);
            contentWidth = Math.Max(contentWidth, content.DesiredSize.Width);
            contentHeight = Math.Max(contentHeight, content.DesiredSize.Height);
        }

        if (showBorder)
        {
            contentWidth += 2;
            contentHeight += 2;
        }

        var width = Math.Max(headerTotalWidth, contentWidth);
        var height = headerHeight + contentHeight;

        return new Size(Math.Min(availableSize.Width, width), Math.Min(availableSize.Height, height));
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        var style = Get<TabControlStyle>();
        var pad = style.TabPadding;
        var showBorder = style.ShowBorder;

        var headerHeight = 1;
        for (var i = 0; i < _tabs.Count; i++)
        {
            headerHeight = Math.Max(headerHeight, _tabs[i].Header.DesiredSize.Height);
        }

        _headerHeight = Math.Max(1, Math.Min(headerHeight, finalRect.Height));

        _hitRanges.Clear();

        var x0 = finalRect.X;
        for (var i = 0; i < _tabs.Count && x0 < finalRect.Right; i++)
        {
            var header = _tabs[i].Header;
            var headerWidth = header.DesiredSize.Width;

            var tabWidth = Math.Min(finalRect.Right - x0, headerWidth + pad.Horizontal);
            if (tabWidth <= 0)
            {
                break;
            }

            var headerSlot = new Rectangle(
                x0 + pad.Left,
                finalRect.Y,
                Math.Max(0, tabWidth - pad.Horizontal),
                _headerHeight);

            header.Arrange(headerSlot);

            _hitRanges.Add(new TabHitRange(i, x0 - finalRect.X, (x0 - finalRect.X) + tabWidth));
            x0 += tabWidth + 1;
        }

        var contentTop = finalRect.Y + _headerHeight;
        var contentHeight = Math.Max(0, finalRect.Height - _headerHeight);

        var inner = new Rectangle(finalRect.X, contentTop, finalRect.Width, contentHeight);
        if (showBorder)
        {
            inner = new Rectangle(
                inner.X + 1,
                inner.Y + 1,
                Math.Max(0, inner.Width - 2),
                Math.Max(0, inner.Height - 2));
        }

        for (var i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].Content.Arrange(inner);
        }
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = Get<TabControlStyle>();
        var showBorder = style.ShowBorder;

        var headerHeight = Math.Min(Math.Max(1, _headerHeight), rect.Height);
        var stripStyle = style.ResolveStripStyle(theme);

        // Header strip.
        for (var y = rect.Y; y < rect.Y + headerHeight; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), stripStyle);
            }
        }

        for (var i = 0; i < _hitRanges.Count; i++)
        {
            var range = _hitRanges[i];
            if ((uint)range.Index >= (uint)_tabs.Count)
            {
                continue;
            }

            var tab = _tabs[range.Index];
            var selected = range.Index == SelectedIndex;
            var hovered = range.Index == _hoveredIndex;
            var tabStyle = style.ResolveTabStyle(theme, tab.Content.IsEnabled, selected, hovered);

            var xStart = rect.X + range.Start;
            var xEnd = rect.X + range.End;

            for (var y = rect.Y; y < rect.Y + headerHeight; y++)
            {
                for (var x = xStart; x < xEnd && x < rect.X + rect.Width; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), tabStyle);
                }
            }
        }

        if (showBorder && rect.Height >= headerHeight + 2)
        {
            RenderBorder(buffer, rect, headerHeight, theme);
        }
    }

    private static void RenderBorder(CellBuffer buffer, Rectangle rect, int headerHeight, Theme theme)
    {
        var glyphs = theme.Lines;
        var border = theme.BorderStyle(focused: false);

        var left = rect.X;
        var top = rect.Y + headerHeight;
        var right = rect.X + rect.Width - 1;
        var bottom = rect.Y + rect.Height - 1;

        buffer.SetCell(left, top, glyphs.TopLeft, border);
        buffer.SetCell(right, top, glyphs.TopRight, border);
        buffer.SetCell(left, bottom, glyphs.BottomLeft, border);
        buffer.SetCell(right, bottom, glyphs.BottomRight, border);

        for (var x = left + 1; x < right; x++)
        {
            buffer.SetCell(x, top, glyphs.Horizontal, border);
            buffer.SetCell(x, bottom, glyphs.Horizontal, border);
        }

        for (var y = top + 1; y < bottom; y++)
        {
            buffer.SetCell(left, y, glyphs.Vertical, border);
            buffer.SetCell(right, y, glyphs.Vertical, border);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_tabs.Count == 0)
        {
            return;
        }

        switch (e.Key)
        {
            case TerminalKey.Left:
                SelectedIndex = Math.Max(0, SelectedIndex - 1);
                e.Handled = true;
                return;
            case TerminalKey.Right:
                SelectedIndex = Math.Min(_tabs.Count - 1, SelectedIndex + 1);
                e.Handled = true;
                return;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var localX = e.UiX - Bounds.X;
        var localY = e.UiY - Bounds.Y;
        if (localY < 0 || localY >= _headerHeight)
        {
            UpdateHoveredIndex(-1);
            return;
        }

        var index = HitTestTabIndex(localX);
        UpdateHoveredIndex(index);
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var localX = e.UiX - Bounds.X;
        var localY = e.UiY - Bounds.Y;
        if (localY < 0 || localY >= _headerHeight)
        {
            return;
        }

        var index = HitTestTabIndex(localX);
        if (index >= 0)
        {
            SelectedIndex = index;
            e.Handled = true;
        }
    }

    private int HitTestTabIndex(int localX)
    {
        for (var i = 0; i < _hitRanges.Count; i++)
        {
            var range = _hitRanges[i];
            if (localX >= range.Start && localX < range.End)
            {
                return range.Index;
            }
        }

        return -1;
    }

    private void UpdateHoveredIndex(int index)
    {
        if (_hoveredIndex == index)
        {
            return;
        }

        _hoveredIndex = index;
        Invalidate();
    }

    private void UpdateTabVisibility()
    {
        var selected = _selectedIndex;
        for (var i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].Content.IsVisible = i == selected;
        }
    }

    private readonly record struct TabHitRange(int Index, int Start, int End);
}
