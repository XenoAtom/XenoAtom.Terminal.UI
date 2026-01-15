// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Text;

public readonly record struct TextPosition(int Index);

public readonly record struct TextRange(int Start, int Length)
{
    public int End => Start + Length;
}
