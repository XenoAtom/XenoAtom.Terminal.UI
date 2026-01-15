// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public partial class TextBox : TextEditorBase
{
    public TextBox()
    {
        this.HorizontalAlignment(HorizontalAlignment.Stretch);
    }

    [Bindable]
    public partial TextAlignment TextAlignment { get; set; }

    protected override bool IsSingleLine => true;

    protected override bool AcceptsReturn => false;

    protected override TextAlignment Alignment => TextAlignment;

    protected override bool ShowPlaceholderWhenUnfocusedOnly => true;

    protected virtual TextBoxStyle GetTextBoxStyle() => Get<TextBoxStyle>();

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var availableSize = new Size(constraints.MaxWidth, constraints.MaxHeight);
        var width = Math.Max(10, Math.Min(availableSize.Width, 24));
        var height = GetTextBoxStyle().ShowBorder ? 3 : 1;
        return SizeHints.Fixed(new Size(width, Math.Min(availableSize.Height, height)));
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var style = GetTextBoxStyle();
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
        var textBoxStyle = GetTextBoxStyle();
        var borderStyle = textBoxStyle.BorderStyle(theme, isFocused);
        var selectionStyle = textBoxStyle.SelectionStyle(theme);
        var backgroundStyle = textBoxStyle.BackgroundStyle(theme);
        var placeholderStyle = textBoxStyle.PlaceholderStyle(theme);
        var padding = textBoxStyle.Padding;
        var showBorder = textBoxStyle.ShowBorder;

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
