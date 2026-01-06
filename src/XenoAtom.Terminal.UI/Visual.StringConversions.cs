// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public abstract partial class Visual
{
    public static implicit operator Visual(string text)
        => new Controls.TextBlock(text ?? string.Empty);
}

