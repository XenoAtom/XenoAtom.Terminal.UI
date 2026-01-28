// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Rendering;

/// <summary>
/// Internal helper to apply a theme via environment to a visual subtree without mutating the subtree itself.
/// </summary>
internal sealed class ThemedHost : ContentVisual
{
    public ThemedHost(Visual content, Theme theme)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(theme);
        SetStyle(Theme.Key, theme);
        Content = content;
    }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var content = Content;
        if (content is null)
        {
            return SizeHints.Fixed(Size.Zero);
        }

        return content.Measure(constraints);
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Content?.Arrange(finalRect);
    }
}

