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
public sealed partial class Popup : Visual, IModalVisual
{
    private readonly ScrollViewer _scrollViewer;
    private Rectangle _layoutSlot;
    private Rectangle _popupRect;
    private bool _isOpen;
    private bool _dragging;
    private int _dragStartUiX;
    private int _dragStartUiY;
    private int _dragStartOffsetX;
    private int _dragStartOffsetY;
    private PopupClosedEventArgs? _pendingCloseArgs;

    /// <summary>
    /// Initializes a new instance of the <see cref="Popup"/> class.
    /// </summary>
    public Popup()
    {
        _scrollViewer = new ScrollViewer(focusable: false);
        AttachChild(_scrollViewer);

        this.HorizontalAlignment(Align.Stretch);
        this.VerticalAlignment(Align.Stretch);
        this.MatchAnchorWidth(true);
        this.Placement(PopupPlacement.Below);
        this.CloseOnTab(true);
        this.HorizontalPopupAlignment(Align.Center);
        this.VerticalPopupAlignment(Align.Center);
    }

    /// <summary>
    /// Gets or sets the popup content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Popups are automatically protected by an internal <see cref="ScrollViewer"/>, so large content can be scrolled
    /// without requiring callers to wrap it explicitly.
    /// </para>
    /// <para>
    /// The internal scroll viewer is not focusable, ensuring the popup content continues to receive focus and key input
    /// (for example, for lists or text editors hosted inside the popup).
    /// </para>
    /// </remarks>
    [Bindable(NoVisualAttach = true)]
    public partial Visual? Content { get; set; }

    internal ScrollViewer ScrollHost => _scrollViewer;

