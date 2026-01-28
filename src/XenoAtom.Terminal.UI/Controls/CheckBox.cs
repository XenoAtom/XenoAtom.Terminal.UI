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

/// <summary>
/// A toggle control that represents a boolean value.
/// </summary>
public sealed partial class CheckBox : Visual
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CheckBox"/> class.
    /// </summary>
    public CheckBox()
    {
        Focusable = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckBox"/> class with content.
    /// </summary>
    /// <param name="text">The checkbox label.</param>
    /// <param name="isChecked">The initial checked state.</param>
    public CheckBox(string text, bool isChecked = false) : this()
    {
        Text = text;
        IsChecked = isChecked;
    }

    /// <summary>
    /// Gets or sets the label visual.
    /// </summary>
    [Bindable]
    public partial Visual? Text { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the checkbox is checked.
    /// </summary>
    [Bindable]
    public partial bool IsChecked { get; set; }

    /// <inheritdoc/>
    protected override int ChildrenCount => _text is null ? 0 : 1;

    /// <inheritdoc/>
    protected override Visual GetChild(int index)
        => index == 0 && _text is not null ? _text : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var checkBoxStyle = GetStyle<CheckBoxStyle>();
        var gap = Math.Max(0, checkBoxStyle.SpaceBetweenGlyphAndText);
        var glyph = IsChecked ? checkBoxStyle.CheckedGlyph : checkBoxStyle.UncheckedGlyph;
        var glyphWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(glyph));

        var textWidth = 0;
        var textVisual = Text;
        if (textVisual is not null)
        {
            textVisual.Measure(new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1));
            textWidth = textVisual.DesiredSize.Width;
        }

        var width = LayoutConstants.ClampFinite(textWidth + glyphWidth + gap);
        return SizeHints.Fixed(constraints.Clamp(new Size(width, 1)));
    }

    /// <inheritdoc/>
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var textVisual = Text;
        if (textVisual is null)
        {
            return;
        }

        var checkBoxStyle = GetStyle<CheckBoxStyle>();
        var gap = Math.Max(0, checkBoxStyle.SpaceBetweenGlyphAndText);
        var glyph = IsChecked ? checkBoxStyle.CheckedGlyph : checkBoxStyle.UncheckedGlyph;
        var glyphWidth = TerminalTextUtility.GetRuneWidth(glyph);

        var textX = finalRect.X + Math.Max(1, glyphWidth) + gap;
        var available = Math.Max(0, finalRect.Width - (textX - finalRect.X));
        var desired = Math.Min(available, textVisual.DesiredSize.Width);
        textVisual.Arrange(new Rectangle(textX, finalRect.Y, desired, 1));
    }

    /// <inheritdoc/>
    protected override void RenderOverride(CellBuffer buffer)
    {
        var isFocused = HasFocus;
        var theme = GetTheme();
        var checkBoxStyle = GetStyle<CheckBoxStyle>();
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

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is TerminalKey.Space or TerminalKey.Enter)
        {
            IsChecked = !IsChecked;
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
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
