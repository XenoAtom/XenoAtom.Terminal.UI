// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class CheckBox : Visual
{
    public CheckBox()
    {
        Focusable = true;
    }

    public CheckBox(string text, bool isChecked = false) : this()
    {
        Text = text;
        IsChecked = isChecked;
    }

    [Bindable]
    public partial Visual? Text { get; set; }

    [Bindable]
    public partial bool IsChecked { get; set; }

    protected override int ChildrenCount => _text is null ? 0 : 1;

    protected override Visual GetChild(int index)
        => index == 0 && _text is not null ? _text : throw new ArgumentOutOfRangeException(nameof(index));

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var availableSize = new Size(constraints.MaxWidth, constraints.MaxHeight);
        var checkBoxStyle = Get<CheckBoxStyle>();
        var gap = Math.Max(0, checkBoxStyle.SpaceBetweenGlyphAndText);
        var glyph = IsChecked ? checkBoxStyle.CheckedGlyph : checkBoxStyle.UncheckedGlyph;
        var glyphWidth = TerminalTextUtility.GetRuneWidth(glyph);

        var textWidth = 0;
        var textVisual = Text;
        if (textVisual is not null)
        {
            textVisual.Measure(new Size(LayoutConstants.Infinite, 1));
            textWidth = textVisual.DesiredSize.Width;
        }

        var width = Math.Min(availableSize.Width, textWidth + glyphWidth + gap);
        return SizeHints.Fixed(new Size(width, 1));
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var textVisual = Text;
        if (textVisual is null)
        {
            return;
        }

        var checkBoxStyle = Get<CheckBoxStyle>();
        var gap = Math.Max(0, checkBoxStyle.SpaceBetweenGlyphAndText);
        var glyph = IsChecked ? checkBoxStyle.CheckedGlyph : checkBoxStyle.UncheckedGlyph;
        var glyphWidth = TerminalTextUtility.GetRuneWidth(glyph);

        var textX = finalRect.X + Math.Max(1, glyphWidth) + gap;
        var available = Math.Max(0, finalRect.Width - (textX - finalRect.X));
        var desired = Math.Min(available, textVisual.DesiredSize.Width);
        textVisual.Arrange(new Rectangle(textX, finalRect.Y, desired, 1));
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();
        var checkBoxStyle = Get<CheckBoxStyle>();
        var style = checkBoxStyle.Resolve(theme, IsEnabled, isFocused, IsHovered);
        var gap = Math.Max(0, checkBoxStyle.SpaceBetweenGlyphAndText);

        var rect = Bounds;

        for (var x = rect.X; x < rect.X + rect.Width; x++)
        {
            buffer.SetCell(x, rect.Y, new Rune(' '), style);
        }

        var glyph = IsChecked ? checkBoxStyle.CheckedGlyph : checkBoxStyle.UncheckedGlyph;
        buffer.SetCell(rect.X, rect.Y, glyph, style | TextStyle.Bold);

        var textVisual = Text;
        if (textVisual is not null)
        {
            var glyphWidth = TerminalTextUtility.GetRuneWidth(glyph);
            var gapX = rect.X + Math.Max(1, glyphWidth);
            for (var i = 0; i < gap && gapX + i < rect.X + rect.Width; i++)
            {
                buffer.SetCell(gapX + i, rect.Y, new Rune(' '), style);
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is TerminalKey.Space or TerminalKey.Enter)
        {
            IsChecked = !IsChecked;
            e.Handled = true;
        }
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        IsChecked = !IsChecked;
        e.Handled = true;
    }
}
