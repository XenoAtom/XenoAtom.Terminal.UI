// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Specifies when a <see cref="MaskedInput"/> should reveal its text.
/// </summary>
public enum MaskedInputRevealMode
{
    /// <summary>
    /// Never reveal the text (always masked).
    /// </summary>
    Never = 0,
    
    /// <summary>
    /// Reveal the text while the control is focused.
    /// </summary>
    WhileFocused = 1,
    
    /// <summary>
    /// Always reveal the text.
    /// </summary>
    Always = 2,
}

/// <summary>
/// Specifies clipboard behavior for a <see cref="MaskedInput"/>.
/// </summary>
public enum MaskedInputClipboardMode
{
    /// <summary>
    /// Disable clipboard operations.
    /// </summary>
    Disabled = 0,
    
    /// <summary>
    /// Allow copy/cut of the real text.
    /// </summary>
    CopyText = 1,
}

/// <summary>
/// A text box that masks its content for password-like input.
/// </summary>
public sealed partial class MaskedInput : TextBox
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaskedInput"/> class.
    /// </summary>
    public MaskedInput()
    {
        this.RevealMode(MaskedInputRevealMode.Never);
        this.ClipboardMode(MaskedInputClipboardMode.Disabled);
    }

    /// <summary>
    /// Gets or sets the reveal mode.
    /// </summary>
    [Bindable]
    public partial MaskedInputRevealMode RevealMode { get; set; }

    /// <summary>
    /// Gets or sets the clipboard mode.
    /// </summary>
    [Bindable]
    public partial MaskedInputClipboardMode ClipboardMode { get; set; }

    /// <inheritdoc/>
    protected override TextBoxStyle GetTextBoxStyle() => Get<MaskedInputStyle>();

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if ((e.Modifiers & TerminalModifiers.Ctrl) != 0 && ClipboardMode != MaskedInputClipboardMode.CopyText)
        {
            if (e.Char is TerminalChar.CtrlC or TerminalChar.CtrlX)
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
