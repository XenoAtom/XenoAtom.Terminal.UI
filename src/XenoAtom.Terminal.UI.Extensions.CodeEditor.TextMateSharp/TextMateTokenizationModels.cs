// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;

internal readonly record struct TextMateTokenizedSegment(int Start, int End, int Metadata);

internal sealed class TextMateTokenizedLine
{
    public static readonly TextMateTokenizedLine Empty = new(Array.Empty<TextMateTokenizedSegment>());

    public TextMateTokenizedLine(TextMateTokenizedSegment[] segments)
    {
        Segments = segments;
    }

    public TextMateTokenizedSegment[] Segments { get; }

    public static TextMateTokenizedLine Create(int contentLength, int[] binaryTokens)
    {
        ArgumentNullException.ThrowIfNull(binaryTokens);

        if (contentLength <= 0 || binaryTokens.Length < 2)
        {
            return Empty;
        }

        var pairCount = binaryTokens.Length / 2;
        var segments = new List<TextMateTokenizedSegment>(pairCount);
        for (var pairIndex = 0; pairIndex < pairCount; pairIndex++)
        {
            var tokenIndex = pairIndex * 2;
            var start = Math.Clamp(binaryTokens[tokenIndex], 0, contentLength);
            var end = pairIndex + 1 < pairCount
                ? Math.Clamp(binaryTokens[tokenIndex + 2], 0, contentLength)
                : contentLength;
            if (end <= start)
            {
                continue;
            }

            segments.Add(new TextMateTokenizedSegment(start, end, binaryTokens[tokenIndex + 1]));
        }

        return segments.Count == 0 ? Empty : new TextMateTokenizedLine(segments.ToArray());
    }

    public static TextMateTokenizedLine ShiftForIntraLineEdit(TextMateTokenizedLine source, int changeStart, int removedLength, int insertedLength, int newContentLength)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Segments.Length == 0 || newContentLength <= 0)
        {
            return Empty;
        }

        changeStart = Math.Clamp(changeStart, 0, newContentLength);
        removedLength = Math.Max(0, removedLength);
        insertedLength = Math.Max(0, insertedLength);

        var oldChangeEnd = changeStart + removedLength;
        var delta = insertedLength - removedLength;
        var rebuilt = new List<TextMateTokenizedSegment>(source.Segments.Length + 2);
        var insertedMetadata = -1;

        for (var i = 0; i < source.Segments.Length; i++)
        {
            var segment = source.Segments[i];
            if (segment.End <= changeStart)
            {
                AddSegment(rebuilt, segment.Start, segment.End, segment.Metadata, newContentLength);
                if (segment.End == changeStart)
                {
                    insertedMetadata = segment.Metadata;
                }

                continue;
            }

            if (segment.Start >= oldChangeEnd)
            {
                AddSegment(rebuilt, segment.Start + delta, segment.End + delta, segment.Metadata, newContentLength);
                continue;
            }

            if (segment.Start < changeStart)
            {
                AddSegment(rebuilt, segment.Start, changeStart, segment.Metadata, newContentLength);
                insertedMetadata = segment.Metadata;
            }
            else if (insertedMetadata < 0)
            {
                insertedMetadata = segment.Metadata;
            }

            if (segment.End > oldChangeEnd)
            {
                AddSegment(rebuilt, changeStart + insertedLength, segment.End + delta, segment.Metadata, newContentLength);
            }
        }

        if (insertedLength > 0)
        {
            if (insertedMetadata < 0 && rebuilt.Count > 0)
            {
                insertedMetadata = rebuilt[^1].Metadata;
            }

            if (insertedMetadata >= 0)
            {
                AddSegment(rebuilt, changeStart, changeStart + insertedLength, insertedMetadata, newContentLength);
            }
        }

        return rebuilt.Count == 0 ? Empty : new TextMateTokenizedLine(rebuilt.ToArray());
    }

    public static bool TryMapLineBreakInsertion(
        TextMateTokenizedLine source,
        TextLine oldLine,
        ITextSnapshot newSnapshot,
        TextDocumentChangedEventArgs change,
        out int newLineIndex,
        out TextMateTokenizedLine mappedLine)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(newSnapshot);
        ArgumentNullException.ThrowIfNull(change);

        newLineIndex = -1;
        mappedLine = Empty;

        if (change.RemovedLength != 0
            || string.IsNullOrEmpty(change.InsertedTextHint)
            || !ContainsLineBreak(change.InsertedTextHint))
        {
            return false;
        }

        var changeStartInLine = change.Position - oldLine.Start;
        if (changeStartInLine < 0 || changeStartInLine > oldLine.Length)
        {
            return false;
        }

        var lineIndex = newSnapshot.GetLineIndexFromPosition(Math.Clamp(change.Position, 0, newSnapshot.Length));
        if ((uint)lineIndex >= (uint)newSnapshot.LineCount)
        {
            return false;
        }

        var newLine = newSnapshot.GetLine(lineIndex);
        if (newLine.Start != oldLine.Start || newLine.Length != changeStartInLine)
        {
            return false;
        }

        newLineIndex = lineIndex;
        mappedLine = Slice(source, 0, changeStartInLine);
        return true;
    }

    public static TextMateTokenizedLine Slice(TextMateTokenizedLine source, int start, int length)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (length <= 0 || source.Segments.Length == 0)
        {
            return Empty;
        }

        start = Math.Max(0, start);
        var end = start + length;
        var rebuilt = new List<TextMateTokenizedSegment>(source.Segments.Length);
        for (var i = 0; i < source.Segments.Length; i++)
        {
            var segment = source.Segments[i];
            var segmentStart = Math.Max(segment.Start, start);
            var segmentEnd = Math.Min(segment.End, end);
            if (segmentEnd <= segmentStart)
            {
                continue;
            }

            AddSegment(rebuilt, segmentStart - start, segmentEnd - start, segment.Metadata, length);
        }

        return rebuilt.Count == 0 ? Empty : new TextMateTokenizedLine(rebuilt.ToArray());
    }

    private static bool ContainsLineBreak(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is '\r' or '\n')
            {
                return true;
            }
        }

        return false;
    }

    private static void AddSegment(List<TextMateTokenizedSegment> segments, int start, int end, int metadata, int contentLength)
    {
        start = Math.Clamp(start, 0, contentLength);
        end = Math.Clamp(end, 0, contentLength);
        if (end <= start)
        {
            return;
        }

        if (segments.Count > 0)
        {
            var previous = segments[^1];
            if (previous.End == start && previous.Metadata == metadata)
            {
                segments[^1] = previous with { End = end };
                return;
            }
        }

        segments.Add(new TextMateTokenizedSegment(start, end, metadata));
    }
}

internal static class TextMateRunBuilder
{
    public static void AddStyledRuns(List<StyledRun> destination, int baseOffset, TextMateTokenizedSegment[] segments, TextMateThemePalette palette)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(palette);

        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            var length = segment.End - segment.Start;
            if (length <= 0)
            {
                continue;
            }

            var style = palette.GetStyle(segment.Metadata);
            if (style == Style.None)
            {
                continue;
            }

            var start = baseOffset + segment.Start;
            if (destination.Count > 0)
            {
                var previous = destination[^1];
                if (previous.Start + previous.Length == start && previous.Style == style)
                {
                    destination[^1] = new StyledRun(previous.Start, previous.Length + length, previous.Style);
                    continue;
                }
            }

            destination.Add(new StyledRun(start, length, style));
        }
    }
}
