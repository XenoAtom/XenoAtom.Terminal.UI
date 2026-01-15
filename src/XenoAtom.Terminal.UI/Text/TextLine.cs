// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Text;

public readonly struct TextLine
{
    public TextLine(int index, int start, int length, int lineBreakLength)
    {
        Index = index;
        Start = start;
        Length = length;
        LineBreakLength = lineBreakLength;
    }

    public int Index { get; }
    public int Start { get; }
    public int Length { get; }
    public int LineBreakLength { get; }

    public int End => Start + Length;
    public int EndIncludingBreak => End + LineBreakLength;
}
