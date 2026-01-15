// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class TextArea : TextEditorBase
{
    public TextArea()
    {
        this.AcceptTab(true);
        this.WordWrap(true);
        this.HorizontalAlignment(HorizontalAlignment.Stretch);
        this.VerticalAlignment(VerticalAlignment.Stretch);
    }

    protected override bool IsSingleLine => false;

    protected override bool AcceptsReturn => true;

    protected override bool ShowPlaceholderWhenUnfocusedOnly => false;

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = Get<TextAreaStyle>();
        var showBorder = style.ShowBorder;

        var width = 32;
        var height = 10;

        if (showBorder)
        {
            width = Math.Max(width, 3);
            height = Math.Max(height, 3);
        }

        return SizeHints.Fixed(constraints.Clamp(new Size(width, height)));
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var style = Get<TextAreaStyle>();
        var showBorder = style.ShowBorder;
        var padding = style.Padding;

        var innerLeft = finalRect.X;
        var innerTop = finalRect.Y;
        var innerWidth = finalRect.Width;
        var innerHeight = finalRect.Height;
        if (showBorder && finalRect.Width >= 2 && finalRect.Height >= 2)
        {
            innerLeft++;
            innerTop++;
            innerWidth = Math.Max(0, innerWidth - 2);
            innerHeight = Math.Max(0, innerHeight - 2);
        }

        var contentRect = new Rectangle(
            innerLeft + padding.Left,
            innerTop + padding.Top,
            Math.Max(0, innerWidth - padding.Horizontal),
            Math.Max(0, innerHeight - padding.Vertical));

        UpdateEditorLayout(contentRect);
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();
        var style = Get<TextAreaStyle>();
        var showBorder = style.ShowBorder;
        var borderStyle = style.BorderStyle(theme, isFocused);
        var selectionStyle = style.SelectionStyle(theme);
        var backgroundStyle = style.BackgroundStyle(theme);
        var placeholderStyle = style.PlaceholderStyle(theme);
        var padding = style.Padding;

        var innerLeft = rect.X;
        var innerTop = rect.Y;
        var innerWidth = rect.Width;
        var innerHeight = rect.Height;
        if (showBorder && rect.Width >= 2 && rect.Height >= 2)
        {
            var glyphs = theme.Lines;
            var left = rect.X;
            var top = rect.Y;
            var right = rect.X + rect.Width - 1;
            var bottom = rect.Y + rect.Height - 1;

            buffer.SetCell(left, top, glyphs.TopLeft, borderStyle);
            buffer.SetCell(right, top, glyphs.TopRight, borderStyle);
            buffer.SetCell(left, bottom, glyphs.BottomLeft, borderStyle);
            buffer.SetCell(right, bottom, glyphs.BottomRight, borderStyle);

            for (var x = left + 1; x < right; x++)
            {
                buffer.SetCell(x, top, glyphs.Horizontal, borderStyle);
                buffer.SetCell(x, bottom, glyphs.Horizontal, borderStyle);
            }

            for (var y = top + 1; y < bottom; y++)
            {
                buffer.SetCell(left, y, glyphs.Vertical, borderStyle);
                buffer.SetCell(right, y, glyphs.Vertical, borderStyle);
            }

            innerLeft = rect.X + 1;
            innerTop = rect.Y + 1;
            innerWidth = Math.Max(0, rect.Width - 2);
            innerHeight = Math.Max(0, rect.Height - 2);
        }

        var contentRect = new Rectangle(
            innerLeft + padding.Left,
            innerTop + padding.Top,
            Math.Max(0, innerWidth - padding.Horizontal),
            Math.Max(0, innerHeight - padding.Vertical));

        if (contentRect.Width > 0 && contentRect.Height > 0)
        {
            for (var y = contentRect.Y; y < contentRect.Y + contentRect.Height; y++)
            {
                for (var x = contentRect.X; x < contentRect.X + contentRect.Width; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), backgroundStyle);
                }
            }
        }

        RenderEditor(buffer, contentRect, backgroundStyle, selectionStyle, placeholderStyle);
    }
}
