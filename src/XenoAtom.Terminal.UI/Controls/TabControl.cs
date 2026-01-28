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

    [Bindable]
    internal partial int HoveredIndex { get; set; }

    [Bindable]
    internal partial int PressedIndex { get; set; }

    [Bindable]
    internal partial bool IsPressedInside { get; set; }

    private int _headerHeight = 1;
    private readonly TabContentHost _contentHost = new();
    private ContentVisual? _contentTemplate;
    private Visual? _contentRoot;
    private Func<Visual, ContentVisual?>? _contentTemplateFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TabControl"/> class.
    /// </summary>
    public TabControl()
    {
        Focusable = true;
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;

        _contentHost.HorizontalAlignment = Align.Stretch;
        _contentHost.VerticalAlignment = Align.Stretch;
        HoveredIndex = -1;
        PressedIndex = -1;
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

    /// <inheritdoc/>
    protected override void PrepareChildren()
    {
        var style = GetStyle<TabControlStyle>();
        EnsureContentTemplate(style);

        if (_tabs.Count == 0)
        {
            _contentHost.Content = null;
            return;
        }

        var selected = Math.Clamp(SelectedIndex, 0, _tabs.Count - 1);
        _contentHost.Content = _tabs[selected].Content;
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
    protected override int ChildrenCount => _tabs.Count + (_contentRoot is null ? 0 : 1);

    /// <inheritdoc/>
    protected override Visual GetChild(int index)
    {
        if ((uint)index >= (uint)ChildrenCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (index < _tabs.Count)
        {
            return _tabs[index].Header;
        }

        if (index == _tabs.Count && _contentRoot is not null)
        {
            return _contentRoot;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = GetStyle<TabControlStyle>();
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
        if (_contentRoot is not null)
        {
            var contentHints = _contentRoot.Measure(new LayoutConstraints(0, contentMaxW, 0, contentMaxH));
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
        var style = GetStyle<TabControlStyle>();
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

        _contentRoot?.Arrange(inner);
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
        var style = GetStyle<TabControlStyle>();
        var focused = HasFocus;

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
            var hovered = range.Index == HoveredIndex;
            var pressed = range.Index == PressedIndex && IsPressedInside;
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
        UpdatePressedInside(index >= 0 && index == PressedIndex);
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
            PressedIndex = index;
            IsPressedInside = true;
            UpdateHoveredIndex(index);
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

        if (PressedIndex < 0)
        {
            return;
        }

        var localX = e.UiX - Bounds.X;
        var localY = e.UiY - Bounds.Y;

        var overHeader = localY >= 0 && localY < _headerHeight;
        var index = overHeader ? HitTestTabIndex(localX) : -1;
        var activate = IsPressedInside && index == PressedIndex;

        PressedIndex = -1;
        IsPressedInside = false;
        UpdateHoveredIndex(index);

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
        if (HoveredIndex == index)
        {
            return;
        }

        HoveredIndex = index;
    }

    private void UpdatePressedInside(bool value)
    {
        if (IsPressedInside == value)
        {
            return;
        }

        IsPressedInside = value;
    }

    private readonly record struct TabHitRange(int Index, int Start, int End);

    private void EnsureContentTemplate(TabControlStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);

        var factory = style.TabContentTemplateFactory;
        if (_contentRoot is not null && ReferenceEquals(factory, _contentTemplateFactory))
        {
            return;
        }

        if (_contentRoot is not null)
        {
            DetachChild(_contentRoot);
            _contentRoot = null;
        }

        if (_contentTemplate is not null)
        {
            _contentTemplate.Content = null;
            _contentTemplate = null;
        }

        _contentTemplateFactory = factory;

        if (factory is null)
        {
            _contentRoot = _contentHost;
        }
        else
        {
            var template = factory(_contentHost);
            if (template is null)
            {
                throw new InvalidOperationException($"{nameof(TabControlStyle)}.{nameof(TabControlStyle.TabContentTemplateFactory)} returned null.");
            }

            if (!ReferenceEquals(template.Content, _contentHost))
            {
                template.Content = _contentHost;
            }

            _contentTemplate = template;
            _contentRoot = template;
        }

        _contentRoot.HorizontalAlignment = Align.Stretch;
        _contentRoot.VerticalAlignment = Align.Stretch;
        AttachChild(_contentRoot);
    }

    private sealed class TabContentHost : ContentVisual
    {
    }
}
