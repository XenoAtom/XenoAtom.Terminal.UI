// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a list box that displays a vertical list of visuals with a single selection.
/// </summary>
public sealed partial class ListBox : Visual
{
    private int _scrollOffset;

    /// <summary>
    /// Gets the collection of items displayed by the list box.
    /// </summary>
    [Bindable]
    public VisualList<Visual> Items { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListBox"/> class.
    /// </summary>
    public ListBox()
    {
        Items = new VisualList<Visual>(this, "Items");
        Focusable = true;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    /// <summary>
    /// Gets or sets the selected item index.
    /// </summary>
    [Bindable]
    public partial int SelectedIndex { get; set; }

    /// <inheritdoc />
    protected override int ChildrenCount => Items.Count;

    /// <inheritdoc />
    protected override Visual GetChild(int index) => Items[index];

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var items = Items;
        var itemWidth = 0;
        if (items.Count > 0)
        {
            var itemConstraints = new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                item.Measure(itemConstraints);
                itemWidth = Math.Max(itemWidth, item.DesiredSize.Width);
            }
        }

        // Marker + space.
        var width = itemWidth + 2;
        var desiredHeight = Math.Max(1, items.Count);

        var min = new Size(2, 1);
        var natural = new Size(Math.Max(min.Width, width), Math.Max(min.Height, desiredHeight));
        var max = new Size(LayoutConstants.Infinite, LayoutConstants.Infinite);
        return SizeHints.Flex(min, natural, max, growX: 1, growY: 1, shrinkX: 1, shrinkY: 1);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var rect = finalRect;
        var items = Items;
        if (rect.Width <= 0 || rect.Height <= 0 || items.Count == 0)
        {
            return;
        }

        var listBoxStyle = Get<ListBoxStyle>();
        var innerLeft = rect.X;
        var innerTop = rect.Y;
        var innerWidth = Math.Max(0, rect.Width);
        var innerHeight = Math.Max(0, rect.Height);

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

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        var items = Items;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var listBoxStyle = Get<ListBoxStyle>();
        var innerLeft = rect.X;
        var innerTop = rect.Y;
        var innerWidth = Math.Max(0, rect.Width);
        var innerHeight = Math.Max(0, rect.Height);

        var count = items.Count;
        var selected = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));

        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();

        // Fill background.
        var background = Style.None;
        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), background);
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

        var index = _scrollOffset + innerY;
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
}
