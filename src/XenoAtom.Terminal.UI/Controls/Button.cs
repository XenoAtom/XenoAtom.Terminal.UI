// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI;

public partial class Button : Visual
{
    public Button()
    {
        Focusable = true;
    }

    public Button(string text) : this()
    {
        Text = text;
    }

    [Bindable]
    public partial string? Text { get; set; }

    protected override CellSize MeasureOverride(CellSize availableSize)
    {
        var text = Text ?? string.Empty;
        var innerWidth = TerminalTextUtility.GetWidth(text.AsSpan());
        var width = Math.Min(availableSize.Width, innerWidth + 4);
        return new CellSize(width, 1);
    }

    protected override void ArrangeOverride(CellRect finalRect)
    {
        Bounds = finalRect;
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var style = isFocused ? CellStyle.Invert : CellStyle.None;

        var rect = Bounds;
        var text = Text ?? string.Empty;

        buffer.WriteText(rect.X, rect.Y, "[ ".AsSpan(), style);
        buffer.WriteText(rect.X + 2, rect.Y, text.AsSpan(), style);
        buffer.WriteText(rect.X + Math.Max(0, rect.Width - 2), rect.Y, " ]".AsSpan(), style);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is TerminalKey.Enter or TerminalKey.Space)
        {
            RaiseEvent(ClickEvent, new ClickEventArgs());
            e.Handled = true;
        }
    }

    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnClick(ClickEventArgs e) { }
}
