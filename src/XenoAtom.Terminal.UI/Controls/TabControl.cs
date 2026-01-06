// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed class TabControl : Visuals.Visual
{
    private readonly List<TabPage> _tabs = new();
    private readonly List<TabHitRange> _hitRanges = new();
    private int _selectedIndex;
    private int _hoveredIndex = -1;

    public TabControl()
    {
        Focusable = true;
        ShowBorder = true;
    }

    public bool ShowBorder { get; set; }

    public int SelectedIndex
    {
        get
        {
            BindingManager.Current.RegisterRead(this, nameof(SelectedIndex));
            return _selectedIndex;
        }
        set
        {
            var clamped = _tabs.Count == 0 ? 0 : Math.Clamp(value, 0, _tabs.Count - 1);
            if (_selectedIndex == clamped)
            {
                return;
            }

            _selectedIndex = clamped;
            BindingManager.Current.NotifyValueChanged(this, nameof(SelectedIndex));
            UpdateTabVisibility();
            App?.RequestRender();
        }
    }

    public IReadOnlyList<TabPage> Tabs => _tabs;

    public void AddTab(string header, Visuals.Visual content)
    {
        ArgumentException.ThrowIfNullOrEmpty(header);
        ArgumentNullException.ThrowIfNull(content);

        if (content.Parent is not null)
        {
            throw new InvalidOperationException("A visual that is already in the UI tree cannot be used as a tab content.");
        }

        var index = _tabs.Count;
        _tabs.Add(new TabPage(header, content));
        AttachChild(content);

        content.IsVisible = index == SelectedIndex;
        if (index == 0)
        {
            UpdateTabVisibility();
        }

        App?.RequestRender();
    }

    protected override int ChildrenCount => _tabs.Count;

    protected override Visuals.Visual GetChild(int index) => _tabs[index].Content;

    protected override Size MeasureOverride(Size availableSize)
    {
        var headerHeight = 1;
        var contentSlot = new Size(
            Math.Max(0, availableSize.Width - (ShowBorder ? 2 : 0)),
            Math.Max(0, availableSize.Height - headerHeight - (ShowBorder ? 2 : 0)));

        var width = 0;
        var height = 0;
        foreach (var tab in _tabs)
        {
            tab.Content.Measure(contentSlot);
            width = Math.Max(width, tab.Content.DesiredSize.Width);
            height = Math.Max(height, tab.Content.DesiredSize.Height);
        }

        width += ShowBorder ? 2 : 0;
        height += headerHeight + (ShowBorder ? 2 : 0);

        return new Size(Math.Min(availableSize.Width, width), Math.Min(availableSize.Height, height));
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        var headerHeight = Math.Min(1, finalRect.Height);
        var contentTop = finalRect.Y + headerHeight;
        var contentHeight = Math.Max(0, finalRect.Height - headerHeight);

        var inner = new Rectangle(finalRect.X, contentTop, finalRect.Width, contentHeight);
        if (ShowBorder)
        {
            inner = new Rectangle(
                inner.X + 1,
                inner.Y + 1,
                Math.Max(0, inner.Width - 2),
                Math.Max(0, inner.Height - 2));
        }

        foreach (var tab in _tabs)
        {
            tab.Content.Arrange(inner);
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
        var style = GetEnvironmentValue(TabControlStyle.Key);

        var stripStyle = style.ResolveStripStyle(theme);

        // Header strip.
        for (var x = rect.X; x < rect.X + rect.Width; x++)
        {
            buffer.SetCell(x, rect.Y, new Rune(' '), stripStyle);
        }

        _hitRanges.Clear();

        var x0 = rect.X;
        for (var i = 0; i < _tabs.Count && x0 < rect.X + rect.Width; i++)
        {
            var tab = _tabs[i];
            var selected = i == SelectedIndex;
            var hovered = i == _hoveredIndex;
            var tabStyle = style.ResolveTabStyle(theme, tab.Content.IsEnabled, selected, hovered);

            var header = tab.Header.AsSpan();
            var headerCells = TerminalTextUtility.GetWidth(header);
            var pad = style.TabPadding;
            var tabWidth = Math.Min(rect.X + rect.Width - x0, headerCells + pad.Horizontal);
            if (tabWidth <= 0)
            {
                break;
            }

            for (var x = 0; x < tabWidth; x++)
            {
                buffer.SetCell(x0 + x, rect.Y, new Rune(' '), tabStyle);
            }

            var textX = x0 + pad.Left;
            var availableText = Math.Max(0, tabWidth - pad.Horizontal);
            if (availableText > 0)
            {
                if (TerminalTextUtility.TryGetIndexAtCell(header, availableText, out var endIndex))
                {
                    header = header[..endIndex];
                }

                buffer.WriteText(textX, rect.Y, header, tabStyle);
            }

            _hitRanges.Add(new TabHitRange(i, x0 - rect.X, (x0 - rect.X) + tabWidth));
            x0 += tabWidth + 1;
        }

        if (ShowBorder && rect.Height >= 3)
        {
            RenderBorder(buffer, rect, theme);
        }
    }

    private static void RenderBorder(CellBuffer buffer, Rectangle rect, Theme theme)
    {
        var glyphs = theme.Lines;
        var border = theme.BorderStyle(focused: false);
        var surface = theme.SurfaceStyle();

        var left = rect.X;
        var top = rect.Y + 1;
        var right = rect.X + rect.Width - 1;
        var bottom = rect.Y + rect.Height - 1;

        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), surface);
            }
        }

        buffer.SetCell(left, top, new Rune(glyphs.TopLeft), border);
        buffer.SetCell(right, top, new Rune(glyphs.TopRight), border);
        buffer.SetCell(left, bottom, new Rune(glyphs.BottomLeft), border);
        buffer.SetCell(right, bottom, new Rune(glyphs.BottomRight), border);

        for (var x = left + 1; x < right; x++)
        {
            buffer.SetCell(x, top, new Rune(glyphs.Horizontal), border);
            buffer.SetCell(x, bottom, new Rune(glyphs.Horizontal), border);
        }

        for (var y = top + 1; y < bottom; y++)
        {
            buffer.SetCell(left, y, new Rune(glyphs.Vertical), border);
            buffer.SetCell(right, y, new Rune(glyphs.Vertical), border);
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
        if (localY != 0)
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
        if (localY != 0)
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
        App?.RequestRender();
    }

    private void UpdateTabVisibility()
    {
        var selected = SelectedIndex;
        for (var i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].Content.IsVisible = i == selected;
        }
    }

    private readonly record struct TabHitRange(int Index, int Start, int End);

    public readonly record struct TabPage(string Header, Visuals.Visual Content);
}
