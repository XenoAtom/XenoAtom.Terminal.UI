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

public sealed partial class OptionList : Visual
{
    private int _scrollOffset;
    private int _hoveredIndex = -1;
    private int _itemHeight = 1;

    private bool _pressed;
    private int _pressedIndex = -1;
    private int _oldSelectedForEvent;

    private string _typeBuffer = string.Empty;
    private long _typeLastTick;

    public VisualList<OptionListItem> Items { get; }

    public OptionList()
    {
        Items = new VisualList<OptionListItem>(this, "Items");
        Focusable = true;
        this.Height(8);
    }

    [Bindable]
    public partial int SelectedIndex { get; set; }

    [Bindable]
    public partial int Height { get; set; }

    [Bindable]
    public partial bool ActivateOnClick { get; set; }

    partial void OnActivateOnClickChanged(bool value)
    {
        _ = value;
    }

    partial void OnSelectedIndexChanging(ref int value)
    {
        _oldSelectedForEvent = _selectedIndex;
        value = ClampToEnabledIndex(value);
    }

    partial void OnSelectedIndexChanged(int value)
    {
        if (_oldSelectedForEvent != value)
        {
            RaiseEvent(SelectionChangedEvent, new SelectionChangedEventArgs { OldIndex = _oldSelectedForEvent, NewIndex = value });
        }
    }

    protected override int ChildrenCount => Items.Count;

    protected override Visual GetChild(int index) => Items[index];

