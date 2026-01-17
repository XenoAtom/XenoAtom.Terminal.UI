// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Displays one tab page at a time with a clickable header strip.
/// </summary>
public sealed partial class TabControl : Visual
{
    private readonly List<TabPage> _tabs = new();
    private readonly List<TabHitRange> _hitRanges = new();
    private int _hoveredIndex = -1;
    private int _pressedIndex = -1;
    private bool _pressedInside;
    private int _headerHeight = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="TabControl"/> class.
    /// </summary>
    public TabControl()
    {
        Focusable = true;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    /// <summary>
    /// Initializes a new tab control with the provided tab pages.
    /// </summary>
    /// <param name="tabs">The tab pages.</param>
    public TabControl(params TabPage[] tabs) : this()
    {
        ArgumentNullException.ThrowIfNull(tabs);
        for (var i = 0; i < tabs.Length; i++)
        {
            AddTab(tabs[i]);
        }
    }

    /// <summary>
    /// Gets or sets the selected tab index.
    /// </summary>
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
        Invalidate();
    }

    /// <summary>
    /// Gets the tab pages owned by this control.
    /// </summary>
    public IReadOnlyList<TabPage> Tabs => _tabs;

    /// <summary>
    /// Adds a tab page from a header and a content visual.
    /// </summary>
    /// <param name="header">The tab header visual.</param>
    /// <param name="content">The tab content visual.</param>
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

    /// <summary>
    /// Adds a tab page.
    /// </summary>
    /// <param name="page">The tab page to add.</param>
    public void AddTab(TabPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        AddTab(page.Header, page.Content);
    }

    /// <inheritdoc/>
    protected override int ChildrenCount => _tabs.Count * 2;

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = Get<TabControlStyle>();
        var pad = style.TabPadding;

        var headerHeight = 1;
        var headerTotalWidth = 0;
        var headerConstraints = new LayoutConstraints(0, LayoutConstants.Infinite, 0, constraints.MaxHeight);

        for (var i = 0; i < _tabs.Count; i++)
        {
            var header = _tabs[i].Header;
            var headerHints = header.Measure(headerConstraints);
            headerHeight = Math.Max(headerHeight, headerHints.Natural.Height);

            var tabWidth = headerHints.Natural.Width + pad.Horizontal;
            headerTotalWidth += tabWidth;
            if (i + 1 < _tabs.Count)
            {
                headerTotalWidth += 1;
            }
        }

        headerHeight = Math.Max(1, headerHeight);

        var contentMaxW = constraints.MaxWidth == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxWidth);
        var contentMaxH = constraints.MaxHeight == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxHeight - headerHeight);

        var contentWidth = 0;
        var contentHeight = 0;
        if (_tabs.Count > 0)
        {
            var selected = Math.Clamp(SelectedIndex, 0, _tabs.Count - 1);
            var content = _tabs[selected].Content;
            var contentHints = content.Measure(new LayoutConstraints(0, contentMaxW, 0, contentMaxH));
            contentWidth = contentHints.Natural.Width;
            contentHeight = contentHints.Natural.Height;
        }

        var width = Math.Max(headerTotalWidth, contentWidth);
        var height = headerHeight + contentHeight;

        var min = new Size(Math.Min(width, LayoutConstants.MaxFinite), Math.Min(height, LayoutConstants.MaxFinite));
        var natural = min;
        var max = new Size(LayoutConstants.Infinite, LayoutConstants.Infinite);
        return SizeHints.Flex(min, natural, max, growX: 1, growY: 1, shrinkX: 1, shrinkY: 1);
    }

    /// <inheritdoc/>
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var style = Get<TabControlStyle>();
        var pad = style.TabPadding;

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

        for (var i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].Content.Arrange(inner);
        }
    }

    /// <inheritdoc/>
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = Get<TabControlStyle>();
        var focused = ReferenceEquals(App?.FocusedElement, this);

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
            var pressed = range.Index == _pressedIndex && _pressedInside;
            var tabStyle = style.ResolveTabStyle(theme, tab.Content.IsEnabled, focused, selected, hovered, pressed);

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

    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var localX = e.UiX - Bounds.X;
        var localY = e.UiY - Bounds.Y;
        if (localY < 0 || localY >= _headerHeight)
        {
            UpdateHoveredIndex(-1);
            UpdatePressedInside(false);
            return;
        }

        var index = HitTestTabIndex(localX);
        UpdateHoveredIndex(index);
        UpdatePressedInside(index >= 0 && index == _pressedIndex);
    }

    /// <inheritdoc/>
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
            _pressedIndex = index;
            _pressedInside = true;
            UpdateHoveredIndex(index);
            Invalidate();
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (_pressedIndex < 0)
        {
            return;
        }

        var localX = e.UiX - Bounds.X;
        var localY = e.UiY - Bounds.Y;

        var overHeader = localY >= 0 && localY < _headerHeight;
        var index = overHeader ? HitTestTabIndex(localX) : -1;
        var activate = _pressedInside && index == _pressedIndex;

        _pressedIndex = -1;
        _pressedInside = false;
        UpdateHoveredIndex(index);
        Invalidate();

        if (activate)
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

    private void UpdatePressedInside(bool value)
    {
        if (_pressedInside == value)
        {
            return;
        }

        _pressedInside = value;
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
