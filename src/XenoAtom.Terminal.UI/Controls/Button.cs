// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// A clickable control that raises a <c>Click</c> routed event when activated.
/// </summary>
public partial class Button : ContentVisual
{
    private bool _pressedInside;

    /// <summary>
    /// Initializes a new instance of the <see cref="Button"/> class.
    /// </summary>
    public Button()
    {
        Focusable = true;
        Tone = ControlTone.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Button"/> class with string content.
    /// </summary>
    /// <param name="text">The button text.</param>
    public Button(string text) : this()
    {
        this.Content(text);
    }

    /// <summary>
    /// Gets or sets the semantic tone of the button.
    /// </summary>
    [Bindable]
    public partial ControlTone Tone { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the button is in the pressed state.
    /// </summary>
    [Bindable]
    public partial bool IsPressed { get; set; }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = Get<ButtonStyle>();
        var padding = style.Padding;
        var padH = LayoutConstants.ClampFinite(Math.Max(0, padding.Horizontal));
        var padV = LayoutConstants.ClampFinite(Math.Max(0, padding.Vertical));

        var borderPad = style.ShowBorder ? 1 : 0;

        var innerMaxW = constraints.MaxWidth == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxWidth - padH - (borderPad * 2));
        var innerMaxH = constraints.MaxHeight == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxHeight - padV - (borderPad * 2));

        var innerConstraints = new LayoutConstraints(0, innerMaxW, 0, innerMaxH);

        var content = Content;
        var contentHints = content is null ? SizeHints.Fixed(Size.Zero) : content.Measure(innerConstraints);

        var addW = LayoutConstants.ClampFinite(padH + (borderPad * 2));
        var addH = LayoutConstants.ClampFinite(padV + (borderPad * 2));

        var minW = LayoutConstants.ClampFinite(contentHints.Min.Width + addW);
        var minH = LayoutConstants.ClampFinite(contentHints.Min.Height + addH);
        var natW = LayoutConstants.ClampFinite(contentHints.Natural.Width + addW);
        var natH = LayoutConstants.ClampFinite(contentHints.Natural.Height + addH);

        if (borderPad != 0)
        {
            minW = Math.Max(minW, 3);
            minH = Math.Max(minH, 3);
            natW = Math.Max(natW, 3);
            natH = Math.Max(natH, 3);
        }

        int maxW, maxH;
        if (LayoutConstants.IsInfinite(contentHints.Max.Width))
        {
            maxW = LayoutConstants.Infinite;
        }
        else
        {
            maxW = LayoutConstants.ClampOrInfinite(contentHints.Max.Width + addW);
        }

        if (LayoutConstants.IsInfinite(contentHints.Max.Height))
        {
            maxH = LayoutConstants.Infinite;
        }
        else
        {
            maxH = LayoutConstants.ClampOrInfinite(contentHints.Max.Height + addH);
        }

        return SizeHints.Flex(
            new Size(minW, minH),
            new Size(natW, natH),
            new Size(maxW, maxH),
            contentHints.FlexGrowX,
            contentHints.FlexGrowY,
            contentHints.FlexShrinkX,
            contentHints.FlexShrinkY).Normalize();
    }

    /// <inheritdoc/>
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var content = Content;
        if (content is null)
        {
            return;
        }

        var buttonStyle = Get<ButtonStyle>();
        var borderPad = buttonStyle.ShowBorder ? 1 : 0;

        var padding = buttonStyle.Padding;
        var contentX = finalRect.X + borderPad + padding.Left;
        var contentY = finalRect.Y + borderPad + padding.Top;
        var contentWidth = Math.Max(0, finalRect.Width - (borderPad * 2) - padding.Horizontal);
        var contentHeight = Math.Max(0, finalRect.Height - (borderPad * 2) - padding.Vertical);

        if (contentWidth <= 0 || contentHeight <= 0)
        {
            return;
        }

        var w = Math.Min(contentWidth, content.DesiredSize.Width);
        var h = Math.Min(contentHeight, content.DesiredSize.Height);
        var x = contentX + Math.Max(0, (contentWidth - w) / 2);
        var y = contentY + Math.Max(0, (contentHeight - h) / 2);
        content.Arrange(new Rectangle(x, y, w, h));
    }

    /// <inheritdoc/>
    protected override void RenderOverride(CellBuffer buffer)
    {
        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();
        var buttonStyle = Get<ButtonStyle>();
        var pressed = IsPressed && _pressedInside;
        var hovered = IsPressed ? _pressedInside : IsHovered;
        var style = buttonStyle.Resolve(theme, IsEnabled, isFocused, hovered: hovered, pressed: pressed, Tone);

        var rect = Bounds;

        // Background fill.
        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), style);
            }
        }

        var borderYPad = buttonStyle.ShowBorder ? 1 : 0;
        if (borderYPad != 0 && rect.Height >= 2)
        {
            var border = theme.BorderStyle(isFocused);

            var left = rect.X;
            var top = rect.Y;
            var right = rect.X + rect.Width - 1;
            var bottom = rect.Y + rect.Height - 1;

            for (var x = left; x <= right; x++)
            {
                buffer.SetCell(x, top, buttonStyle.BorderTopGlyph, border);
                buffer.SetCell(x, bottom, buttonStyle.BorderBottomGlyph, border);
            }
        }

        var padding = buttonStyle.Padding;
        var contentX = rect.X + padding.Left;
        var contentWidth = Math.Max(0, rect.Width - padding.Horizontal);
        var contentY = rect.Y + borderYPad + padding.Top;
        var contentHeight = Math.Max(0, rect.Height - (borderYPad * 2) - padding.Vertical);

        var content = Content;
        if (content is not null && contentWidth > 0 && contentHeight > 0)
        {
            // Preserve a consistent background for the whole button.
            // The button background may be an RGBA overlay that is blended into the cell buffer when written.
            // Filling the content region with the same RGBA background twice would apply the overlay twice and
            // visually create a lighter/darker strip behind the label. We only need to stamp the text style here
            // (e.g. Bold/Underline), while letting the background come from the initial full-rect fill.
            var contentFill = style.ClearBackground() | TextStyle.Bold;
            for (var y = contentY; y < contentY + contentHeight; y++)
            {
                for (var x = contentX; x < contentX + contentWidth; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), contentFill);
                }
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is TerminalKey.Enter or TerminalKey.Space)
        {
            RaiseEvent(Button.ClickEvent, new ClickEventArgs());
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        _pressedInside = Bounds.Contains(e.UiX, e.UiY);
        IsPressed = true;
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!IsPressed)
        {
            return;
        }

        var inside = Bounds.Contains(e.UiX, e.UiY);
        if (_pressedInside != inside)
        {
            _pressedInside = inside;
            Invalidate();
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (IsPressed)
        {
            _pressedInside = Bounds.Contains(e.UiX, e.UiY);
            IsPressed = false;
            if (_pressedInside)
            {
                RaiseEvent(Button.ClickEvent, new ClickEventArgs());
            }
            else
            {
                IsHovered = false;
            }
            e.Handled = true;
        }
    }

    /// <summary>
    /// Called when the button is activated (clicked or keyboard-invoked).
    /// </summary>
    /// <param name="e">The event arguments.</param>
    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnClick(ClickEventArgs e) { }
}
