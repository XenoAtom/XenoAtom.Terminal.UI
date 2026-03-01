// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// A lightweight immediate-mode drawing surface for cell-based terminal graphics.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Canvas"/> is designed for quick visualization and custom rendering (plots, mini-maps, sparklines,
/// diagram-like widgets). It does not allocate a backing buffer; instead, it invokes <see cref="Painter"/> during
/// rendering so you can draw directly into the current <see cref="CellBuffer"/>.
/// </para>
/// <para>
/// For dynamic content, bind <see cref="Painter"/> using a lambda/state so it will automatically re-render when
/// referenced bindings change.
/// </para>
/// </remarks>
public sealed partial class Canvas : Visual
{
    private byte[]? _finePixelMask;
    private int _finePixelMaskWidth;
    private int _finePixelMaskHeight;

    /// <summary>
    /// Initializes a new instance of the <see cref="Canvas"/> class.
    /// </summary>
    public Canvas()
    {
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Canvas"/> class with a drawing callback.
    /// </summary>
    /// <param name="painter">The drawing callback executed during render.</param>
    public Canvas(Action<CanvasContext> painter) : this()
    {
        this.Painter(painter);
    }

    /// <summary>
    /// Gets or sets the drawing callback executed during render.
    /// </summary>
    /// <remarks>
    /// The callback is invoked after the framework has established clipping to <see cref="Visual.Bounds"/>.
    /// Use <see cref="CanvasContext.Bounds"/> and coordinates relative to the canvas origin (0,0).
    /// </remarks>
    [Bindable]
    public partial Delegator<Action<CanvasContext>> Painter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether canvas primitives should render using a fine (sub-cell) dot grid.
    /// </summary>
    /// <remarks>
    /// When enabled, primitives that use the default rune (e.g. line/circle overloads without an explicit rune) are
    /// rasterized using a 2×4 dot grid per cell and emitted as Unicode dot-pattern glyphs.
    /// Cell-based operations such as <see cref="CanvasContext.FillRect(int,int,int,int,Rune,Style)"/> and
    /// <see cref="CanvasContext.WriteText(int,int,ReadOnlySpan{char},Style)"/>
    /// remain cell-based.
    /// </remarks>
    [Bindable]
    public partial bool UseFinePixels { get; set; }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        => SizeHints.Flex(
            min: Size.Zero,
            natural: Size.Zero,
            max: new Size(LayoutConstants.Infinite, LayoutConstants.Infinite),
            growX: HorizontalAlignment == Align.Stretch ? 1 : 0,
            growY: VerticalAlignment == Align.Stretch ? 1 : 0,
            shrinkX: 1,
            shrinkY: 1);

    /// <inheritdoc/>
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var painter = Painter.Invoke;
        if (painter is null)
        {
            return;
        }

        var theme = GetTheme();
        var style = GetStyle<CanvasStyle>();
        var useFinePixels = UseFinePixels;
        byte[]? finePixelMask = null;
        if (useFinePixels)
        {
            finePixelMask = EnsureFinePixelMask(rect.Width, rect.Height);
            Array.Clear(finePixelMask);
        }

        var ctx = new CanvasContext(buffer, rect, style.ResolveDefaultStyle(theme), style.DefaultRune, finePixelMask);
        painter(ctx);
    }

    private byte[] EnsureFinePixelMask(int width, int height)
    {
        width = Math.Max(0, width);
        height = Math.Max(0, height);
        if (_finePixelMask is null || _finePixelMaskWidth != width || _finePixelMaskHeight != height)
        {
            _finePixelMask = width <= 0 || height <= 0 ? Array.Empty<byte>() : new byte[width * height];
            _finePixelMaskWidth = width;
            _finePixelMaskHeight = height;
        }

        return _finePixelMask;
    }
}

