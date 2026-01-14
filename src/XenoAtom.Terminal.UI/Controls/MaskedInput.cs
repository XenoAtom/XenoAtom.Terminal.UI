// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public enum MaskedInputRevealMode
{
    Never = 0,
    WhileFocused = 1,
    Always = 2,
}

public enum MaskedInputClipboardMode
{
    Disabled = 0,
    CopyText = 1,
}

public sealed partial class MaskedInput : TextBox
{
    public MaskedInput()
    {
        this.RevealMode(MaskedInputRevealMode.Never);
        this.ClipboardMode(MaskedInputClipboardMode.Disabled);
    }

    [Bindable]
    public partial MaskedInputRevealMode RevealMode { get; set; }

    [Bindable]
    public partial MaskedInputClipboardMode ClipboardMode { get; set; }

    protected override TextBoxStyle GetTextBoxStyle() => Get<MaskedInputStyle>();

    protected override void WriteTextSegment(CellBuffer buffer, int x, int y, ReadOnlySpan<char> text, CellStyle cellStyle, bool isPlaceholder)
    {
        if (isPlaceholder || ShouldReveal())
        {
            base.WriteTextSegment(buffer, x, y, text, cellStyle, isPlaceholder);
            return;
        }

        var style = (MaskedInputStyle)GetTextBoxStyle();
        var rune = style.MaskGlyph;
        var runeWidth = TerminalTextUtility.GetRuneWidth(rune);
        if (runeWidth != 1)
        {
            rune = new Rune('*');
        }

        var totalCells = TerminalTextUtility.GetWidth(text);
        for (var i = 0; i < totalCells; i++)
        {
            buffer.SetCell(x + i, y, rune, cellStyle);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if ((e.Modifiers & TerminalModifiers.Ctrl) != 0 && ClipboardMode != MaskedInputClipboardMode.CopyText)
        {
            if (e.Char is 'c' or 'C' or 'x' or 'X')
            {
                e.Handled = true;
                return;
            }
        }

        base.OnKeyDown(e);
    }

    private bool ShouldReveal()
    {
        var mode = RevealMode;
        return mode == MaskedInputRevealMode.Always
               || (mode == MaskedInputRevealMode.WhileFocused && ReferenceEquals(App?.FocusedElement, this));
    }
}
