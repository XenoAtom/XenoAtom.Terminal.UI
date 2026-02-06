// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Rendering;

/// <summary>
/// Renders a visual subtree into an offscreen <see cref="CellBuffer"/> for diagnostics and content export.
/// </summary>
public static class VisualSnapshotRenderer
{
    /// <summary>
    /// Renders the <paramref name="visual"/> into a new <see cref="CellBuffer"/> and returns it.
    /// </summary>
    /// <param name="visual">The visual subtree root. Must not already be attached to a visual tree.</param>
    /// <param name="width">The buffer width in cells.</param>
    /// <param name="maxHeight">The maximum buffer height in cells.</param>
    /// <param name="theme">An optional theme applied to the subtree if it has no local theme.</param>
    public static CellBuffer Render(Visual visual, int width, int maxHeight = 200, Theme? theme = null)
    {
        ArgumentNullException.ThrowIfNull(visual);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHeight);

        if (visual.Parent is not null)
        {
            throw new InvalidOperationException("The visual is already attached to a visual tree.");
        }

        var hasTheme = visual.HasLocalStyle(Theme.Key);
        if (!hasTheme && theme is not null)
        {
            visual.SetStyle(Theme.Key, theme);
        }

        var constraints = new LayoutConstraints(0, width, 0, LayoutConstants.Infinite);
        visual.Measure(constraints);

        var height = Math.Max(1, Math.Min(maxHeight, visual.DesiredSize.Height));
        visual.Arrange(new Rectangle(0, 0, width, height));

        var buffer = new CellBuffer(width, height);
        buffer.Clear(visual.GetTheme().BaseTextStyle());
        visual.RenderTree(buffer);
        return buffer;
    }
}

