// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI;

public sealed partial class ListBox : Visual
{
    private int _scrollOffset;

    public ListBox()
    {
        Focusable = true;
        Height = 6;
    }

    [Bindable]
    public partial IReadOnlyList<string>? Items { get; set; }

    [Bindable]
    public partial int SelectedIndex { get; set; }

    [Bindable]
    public partial int Height { get; set; }

    protected override CellSize MeasureOverride(CellSize availableSize)
    {
        var height = Math.Max(1, Height);
        var width = 0;

        var items = Items;
        if (items is not null)
        {
            foreach (var item in items)
            {
                width = Math.Max(width, TerminalTextUtility.GetWidth(item.AsSpan()));
            }
        }

        width = Math.Min(availableSize.Width, width);
        return new CellSize(width, Math.Min(height, availableSize.Height));
    }

    protected override void ArrangeOverride(CellRect finalRect)
    {
        Bounds = finalRect;
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        var items = Items;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var count = items?.Count ?? 0;
        var selected = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));

        if (selected < _scrollOffset)
        {
            _scrollOffset = selected;
        }
        else if (selected >= _scrollOffset + rect.Height)
        {
            _scrollOffset = Math.Max(0, selected - rect.Height + 1);
        }

        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();
        var listBoxTheme = GetEnvironmentValue(ListBoxTheme.Key);

        for (var row = 0; row < rect.Height; row++)
        {
            var itemIndex = _scrollOffset + row;
            var y = rect.Y + row;

            if ((uint)itemIndex >= (uint)count)
            {
                buffer.WriteText(rect.X, y, new string(' ', rect.Width).AsSpan(), CellStyle.None);
                continue;
            }

            var item = items![itemIndex];
            var isSelected = itemIndex == selected;
            var style = listBoxTheme.ResolveItemStyle(theme, IsEnabled, isSelected, isFocused);

            buffer.WriteText(rect.X, y, (isSelected ? "> " : "  ").AsSpan(), style);
            buffer.WriteText(rect.X + 2, y, item.AsSpan(), style);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var items = Items;
        var count = items?.Count ?? 0;
        if (count == 0)
        {
            return;
        }

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
        }
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var items = Items;
        var count = items?.Count ?? 0;
        if (count == 0)
        {
            return;
        }

        var index = _scrollOffset + Math.Clamp(e.LocalY, 0, Bounds.Height - 1);
        if ((uint)index < (uint)count)
        {
            SelectedIndex = index;
            e.Handled = true;
        }
    }

    protected override void OnPointerWheel(PointerEventArgs e)
    {
        var items = Items;
        var count = items?.Count ?? 0;
        if (count == 0 || e.WheelDelta == 0)
        {
            return;
        }

        var selected = Math.Clamp(SelectedIndex, 0, count - 1);
        SelectedIndex = e.WheelDelta > 0 ? Math.Max(0, selected - 1) : Math.Min(count - 1, selected + 1);
        e.Handled = true;
    }
}
