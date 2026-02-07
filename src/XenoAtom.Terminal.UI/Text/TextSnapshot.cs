// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Text;

internal sealed class TextSnapshot : ITextSnapshot
{
    private readonly TextPieceTable _table;
    private readonly TextPiece[] _pieces;
    private readonly int[] _pieceStarts;
    private readonly int _length;
    private readonly int[] _lineStarts;
    private readonly byte[] _lineBreakLengths;
    private string? _text;

    public TextSnapshot(int version, string text, List<int> lineStarts, List<byte> lineBreakLengths)
    {
        Version = version;
        _table = new TextPieceTable(text);
        _pieces = text.Length == 0
            ? Array.Empty<TextPiece>()
            : [new TextPiece(TextPieceSource.Original, 0, text.Length)];
        _length = text.Length;

        _pieceStarts = new int[_pieces.Length];
        var position = 0;
        for (var i = 0; i < _pieces.Length; i++)
        {
            _pieceStarts[i] = position;
            position += _pieces[i].Length;
        }

        _lineStarts = lineStarts.ToArray();
        _lineBreakLengths = lineBreakLengths.ToArray();
    }

    public TextSnapshot(int version, TextPieceTable table, TextPiece[] pieces, int length)
    {
        Version = version;
        _table = table;
        _pieces = pieces;
        _length = length;

        _pieceStarts = new int[_pieces.Length];
        var position = 0;
        for (var i = 0; i < _pieces.Length; i++)
        {
            _pieceStarts[i] = position;
            position += _pieces[i].Length;
        }

        var lineStarts = new List<int>(Math.Max(2, _pieces.Length + 1)) { 0 };
        var lineBreakLengths = new List<byte>(lineStarts.Capacity) { 0 };

        var globalIndex = 0;
        var pendingCarriageReturn = false;

        for (var i = 0; i < _pieces.Length; i++)
        {
            var span = _table.GetPieceSpan(_pieces[i]);
            for (var j = 0; j < span.Length; j++)
            {
                var ch = span[j];
                if (pendingCarriageReturn)
                {
                    if (ch == '\n')
                    {
                        lineBreakLengths[^1] = 2;
                        lineStarts.Add(globalIndex + 1);
                        lineBreakLengths.Add(0);
                        pendingCarriageReturn = false;
                        globalIndex++;
                        continue;
                    }

                    lineBreakLengths[^1] = 1;
                    lineStarts.Add(globalIndex);
                    lineBreakLengths.Add(0);
                    pendingCarriageReturn = false;
                }

                if (ch == '\r')
                {
                    pendingCarriageReturn = true;
                    globalIndex++;
                    continue;
                }

                if (ch == '\n')
                {
                    lineBreakLengths[^1] = 1;
                    lineStarts.Add(globalIndex + 1);
                    lineBreakLengths.Add(0);
                }

                globalIndex++;
            }
        }

        if (pendingCarriageReturn)
        {
            lineBreakLengths[^1] = 1;
            lineStarts.Add(globalIndex);
            lineBreakLengths.Add(0);
        }

        _lineStarts = lineStarts.ToArray();
        _lineBreakLengths = lineBreakLengths.ToArray();
    }

    public int Version { get; }

    public int Length => _length;

    public int LineCount => _lineStarts.Length;

    public char this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var pieceIndex = FindPieceIndex(index);
            var pieceOffset = index - _pieceStarts[pieceIndex];
            return _table.GetPieceSpan(_pieces[pieceIndex])[pieceOffset];
        }
    }

    public TextLine GetLine(int lineIndex)
    {
        if ((uint)lineIndex >= (uint)_lineStarts.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(lineIndex));
        }

        var start = _lineStarts[lineIndex];
        if (lineIndex + 1 >= _lineStarts.Length)
        {
            var length = Math.Max(0, _length - start);
            return new TextLine(lineIndex, start, length, 0);
        }

        var breakLen = (int)_lineBreakLengths[lineIndex];
        var nextStart = _lineStarts[lineIndex + 1];
        var endExclusive = Math.Max(start, nextStart - breakLen);
        var lineLength = Math.Max(0, endExclusive - start);
        return new TextLine(lineIndex, start, lineLength, breakLen);
    }

    public int GetLineIndexFromPosition(int position)
    {
        position = Math.Clamp(position, 0, _length);
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
        if (start < 0 || start > _length)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        if (destination.Length == 0)
        {
            return;
        }

        _table.CopyTo(_pieces, _length, start, destination);
    }

    public string Text
    {
        get
        {
            if (_text is not null)
            {
                return _text;
            }

            if (_length == 0)
            {
                _text = string.Empty;
                return _text;
            }

            _text = string.Create(_length, this, static (destination, snapshot) =>
            {
                snapshot.CopyTo(0, destination);
            });
            return _text;
        }
    }

    public IReadOnlyList<int> LineStarts => _lineStarts;

    private int FindPieceIndex(int position)
    {
        var index = Array.BinarySearch(_pieceStarts, position);
        if (index >= 0)
        {
            return index;
        }

        var insertion = ~index;
        return Math.Max(0, insertion - 1);
    }
}
