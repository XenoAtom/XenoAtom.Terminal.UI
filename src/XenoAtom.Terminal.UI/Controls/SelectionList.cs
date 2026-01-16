// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class SelectionList : Visual
{
    private int _scrollOffset;

    public SelectionList()
    {
        Items = new VisualList<SelectionListItem>(this, "SelectionList.Items");
        Focusable = true;
    }

    public VisualList<SelectionListItem> Items { get; }

    [Bindable]
    public partial int SelectedIndex { get; set; }

    protected override int ChildrenCount => Items.Count;

    protected override Visual GetChild(int index) => Items[index];

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = Get<SelectionListStyle>();
        var showBorder = style.ShowBorder;
        var gap = Math.Max(0, style.SpaceBetweenGlyphAndText);
        var markerWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.FocusMarkerGlyph));
        var checkWidth = Math.Max(1, Math.Max(TerminalTextUtility.GetRuneWidth(style.CheckedGlyph), TerminalTextUtility.GetRuneWidth(style.UncheckedGlyph)));
        var prefixWidth = markerWidth + checkWidth + gap;

        var items = Items;
        var itemWidth = 0;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            item.Measure(new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1));
            itemWidth = Math.Max(itemWidth, item.DesiredSize.Width);
        }

        var width = itemWidth + prefixWidth;
        var desiredHeight = Math.Max(1, Items.Count);
        if (showBorder)
        {
            width += 2;
            desiredHeight += 2;
        }

        var min = new Size(showBorder ? 3 : 1, showBorder ? 3 : 1);
        var natural = new Size(Math.Max(min.Width, width), Math.Max(min.Height, desiredHeight));
        var max = new Size(LayoutConstants.Infinite, LayoutConstants.Infinite);
        return SizeHints.Flex(min, natural, max, growX: 1, growY: 1, shrinkX: 1, shrinkY: 1);
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var rect = finalRect;
        var items = Items;
        if (rect.Width <= 0 || rect.Height <= 0 || items.Count == 0)
        {
            return;
        }

        var style = Get<SelectionListStyle>();
        var showBorder = style.ShowBorder;
        var gap = Math.Max(0, style.SpaceBetweenGlyphAndText);
        var markerWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.FocusMarkerGlyph));
        var checkWidth = Math.Max(1, Math.Max(TerminalTextUtility.GetRuneWidth(style.CheckedGlyph), TerminalTextUtility.GetRuneWidth(style.UncheckedGlyph)));
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

        var prefixWidth = Math.Min(innerWidth, markerWidth + checkWidth + gap);
        var itemLeft = innerLeft + prefixWidth;
        var itemWidth = Math.Max(0, innerWidth - prefixWidth);
        for (var i = 0; i < count; i++)
        {
            var y = innerTop + (i - _scrollOffset);
            items[i].Arrange(new Rectangle(itemLeft, y, itemWidth, 1));
        }
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var items = Items;
        var style = Get<SelectionListStyle>();
        var showBorder = style.ShowBorder;
        var gap = Math.Max(0, style.SpaceBetweenGlyphAndText);
        var markerWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.FocusMarkerGlyph));
        var checkWidth = Math.Max(1, Math.Max(TerminalTextUtility.GetRuneWidth(style.CheckedGlyph), TerminalTextUtility.GetRuneWidth(style.UncheckedGlyph)));
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

            var item = items[itemIndex];
            var isSelected = itemIndex == selected;
            var rowStyle = style.ResolveItemStyle(theme, IsEnabled, isSelected, isFocused);

            // Fill row background/style so that child visuals using CellStyle.None inherit.
            for (var x = 0; x < innerWidth; x++)
            {
                buffer.SetCell(innerLeft + x, y, new Rune(' '), rowStyle);
            }

            var xCursor = innerLeft;
            if (innerWidth > 0)
            {
                var marker = isSelected ? style.FocusMarkerGlyph : new Rune(' ');
                buffer.SetCell(xCursor, y, marker, rowStyle);
                xCursor += markerWidth;
            }

            if (xCursor < innerLeft + innerWidth)
            {
                var check = item.IsChecked ? style.CheckedGlyph : style.UncheckedGlyph;
                buffer.SetCell(xCursor, y, check, rowStyle);
                xCursor += checkWidth;
            }

            for (var i = 0; i < gap && xCursor < innerLeft + innerWidth; i++)
            {
                buffer.SetCell(xCursor, y, new Rune(' '), rowStyle);
                xCursor++;
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

        var style = Get<SelectionListStyle>();
        var showBorder = style.ShowBorder;
        var viewportHeight = Math.Max(1, Bounds.Height - (showBorder ? 2 : 0));

        var selected = Math.Clamp(SelectedIndex, 0, count - 1);
        var ctrl = (e.Modifiers & TerminalModifiers.Ctrl) != 0;

        if (ctrl && e.Char is TerminalChar.CtrlA)
        {
            for (var i = 0; i < count; i++)
            {
                Items[i].IsChecked = true;
            }
            e.Handled = true;
            return;
        }

        if (ctrl && e.Char is TerminalChar.CtrlI)
        {
            for (var i = 0; i < count; i++)
            {
                Items[i].IsChecked = !Items[i].IsChecked;
            }
            e.Handled = true;
            return;
        }

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
            case TerminalKey.Space:
            case TerminalKey.Enter:
                Items[selected].IsChecked = !Items[selected].IsChecked;
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

        var style = Get<SelectionListStyle>();
        var showBorder = style.ShowBorder;
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
            Items[index].IsChecked = !Items[index].IsChecked;
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
