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
using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a single-selection list rendered using radio button glyphs.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed partial class RadioButtonList<T> : Visual, IScrollable
{
    private readonly ScrollModel _scroll;
    private bool _ensureSelectedVisible;

    private readonly BindableList<Visual> _itemVisuals;
    private readonly List<Visual> _recyclePool = new();
    private int _lastItemsVersion = -1;
    private DataTemplate<T> _lastResolvedTemplate;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadioButtonList{T}"/> class.
    /// </summary>
    public RadioButtonList()
    {
        Items = new BindableList<T>(this, "RadioButtonList.Items");
        _scroll = new ScrollModel(this);

        _itemVisuals = new BindableList<Visual>(
            this,
            "RadioButtonList.ItemVisuals",
            onAdding: AttachCollectionChild,
            onRemoving: v =>
            {
                DetachCollectionChild(v);
                _recyclePool.Add(v);
            });

        Focusable = true;
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RadioButtonList{T}"/> class with items.
    /// </summary>
    /// <param name="items">The items displayed by the list.</param>
    public RadioButtonList(IEnumerable<T> items) : this()
    {
        ArgumentNullException.ThrowIfNull(items);
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RadioButtonList{T}"/> class with items and selected index.
    /// </summary>
    /// <param name="items">The items displayed by the list.</param>
    /// <param name="selectedIndex">The selected item index.</param>
    public RadioButtonList(IEnumerable<T> items, int selectedIndex) : this(items)
    {
        this.SelectedIndex(selectedIndex);
    }

    /// <summary>
    /// Gets the scroll model for this list.
    /// </summary>
    public ScrollModel Scroll => _scroll;

    /// <summary>
    /// Gets the items displayed by the list.
    /// </summary>
    [Bindable]
    public BindableList<T> Items { get; }

    /// <summary>
    /// Gets or sets the selected item index.
    /// </summary>
    [Bindable]
    public partial int SelectedIndex { get; set; }

    /// <summary>
    /// Gets or sets the template used to create visuals for items.
    /// </summary>
    [Bindable]
    public partial DataTemplate<T> ItemTemplate { get; set; }

    [Bindable]
    private partial int ScrollVersion { get; set; }

    [Bindable]
    private partial int MeasuredContentWidth { get; set; }

    partial void OnSelectedIndexChanging(ref int value)
    {
        var count = Items.Count;
        value = count == 0 ? -1 : Math.Clamp(value, 0, count - 1);
    }

    partial void OnSelectedIndexChanged(int value)
    {
        _ensureSelectedVisible = true;

        if (_scroll.ViewportHeight > 0)
        {
            EnsureSelectedVisible(value);
            _ensureSelectedVisible = false;
        }

    }

    /// <inheritdoc />
    protected override int ChildrenCount => _itemVisuals.Count;

    /// <inheritdoc />
    protected override Visual GetChild(int index) => _itemVisuals[index];

    /// <inheritdoc />
    protected override void PrepareChildren()
    {
        ScrollVersion = _scroll.Version;
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        EnsureItemVisuals();

        var style = GetStyle<RadioButtonListStyle>();
        var checkWidth = Math.Max(1, Math.Max(TerminalTextUtility.GetRuneWidth(style.CheckedGlyph), TerminalTextUtility.GetRuneWidth(style.UncheckedGlyph)));
        var gap = Math.Max(0, style.SpaceBetweenGlyphAndText);
        var prefixWidth = checkWidth + gap;

        var itemWidth = 0;
        if (_itemVisuals.Count > 0)
        {
            var itemConstraints = new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1);
            for (var i = 0; i < _itemVisuals.Count; i++)
            {
                var item = _itemVisuals[i];
                item.Measure(itemConstraints);
                itemWidth = Math.Max(itemWidth, item.DesiredSize.Width);
            }
        }

        var width = prefixWidth + itemWidth;
        MeasuredContentWidth = Math.Max(0, width);
        var desiredHeight = Math.Max(1, _itemVisuals.Count);

        var scrollBarThickness = Math.Max(1, GetStyle<ScrollViewerStyle>().ScrollBarThickness);
        var reserveVerticalBar = constraints.IsHeightBounded && desiredHeight > constraints.MaxHeight;
        var reservedWidth = width + (reserveVerticalBar ? scrollBarThickness : 0);

        var min = new Size(Math.Max(1, prefixWidth), 1);
        var natural = new Size(Math.Max(min.Width, reservedWidth), Math.Max(min.Height, desiredHeight));
        var max = new Size(LayoutConstants.Infinite, LayoutConstants.Infinite);
        return SizeHints.Flex(min, natural, max, growX: 1, growY: 1, shrinkX: 1, shrinkY: 1);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        EnsureItemVisuals();

        _ = ScrollVersion;

        var rect = finalRect;
        if (rect.Width <= 0 || rect.Height <= 0 || _itemVisuals.Count == 0)
        {
            _scroll.SetViewport(0, 0);
            _scroll.SetExtent(0, 0);
            return;
        }

        var style = GetStyle<RadioButtonListStyle>();
        var gap = Math.Max(0, style.SpaceBetweenGlyphAndText);
        var checkWidth = Math.Max(1, Math.Max(TerminalTextUtility.GetRuneWidth(style.CheckedGlyph), TerminalTextUtility.GetRuneWidth(style.UncheckedGlyph)));
        var prefixWidth = checkWidth + gap;

        var innerLeft = rect.X;
        var innerTop = rect.Y;
        var innerWidth = Math.Max(0, rect.Width);
        var innerHeight = Math.Max(0, rect.Height);

        var count = _itemVisuals.Count;
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

        var offsetX = _scroll.OffsetX;
        var itemLeft = innerLeft + prefixWidth - offsetX;
        var itemWidth = Math.Max(0, extentWidth - prefixWidth);
        var scrollOffset = _scroll.OffsetY;
        for (var i = 0; i < count; i++)
        {
            var y = innerTop + (i - scrollOffset);
            _itemVisuals[i].Arrange(new Rectangle(itemLeft, y, itemWidth, 1));
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

        var style = GetStyle<RadioButtonListStyle>();
        var gap = Math.Max(0, style.SpaceBetweenGlyphAndText);
        var checkWidth = Math.Max(1, Math.Max(TerminalTextUtility.GetRuneWidth(style.CheckedGlyph), TerminalTextUtility.GetRuneWidth(style.UncheckedGlyph)));
        var innerLeft = rect.X;
        var innerTop = rect.Y;
        var innerWidth = Math.Max(0, rect.Width);
        var innerHeight = Math.Max(0, rect.Height);

        var count = _itemVisuals.Count;
        var selected = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));

        var isFocused = HasFocus;
        var theme = GetTheme();

        // Fill background.
        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), Style.None);
            }
        }

        for (var row = 0; row < innerHeight; row++)
        {
            var itemIndex = _scroll.OffsetY + row;
            var y = innerTop + row;
            if ((uint)itemIndex >= (uint)count)
            {
                continue;
            }

            var isSelected = itemIndex == selected;
            var rowStyle = style.ResolveItemStyle(theme, IsEnabled, isSelected, isFocused);

            for (var x = 0; x < innerWidth; x++)
            {
                buffer.SetCell(innerLeft + x, y, new Rune(' '), rowStyle);
            }

            var xCursor = innerLeft - _scroll.OffsetX;
            if (innerWidth > 0)
            {
                var glyph = isSelected ? style.CheckedGlyph : style.UncheckedGlyph;
                buffer.SetCell(xCursor, y, glyph, rowStyle | TextStyle.Bold);
                xCursor += checkWidth;
            }

            for (var i = 0; i < gap && xCursor < innerLeft + innerWidth; i++)
            {
                buffer.SetCell(xCursor, y, new Rune(' '), rowStyle);
                xCursor++;
            }
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        var count = Items.Count;
        if (count == 0)
        {
            return;
        }

        var viewportHeight = Math.Max(1, Bounds.Height);
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
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var count = Items.Count;
        if (count == 0)
        {
            return;
        }

        var innerY = e.UiY - Bounds.Y;
        var innerHeight = Math.Max(0, Bounds.Height);
        if ((uint)innerY >= (uint)innerHeight)
        {
            return;
        }

        var index = _scroll.OffsetY + innerY;
        if ((uint)index < (uint)count)
        {
            SelectedIndex = index;
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerWheel(PointerEventArgs e)
    {
        var count = Items.Count;
        if (count == 0 || e.WheelDelta == 0)
        {
            return;
        }

        var selected = Math.Clamp(SelectedIndex, 0, count - 1);
        SelectedIndex = e.WheelDelta > 0 ? Math.Max(0, selected - 1) : Math.Min(count - 1, selected + 1);
        e.Handled = true;
    }

    private void EnsureSelectedVisible(int selectedIndex)
    {
        var count = Items.Count;
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

    private void EnsureItemVisuals()
    {
        var items = Items;
        var template = ResolveItemTemplate();

        if (items.Version == _lastItemsVersion && template.Equals(_lastResolvedTemplate))
        {
            return;
        }

        _lastItemsVersion = items.Version;
        _lastResolvedTemplate = template;

        _itemVisuals.Clear();

        if (items.Count == 0)
        {
            _recyclePool.Clear();
            return;
        }

        var ctxBase = new DataTemplateContext(this, DataTemplateRole.Display, -1, DataTemplateItemState.None);
        for (var i = 0; i < items.Count; i++)
        {
            var value = items[i];
            if (value is Visual asVisual)
            {
                _itemVisuals.Add(asVisual);
                continue;
            }

            if (template.IsEmpty || template.Display is null)
            {
                _itemVisuals.Add(new TextBlock(() => ToStringObject(value)));
                continue;
            }

            var templateValue = new DataTemplateValue<T>(value);

            Visual? reused = null;
            if (_recyclePool.Count != 0)
            {
                var last = _recyclePool.Count - 1;
                reused = _recyclePool[last];
                _recyclePool.RemoveAt(last);
            }

            var ctx = ctxBase with { Index = i };
            if (reused is not null && template.TryUpdate is { } updater && updater(reused, templateValue, ctx))
            {
                _itemVisuals.Add(reused);
                continue;
            }

            if (reused is not null && template.Release is { } release)
            {
                release(reused);
            }

            _itemVisuals.Add(template.Display(templateValue, ctx));
        }

        _recyclePool.Clear();
    }

    private DataTemplate<T> ResolveItemTemplate()
    {
        var template = ItemTemplate;
        if (!template.IsEmpty)
        {
            return template;
        }

        var templates = GetStyle<DataTemplates>();
        if (templates.TryResolve(DataTemplateRole.Display, out template))
        {
            return template;
        }

        return default;
    }
}
