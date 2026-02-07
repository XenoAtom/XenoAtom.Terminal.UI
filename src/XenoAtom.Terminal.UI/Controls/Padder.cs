// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Adds padding around a single content visual.
/// </summary>
/// <remarks>
/// This control is useful as a lightweight layout primitive and can also be used as a base class
/// for content controls that need padding behavior.
/// </remarks>
public partial class Padder : ContentVisual
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Padder"/> control.
    /// </summary>
    public Padder()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Padder"/> control.
    /// </summary>
    /// <param name="content">The padder content.</param>
    public Padder(Visual content)
        : this()
    {
        Content = content;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Padder"/> control with a dynamic content factory.
    /// </summary>
    /// <param name="contentFactory">The content factory. It is evaluated as part of dynamic updates.</param>
    public Padder(Func<Visual> contentFactory)
        : this()
    {
        ArgumentNullException.ThrowIfNull(contentFactory);
        this.Content(contentFactory);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Padder"/> control with bound content.
    /// </summary>
    /// <param name="content">A binding that supplies the padder content visual.</param>
    public Padder(Binding<Visual?> content)
        : this()
    {
        this.Content(content);
    }

    /// <summary>
    /// Gets or sets the padding applied around the content.
    /// </summary>
    [Bindable]
    public partial Thickness Padding { get; set; }

    /// <summary>
    /// Gets the internal inset applied in addition to <see cref="Padding"/>.
    /// </summary>
    /// <remarks>
    /// Derived controls can override this value to reserve internal space (e.g., for drawing chrome)
    /// while still benefiting from the padding layout behavior.
    /// </remarks>
    protected virtual Thickness Inset => Thickness.Zero;

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var padding = GetEffectivePadding();
        var padH = LayoutConstants.ClampFinite(padding.Horizontal);
        var padV = LayoutConstants.ClampFinite(padding.Vertical);

        var maxW = constraints.MaxWidth == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxWidth - padH);
        var maxH = constraints.MaxHeight == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxHeight - padV);

        var childConstraints = new LayoutConstraints(0, maxW, 0, maxH);

        var content = Content;
        var contentHints = content is null ? SizeHints.Fixed(Size.Zero) : content.Measure(childConstraints);

        var addW = LayoutConstants.ClampFinite(padH);
        var addH = LayoutConstants.ClampFinite(padV);

        var minW = LayoutConstants.ClampFinite(contentHints.Min.Width + addW);
        var minH = LayoutConstants.ClampFinite(contentHints.Min.Height + addH);
        var natW = LayoutConstants.ClampFinite(contentHints.Natural.Width + addW);
        var natH = LayoutConstants.ClampFinite(contentHints.Natural.Height + addH);

        int maxWidth, maxHeight;
        if (LayoutConstants.IsInfinite(contentHints.Max.Width))
        {
            maxWidth = LayoutConstants.Infinite;
        }
        else
        {
            maxWidth = LayoutConstants.ClampOrInfinite(contentHints.Max.Width + addW);
        }

        if (LayoutConstants.IsInfinite(contentHints.Max.Height))
        {
            maxHeight = LayoutConstants.Infinite;
        }
        else
        {
            maxHeight = LayoutConstants.ClampOrInfinite(contentHints.Max.Height + addH);
        }

        return SizeHints.Flex(
            new Size(minW, minH),
            new Size(natW, natH),
            new Size(maxWidth, maxHeight),
            contentHints.FlexGrowX,
            contentHints.FlexGrowY,
            contentHints.FlexShrinkX,
            contentHints.FlexShrinkY).Normalize();
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var padding = GetEffectivePadding();
        var padH = padding.Horizontal;
        var padV = padding.Vertical;

        var content = Content;
        if (content is not null)
        {
            var inner = new Rectangle(
                finalRect.X + padding.Left,
                finalRect.Y + padding.Top,
                Math.Max(0, finalRect.Width - padH),
                Math.Max(0, finalRect.Height - padV));

            content.Arrange(inner);
        }
    }

    private Thickness GetEffectivePadding()
    {
        var padding = Padding;
        var inset = Inset;

        var left = Math.Max(0, padding.Left + inset.Left);
        var top = Math.Max(0, padding.Top + inset.Top);
        var right = Math.Max(0, padding.Right + inset.Right);
        var bottom = Math.Max(0, padding.Bottom + inset.Bottom);

        return new Thickness(left, top, right, bottom);
    }
}

