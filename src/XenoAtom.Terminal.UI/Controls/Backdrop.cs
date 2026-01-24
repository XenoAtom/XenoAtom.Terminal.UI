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
/// Renders a dimmed full-screen surface, typically used behind modal content.
/// </summary>
public sealed partial class Backdrop : Visual
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Backdrop"/> class.
    /// </summary>
    public Backdrop()
    {
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;
    }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        => SizeHints.Flex(
            min: Size.Zero,
            natural: Size.Zero,
            max: new Size(LayoutConstants.Infinite, LayoutConstants.Infinite),
            growX: 1,
            growY: 1,
            shrinkX: 0,
            shrinkY: 0);

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

        var theme = GetTheme();
        var backdropStyle = GetStyle<BackdropStyle>();
        var style = backdropStyle.Resolve(theme);
        var rune = backdropStyle.FillRune;

        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, rune, style);
            }
        }
    }
}
