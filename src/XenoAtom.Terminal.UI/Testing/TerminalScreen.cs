// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Testing;

/// <summary>
/// A read-only view of the last frame rendered by a <see cref="TerminalApp"/>, cell by cell, read from the app's cell buffer
/// rather than re-parsed from its ANSI output.
/// </summary>
/// <remarks>
/// <para>
/// The view always reflects the latest frame: read it after a <see cref="TerminalAppDriver.Tick()"/> or
/// <see cref="TerminalAppDriver.Render"/>. Coordinates are zero-based cells; <c>x</c> is the column and <c>y</c> the row.
/// Members must be called on the app's UI thread.
/// </para>
/// <para>
/// It exposes the text and style of each cell only: hyperlinks are not exposed, and the terminal cursor and the debug overlay
/// (composited into the terminal output, not into the frame) are not part of it. With
/// <see cref="Hosting.TerminalHostKind.Inline"/> it covers the app's live region only, not the flow output written above it or
/// the rest of the terminal.
/// </para>
/// </remarks>
public sealed class TerminalScreen
{
    private readonly TerminalApp _app;

    internal TerminalScreen(TerminalApp app)
    {
        _app = app;
    }

    /// <summary>
    /// Gets the width of the last frame, in cells.
    /// </summary>
    /// <exception cref="InvalidOperationException">No frame has been rendered yet.</exception>
    public int Width => GetBuffer().Width;

    /// <summary>
    /// Gets the height of the last frame, in cells.
    /// </summary>
    /// <exception cref="InvalidOperationException">No frame has been rendered yet.</exception>
    public int Height => GetBuffer().Height;

    /// <summary>
    /// Gets the cell at the given position in the last frame.
    /// </summary>
    /// <param name="x">The column, from 0.</param>
    /// <param name="y">The row, from 0.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="x"/> or <paramref name="y"/> is outside the frame.</exception>
    /// <exception cref="InvalidOperationException">No frame has been rendered yet.</exception>
    public TerminalCell this[int x, int y]
    {
        get
        {
            var buffer = GetBuffer();
            ArgumentOutOfRangeException.ThrowIfNegative(x);
            ArgumentOutOfRangeException.ThrowIfNegative(y);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, buffer.Width);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, buffer.Height);

            var index = (y * buffer.Width) + x;
            var style = buffer.UnsafeCells[index];
            if (style.IsContinuation)
            {
                return new TerminalCell(" ", style.WithoutContinuation(), IsWideContinuation: true);
            }

            return new TerminalCell(GetCellText(buffer, buffer.UnsafeScalars[index]), style, IsWideContinuation: false);
        }
    }

    /// <summary>
    /// Gets the text of one row of the last frame. Trailing spaces are kept.
    /// </summary>
    /// <param name="y">The row, from 0.</param>
    /// <remarks>
    /// The string's length is not the number of cells: a glyph two cells wide contributes its text once, and a grapheme (an
    /// emoji sequence, a flag, a character outside the Basic Multilingual Plane) can be several UTF-16 characters. Use the
    /// indexer for cell-exact positions.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="y"/> is outside the frame.</exception>
    /// <exception cref="InvalidOperationException">No frame has been rendered yet.</exception>
    public string GetLine(int y)
    {
        var buffer = GetBuffer();
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, buffer.Height);

        var builder = new StringBuilder(buffer.Width);
        AppendLine(buffer, y, builder);
        return builder.ToString();
    }

    /// <summary>
    /// Gets the text of the last frame: the rows joined by <c>'\n'</c>, trailing spaces kept.
    /// </summary>
    /// <exception cref="InvalidOperationException">No frame has been rendered yet.</exception>
    public string GetText()
    {
        var buffer = GetBuffer();
        var builder = new StringBuilder((buffer.Width + 1) * buffer.Height);
        for (var y = 0; y < buffer.Height; y++)
        {
            if (y > 0)
            {
                builder.Append('\n');
            }

            AppendLine(buffer, y, builder);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Returns the text of the last frame (see <see cref="GetText"/>), or a placeholder when no frame was rendered yet or when
    /// called outside of the app's UI thread (for example by a debugger). Never throws.
    /// </summary>
    /// <returns>The text of the last frame, or a placeholder.</returns>
    public override string ToString()
    {
        if (!_app.CheckAccess())
        {
            return "<TerminalScreen: read it on the app's UI thread>";
        }

        return _app.LastRenderBuffer is null ? "<no frame rendered>" : GetText();
    }

    private static void AppendLine(CellBuffer buffer, int y, StringBuilder builder)
    {
        var scalars = buffer.UnsafeScalars;
        var cells = buffer.UnsafeCells;
        var rowStart = y * buffer.Width;
        for (var x = 0; x < buffer.Width; x++)
        {
            var index = rowStart + x;
            if (cells[index].IsContinuation)
            {
                continue;
            }

            builder.Append(GetCellText(buffer, scalars[index]));
        }
    }

    private static string GetCellText(CellBuffer buffer, int scalar)
    {
        if (scalar == 0 || scalar == ' ')
        {
            return " ";
        }

        if (scalar < 0)
        {
            return buffer.TryGetTextElement(scalar, out var text, out _) ? text : " ";
        }

        return new Rune(scalar).ToString();
    }

    private CellBuffer GetBuffer()
    {
        _app.VerifyAccess();
        return _app.LastRenderBuffer ?? throw new InvalidOperationException("No frame has been rendered yet. Call Tick() or Render() on the driver first.");
    }
}
