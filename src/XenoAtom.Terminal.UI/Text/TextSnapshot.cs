// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Text;

internal sealed class TextSnapshot : ITextSnapshot
{
    private readonly string _text;
    private readonly int[] _lineStarts;

    public TextSnapshot(int version, string text, List<int> lineStarts)
    {
        Version = version;
        _text = text;
        _lineStarts = lineStarts.ToArray();
    }

    public int Version { get; }

    public int Length => _text.Length;

    public int LineCount => _lineStarts.Length;

    public char this[int index] => _text[index];

    public TextLine GetLine(int lineIndex)
    {
        if ((uint)lineIndex >= (uint)_lineStarts.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(lineIndex));
        }

        var start = _lineStarts[lineIndex];
        var end = lineIndex + 1 < _lineStarts.Length ? _lineStarts[lineIndex + 1] - 1 : _text.Length;
        if (end < start)
        {
            end = start;
        }

        var length = end - start;
        var lineBreakLength = lineIndex + 1 < _lineStarts.Length ? 1 : 0;
        return new TextLine(lineIndex, start, length, lineBreakLength);
    }

    public int GetLineIndexFromPosition(int position)
    {
        position = Math.Clamp(position, 0, _text.Length);
        var index = Array.BinarySearch(_lineStarts, position);
        if (index >= 0)
        {
            return index;
        }

        var insertion = ~index;
        return Math.Max(0, insertion - 1);
    }

    public void CopyTo(int start, Span<char> destination)
    {
        if (start < 0 || start > _text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        if (destination.Length == 0)
        {
            return;
        }

        var available = _text.Length - start;
        var count = Math.Min(available, destination.Length);
        _text.AsSpan(start, count).CopyTo(destination);
    }

    public string Text => _text;

    public IReadOnlyList<int> LineStarts => _lineStarts;
}
