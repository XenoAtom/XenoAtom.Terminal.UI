// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI;

public sealed partial class TextBlock : Visual
{
    public TextBlock()
    {
    }

    public TextBlock(string text)
    {
        Text = text;
    }

    [Bindable]
    public partial string? Text { get; set; }

    protected override CellSize MeasureOverride(CellSize availableSize)
    {
        var text = Text ?? string.Empty;
        var width = Math.Min(availableSize.Width, TerminalTextUtility.GetWidth(text.AsSpan()));
        return new CellSize(width, 1);
    }

    protected override void ArrangeOverride(CellRect finalRect)
    {
        Bounds = finalRect;
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var text = Text ?? string.Empty;
        buffer.WriteText(Bounds.X, Bounds.Y, text.AsSpan(), CellStyle.None);
    }
}
