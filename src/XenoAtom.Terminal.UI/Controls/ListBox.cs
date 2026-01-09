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

public sealed partial class ListBox : Visual
{
    private int _scrollOffset;

    public VisualList<Visual> Items { get; }

    public ListBox()
    {
        Items = new VisualList<Visual>(this, "Items");
        Focusable = true;
        this.Height(6);
    }

    [Bindable]
    public partial int SelectedIndex { get; set; }

    [Bindable]
    public partial int Height { get; set; }

    protected override int ChildrenCount => Items.Count;

    protected override Visual GetChild(int index) => Items[index];

    protected override Size MeasureOverride(Size availableSize)
    {
        var height = Math.Max(1, Height);
        var listBoxStyle = Get<ListBoxStyle>();
        var showBorder = listBoxStyle.ShowBorder;

        var items = Items;
        var itemWidth = 0;
        if (items.Count > 0)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                item.Measure(new Size(int.MaxValue / 4, 1));
                itemWidth = Math.Max(itemWidth, item.DesiredSize.Width);
            }
        }

        // Marker + space.
        var width = Math.Min(availableSize.Width, itemWidth + 2);

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
        var items = Items;
        if (rect.Width <= 0 || rect.Height <= 0 || items.Count == 0)
        {
            return;
        }

        var listBoxStyle = Get<ListBoxStyle>();
        var showBorder = listBoxStyle.ShowBorder;
        var innerLeft = rect.X + (showBorder ? 1 : 0);
        var innerTop = rect.Y + (showBorder ? 1 : 0);
        var innerWidth = Math.Max(0, rect.Width - (showBorder ? 2 : 0));
        var innerHeight = Math.Max(0, rect.Height - (showBorder ? 2 : 0));

        var count = items.Count;
        var selected = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));

        if (selected < _scrollOffset)
        {
            _scrollOffset = selected;
        }
        else if (selected >= _scrollOffset + Math.Max(1, innerHeight))
        {
            _scrollOffset = Math.Max(0, selected - Math.Max(1, innerHeight) + 1);
        }

        var itemLeft = innerLeft + 2;
        var itemWidth = Math.Max(0, innerWidth - 2);
        for (var i = 0; i < count; i++)
        {
            var y = innerTop + (i - _scrollOffset);
            items[i].Arrange(new Rectangle(itemLeft, y, itemWidth, 1));
        }
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        var items = Items;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var listBoxStyle = Get<ListBoxStyle>();
        var showBorder = listBoxStyle.ShowBorder;
        var innerLeft = rect.X + (showBorder ? 1 : 0);
        var innerTop = rect.Y + (showBorder ? 1 : 0);
        var innerWidth = Math.Max(0, rect.Width - (showBorder ? 2 : 0));
        var innerHeight = Math.Max(0, rect.Height - (showBorder ? 2 : 0));

        var count = items.Count;
        var selected = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));

        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();
        var border = theme.BorderStyle(isFocused);
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

        for (var row = 0; row < innerHeight; row++)
        {
            var itemIndex = _scrollOffset + row;
            var y = innerTop + row;

            if ((uint)itemIndex >= (uint)count)
            {
                continue;
            }

            var isSelected = itemIndex == selected;
            var style = listBoxStyle.ResolveItemStyle(theme, IsEnabled, isSelected, isFocused);

            if (innerWidth <= 0)
            {
                continue;
            }

            // Fill row background/style so that child visuals using CellStyle.None inherit foreground/background.
            for (var x = 0; x < innerWidth; x++)
            {
                buffer.SetCell(innerLeft + x, y, new Rune(' '), style);
            }

            if (innerWidth >= 2)
            {
                buffer.SetCell(innerLeft, y, isSelected ? listBoxStyle.MarkerGlyph : new Rune(' '), style);
                buffer.SetCell(innerLeft + 1, y, new Rune(' '), style);
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var count = Items.Count;
        if (count == 0)
        {
            return;
        }

        var showBorder = Get<ListBoxStyle>().ShowBorder;
        var viewportHeight = Math.Max(1, Bounds.Height - (showBorder ? 2 : 0));
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

        var showBorder = Get<ListBoxStyle>().ShowBorder;
        var innerY = (e.UiY - Bounds.Y) - (showBorder ? 1 : 0);
        var innerHeight = Math.Max(0, Bounds.Height - (showBorder ? 2 : 0));
        if ((uint)innerY >= (uint)innerHeight)
        {
            return;
        }

        var index = _scrollOffset + innerY;
        if ((uint)index < (uint)count)
        {
            SelectedIndex = index;
            e.Handled = true;
        }
    }

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
}