    protected override Size MeasureOverride(Size availableSize)
    {
        var height = Math.Max(1, Height);
        var style = Get<OptionListStyle>();
        var showBorder = style.ShowBorder;

        var prefixWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.MarkerGlyph)) + Math.Max(0, style.SpaceBetweenGlyphAndText);

        var itemWidth = 0;
        var itemHeight = 1;
        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            item.Measure(new Size(int.MaxValue / 4, int.MaxValue / 4));
            itemWidth = Math.Max(itemWidth, item.DesiredSize.Width);
            itemHeight = Math.Max(itemHeight, Math.Max(1, item.DesiredSize.Height));
        }

        _itemHeight = itemHeight;

        var width = Math.Min(availableSize.Width, prefixWidth + itemWidth);
        var desiredHeight = Math.Min(height, availableSize.Height);

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

        var rect = finalRect;
        if (rect.Width <= 0 || rect.Height <= 0 || Items.Count == 0)
        {
            return;
        }

        var style = Get<OptionListStyle>();
        var showBorder = style.ShowBorder;
        var innerLeft = rect.X + (showBorder ? 1 : 0);
        var innerTop = rect.Y + (showBorder ? 1 : 0);
        var innerWidth = Math.Max(0, rect.Width - (showBorder ? 2 : 0));
        var innerHeight = Math.Max(0, rect.Height - (showBorder ? 2 : 0));
        var itemHeight = Math.Max(1, _itemHeight);
        var viewportItems = Math.Max(1, innerHeight / itemHeight);

        var prefixWidth = Math.Min(innerWidth, Math.Max(1, TerminalTextUtility.GetRuneWidth(style.MarkerGlyph)) + Math.Max(0, style.SpaceBetweenGlyphAndText));
        var itemLeft = innerLeft + prefixWidth;
        var itemWidth = Math.Max(0, innerWidth - prefixWidth);

        var count = Items.Count;
        var selected = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));

        if (selected < _scrollOffset)
        {
            _scrollOffset = selected;
        }
        else if (selected >= _scrollOffset + viewportItems)
        {
            _scrollOffset = Math.Max(0, selected - viewportItems + 1);
        }

        for (var i = 0; i < count; i++)
        {
            var y = innerTop + ((i - _scrollOffset) * itemHeight);
            Items[i].Arrange(new Rectangle(itemLeft, y, itemWidth, itemHeight));
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
        var style = Get<OptionListStyle>();
        var showBorder = style.ShowBorder;

        var innerLeft = rect.X + (showBorder ? 1 : 0);
        var innerTop = rect.Y + (showBorder ? 1 : 0);
        var innerWidth = Math.Max(0, rect.Width - (showBorder ? 2 : 0));
        var innerHeight = Math.Max(0, rect.Height - (showBorder ? 2 : 0));
        var itemHeight = Math.Max(1, _itemHeight);
        var viewportItems = Math.Max(1, innerHeight / itemHeight);

        var count = Items.Count;
        var selected = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));
        var isFocused = ReferenceEquals(App?.FocusedElement, this);

        // Fill background (inherit terminal theme).
        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), CellStyle.None);
            }
        }

        if (showBorder && rect.Width >= 2 && rect.Height >= 2)
        {
            var border = theme.BorderStyle(isFocused);
            var glyphs = theme.Lines;
            var left = rect.X;
            var top = rect.Y;
            var right = rect.Right - 1;
            var bottom = rect.Bottom - 1;

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

        var markerWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.MarkerGlyph));
        var gap = Math.Max(0, style.SpaceBetweenGlyphAndText);
        var prefixWidth = Math.Min(innerWidth, markerWidth + gap);

        for (var visibleIndex = 0; visibleIndex < viewportItems; visibleIndex++)
        {
            var index = _scrollOffset + visibleIndex;
            if ((uint)index >= (uint)count)
            {
                continue;
            }

            var item = Items[index];
            var itemEnabled = item.IsEnabled;
            var isSelected = index == selected;
            var isHovered = index == _hoveredIndex;
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

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var index = TryGetIndexAtPoint(e.UiX, e.UiY);
        if (_hoveredIndex != index)
        {
            _hoveredIndex = index;
            Invalidate();
        }
    }

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
        Invalidate();
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var wasPressed = _pressed;
        _pressed = false;
        var releasedIndex = TryGetIndexAtPoint(e.UiX, e.UiY);

        if (wasPressed && _pressedIndex >= 0 && releasedIndex == _pressedIndex)
        {
            if (ActivateOnClick)
            {
                ActivateIndex(_pressedIndex);
            }
        }

        _pressedIndex = -1;
        e.Handled = true;
        Invalidate();
    }

    protected override void OnPointerWheel(PointerEventArgs e)
    {
        if (Items.Count == 0)
        {
            return;
        }

        var delta = e.RawEvent.WheelDelta;
        if (delta == 0)
        {
            return;
        }

        // WheelDelta > 0 is typically up.
        SelectedIndex = Math.Clamp(SelectedIndex - Math.Sign(delta), 0, Math.Max(0, Items.Count - 1));
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var count = Items.Count;
        if (count == 0)
        {
            return;
        }

        var showBorder = Get<OptionListStyle>().ShowBorder;
        var viewportHeight = Math.Max(1, Bounds.Height - (showBorder ? 2 : 0));
        var selected = Math.Clamp(SelectedIndex, 0, count - 1);

        switch (e.Key)
        {
            case TerminalKey.Up:
                SelectedIndex = FindPreviousEnabledIndex(selected - 1);
                e.Handled = true;
                return;
            case TerminalKey.Down:
                SelectedIndex = FindNextEnabledIndex(selected + 1);
                e.Handled = true;
                return;
            case TerminalKey.Home:
                SelectedIndex = FindNextEnabledIndex(0);
                e.Handled = true;
                return;
            case TerminalKey.End:
                SelectedIndex = FindPreviousEnabledIndex(count - 1);
                e.Handled = true;
                return;
            case TerminalKey.PageUp:
                SelectedIndex = FindPreviousEnabledIndex(Math.Max(0, selected - viewportHeight));
                e.Handled = true;
                return;
            case TerminalKey.PageDown:
                SelectedIndex = FindNextEnabledIndex(Math.Min(count - 1, selected + viewportHeight));
                e.Handled = true;
                return;
            case TerminalKey.Enter:
            case TerminalKey.Space:
                ActivateIndex(selected);
                e.Handled = true;
                return;
        }

        // Type-to-jump.
        if (e.Char is { } ch && !char.IsControl(ch) && !char.IsWhiteSpace(ch))
        {
            var now = Environment.TickCount64;
            if (_typeLastTick == 0 || now - _typeLastTick > 700)
            {
                _typeBuffer = string.Empty;
            }

            _typeLastTick = now;
            _typeBuffer += ch;

            var match = FindByPrefix(_typeBuffer, selected + 1);
            if (match < 0)
            {
                match = FindByPrefix(_typeBuffer, 0);
            }

            if (match >= 0)
            {
                SelectedIndex = match;
                e.Handled = true;
            }
        }
    }

    private int TryGetIndexAtPoint(int x, int y)
    {
        var rect = Bounds;
        var style = Get<OptionListStyle>();
        var showBorder = style.ShowBorder;
        var innerTop = rect.Y + (showBorder ? 1 : 0);
        var innerHeight = Math.Max(0, rect.Height - (showBorder ? 2 : 0));
        var itemHeight = Math.Max(1, _itemHeight);

        if (y < innerTop || y >= innerTop + innerHeight)
        {
            return -1;
        }

        var row = (y - innerTop) / itemHeight;
        var index = _scrollOffset + row;
        return (uint)index < (uint)Items.Count ? index : -1;
    }

    private void SelectIndexFromInteraction(int index)
    {
        if ((uint)index >= (uint)Items.Count)
        {
            return;
        }

        if (!Items[index].IsEnabled)
        {
            return;
        }

        SelectedIndex = index;
    }

    private void ActivateIndex(int index)
    {
        if ((uint)index >= (uint)Items.Count)
        {
            return;
        }

        if (!Items[index].IsEnabled)
        {
            return;
        }

        RaiseEvent(ItemActivatedEvent, new ItemActivatedEventArgs { Index = index });
    }

    private int ClampToEnabledIndex(int index)
    {
        if (Items.Count == 0)
        {
            return 0;
        }

        index = Math.Clamp(index, 0, Items.Count - 1);
        if (Items[index].IsEnabled)
        {
            return index;
        }

        var next = FindNextEnabledIndex(index);
        if (next >= 0)
        {
            return next;
        }

        var prev = FindPreviousEnabledIndex(index);
        return prev >= 0 ? prev : 0;
    }

    private int FindNextEnabledIndex(int start)
    {
        for (var i = Math.Max(0, start); i < Items.Count; i++)
        {
            if (Items[i].IsEnabled)
            {
                return i;
            }
        }

        return Math.Max(0, Items.Count - 1);
    }

    private int FindPreviousEnabledIndex(int start)
    {
        for (var i = Math.Min(start, Items.Count - 1); i >= 0; i--)
        {
            if (Items[i].IsEnabled)
            {
                return i;
            }
        }

        return 0;
    }

    private int FindByPrefix(string prefix, int startIndex)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return -1;
        }

        for (var i = startIndex; i < Items.Count; i++)
        {
            if (!Items[i].IsEnabled)
            {
                continue;
            }

            var text = GetSearchText(Items[i]);
            if (text is null)
            {
                continue;
            }

            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static string? GetSearchText(OptionListItem item)
    {
        if (!string.IsNullOrEmpty(item.SearchText))
        {
            return item.SearchText;
        }

        if (item.Content is TextBlock tb && tb.Text is { } text && text.Length > 0)
        {
            return text;
        }

        return null;
    }

    [RoutedEvent(RoutingStrategy.Bubble)]
    private void OnSelectionChanged(SelectionChangedEventArgs e) { }

    [RoutedEvent(RoutingStrategy.Bubble)]
    private void OnItemActivated(ItemActivatedEventArgs e) { }
}
