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
/// Specifies where a popup is positioned relative to its <see cref="Popup.Anchor"/>.
/// </summary>
public enum PopupPlacement
{
    /// <summary>
    /// Places the popup below the anchor.
    /// </summary>
    Below = 0,

    /// <summary>
    /// Places the popup above the anchor.
    /// </summary>
    Above = 1,

    /// <summary>
    /// Places the popup to the right of the anchor.
    /// </summary>
    Right = 2,

    /// <summary>
    /// Places the popup to the left of the anchor.
    /// </summary>
    Left = 3,
}

/// <summary>
/// Displays transient content in an overlay layer positioned relative to an optional anchor.
/// </summary>
public sealed partial class Popup : ContentVisual, IModalVisual
{
    private Rectangle _layoutSlot;
    private Rectangle _popupRect;
    private bool _isOpen;

    /// <summary>
    /// Initializes a new instance of the <see cref="Popup"/> class.
    /// </summary>
    public Popup()
    {
        this.HorizontalAlignment(HorizontalAlignment.Stretch);
        this.VerticalAlignment(VerticalAlignment.Stretch);
        this.MatchAnchorWidth(true);
        this.Placement(PopupPlacement.Below);
    }

    /// <summary>
    /// Gets or sets the anchor visual used for positioning the popup.
    /// </summary>
    public Visual? Anchor { get; set; }

    /// <summary>
    /// Gets a value indicating whether the popup is modal.
    /// </summary>
    public bool IsModal => true;

    /// <summary>
    /// Gets or sets a value indicating whether the popup should match the anchor width.
    /// </summary>
    [Bindable]
    public partial bool MatchAnchorWidth { get; set; }
    
    /// <summary>
    /// Gets or sets additional width to add to the computed popup width.
    /// </summary>
    [Bindable]
    public partial int AdditionalWidth { get; set; }

    /// <summary>
    /// Gets or sets the popup placement relative to the anchor.
    /// </summary>
    [Bindable]
    public partial PopupPlacement Placement { get; set; }

    /// <summary>
    /// Opens the popup by adding it to the active <see cref="TerminalApp"/> window layer.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when called while no terminal app is running.</exception>
    public void Show()
    {
        VerifyAccess();

        if (_isOpen)
        {
            return;
        }

        var app = App ?? Dispatcher.AttachedApp;
        if (app is null)
        {
            throw new InvalidOperationException("Popup.Show is only supported while a TerminalApp is running.");
        }

        _isOpen = true;
        app.ShowWindow(this);
    }

    /// <summary>
    /// Closes the popup and raises the <c>Closed</c> routed event.
    /// </summary>
    public void Close()
    {
        VerifyAccess();

        if (!_isOpen)
        {
            return;
        }

        var app = App ?? Dispatcher.AttachedApp;
        if (app is null)
        {
            return;
        }

        _isOpen = false;
        app.CloseWindow(this);
        RaiseEvent(ClosedEvent, new PopupClosedEventArgs());
    }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        // Fill the available space so the popup can detect outside clicks.
        var content = Content;
        if (content is not null)
        {
            content.Measure(new LayoutConstraints(0, constraints.MaxWidth, 0, constraints.MaxHeight));
        }

