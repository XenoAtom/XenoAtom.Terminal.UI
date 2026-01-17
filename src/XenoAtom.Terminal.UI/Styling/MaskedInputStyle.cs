// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling for masked text input controls.
/// </summary>
public sealed record MaskedInputStyle : TextBoxStyle, IStyle<MaskedInputStyle>
{
    /// <summary>
    /// Gets the default masked input style.
    /// </summary>
    public new static MaskedInputStyle Default { get; } = new();

    /// <summary>
    /// Gets the style key for masked inputs.
    /// </summary>
    public new static StyleKey<MaskedInputStyle> Key { get; } = new("MaskedInputStyle", Default);
    
    /// <summary>
    /// Gets the glyph used to mask characters.
    /// </summary>
    public Rune MaskGlyph { get; init; } = new('•');
}
