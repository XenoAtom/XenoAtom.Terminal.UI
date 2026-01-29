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
/// Displays a vertical list of options with selection and activation.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed partial class OptionList<T> : Visual, IScrollable
{
    private readonly BindableList<Visual> _itemVisuals;
    private readonly List<Visual> _recyclePool = new();

    private readonly ScrollModel _scroll;
    private int _itemHeight = 1;
    private bool _ensureSelectedVisible;

    private bool _pressed;
    private int _pressedIndex = -1;
    private int _oldSelectedForEvent;

    private string _typeBuffer = string.Empty;
    private long _typeLastTick;

    private int _lastItemsVersion = -1;
    private DataTemplate<T> _lastResolvedTemplate;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionList{T}"/> control.
    /// </summary>
    public OptionList()
    {
        Items = new BindableList<T>(this, "OptionList.Items");
        _scroll = new ScrollModel(this);
        _itemVisuals = new BindableList<Visual>(
            this,
            "OptionList.ItemVisuals",
            onAdding: AttachCollectionChild,
            onRemoving: v =>
            {
                DetachCollectionChild(v);
                _recyclePool.Add(v);
            });

        Focusable = true;
        HoveredIndex = -1;
    }

    [Bindable]
    internal partial int HoveredIndex { get; set; }

    /// <summary>
    /// Gets the scroll model for this list.
    /// </summary>
    public ScrollModel Scroll => _scroll;

    /// <summary>
    /// Gets the items displayed by this list.
    /// </summary>
    [Bindable]
    public BindableList<T> Items { get; }

    /// <summary>
    /// Gets or sets the selected item index.
    /// </summary>
    [Bindable]
    public partial int SelectedIndex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an item is activated on click.
    /// </summary>
    [Bindable]
    public partial bool ActivateOnClick { get; set; }

    /// <summary>
    /// Gets or sets the template used to create visuals for items.
    /// </summary>
    [Bindable]
    public partial DataTemplate<T> ItemTemplate { get; set; }

    /// <summary>
    /// Gets or sets the factory used to determine whether an item is enabled.
    /// </summary>
    [Bindable]
    public partial Delegator<Func<T, bool>> ItemIsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the factory used to provide search text for type-to-jump.
    /// </summary>
    [Bindable]
    public partial Delegator<Func<T, string?>> ItemSearchText { get; set; }

    [Bindable]
    private partial int ScrollVersion { get; set; }

    partial void OnSelectedIndexChanging(ref int value)
    {
        _oldSelectedForEvent = _selectedIndex;
        value = ClampToEnabledIndex(value);
    }

    partial void OnSelectedIndexChanged(int value)
    {
        if (_oldSelectedForEvent != value)
        {
            _ensureSelectedVisible = true;

            // If we already have a viewport (i.e. the control was arranged at least once), update the scroll model
            // immediately so parents like ScrollViewer can update scroll bars during the same frame.
            if (_scroll.ViewportHeight > 0)
            {
                EnsureSelectedVisible(value);
                _ensureSelectedVisible = false;
            }

            RaiseEvent(SelectionChangedEvent, new SelectionChangedEventArgs { OldIndex = _oldSelectedForEvent, NewIndex = value });
        }
    }

    /// <inheritdoc/>
    protected override int ChildrenCount => _itemVisuals.Count;

    /// <inheritdoc/>
    protected override Visual GetChild(int index) => _itemVisuals[index];

    /// <inheritdoc/>
    protected override void PrepareChildren()
    {
        ScrollVersion = _scroll.Version;
    }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        EnsureItemVisuals();

        var style = GetStyle<OptionListStyle>();
        var prefixWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.MarkerGlyph)) + Math.Max(0, style.SpaceBetweenGlyphAndText);

        var itemWidth = 0;
        var itemHeight = 1;
        for (var i = 0; i < _itemVisuals.Count; i++)
        {
            var item = _itemVisuals[i];
            item.Measure(LayoutConstraints.Unbounded);
            itemWidth = Math.Max(itemWidth, item.DesiredSize.Width);
            itemHeight = Math.Max(itemHeight, Math.Max(1, item.DesiredSize.Height));
        }

        _itemHeight = itemHeight;

        var width = prefixWidth + itemWidth;
        var desiredHeight = Math.Max(1, _itemVisuals.Count * itemHeight);

        var min = new Size(1, 1);
        var natural = new Size(Math.Max(min.Width, width), Math.Max(min.Height, desiredHeight));
        var max = new Size(LayoutConstants.Infinite, LayoutConstants.Infinite);
        return SizeHints.Flex(min, natural, max, growX: 1, growY: 1, shrinkX: 1, shrinkY: 1);
    }

    /// <inheritdoc/>
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        _ = ScrollVersion;
        EnsureItemVisuals();

        var rect = finalRect;
        if (rect.Width <= 0 || rect.Height <= 0 || _itemVisuals.Count == 0)
        {
            _scroll.SetViewport(0, 0);
            _scroll.SetExtent(0, 0);
            return;
        }

        var style = GetStyle<OptionListStyle>();
        var innerLeft = rect.X;
        var innerTop = rect.Y;
        var innerWidth = Math.Max(0, rect.Width);
        var innerHeight = Math.Max(0, rect.Height);
        var itemHeight = Math.Max(1, _itemHeight);
        var viewportItems = Math.Max(1, innerHeight / itemHeight);
        var viewportHeight = viewportItems * itemHeight;

        var prefixWidth = Math.Min(innerWidth, Math.Max(1, TerminalTextUtility.GetRuneWidth(style.MarkerGlyph)) + Math.Max(0, style.SpaceBetweenGlyphAndText));
        var itemLeft = innerLeft + prefixWidth;
        var itemWidth = Math.Max(0, innerWidth - prefixWidth);

        var count = Items.Count;
        var selected = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));

        _scroll.SetViewport(innerWidth, viewportHeight);
        _scroll.SetExtent(prefixWidth + itemWidth, Math.Max(0, count * itemHeight));

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

        // Keep the scroll position aligned to full item rows.
        var alignedOffsetY = (_scroll.OffsetY / itemHeight) * itemHeight;
        if (alignedOffsetY != _scroll.OffsetY)
        {
            _scroll.SetOffset(_scroll.OffsetX, alignedOffsetY);
        }

        var scrollOffset = itemHeight == 0 ? 0 : (_scroll.OffsetY / itemHeight);

        for (var i = 0; i < _itemVisuals.Count; i++)
        {
            var y = innerTop + ((i - scrollOffset) * itemHeight);
            _itemVisuals[i].Arrange(new Rectangle(itemLeft, y, itemWidth, itemHeight));
        }
    }

    /// <inheritdoc/>
    protected override void RenderOverride(CellBuffer buffer)
    {
        _ = ScrollVersion;
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = GetStyle<OptionListStyle>();

        var innerLeft = rect.X;
        var innerTop = rect.Y;
        var innerWidth = Math.Max(0, rect.Width);
        var innerHeight = Math.Max(0, rect.Height);
        var itemHeight = Math.Max(1, _itemHeight);
        var viewportItems = Math.Max(1, innerHeight / itemHeight);

        var count = Items.Count;
        var selected = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));
        var isFocused = HasFocus;

        // Fill background (inherit terminal theme).
        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), Style.None);
            }
        }

        var markerWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.MarkerGlyph));
        var gap = Math.Max(0, style.SpaceBetweenGlyphAndText);
        var prefixWidth = Math.Min(innerWidth, markerWidth + gap);

        var scrollOffset = itemHeight == 0 ? 0 : (_scroll.OffsetY / itemHeight);

        for (var visibleIndex = 0; visibleIndex < viewportItems; visibleIndex++)
        {
            var index = scrollOffset + visibleIndex;
            if ((uint)index >= (uint)count)
            {
                continue;
            }

            var isSelected = index == selected;
            var isHovered = index == HoveredIndex;
            var itemEnabled = IsItemEnabled(index);
            var rowStyle = style.ResolveItemStyle(theme, IsEnabled && itemEnabled, isSelected, isFocused, isHovered);

            var itemTop = innerTop + (visibleIndex * itemHeight);
            for (var line = 0; line < itemHeight; line++)
            {
                var y = itemTop + line;
                if (y >= innerTop + innerHeight)
                {
                    break;
                }

                for (var x = 0; x < innerWidth; x++)
                {
                    buffer.SetCell(innerLeft + x, y, new Rune(' '), rowStyle);
                }

                if (line == 0 && innerWidth > 0)
                {
                    var marker = isSelected ? style.MarkerGlyph : new Rune(' ');
                    buffer.SetCell(innerLeft, y, marker, rowStyle);
                }

                for (var i = 0; i < gap && markerWidth + i < prefixWidth; i++)
                {
                    buffer.SetCell(innerLeft + markerWidth + i, y, new Rune(' '), rowStyle);
                }
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var index = TryGetIndexAtPoint(e.UiX, e.UiY);
        if (HoveredIndex != index)
        {
            HoveredIndex = index;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        _pressed = true;
        _pressedIndex = TryGetIndexAtPoint(e.UiX, e.UiY);
        if (_pressedIndex >= 0)
        {
            SelectIndexFromInteraction(_pressedIndex);
        }

        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (!_pressed || e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        _pressed = false;
        var index = TryGetIndexAtPoint(e.UiX, e.UiY);
        var pressedIndex = _pressedIndex;
        _pressedIndex = -1;

        if (ActivateOnClick && index >= 0 && index == pressedIndex && index == SelectedIndex)
        {
            ActivateIndex(index);
        }

        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerWheel(PointerEventArgs e)
    {
        var count = Items.Count;
        if (count == 0 || e.WheelDelta == 0)
        {
            return;
        }

        var selected = Math.Clamp(SelectedIndex, 0, count - 1);
        var delta = e.WheelDelta > 0 ? -1 : 1;
        SelectedIndex = selected + delta;
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        var count = Items.Count;
        if (count == 0)
        {
            return;
        }

        var itemHeight = Math.Max(1, _itemHeight);
        var viewportItems = Math.Max(1, Bounds.Height / itemHeight);
        var selected = Math.Clamp(SelectedIndex, 0, count - 1);

        switch (e.Key)
        {
            case TerminalKey.Up:
                SelectedIndex = selected - 1;
                e.Handled = true;
                return;
            case TerminalKey.Down:
                SelectedIndex = selected + 1;
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
                SelectedIndex = selected - viewportItems;
                e.Handled = true;
                return;
            case TerminalKey.PageDown:
                SelectedIndex = selected + viewportItems;
                e.Handled = true;
                return;
            case TerminalKey.Enter:
            case TerminalKey.Space:
                ActivateIndex(selected);
                e.Handled = true;
                return;
        }

        // Type-to-jump
        if (e.Char is char ch && ch != '\0' && !char.IsControl(ch))
        {
            HandleTypeToJump(ch);
            e.Handled = true;
        }
    }

    private void ActivateIndex(int index)
    {
        if (!IsItemEnabled(index))
        {
            return;
        }

        RaiseEvent(ItemActivatedEvent, new ItemActivatedEventArgs { Index = index });
    }

    private void SelectIndexFromInteraction(int index)
    {
        if (!IsItemEnabled(index))
        {
            return;
        }

        SelectedIndex = index;
    }

    private int ClampToEnabledIndex(int value)
    {
        var count = Items.Count;
        if (count == 0)
        {
            return -1;
        }

        var target = Math.Clamp(value, 0, count - 1);
        if (IsItemEnabled(target))
        {
            return target;
        }

        var direction = value >= _selectedIndex ? 1 : -1;
        for (var i = target; (uint)i < (uint)count; i += direction)
        {
            if (IsItemEnabled(i))
            {
                return i;
            }

            if (i == 0 || i == count - 1)
            {
                break;
            }
        }

        return _selectedIndex;
    }

    private bool IsItemEnabled(int index)
    {
        var count = Items.Count;
        if ((uint)index >= (uint)count)
        {
            return false;
        }

        var value = Items[index];
        var enabled = ItemIsEnabled.Invoke?.Invoke(value);
        if (enabled.HasValue)
        {
            return enabled.Value;
        }

        if (value is Visual v)
        {
            return v.IsEnabled;
        }

        return true;
    }

    private void EnsureSelectedVisible(int selectedIndex)
    {
        var count = Items.Count;
        if ((uint)selectedIndex >= (uint)count)
        {
            return;
        }

        var itemHeight = Math.Max(1, _itemHeight);
        var viewportItems = Math.Max(1, _scroll.ViewportHeight / itemHeight);

        var offsetItems = itemHeight == 0 ? 0 : (_scroll.OffsetY / itemHeight);
        var targetOffsetItems = offsetItems;

        if (selectedIndex < offsetItems)
        {
            targetOffsetItems = selectedIndex;
        }
        else if (selectedIndex >= offsetItems + viewportItems)
        {
            targetOffsetItems = selectedIndex - viewportItems + 1;
        }

        var maxOffsetItems = Math.Max(0, count - viewportItems);
        targetOffsetItems = Math.Clamp(targetOffsetItems, 0, maxOffsetItems);

        var newOffsetY = targetOffsetItems * itemHeight;
        if (newOffsetY != _scroll.OffsetY)
        {
            _scroll.SetOffset(_scroll.OffsetX, newOffsetY);
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

    private int TryGetIndexAtPoint(int x, int y)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return -1;
        }

        var innerY = y - rect.Y;
        if (innerY < 0)
        {
            return -1;
        }

        var itemHeight = Math.Max(1, _itemHeight);
        var scrollOffset = itemHeight == 0 ? 0 : (_scroll.OffsetY / itemHeight);
        var index = scrollOffset + (innerY / itemHeight);
        return (uint)index < (uint)Items.Count ? index : -1;
    }

    private void HandleTypeToJump(char ch)
    {
        var now = Environment.TickCount64;
        if (now - _typeLastTick > 1000)
        {
            _typeBuffer = string.Empty;
        }

        _typeLastTick = now;
        _typeBuffer += ch;

        var idx = FindByPrefix(_typeBuffer);
        if (idx >= 0)
        {
            SelectedIndex = idx;
        }
    }

    private int FindByPrefix(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return -1;
        }

        var items = Items;
        for (var i = 0; i < items.Count; i++)
        {
            var text = GetSearchText(i);
            if (text is not null && text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private string? GetSearchText(int index)
    {
        var items = Items;
        if ((uint)index >= (uint)items.Count)
        {
            return null;
        }

        var value = items[index];
        var search = ItemSearchText.Invoke?.Invoke(value);
        if (!string.IsNullOrEmpty(search))
        {
            return search;
        }

        if (_itemVisuals.Count == items.Count && _itemVisuals[index] is OptionListItem item)
        {
            if (!string.IsNullOrEmpty(item.SearchText))
            {
                return item.SearchText;
            }

            if (item.Content is TextBlock tb && tb.Text is { } text && text.Length > 0)
            {
                return text;
            }
        }

        return value?.ToString();
    }

    [RoutedEvent(RoutingStrategy.Bubble)]
    private void OnSelectionChanged(SelectionChangedEventArgs e) { }

    [RoutedEvent(RoutingStrategy.Bubble)]
    private void OnItemActivated(ItemActivatedEventArgs e) { }
}
