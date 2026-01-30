// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI.Text;

internal static class WordBoundaryUtility
{
    public static bool IsWordBoundary(string text, int start, int length)
        => IsWordBoundary(text.AsSpan(), start, length);

    public static bool IsWordBoundary(ReadOnlySpan<char> text, int start, int length)
    {
        if (text.IsEmpty)
        {
            return start == 0 && length == 0;
        }

        if ((uint)start > (uint)text.Length || length < 0)
        {
            return false;
        }

        var afterIndex = start + length;
        if ((uint)afterIndex > (uint)text.Length)
        {
            return false;
        }

        var before = start > 0 ? text[start - 1] : '\0';
        var after = afterIndex < text.Length ? text[afterIndex] : '\0';

        var beforeOk = start == 0 || !TerminalTextUtility.IsWordChar(before);
        var afterOk = afterIndex >= text.Length || !TerminalTextUtility.IsWordChar(after);
        return beforeOk && afterOk;
    }
}