        return SizeHints.Flex(
            min: Size.Zero,
            natural: Size.Zero,
            max: new Size(LayoutConstants.Infinite, LayoutConstants.Infinite),
            growX: 1,
            growY: 1,
            shrinkX: 0,
            shrinkY: 0);
    }

    /// <inheritdoc/>
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var slot = finalRect;

        _layoutSlot = slot;
        Bounds = slot;

        var style = GetStyle<PopupStyle>();
        var padding = style.Padding;

        var content = Content;
        var contentDesired = content?.DesiredSize ?? default;

        void RemeasureContentForPopupWidth(int popupWidth)
        {
            if (content is null)
            {
                return;
            }

            var innerWidth = Math.Max(0, popupWidth - padding.Horizontal);
            content.Measure(new LayoutConstraints(0, innerWidth, 0, slot.Height));
            contentDesired = content.DesiredSize;
        }

        var desiredWidth = Math.Max(1, padding.Horizontal + contentDesired.Width);
        var desiredHeight = Math.Max(1, padding.Vertical + contentDesired.Height);

        var anchor = Anchor;
        var width = desiredWidth;
        if (MatchAnchorWidth && anchor is not null)
        {
            width = Math.Max(width, anchor.Bounds.Width);
        }
        width += Math.Max(0, AdditionalWidth);

        width = Math.Clamp(width, 1, slot.Width);
        desiredHeight = Math.Clamp(desiredHeight, 1, slot.Height);

        var x = slot.X;
        var y = slot.Y;

        if (anchor is not null)
        {
            var belowY = anchor.Bounds.Y + anchor.Bounds.Height;
            var aboveY = anchor.Bounds.Y - desiredHeight;
            var rightX = anchor.Bounds.X + anchor.Bounds.Width;
            var leftX = anchor.Bounds.X - width;

            switch (Placement)
            {
                case PopupPlacement.Above:
                    x = anchor.Bounds.X;
                    y = aboveY;
                    if (y < slot.Y && belowY + desiredHeight <= slot.Bottom)
                    {
                        y = belowY;
                    }
                    break;

                case PopupPlacement.Right:
                    // Prefer shrinking to the available space on the requested side over clamping the popup
                    // to the left edge, which would make the placement appear to "not work" in UIs like
                    // ControlsDemo where wrapped text can naturally measure to the full viewport width.
                    var maxRightWidth = Math.Max(0, slot.Right - rightX);
                    if (width > Math.Max(1, maxRightWidth))
                    {
                        width = Math.Max(1, Math.Min(width, maxRightWidth));
                        RemeasureContentForPopupWidth(width);
                        desiredHeight = Math.Clamp(Math.Max(1, padding.Vertical + contentDesired.Height), 1, slot.Height);
                    }

                    x = rightX;
                    y = anchor.Bounds.Y;
                    leftX = anchor.Bounds.X - width;
                    if (x + width > slot.Right && leftX >= slot.X)
                    {
                        x = leftX;
                    }
                    break;

                case PopupPlacement.Left:
                    var maxLeftWidth = Math.Max(0, anchor.Bounds.X - slot.X);
                    if (width > Math.Max(1, maxLeftWidth))
                    {
                        width = Math.Max(1, Math.Min(width, maxLeftWidth));
                        RemeasureContentForPopupWidth(width);
                        desiredHeight = Math.Clamp(Math.Max(1, padding.Vertical + contentDesired.Height), 1, slot.Height);
                    }

                    x = leftX;
                    y = anchor.Bounds.Y;
                    x = anchor.Bounds.X - width;
                    if (x < slot.X && rightX + width <= slot.Right)
                    {
                        x = rightX;
                    }
                    break;

                case PopupPlacement.Below:
                default:
                    x = anchor.Bounds.X;
                    y = belowY;
                    if (y + desiredHeight > slot.Bottom && aboveY >= slot.Y)
                    {
                        y = aboveY;
                    }
                    break;
            }
        }
        else
        {
            x = slot.X + Math.Max(0, (slot.Width - width) / 2);
            y = slot.Y + Math.Max(0, (slot.Height - desiredHeight) / 2);
        }

        x = Math.Clamp(x, slot.X, Math.Max(slot.X, slot.Right - width));
        y = Math.Clamp(y, slot.Y, Math.Max(slot.Y, slot.Bottom - desiredHeight));

        _popupRect = new Rectangle(x, y, width, desiredHeight);

        if (content is not null)
        {
            var inner = new Rectangle(
                _popupRect.X + padding.Left,
                _popupRect.Y + padding.Top,
                Math.Max(0, _popupRect.Width - padding.Horizontal),
                Math.Max(0, _popupRect.Height - padding.Vertical));

            content.Arrange(inner);
        }
    }

    /// <inheritdoc/>
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = _popupRect;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = GetStyle<PopupStyle>();
        var surface = style.ResolveSurfaceStyle(theme);

        // Fill popup surface.
        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), surface);
            }
        }

        return;
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        // Close on clicks outside the popup content area.
        if (!_popupRect.Contains(e.UiX, e.UiY))
        {
            Close();
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == TerminalKey.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    [RoutedEvent(RoutingStrategy.Direct)]
    private void OnClosed(PopupClosedEventArgs e) { }
}