    /// <inheritdoc />
    protected override int ChildrenCount => 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index)
        => index == 0 ? _scrollViewer : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc />
    protected override void PrepareChildren()
    {
        _scrollViewer.Content = Content;
    }

    /// <summary>
    /// Gets or sets the anchor visual used for positioning the popup.
    /// </summary>
    public Visual? Anchor { get; set; }

    /// <summary>
    /// Gets or sets an explicit anchor rectangle (in UI coordinates) used for positioning the popup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set, this value takes precedence over <see cref="Anchor"/> and allows positioning a popup relative to a point
    /// (for example, context menus opened by a right-click).
    /// </para>
    /// <para>
    /// The rectangle is interpreted in the same coordinate space as <see cref="Visual.Bounds"/>.
    /// </para>
    /// </remarks>
    public Rectangle? AnchorRect { get; set; }

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
    /// Gets or sets a value indicating whether the popup closes when the user presses Tab.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, popups close on Tab to allow focus traversal to continue in the underlying UI (useful for dropdowns).
    /// </para>
    /// <para>
    /// Set this to <see langword="false"/> for popups that should allow keyboard navigation within their content,
    /// such as a Find/Replace popup.
    /// </para>
    /// </remarks>
    [Bindable]
    public partial bool CloseOnTab { get; set; }

    /// <summary>
    /// Gets or sets the horizontal alignment used when the popup is not anchored.
    /// </summary>
    /// <remarks>
    /// When <see cref="Anchor"/> and <see cref="AnchorRect"/> are <see langword="null"/>, the popup is positioned
    /// within the available slot based on this alignment.
    /// </remarks>
    [Bindable]
    public partial Align HorizontalPopupAlignment { get; set; }

    /// <summary>
    /// Gets or sets the vertical alignment used when the popup is not anchored.
    /// </summary>
    /// <remarks>
    /// When <see cref="Anchor"/> and <see cref="AnchorRect"/> are <see langword="null"/>, the popup is positioned
    /// within the available slot based on this alignment.
    /// </remarks>
    [Bindable]
    public partial Align VerticalPopupAlignment { get; set; }

    /// <summary>
    /// Gets or sets a horizontal offset applied to the computed popup position.
    /// </summary>
    [Bindable]
    public partial int OffsetX { get; set; }

    /// <summary>
    /// Gets or sets a vertical offset applied to the computed popup position.
    /// </summary>
    [Bindable]
    public partial int OffsetY { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the popup can be repositioned by dragging.
    /// </summary>
    [Bindable]
    public partial bool IsDraggable { get; set; }

    /// <summary>
    /// Gets or sets the height (in rows) of the draggable area at the top of the popup.
    /// </summary>
    /// <remarks>
    /// When <see cref="IsDraggable"/> is enabled, pressing the left mouse button within the draggable area allows
    /// the popup to be moved by dragging.
    /// </remarks>
    [Bindable]
    public partial int DragHandleHeight { get; set; }

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

        // Ensure the internal scroll host is synchronized before the popup is shown.
        // This allows callers to immediately focus elements inside the popup (e.g. command palette search box)
        // without waiting for the first layout pass.
        _scrollViewer.Content = Content;

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

        var closeArgs = _pendingCloseArgs ?? new PopupClosedEventArgs();
        _pendingCloseArgs = null;

        _isOpen = false;
        app.CloseWindow(this);
        RaiseEvent(ClosedEvent, closeArgs);
    }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        // Fill the available space so the popup can detect outside clicks.
        var content = _content;
        if (content is not null)
        {
            content.Measure(new LayoutConstraints(0, constraints.MaxWidth, 0, constraints.MaxHeight));
        }

        _scrollViewer.Measure(new LayoutConstraints(0, constraints.MaxWidth, 0, constraints.MaxHeight));

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

        var style = GetStyle<PopupStyle>();
        var padding = style.Padding;

        var content = _content;
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
        var anchorRect = AnchorRect ?? anchor?.Bounds;
        var width = desiredWidth;
        if (MatchAnchorWidth && anchorRect is not null)
        {
            width = Math.Max(width, anchorRect.Value.Width);
        }
        width += Math.Max(0, AdditionalWidth);

        width = Math.Clamp(width, 1, slot.Width);
        desiredHeight = Math.Clamp(desiredHeight, 1, slot.Height);

        var x = slot.X;
        var y = slot.Y;

        if (anchorRect is not null)
        {
            var anchorBounds = anchorRect.Value;
            var belowY = anchorBounds.Y + anchorBounds.Height;
            var aboveY = anchorBounds.Y - desiredHeight;
            var rightX = anchorBounds.X + anchorBounds.Width;
            var leftX = anchorBounds.X - width;

            switch (Placement)
            {
                case PopupPlacement.Above:
                    x = anchorBounds.X;
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
                    y = anchorBounds.Y;
                    leftX = anchorBounds.X - width;
                    if (x + width > slot.Right && leftX >= slot.X)
                    {
                        x = leftX;
                    }
                    break;

                case PopupPlacement.Left:
                    var maxLeftWidth = Math.Max(0, anchorBounds.X - slot.X);
                    if (width > Math.Max(1, maxLeftWidth))
                    {
                        width = Math.Max(1, Math.Min(width, maxLeftWidth));
                        RemeasureContentForPopupWidth(width);
                        desiredHeight = Math.Clamp(Math.Max(1, padding.Vertical + contentDesired.Height), 1, slot.Height);
                    }

                    x = leftX;
                    y = anchorBounds.Y;
                    x = anchorBounds.X - width;
                    if (x < slot.X && rightX + width <= slot.Right)
                    {
                        x = rightX;
                    }
                    break;

                case PopupPlacement.Below:
                default:
                    x = anchorBounds.X;
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
            if (HorizontalPopupAlignment == Align.Stretch)
            {
                width = slot.Width;
                RemeasureContentForPopupWidth(width);
                desiredHeight = Math.Clamp(Math.Max(1, padding.Vertical + contentDesired.Height), 1, slot.Height);
                x = slot.X;
            }
            else if (HorizontalPopupAlignment == Align.End)
            {
                x = slot.Right - width;
            }
            else if (HorizontalPopupAlignment == Align.Center)
            {
                x = slot.X + Math.Max(0, (slot.Width - width) / 2);
            }
            else
            {
                x = slot.X;
            }

            if (VerticalPopupAlignment == Align.Stretch)
            {
                desiredHeight = slot.Height;
                y = slot.Y;
            }
            else if (VerticalPopupAlignment == Align.End)
            {
                y = slot.Bottom - desiredHeight;
            }
            else if (VerticalPopupAlignment == Align.Center)
            {
                y = slot.Y + Math.Max(0, (slot.Height - desiredHeight) / 2);
            }
            else
            {
                y = slot.Y;
            }
        }

        x += OffsetX;
        y += OffsetY;

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

            _scrollViewer.Arrange(inner);
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

        if (IsDraggable && _popupRect.Contains(e.UiX, e.UiY))
        {
            var handleHeight = Math.Max(1, DragHandleHeight);
            if (e.UiY >= _popupRect.Y && e.UiY < _popupRect.Y + handleHeight)
            {
                _dragging = true;
                _dragStartUiX = e.UiX;
                _dragStartUiY = e.UiY;
                _dragStartOffsetX = OffsetX;
                _dragStartOffsetY = OffsetY;
                e.Handled = true;
                return;
            }
        }

        // Close on clicks outside the popup content area.
        if (!_popupRect.Contains(e.UiX, e.UiY))
        {
            _pendingCloseArgs = new PopupClosedEventArgs(
                PopupCloseReason.OutsidePointerPress,
                outsidePointerX: e.UiX,
                outsidePointerY: e.UiY);
            Close();
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_dragging || e.Kind != TerminalMouseKind.Drag)
        {
            return;
        }

        OffsetX = _dragStartOffsetX + (e.UiX - _dragStartUiX);
        OffsetY = _dragStartOffsetY + (e.UiY - _dragStartUiY);
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (!_dragging || e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        _dragging = false;
        e.Handled = true;
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