/// <summary>
/// A drawing context for <see cref="Canvas"/> operations.
/// </summary>
public readonly struct CanvasContext
{
    private readonly CellBuffer _buffer;
    private readonly Rectangle _bounds;
    private readonly Style _defaultStyle;
    private readonly Rune _defaultRune;
    private readonly byte[]? _finePixelMask;

    internal CanvasContext(CellBuffer buffer, Rectangle bounds, Style defaultStyle, Rune defaultRune, byte[]? finePixelMask)
    {
        _buffer = buffer;
        _bounds = bounds;
        _defaultStyle = defaultStyle;
        _defaultRune = defaultRune;
        _finePixelMask = finePixelMask;
    }

    /// <summary>
    /// Gets the canvas bounds in UI coordinates.
    /// </summary>
    public Rectangle Bounds => _bounds;

    /// <summary>
    /// Gets the canvas size in cells.
    /// </summary>
    public Size Size => new(_bounds.Width, _bounds.Height);

    /// <summary>
    /// Clears the canvas using the specified rune and style.
    /// </summary>
    /// <param name="rune">The rune used to fill.</param>
    /// <param name="style">The style used to fill.</param>
    public void Clear(Rune rune, Style style)
        => FillRect(0, 0, _bounds.Width, _bounds.Height, rune, style);

    /// <summary>
    /// Clears the canvas using the default rune and style.
    /// </summary>
    public void Clear()
        => Clear(_defaultRune, _defaultStyle);

    /// <summary>
    /// Sets a single cell at the specified canvas coordinates.
    /// </summary>
    /// <param name="x">The X coordinate relative to the canvas origin.</param>
    /// <param name="y">The Y coordinate relative to the canvas origin.</param>
    /// <param name="rune">The rune to draw.</param>
    /// <param name="style">The style to apply.</param>
    public void SetPixel(int x, int y, Rune rune, Style style)
    {
        if ((uint)x >= (uint)_bounds.Width || (uint)y >= (uint)_bounds.Height)
        {
            return;
        }

        if (IsFineRasterCandidate(rune))
        {
            SetFinePixelCenter(x, y, style);
            return;
        }

        ClearFineCell(x, y);
        _buffer.SetCell(_bounds.X + x, _bounds.Y + y, rune, style);
    }

    /// <summary>
    /// Sets a single cell using the default style.
    /// </summary>
    public void SetPixel(int x, int y, Rune rune)
        => SetPixel(x, y, rune, _defaultStyle);

    /// <summary>
    /// Sets a single cell using the default rune.
    /// </summary>
    public void SetPixel(int x, int y, Style style)
        => SetPixel(x, y, _defaultRune, style);

    /// <summary>
    /// Sets a single cell using the default rune and style.
    /// </summary>
    public void SetPixel(int x, int y)
        => SetPixel(x, y, _defaultRune, _defaultStyle);

    /// <summary>
    /// Draws a horizontal line.
    /// </summary>
    public void DrawHLine(int x, int y, int length, Rune rune, Style style)
    {
        if (length <= 0)
        {
            return;
        }

        if (IsFineRasterCandidate(rune))
        {
            DrawFineLine(x, y, x + length - 1, y, style);
            return;
        }

        for (var i = 0; i < length; i++)
        {
            SetPixel(x + i, y, rune, style);
        }
    }

    /// <summary>
    /// Draws a horizontal line.
    /// </summary>
    public void DrawHLine(int x, int y, int length, Style style) => DrawHLine(x, y, length, _defaultRune, style);

    /// <summary>
    /// Draws a horizontal line.
    /// </summary>
    public void DrawHLine(int x, int y, int length) => DrawHLine(x, y, length, _defaultRune, _defaultStyle);

    /// <summary>
    /// Draws a vertical line.
    /// </summary>
    public void DrawVLine(int x, int y, int length, Rune rune, Style style)
    {
        if (length <= 0)
        {
            return;
        }

        if (IsFineRasterCandidate(rune))
        {
            DrawFineLine(x, y, x, y + length - 1, style);
            return;
        }

        for (var i = 0; i < length; i++)
        {
            SetPixel(x, y + i, rune, style);
        }
    }

    /// <summary>
    /// Draws a vertical line.
    /// </summary>
    public void DrawVLine(int x, int y, int length, Style style) => DrawVLine(x, y, length, _defaultRune, style);

    /// <summary>
    /// Draws a vertical line.
    /// </summary>
    public void DrawVLine(int x, int y, int length) => DrawVLine(x, y, length, _defaultRune, _defaultStyle);

    /// <summary>
    /// Draws a line between two points using an integer Bresenham algorithm.
    /// </summary>
    public void DrawLine(int x0, int y0, int x1, int y1, Rune rune, Style style)
    {
        if (IsFineRasterCandidate(rune))
        {
            DrawFineLine(x0, y0, x1, y1, style);
            return;
        }

        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var err = dx + dy;

        while (true)
        {
            SetPixel(x0, y0, rune, style);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }
            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    /// <summary>
    /// Draws a line between two points using an integer Bresenham algorithm.
    /// </summary>
    public void DrawLine(int x0, int y0, int x1, int y1, Style style) => DrawLine(x0, y0, x1, y1, _defaultRune, style);

    /// <summary>
    /// Draws a line between two points using an integer Bresenham algorithm.
    /// </summary>
    public void DrawLine(int x0, int y0, int x1, int y1) => DrawLine(x0, y0, x1, y1, _defaultRune, _defaultStyle);

    /// <summary>
    /// Draws an outline rectangle.
    /// </summary>
    public void DrawRect(int x, int y, int width, int height, Rune rune, Style style)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (height == 1)
        {
            DrawHLine(x, y, width, rune, style);
            return;
        }

        if (width == 1)
        {
            DrawVLine(x, y, height, rune, style);
            return;
        }

        DrawHLine(x, y, width, rune, style);
        DrawHLine(x, y + height - 1, width, rune, style);
        DrawVLine(x, y + 1, height - 2, rune, style);
        DrawVLine(x + width - 1, y + 1, height - 2, rune, style);
    }

    /// <summary>
    /// Draws an outline rectangle.
    /// </summary>
    public void DrawRect(int x, int y, int width, int height, Style style) => DrawRect(x, y, width, height, _defaultRune, style);

    /// <summary>
    /// Draws an outline rectangle.
    /// </summary>
    public void DrawRect(int x, int y, int width, int height) => DrawRect(x, y, width, height, _defaultRune, _defaultStyle);

    /// <summary>
    /// Draws a box rectangle using line glyphs (corners + edges).
    /// </summary>
    public void DrawBox(int x, int y, int width, int height, LineGlyphs glyphs, Style style)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (width == 1 && height == 1)
        {
            SetPixel(x, y, glyphs.TopLeft, style);
            return;
        }

        if (height == 1)
        {
            SetPixel(x, y, glyphs.TopLeft, style);
            if (width > 1)
            {
                DrawHLine(x + 1, y, width - 2, glyphs.Horizontal, style);
                SetPixel(x + width - 1, y, glyphs.TopRight, style);
            }
            return;
        }

        if (width == 1)
        {
            SetPixel(x, y, glyphs.TopLeft, style);
            DrawVLine(x, y + 1, height - 2, glyphs.Vertical, style);
            SetPixel(x, y + height - 1, glyphs.BottomLeft, style);
            return;
        }

        SetPixel(x, y, glyphs.TopLeft, style);
        DrawHLine(x + 1, y, width - 2, glyphs.Horizontal, style);
        SetPixel(x + width - 1, y, glyphs.TopRight, style);

        DrawVLine(x, y + 1, height - 2, glyphs.Vertical, style);
        DrawVLine(x + width - 1, y + 1, height - 2, glyphs.Vertical, style);

        SetPixel(x, y + height - 1, glyphs.BottomLeft, style);
        DrawHLine(x + 1, y + height - 1, width - 2, glyphs.Horizontal, style);
        SetPixel(x + width - 1, y + height - 1, glyphs.BottomRight, style);
    }

    /// <summary>
    /// Draws a box rectangle using line glyphs (corners + edges).
    /// </summary>
    public void DrawBox(int x, int y, int width, int height, Style style) => DrawBox(x, y, width, height, LineGlyphs.Single, style);

    /// <summary>
    /// Draws a box rectangle using line glyphs (corners + edges).
    /// </summary>
    public void DrawBox(int x, int y, int width, int height) => DrawBox(x, y, width, height, LineGlyphs.Single, _defaultStyle);

    /// <summary>
    /// Fills a rectangle.
    /// </summary>
    public void FillRect(int x, int y, int width, int height, Rune rune, Style style)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        for (var yy = 0; yy < height; yy++)
        {
            for (var xx = 0; xx < width; xx++)
            {
                var px = x + xx;
                var py = y + yy;
                if ((uint)px >= (uint)_bounds.Width || (uint)py >= (uint)_bounds.Height)
                {
                    continue;
                }

                ClearFineCell(px, py);
                _buffer.SetCell(_bounds.X + px, _bounds.Y + py, rune, style);
            }
        }
    }

    /// <summary>
    /// Fills a rectangle.
    /// </summary>
    public void FillRect(int x, int y, int width, int height, Style style) => FillRect(x, y, width, height, _defaultRune, style);

    /// <summary>
    /// Fills a rectangle.
    /// </summary>
    public void FillRect(int x, int y, int width, int height) => FillRect(x, y, width, height, _defaultRune, _defaultStyle);

    /// <summary>
    /// Draws a circle outline using the midpoint circle algorithm.
    /// </summary>
    public void DrawCircle(int centerX, int centerY, int radius, Rune rune, Style style)
    {
        if (radius < 0)
        {
            return;
        }

        if (IsFineRasterCandidate(rune))
        {
            DrawFineCircle(centerX, centerY, radius, style);
            return;
        }

        var x = radius;
        var y = 0;
        var err = 1 - x;

        while (x >= y)
        {
            PlotCircle8(centerX, centerY, x, y, rune, style);
            y++;
            if (err < 0)
            {
                err += 2 * y + 1;
            }
            else
            {
                x--;
                err += 2 * (y - x) + 1;
            }
        }
    }

    /// <summary>
    /// Draws a circle outline using the midpoint circle algorithm.
    /// </summary>
    public void DrawCircle(int centerX, int centerY, int radius, Style style) => DrawCircle(centerX, centerY, radius, _defaultRune, style);


    /// <summary>
    /// Draws a circle outline using the midpoint circle algorithm.
    /// </summary>
    public void DrawCircle(int centerX, int centerY, int radius) => DrawCircle(centerX, centerY, radius, _defaultRune, _defaultStyle);

    /// <summary>
    /// Writes plain text at the specified position (clipped to the canvas bounds).
    /// </summary>
    public void WriteText(int x, int y, ReadOnlySpan<char> text, Style style)
    {
        if ((uint)y >= (uint)_bounds.Height)
        {
            return;
        }

        var maxWidth = _bounds.Width - x;
        if (maxWidth <= 0)
        {
            return;
        }

        var slice = text;
        if (slice.Length > maxWidth)
        {
            slice = slice[..maxWidth];
        }

        ClearFineSpan(x, y, GetCellWidth(slice, maxWidth));
        _buffer.WriteText(_bounds.X + x, _bounds.Y + y, slice, style);
    }


    /// <summary>
    /// Writes plain text at the specified position using the default style.
    /// </summary>
    public void WriteText(int x, int y, ReadOnlySpan<char> text)
        => WriteText(x, y, text, _defaultStyle);

    private void PlotCircle8(int cx, int cy, int x, int y, Rune rune, Style style)
    {
        SetPixel(cx + x, cy + y, rune, style);
        SetPixel(cx + y, cy + x, rune, style);
        SetPixel(cx - y, cy + x, rune, style);
        SetPixel(cx - x, cy + y, rune, style);
        SetPixel(cx - x, cy - y, rune, style);
        SetPixel(cx - y, cy - x, rune, style);
        SetPixel(cx + y, cy - x, rune, style);
        SetPixel(cx + x, cy - y, rune, style);
    }

    private bool IsFineRasterCandidate(Rune rune)
        => _finePixelMask is not null && rune.Value == _defaultRune.Value;

    private void ClearFineCell(int x, int y)
    {
        var mask = _finePixelMask;
        if (mask is null)
        {
            return;
        }

        var index = y * _bounds.Width + x;
        if ((uint)index >= (uint)mask.Length)
        {
            return;
        }

        mask[index] = 0;
    }

    private void ClearFineSpan(int x, int y, int length)
    {
        var mask = _finePixelMask;
        if (mask is null || length <= 0 || (uint)y >= (uint)_bounds.Height)
        {
            return;
        }

        var start = Math.Clamp(x, 0, _bounds.Width - 1);
        var endExclusive = Math.Clamp(x + length, 0, _bounds.Width);
        var count = endExclusive - start;
        if (count <= 0)
        {
            return;
        }

        Array.Clear(mask, y * _bounds.Width + start, count);
    }

    private void SetFinePixelCenter(int x, int y, Style style)
        => SetFineDot(x * 2 + 1, y * 4 + 2, style);

    private void SetFineDot(int dotX, int dotY, Style style)
    {
        var mask = _finePixelMask;
        if (mask is null)
        {
            return;
        }

        var width = _bounds.Width;
        var height = _bounds.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var maxDotX = width * 2;
        var maxDotY = height * 4;
        if ((uint)dotX >= (uint)maxDotX || (uint)dotY >= (uint)maxDotY)
        {
            return;
        }

        var cellX = dotX >> 1;
        var cellY = dotY >> 2;
        if ((uint)cellX >= (uint)width || (uint)cellY >= (uint)height)
        {
            return;
        }

        var localX = dotX & 1;
        var localY = dotY & 3;
        var bit = GetDotBit(localX, localY);

        var index = cellY * width + cellX;
        var next = (byte)(mask[index] | bit);
        if (next == mask[index])
        {
            return;
        }

        mask[index] = next;
        _buffer.SetCell(_bounds.X + cellX, _bounds.Y + cellY, new Rune(0x2800 + next), style);
    }

    private static byte GetDotBit(int localX, int localY)
    {
        // Dot mapping for Unicode 8-dot patterns (2×4 grid):
        // left column:  y=0..3 => 1,2,3,7
        // right column: y=0..3 => 4,5,6,8
        return (localX, localY) switch
        {
            (0, 0) => 0b0000_0001,
            (0, 1) => 0b0000_0010,
            (0, 2) => 0b0000_0100,
            (0, 3) => 0b0100_0000,
            (1, 0) => 0b0000_1000,
            (1, 1) => 0b0001_0000,
            (1, 2) => 0b0010_0000,
            (1, 3) => 0b1000_0000,
            _ => 0,
        };
    }

    private void DrawFineLine(int x0, int y0, int x1, int y1, Style style)
    {
        var dx0 = x0 * 2 + 1;
        var dy0 = y0 * 4 + 2;
        var dx1 = x1 * 2 + 1;
        var dy1 = y1 * 4 + 2;

        var dx = Math.Abs(dx1 - dx0);
        var sx = dx0 < dx1 ? 1 : -1;
        var dy = -Math.Abs(dy1 - dy0);
        var sy = dy0 < dy1 ? 1 : -1;
        var err = dx + dy;

        while (true)
        {
            SetFineDot(dx0, dy0, style);
            if (dx0 == dx1 && dy0 == dy1)
            {
                break;
            }

            var e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                dx0 += sx;
            }
            if (e2 <= dx)
            {
                err += dx;
                dy0 += sy;
            }
        }
    }

    private void DrawFineCircle(int centerX, int centerY, int radius, Style style)
    {
        if (radius == 0)
        {
            SetFinePixelCenter(centerX, centerY, style);
            return;
        }

        var cx = centerX * 2 + 1;
        var cy = centerY * 4 + 2;
        var rx = radius * 2;
        var ry = radius * 4;
        if (rx <= 0 || ry <= 0)
        {
            return;
        }

        var rxSq = (double)rx * rx;
        var rySq = (double)ry * ry;
        var twoRxSq = 2.0 * rxSq;
        var twoRySq = 2.0 * rySq;

        var x = 0;
        var y = ry;
        var dx = 0.0;
        var dy = twoRxSq * y;
        var d1 = rySq - (rxSq * ry) + (0.25 * rxSq);

        while (dx < dy)
        {
            PlotEllipse4(cx, cy, x, y, style);
            if (d1 < 0)
            {
                x++;
                dx += twoRySq;
                d1 += dx + rySq;
            }
            else
            {
                x++;
                y--;
                dx += twoRySq;
                dy -= twoRxSq;
                d1 += dx - dy + rySq;
            }
        }

        var d2 = (rySq * Math.Pow(x + 0.5, 2)) + (rxSq * Math.Pow(y - 1, 2)) - (rxSq * rySq);
        while (y >= 0)
        {
            PlotEllipse4(cx, cy, x, y, style);
            if (d2 > 0)
            {
                y--;
                dy -= twoRxSq;
                d2 += rxSq - dy;
            }
            else
            {
                y--;
                x++;
                dx += twoRySq;
                dy -= twoRxSq;
                d2 += dx - dy + rxSq;
            }
        }
    }

    private void PlotEllipse4(int cx, int cy, int x, int y, Style style)
    {
        SetFineDot(cx + x, cy + y, style);
        SetFineDot(cx - x, cy + y, style);
        SetFineDot(cx + x, cy - y, style);
        SetFineDot(cx - x, cy - y, style);
    }

    private static int GetCellWidth(ReadOnlySpan<char> text, int maxWidth)
    {
        if (text.IsEmpty)
        {
            return 0;
        }

        var width = TerminalTextUtility.GetWidth(text);
        if (width <= 0)
        {
            return 0;
        }

        return Math.Min(maxWidth, width);
    }
}
