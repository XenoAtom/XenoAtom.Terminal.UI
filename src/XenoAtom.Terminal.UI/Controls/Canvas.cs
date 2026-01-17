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
    /// <summary>
    /// Initializes a new instance of the <see cref="Canvas"/> class.
    /// </summary>
    public Canvas()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
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

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        => SizeHints.Flex(
            min: Size.Zero,
            natural: Size.Zero,
            max: new Size(LayoutConstants.Infinite, LayoutConstants.Infinite),
            growX: HorizontalAlignment == HorizontalAlignment.Stretch ? 1 : 0,
            growY: VerticalAlignment == VerticalAlignment.Stretch ? 1 : 0,
            shrinkX: 1,
            shrinkY: 1);

    /// <inheritdoc/>
    protected override void ArrangeCore(in Rectangle finalRect) => Bounds = finalRect;

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
        var style = Get<CanvasStyle>();
        var ctx = new CanvasContext(buffer, rect, style.ResolveDefaultStyle(theme), style.DefaultRune);
        painter(ctx);
    }
}

/// <summary>
/// A drawing context for <see cref="Canvas"/> operations.
/// </summary>
public readonly struct CanvasContext
{
    private readonly CellBuffer _buffer;
    private readonly Rectangle _bounds;
    private readonly CellStyle _defaultStyle;
    private readonly Rune _defaultRune;

    internal CanvasContext(CellBuffer buffer, Rectangle bounds, CellStyle defaultStyle, Rune defaultRune)
    {
        _buffer = buffer;
        _bounds = bounds;
        _defaultStyle = defaultStyle;
        _defaultRune = defaultRune;
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
    public void Clear(Rune rune, CellStyle style)
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
    public void SetPixel(int x, int y, Rune rune, CellStyle style)
    {
        if ((uint)x >= (uint)_bounds.Width || (uint)y >= (uint)_bounds.Height)
        {
            return;
        }

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
    public void SetPixel(int x, int y, CellStyle style)
        => SetPixel(x, y, _defaultRune, style);

    /// <summary>
    /// Sets a single cell using the default rune and style.
    /// </summary>
    public void SetPixel(int x, int y)
        => SetPixel(x, y, _defaultRune, _defaultStyle);

    /// <summary>
    /// Draws a horizontal line.
    /// </summary>
    public void DrawHLine(int x, int y, int length, Rune rune, CellStyle style)
    {
        if (length <= 0)
        {
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
    public void DrawHLine(int x, int y, int length, CellStyle style) => DrawHLine(x, y, length, _defaultRune, style);

    /// <summary>
    /// Draws a horizontal line.
    /// </summary>
    public void DrawHLine(int x, int y, int length) => DrawHLine(x, y, length, _defaultRune, _defaultStyle);

    /// <summary>
    /// Draws a vertical line.
    /// </summary>
    public void DrawVLine(int x, int y, int length, Rune rune, CellStyle style)
    {
        if (length <= 0)
        {
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
    public void DrawVLine(int x, int y, int length, CellStyle style) => DrawVLine(x, y, length, _defaultRune, style);

    /// <summary>
    /// Draws a vertical line.
    /// </summary>
    public void DrawVLine(int x, int y, int length) => DrawVLine(x, y, length, _defaultRune, _defaultStyle);

    /// <summary>
    /// Draws a line between two points using an integer Bresenham algorithm.
    /// </summary>
    public void DrawLine(int x0, int y0, int x1, int y1, Rune rune, CellStyle style)
    {
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
    public void DrawLine(int x0, int y0, int x1, int y1, CellStyle style) => DrawLine(x0, y0, x1, y1, _defaultRune, style);

    /// <summary>
    /// Draws a line between two points using an integer Bresenham algorithm.
    /// </summary>
    public void DrawLine(int x0, int y0, int x1, int y1) => DrawLine(x0, y0, x1, y1, _defaultRune, _defaultStyle);

    /// <summary>
    /// Draws an outline rectangle.
    /// </summary>
    public void DrawRect(int x, int y, int width, int height, Rune rune, CellStyle style)
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
    public void DrawRect(int x, int y, int width, int height, CellStyle style) => DrawRect(x, y, width, height, _defaultRune, style);

    /// <summary>
    /// Draws an outline rectangle.
    /// </summary>
    public void DrawRect(int x, int y, int width, int height) => DrawRect(x, y, width, height, _defaultRune, _defaultStyle);

    /// <summary>
    /// Draws a box rectangle using line glyphs (corners + edges).
    /// </summary>
    public void DrawBox(int x, int y, int width, int height, LineGlyphs glyphs, CellStyle style)
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
    public void DrawBox(int x, int y, int width, int height, CellStyle style) => DrawBox(x, y, width, height, LineGlyphs.Single, style);

    /// <summary>
    /// Draws a box rectangle using line glyphs (corners + edges).
    /// </summary>
    public void DrawBox(int x, int y, int width, int height) => DrawBox(x, y, width, height, LineGlyphs.Single, _defaultStyle);

    /// <summary>
    /// Fills a rectangle.
    /// </summary>
    public void FillRect(int x, int y, int width, int height, Rune rune, CellStyle style)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        for (var yy = 0; yy < height; yy++)
        {
            for (var xx = 0; xx < width; xx++)
            {
                SetPixel(x + xx, y + yy, rune, style);
            }
        }
    }

    /// <summary>
    /// Fills a rectangle.
    /// </summary>
    public void FillRect(int x, int y, int width, int height, CellStyle style) => FillRect(x, y, width, height, _defaultRune, style);

    /// <summary>
    /// Fills a rectangle.
    /// </summary>
    public void FillRect(int x, int y, int width, int height) => FillRect(x, y, width, height, _defaultRune, _defaultStyle);

    /// <summary>
    /// Draws a circle outline using the midpoint circle algorithm.
    /// </summary>
    public void DrawCircle(int centerX, int centerY, int radius, Rune rune, CellStyle style)
    {
        if (radius < 0)
        {
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
    public void DrawCircle(int centerX, int centerY, int radius, CellStyle style) => DrawCircle(centerX, centerY, radius, _defaultRune, style);


    /// <summary>
    /// Draws a circle outline using the midpoint circle algorithm.
    /// </summary>
    public void DrawCircle(int centerX, int centerY, int radius) => DrawCircle(centerX, centerY, radius, _defaultRune, _defaultStyle);

    /// <summary>
    /// Writes plain text at the specified position (clipped to the canvas bounds).
    /// </summary>
    public void WriteText(int x, int y, ReadOnlySpan<char> text, CellStyle style)
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

        _buffer.WriteText(_bounds.X + x, _bounds.Y + y, slice, style);
    }


    /// <summary>
    /// Writes plain text at the specified position using the default style.
    /// </summary>
    public void WriteText(int x, int y, ReadOnlySpan<char> text)
        => WriteText(x, y, text, _defaultStyle);

    private void PlotCircle8(int cx, int cy, int x, int y, Rune rune, CellStyle style)
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
}
