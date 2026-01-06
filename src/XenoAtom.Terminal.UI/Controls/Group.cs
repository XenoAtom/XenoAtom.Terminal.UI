// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Group : Visual
{
    [Bindable]
    public partial Thickness Padding { get; set; }

    [Bindable]
    public partial string? TopLeftText { get; set; }

    [Bindable]
    public partial string? TopRightText { get; set; }

    [Bindable]
    public partial string? BottomLeftText { get; set; }

    [Bindable]
    public partial string? BottomRightText { get; set; }

    [Bindable]
    public partial Visual? Content { get; set; }

    protected override int ChildrenCount => _content is null ? 0 : 1;

    protected override Visual GetChild(int index)
        => index == 0 && _content is not null ? _content : throw new ArgumentOutOfRangeException(nameof(index));

    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var innerWidth = Math.Max(0, availableSize.Width - 2 - padding.Horizontal);
        var innerHeight = Math.Max(0, availableSize.Height - 2 - padding.Vertical);

        var content = Content;
        content?.Measure(new Size(innerWidth, innerHeight));

        var desiredWidth = 2 + padding.Horizontal + (content?.DesiredSize.Width ?? 0);
        var desiredHeight = 2 + padding.Vertical + (content?.DesiredSize.Height ?? 0);

        return new Size(Math.Min(availableSize.Width, desiredWidth), Math.Min(availableSize.Height, desiredHeight));
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        var content = Content;
        if (content is null)
        {
            return;
        }

        var padding = Padding;
        var inner = new Rectangle(
            finalRect.X + 1 + padding.Left,
            finalRect.Y + 1 + padding.Top,
            Math.Max(0, finalRect.Width - 2 - padding.Horizontal),
            Math.Max(0, finalRect.Height - 2 - padding.Vertical));

        content.Arrange(inner);
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var focused = false;
        for (var v = App?.FocusedElement; v is not null; v = v.Parent)
        {
            if (ReferenceEquals(v, this))
            {
                focused = true;
                break;
            }
        }

        var theme = GetTheme();
        var glyphs = theme.Lines;
        var borderStyle = theme.BorderStyle(focused);

        var left = rect.X;
        var top = rect.Y;
        var right = rect.X + rect.Width - 1;
        var bottom = rect.Y + rect.Height - 1;

        if (rect.Width >= 2 && rect.Height >= 2)
        {
            buffer.SetCell(left, top, new Rune(glyphs.TopLeft), borderStyle);
            buffer.SetCell(right, top, new Rune(glyphs.TopRight), borderStyle);
            buffer.SetCell(left, bottom, new Rune(glyphs.BottomLeft), borderStyle);
            buffer.SetCell(right, bottom, new Rune(glyphs.BottomRight), borderStyle);

            for (var x = left + 1; x < right; x++)
            {
                buffer.SetCell(x, top, new Rune(glyphs.Horizontal), borderStyle);
                buffer.SetCell(x, bottom, new Rune(glyphs.Horizontal), borderStyle);
            }

            for (var y = top + 1; y < bottom; y++)
            {
                buffer.SetCell(left, y, new Rune(glyphs.Vertical), borderStyle);
                buffer.SetCell(right, y, new Rune(glyphs.Vertical), borderStyle);
            }
        }

        var labelStyle = CellStyle.None | TextStyle.Bold;
        WriteLabel(buffer, left + 1, top, right - left - 1, TopLeftText, labelStyle);
        WriteLabelRight(buffer, left + 1, top, right - left - 1, TopRightText, labelStyle);
        WriteLabel(buffer, left + 1, bottom, right - left - 1, BottomLeftText, labelStyle);
        WriteLabelRight(buffer, left + 1, bottom, right - left - 1, BottomRightText, labelStyle);
    }

    private static void WriteLabel(CellBuffer buffer, int x, int y, int maxWidth, string? text, CellStyle style)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0)
        {
            return;
        }

        var label = $" {text} ";
        var span = label.AsSpan();
        if (TerminalTextUtility.TryGetIndexAtCell(span, maxWidth, out var endIndex))
        {
            span = span[..endIndex];
        }

        buffer.WriteText(x, y, span, style);
    }

    private static void WriteLabelRight(CellBuffer buffer, int x, int y, int maxWidth, string? text, CellStyle style)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0)
        {
            return;
        }

        var label = $" {text} ";
        var span = label.AsSpan();
        if (TerminalTextUtility.TryGetIndexAtCell(span, maxWidth, out var endIndex))
        {
            span = span[..endIndex];
        }

        var cells = TerminalTextUtility.GetWidth(span);
        var startX = x + Math.Max(0, maxWidth - cells);
        buffer.WriteText(startX, y, span, style);
    }
}
