// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class CheckBox : Visuals.Visual
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
    public partial string? Text { get; set; }

    [Bindable]
    public partial bool IsChecked { get; set; }

    protected override CellSize MeasureOverride(CellSize availableSize)
    {
        var text = Text ?? string.Empty;
        var width = Math.Min(availableSize.Width, TerminalTextUtility.GetWidth(text.AsSpan()) + 4);
        return new CellSize(width, 1);
    }

    protected override void ArrangeOverride(CellRect finalRect)
    {
        Bounds = finalRect;
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();
        var checkBoxStyle = GetEnvironmentValue(CheckBoxStyle.Key);
        var style = checkBoxStyle.Resolve(theme, IsEnabled, isFocused, IsHovered);

        var rect = Bounds;
        var text = Text ?? string.Empty;

        for (var x = rect.X; x < rect.X + rect.Width; x++)
        {
            buffer.SetCell(x, rect.Y, new Rune(' '), style);
        }

        var glyph = IsChecked ? checkBoxStyle.CheckedGlyph : checkBoxStyle.UncheckedGlyph;
        buffer.SetCell(rect.X, rect.Y, new Rune(glyph), style | TextStyle.Bold);
        buffer.WriteText(rect.X + 2, rect.Y, text.AsSpan(), style);
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
