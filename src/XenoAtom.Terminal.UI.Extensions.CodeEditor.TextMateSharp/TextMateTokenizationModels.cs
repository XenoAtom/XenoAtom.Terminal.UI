// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using TextMateSharp.Grammars;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;

internal readonly record struct TextMateTokenizedSegment(int Start, int End, string[] Scopes, string ScopeKey);

internal sealed class TextMateTokenizedLine
{
    public static readonly TextMateTokenizedLine Empty = new(Array.Empty<TextMateTokenizedSegment>());

    public TextMateTokenizedLine(TextMateTokenizedSegment[] segments)
    {
        Segments = segments;
    }

    public TextMateTokenizedSegment[] Segments { get; }

    public static TextMateTokenizedLine Create(int contentLength, IToken[] tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        if (contentLength <= 0)
        {
            return Empty;
        }

        if (tokens.Length == 0)
        {
            return new TextMateTokenizedLine(Array.Empty<TextMateTokenizedSegment>());
        }

        var segments = new List<TextMateTokenizedSegment>(tokens.Length);
        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index];
            var start = Math.Clamp(token.StartIndex, 0, contentLength);
            var end = index + 1 < tokens.Length
                ? Math.Clamp(tokens[index + 1].StartIndex, 0, contentLength)
                : contentLength;
            if (end <= start)
            {
                continue;
            }

            var scopes = token.Scopes.ToArray();
            segments.Add(new TextMateTokenizedSegment(start, end, scopes, CreateScopeKey(scopes)));
        }

        return segments.Count == 0 ? Empty : new TextMateTokenizedLine(segments.ToArray());
    }

    private static string CreateScopeKey(string[] scopes)
    {
        if (scopes.Length == 0)
        {
            return string.Empty;
        }

        return string.Join('\u001f', scopes);
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

            var style = palette.GetStyle(segment);
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
