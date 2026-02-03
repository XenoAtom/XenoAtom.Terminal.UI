// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.


// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Centers its content within the available bounds.
/// </summary>
public sealed partial class Center : ContentVisual
{
    /// <summary>
    /// Initializes a new instance of the Center class.
    /// </summary>
    public Center()
    {
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;
    }

    /// <summary>
    /// Initializes a new instance of the Center class with the specified visual content.
    /// </summary>
    /// <param name="content">The visual element to be centered. Cannot be null.</param>
    public Center(Visual content) : this()
    {
        Content = content;
    }

    /// <summary>
    /// Initializes a new instance of the Center class with the specified content generator.
    /// </summary>
    /// <param name="content">A delegate that returns the Visual to be centered. This function is invoked to generate the content when needed
    /// and must not be null.</param>
    public Center(Func<Visual> content) : this()
    {
        Content = new ComputedVisual(content);
    }
    
    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var content = Content;
        if (content is null)
        {
            return SizeHints.Fixed(Size.Zero);
        }

        return content.Measure(constraints);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var content = Content;
        if (content is null)
        {
            return;
        }

        var w = Math.Min(finalRect.Width, content.DesiredSize.Width);
        var h = Math.Min(finalRect.Height, content.DesiredSize.Height);
        var x = finalRect.X + Math.Max(0, (finalRect.Width - w) / 2);
        var y = finalRect.Y + Math.Max(0, (finalRect.Height - h) / 2);

        content.Arrange(new Rectangle(x, y, w, h));
    }
}
