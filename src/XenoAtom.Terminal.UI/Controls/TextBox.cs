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
    private Rectangle _editorRect;
    private bool _showOverflowIndicatorLeft;
    private bool _showOverflowIndicatorRight;

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

        var baseRect = new Rectangle(
            innerLeft + padding.Left,
            innerTop + padding.Top,
            Math.Max(0, innerWidth - padding.Horizontal),
            Math.Max(0, innerHeight - padding.Vertical));

        UpdateEditorLayoutForOverflowIndicators(baseRect, style);
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

        var baseRect = new Rectangle(
            innerLeft + padding.Left,
            innerTop + padding.Top,
            Math.Max(0, innerWidth - padding.Horizontal),
            Math.Max(0, innerHeight - padding.Vertical));

        if (baseRect.Width > 0 && baseRect.Height > 0)
        {
            for (var y = baseRect.Y; y < baseRect.Y + baseRect.Height; y++)
            {
                for (var x = baseRect.X; x < baseRect.X + baseRect.Width; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), backgroundStyle);
                }
            }
        }

        var editorRect = _editorRect.Width <= 0 || _editorRect.Height <= 0 ? baseRect : _editorRect;
        RenderEditor(buffer, editorRect, backgroundStyle, selectionStyle, placeholderStyle);

        if (editorRect.Width > 0 && editorRect.Height > 0)
        {
            var y = editorRect.Y;
            var indicatorStyle = textBoxStyle.OverflowIndicatorStyle(theme);

            if (_showOverflowIndicatorLeft && textBoxStyle.OverflowIndicatorLeft is { } left)
            {
                var x = editorRect.X - 1;
                if (x >= baseRect.X && x < baseRect.X + baseRect.Width)
                {
                    buffer.SetCell(x, y, left, indicatorStyle);
                }
            }

            if (_showOverflowIndicatorRight && textBoxStyle.OverflowIndicatorRight is { } right)
            {
                var x = editorRect.X + editorRect.Width;
                if (x >= baseRect.X && x < baseRect.X + baseRect.Width)
                {
                    buffer.SetCell(x, y, right, indicatorStyle);
                }
            }
        }
    }

    private void UpdateEditorLayoutForOverflowIndicators(Rectangle baseRect, TextBoxStyle style)
    {
        _editorRect = baseRect;
        _showOverflowIndicatorLeft = false;
        _showOverflowIndicatorRight = false;

        for (var pass = 0; pass < 3; pass++)
        {
            UpdateEditorLayout(_editorRect);

            var canShowLeft = style.OverflowIndicatorLeft is not null;
            var canShowRight = style.OverflowIndicatorRight is not null;

            var showLeft = canShowLeft && Scroll.OffsetX > 0;
            var showRight = canShowRight && Scroll.OffsetX + Scroll.ViewportWidth < Scroll.ExtentWidth;

            var nextRect = baseRect;
            if (showLeft)
            {
                nextRect = new Rectangle(nextRect.X + 1, nextRect.Y, Math.Max(0, nextRect.Width - 1), nextRect.Height);
            }

            if (showRight)
            {
                nextRect = new Rectangle(nextRect.X, nextRect.Y, Math.Max(0, nextRect.Width - 1), nextRect.Height);
            }

            if (nextRect == _editorRect && showLeft == _showOverflowIndicatorLeft && showRight == _showOverflowIndicatorRight)
            {
                return;
            }

            _editorRect = nextRect;
            _showOverflowIndicatorLeft = showLeft;
            _showOverflowIndicatorRight = showRight;
        }
    }
}
