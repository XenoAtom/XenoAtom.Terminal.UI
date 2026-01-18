// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Draws a border around a single content visual.
/// </summary>
public sealed partial class Border : Padder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Border"/> control.
    /// </summary>
    public Border()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Border"/> control.
    /// </summary>
    /// <param name="content">The border content.</param>
    public Border(Visual content)
    {
        Content = content;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Border"/> control with a dynamic content factory.
    /// </summary>
    /// <param name="contentFactory">The content factory. It is evaluated as part of dynamic updates.</param>
    public Border(Func<Visual> contentFactory)
    {
        ArgumentNullException.ThrowIfNull(contentFactory);
        this.Content(contentFactory);
    }

    /// <inheritdoc />
    protected override Thickness Inset => new(1);

    /// <inheritdoc/>
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var glyphs = theme.Lines;
        var style = theme.BorderStyle(focused: false);
        var clearStyle = CellStyle.None;

        var left = rect.X;
        var top = rect.Y;
        var right = rect.X + rect.Width - 1;
        var bottom = rect.Y + rect.Height - 1;

        // Fill background.
        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), clearStyle);
            }
        }

        buffer.SetCell(left, top, glyphs.TopLeft, style);
        buffer.SetCell(right, top, glyphs.TopRight, style);
        buffer.SetCell(left, bottom, glyphs.BottomLeft, style);
        buffer.SetCell(right, bottom, glyphs.BottomRight, style);

        for (var x = left + 1; x < right; x++)
        {
            buffer.SetCell(x, top, glyphs.Horizontal, style);
            buffer.SetCell(x, bottom, glyphs.Horizontal, style);
        }

        for (var y = top + 1; y < bottom; y++)
        {
            buffer.SetCell(left, y, glyphs.Vertical, style);
            buffer.SetCell(right, y, glyphs.Vertical, style);
        }
    }
}
