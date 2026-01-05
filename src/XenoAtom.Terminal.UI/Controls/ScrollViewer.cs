// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI;

public sealed partial class ScrollViewer : Visual
{
    private int _contentHeight;

    public ScrollViewer()
    {
        Focusable = true;
        Height = 6;
    }

    [Bindable]
    public partial Visual? Child { get; set; }

    [Bindable]
    public partial int VerticalOffset { get; set; }

    [Bindable]
    public partial int Height { get; set; }

    protected override void OnAttachedToApp(TerminalApp app)
    {
        _ = app;
        if (Child is { } child)
        {
            AddChild(child);
        }
    }

    protected override void OnDetachedFromApp(TerminalApp app)
    {
        _ = app;
        ClearChildren();
    }

    protected override CellSize MeasureOverride(CellSize availableSize)
    {
        var height = Math.Max(1, Height);
        var child = Child;
        if (child is not null)
        {
            child.Measure(new CellSize(availableSize.Width, int.MaxValue / 4));
            _contentHeight = child.DesiredSize.Height;
        }
        else
        {
            _contentHeight = 0;
        }

        var desiredHeight = Math.Min(Math.Min(height, availableSize.Height), Math.Max(1, _contentHeight));
        return new CellSize(availableSize.Width, desiredHeight);
    }

    protected override void ArrangeOverride(CellRect finalRect)
    {
        Bounds = finalRect;

        var child = Child;
        if (child is null)
        {
            return;
        }

        var viewportHeight = Math.Max(1, finalRect.Height);
        var maxOffset = Math.Max(0, _contentHeight - viewportHeight);
        var offset = Math.Clamp(VerticalOffset, 0, maxOffset);
        if (offset != VerticalOffset)
        {
            VerticalOffset = offset;
        }

        child.Arrange(new CellRect(finalRect.X, finalRect.Y - offset, finalRect.Width, _contentHeight));
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var child = Child;
        if (child is null)
        {
            return;
        }

        var theme = GetTheme();
        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var borderStyle = theme.BorderStyle(isFocused);

        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            buffer.SetCell(rect.X + rect.Width - 1, y, new Rune('│'), borderStyle);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var viewportHeight = Math.Max(1, Bounds.Height);
        var maxOffset = Math.Max(0, _contentHeight - viewportHeight);
        if (maxOffset == 0)
        {
            return;
        }

        switch (e.Key)
        {
            case TerminalKey.Up:
                VerticalOffset = Math.Max(0, VerticalOffset - 1);
                e.Handled = true;
                return;
            case TerminalKey.Down:
                VerticalOffset = Math.Min(maxOffset, VerticalOffset + 1);
                e.Handled = true;
                return;
            case TerminalKey.PageUp:
                VerticalOffset = Math.Max(0, VerticalOffset - viewportHeight);
                e.Handled = true;
                return;
            case TerminalKey.PageDown:
                VerticalOffset = Math.Min(maxOffset, VerticalOffset + viewportHeight);
                e.Handled = true;
                return;
            case TerminalKey.Home:
                VerticalOffset = 0;
                e.Handled = true;
                return;
            case TerminalKey.End:
                VerticalOffset = maxOffset;
                e.Handled = true;
                return;
        }
    }

    protected override void OnPointerWheel(PointerEventArgs e)
    {
        if (e.WheelDelta == 0)
        {
            return;
        }

        var viewportHeight = Math.Max(1, Bounds.Height);
        var maxOffset = Math.Max(0, _contentHeight - viewportHeight);
        if (maxOffset == 0)
        {
            return;
        }

        VerticalOffset = e.WheelDelta > 0 ? Math.Max(0, VerticalOffset - 1) : Math.Min(maxOffset, VerticalOffset + 1);
        e.Handled = true;
    }
}
