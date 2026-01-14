// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record MaskedInputStyle : TextBoxStyle, IStyle<MaskedInputStyle>
{
    public new static MaskedInputStyle Default { get; } = new();

    public new static StyleKey<MaskedInputStyle> Key { get; } = new("MaskedInputStyle", Default);
    
    public Rune MaskGlyph { get; init; } = new('•');
}
