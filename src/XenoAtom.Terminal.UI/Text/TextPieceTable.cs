// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;

namespace XenoAtom.Terminal.UI.Text;

internal sealed class TextPieceTable
{
    private readonly string _original;
    private readonly List<TextPiece> _pieces;
    private char[] _addedBuffer;
    private int _addedLength;
    private int _length;

    public TextPieceTable(string text)
    {
        _original = text;
        _pieces = new List<TextPiece>(capacity: 4);
        _addedBuffer = Array.Empty<char>();
        _addedLength = 0;
        _length = text.Length;
        if (text.Length > 0)
        {
            _pieces.Add(new TextPiece(TextPieceSource.Original, 0, text.Length));
        }
    }

    public int Length => _length;

    public void Replace(int position, int length, ReadOnlySpan<char> text)
    {
        if (position < 0 || position > _length)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        if (length < 0 || position + length > _length)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if (length == 0 && text.IsEmpty)
        {
            return;
        }

        if (length > 0)
        {
            Remove(position, length);
        }

        if (!text.IsEmpty)
        {
            Insert(position, text);
        }
    }

    public TextSnapshot CreateSnapshot(int version)
        => new(version, this, _pieces.ToArray(), _length);

    public string GetText()
    {
        if (_length == 0)
        {
            return string.Empty;
        }

        return string.Create(_length, this, static (destination, table) =>
        {
            table.CopyToCurrent(destination);
        });
    }

    public ReadOnlySpan<char> GetPieceSpan(in TextPiece piece)
        => piece.Source == TextPieceSource.Original
            ? _original.AsSpan(piece.Start, piece.Length)
            : _addedBuffer.AsSpan(piece.Start, piece.Length);

    public void CopyTo(ReadOnlySpan<TextPiece> pieces, int pieceTotalLength, int start, Span<char> destination)
    {
        if (start < 0 || start > pieceTotalLength)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        if (destination.Length == 0)
        {
            return;
        }

        var available = pieceTotalLength - start;
        if (available <= 0)
        {
            return;
        }

        var toCopy = Math.Min(available, destination.Length);
        var pieceIndex = 0;
        var pieceOffset = start;

        while (pieceIndex < pieces.Length)
        {
            var pieceLength = pieces[pieceIndex].Length;
            if (pieceOffset < pieceLength)
            {
                break;
            }

            pieceOffset -= pieceLength;
            pieceIndex++;
        }

        var written = 0;
        while (pieceIndex < pieces.Length && written < toCopy)
        {
            var piece = pieces[pieceIndex];
            var availableInPiece = piece.Length - pieceOffset;
            if (availableInPiece > 0)
            {
                var chunk = Math.Min(availableInPiece, toCopy - written);
                GetPieceSpan(piece).Slice(pieceOffset, chunk).CopyTo(destination.Slice(written, chunk));
                written += chunk;
            }

            pieceIndex++;
            pieceOffset = 0;
        }
    }

    private void CopyToCurrent(Span<char> destination)
        => CopyTo(CollectionsMarshal.AsSpan(_pieces), _length, 0, destination);

    private void Insert(int position, ReadOnlySpan<char> text)
    {
        var index = SplitAt(position);
        var addedStart = AppendToAddedBuffer(text);
        _pieces.Insert(index, new TextPiece(TextPieceSource.Added, addedStart, text.Length));
        _length += text.Length;
        MergeAdjacentAround(index);
    }

    private void Remove(int position, int length)
    {
        if (length == 0)
        {
            return;
        }

        var startIndex = SplitAt(position);
        var endIndex = SplitAt(position + length);
        if (endIndex > startIndex)
        {
            _pieces.RemoveRange(startIndex, endIndex - startIndex);
            _length -= length;
            MergeAdjacentAround(startIndex);
        }
    }

    private int SplitAt(int position)
    {
        if (position <= 0)
        {
            return 0;
        }

        if (position >= _length)
        {
            return _pieces.Count;
        }

        var remaining = position;
        for (var i = 0; i < _pieces.Count; i++)
        {
            var piece = _pieces[i];
            if (remaining == 0)
            {
                return i;
            }

            if (remaining == piece.Length)
            {
                return i + 1;
            }

            if (remaining < piece.Length)
            {
                var left = new TextPiece(piece.Source, piece.Start, remaining);
                var right = new TextPiece(piece.Source, piece.Start + remaining, piece.Length - remaining);
                _pieces[i] = left;
                _pieces.Insert(i + 1, right);
                return i + 1;
            }

            remaining -= piece.Length;
        }

        return _pieces.Count;
    }

    private int AppendToAddedBuffer(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return _addedLength;
        }

        var start = _addedLength;
        EnsureAddedCapacity(_addedLength + text.Length);
        text.CopyTo(_addedBuffer.AsSpan(_addedLength, text.Length));
        _addedLength += text.Length;
        return start;
    }

    private void EnsureAddedCapacity(int required)
    {
        if (_addedBuffer.Length >= required)
        {
            return;
        }

        var newCapacity = _addedBuffer.Length == 0 ? 64 : _addedBuffer.Length;
        while (newCapacity < required)
        {
            newCapacity *= 2;
        }

        Array.Resize(ref _addedBuffer, newCapacity);
    }

    private void MergeAdjacentAround(int index)
    {
        if (_pieces.Count < 2)
        {
            return;
        }

        var current = Math.Clamp(index, 0, _pieces.Count - 1);
        if (current > 0)
        {
            current--;
        }

        while (current < _pieces.Count - 1)
        {
            var left = _pieces[current];
            var right = _pieces[current + 1];
            if (!CanMerge(left, right))
            {
                current++;
                continue;
            }

            _pieces[current] = new TextPiece(left.Source, left.Start, left.Length + right.Length);
            _pieces.RemoveAt(current + 1);
            if (current > 0)
            {
                current--;
            }
        }
    }

    private static bool CanMerge(in TextPiece left, in TextPiece right)
        => left.Source == right.Source && left.Start + left.Length == right.Start;
}

internal enum TextPieceSource : byte
{
    Original = 0,
    Added = 1,
}

internal readonly record struct TextPiece(TextPieceSource Source, int Start, int Length);
