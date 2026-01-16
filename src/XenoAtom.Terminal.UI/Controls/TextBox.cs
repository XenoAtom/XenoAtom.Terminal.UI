// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

public partial class TextBox : TextEditorBase
{
    public TextBox()
    {
        this.HorizontalAlignment(HorizontalAlignment.Stretch);
        TextDocument = new DynamicTextDocument(
            getter: () => Text ?? string.Empty,
            setter: value => Text = value);
    }

    public TextBox(string? text) : this()
    {
        this.Text(text);
    }

    [Bindable]
    public partial string? Text { get; set; }

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
        var padding = GetTextBoxStyle().Padding;
        var height = Math.Max(1, 1 + padding.Vertical);
        return SizeHints.Fixed(new Size(width, Math.Min(availableSize.Height, height)));
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var style = GetTextBoxStyle();
        var padding = style.Padding;

        var innerLeft = finalRect.X;
        var innerTop = finalRect.Y;
        var innerWidth = finalRect.Width;
        var innerHeight = finalRect.Height;

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
        var selectionStyle = textBoxStyle.SelectionStyle(theme);
        var backgroundStyle = textBoxStyle.BackgroundStyle(theme);
        var placeholderStyle = textBoxStyle.PlaceholderStyle(theme);
        var padding = textBoxStyle.Padding;

        var innerLeft = rect.X;
        var innerTop = rect.Y;
        var innerWidth = rect.Width;
        var innerHeight = rect.Height;

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
