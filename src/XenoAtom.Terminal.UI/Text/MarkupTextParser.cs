// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Text;

/// <summary>
/// Represents a styled span of text produced by parsing markup.
/// </summary>
/// <param name="Start">The start index (UTF-16) within the plain text.</param>
/// <param name="Length">The length (UTF-16) of the span.</param>
/// <param name="Style">The style to apply to this span.</param>
public readonly record struct StyledRun(int Start, int Length, Style Style);

/// <summary>
/// Parses ANSI markup into plain text plus <see cref="StyledRun"/> style runs.
/// </summary>
/// <remarks>
/// <para>
/// This parser is useful when you need to render markup efficiently without allocating a visual per line. It produces:
/// </para>
/// <list type="bullet">
/// <item><description>The plain text output (with markup stripped).</description></item>
/// <item><description>A set of style runs, each describing a span in the plain text.</description></item>
/// </list>
/// <para>
/// Instances are reusable and keep internal buffers to minimize allocations. They are not thread-safe.
/// </para>
/// </remarks>
public sealed class MarkupTextParser
{
    private readonly MarkupCaptureWriter _writer;
    private readonly AnsiMarkup _markup;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkupTextParser"/> class.
    /// </summary>
    public MarkupTextParser()
    {
        _writer = new MarkupCaptureWriter();
        _markup = new AnsiMarkup(_writer);
    }

    /// <summary>
    /// Parses the specified markup text into plain text and style runs.
    /// </summary>
    /// <param name="markup">The markup input. If <see langword="null"/>, it is treated as an empty string.</param>
    /// <param name="runs">Receives the style runs describing spans in the returned plain text.</param>
    /// <returns>The plain text produced by stripping markup from the input.</returns>
    public string Parse(string? markup, out StyledRun[] runs)
    {
        _writer.Reset();
        _markup.Write(markup ?? string.Empty);
        return _writer.GetTextAndRuns(out runs);
    }

    private sealed class MarkupCaptureWriter : IAnsiBasicWriter
    {
        private readonly StringBuilder _buffer;
        private readonly List<StyledRun> _runs;
        private AnsiStyle _style;

        public MarkupCaptureWriter()
        {
            _buffer = new StringBuilder(256);
            _runs = new List<StyledRun>(16);
            _style = AnsiStyle.Default;
            Capabilities = AnsiCapabilities.Default;
        }

        public AnsiCapabilities Capabilities { get; }

        public void Reset()
        {
            _buffer.Clear();
            _runs.Clear();
            _style = AnsiStyle.Default;
        }

        public string GetTextAndRuns(out StyledRun[] runs)
        {
            runs = _runs.Count == 0 ? Array.Empty<StyledRun>() : _runs.ToArray();
            return _buffer.ToString();
        }

        public void Write(ReadOnlySpan<char> text)
        {
            if (text.IsEmpty)
            {
                return;
            }

            var start = _buffer.Length;
            _buffer.Append(text);

            var runStyle = ConvertStyle(_style);
            if (_runs.Count > 0)
            {
                var last = _runs[_runs.Count - 1];
                if (last.Style == runStyle && last.Start + last.Length == start)
                {
                    _runs[_runs.Count - 1] = last with { Length = last.Length + text.Length };
                    return;
                }
            }

            _runs.Add(new StyledRun(start, text.Length, runStyle));
        }

        public void StyleTransition(AnsiStyle from, AnsiStyle to)
        {
            _style = to.ResolveMissingFrom(from);
        }

        private static Style ConvertStyle(AnsiStyle style)
        {
            var cellStyle = Style.None;
            if (style.Foreground is { } fg)
            {
                cellStyle = cellStyle.WithForeground(fg);
            }

            if (style.Background is { } bg)
            {
                cellStyle = cellStyle.WithBackground(bg);
            }

            if (style.Decorations != AnsiDecorations.None)
            {
                cellStyle = cellStyle.AddTextStyle((TextStyle)((int)style.Decorations));
            }

            return cellStyle;
        }
    }
}
