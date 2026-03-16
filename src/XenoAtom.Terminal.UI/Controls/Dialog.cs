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
/// A movable window/dialog visual that can be shown in fullscreen applications.
/// </summary>
public sealed partial class Dialog : Visual, IModalVisual
{
    private enum DialogResizeHandle
    {
        None,
        Left,
        Right,
        Bottom,
        BottomRight,
    }

    private Rectangle _layoutSlot;

    private bool _draggingMove;
    private int _interactionStartUiX;
    private int _interactionStartUiY;
    private int _interactionStartLeft;
    private int _interactionStartTop;
    private int _interactionStartWidth;
    private int _interactionStartHeight;

    [Bindable]
    private partial int HoveredResizeHandleValue { get; set; }

    [Bindable]
    private partial int ActiveResizeHandleValue { get; set; }

    [Bindable]
    private partial bool HoveredMoveHandleValue { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Dialog"/> class.
    /// </summary>
    public Dialog()
    {
        this.HorizontalAlignment(Align.Stretch);
        this.VerticalAlignment(Align.Stretch);
        IsResizable = true;
        HoveredResizeHandle = DialogResizeHandle.None;
        ActiveResizeHandle = DialogResizeHandle.None;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Dialog"/> class with content.
    /// </summary>
    /// <param name="content">The dialog content.</param>
    public Dialog(Visual content) : this()
    {
        this.Content(content);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Dialog"/> class with dynamic content.
    /// </summary>
    /// <param name="content">A delegate that supplies the dialog content.</param>
    public Dialog(Func<Visual> content) : this()
    {
        this.Content(content);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Dialog"/> class with bound content.
    /// </summary>
    /// <param name="content">A binding that supplies the dialog content.</param>
    public Dialog(Binding<Visual?> content) : this()
    {
        this.Content(content);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Dialog"/> class with title and content.
    /// </summary>
    /// <param name="title">The dialog title visual.</param>
    /// <param name="content">The dialog content.</param>
    public Dialog(Visual title, Visual content) : this(content)
    {
        this.Title(title);
    }

    /// <summary>
    /// Shows the dialog by adding it to the current fullscreen <see cref="TerminalApp"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when called while no terminal app is running.</exception>
    public void Show()
    {
        VerifyAccess();

        var app = App ?? Dispatcher.AttachedApp;
        if (app is null)
        {
            throw new InvalidOperationException("Dialog.Show is only supported while a TerminalApp is running.");
        }

        app.ShowWindow(this);
    }

    /// <summary>
    /// Closes the dialog if it is currently shown.
    /// </summary>
    public void Close()
    {
        VerifyAccess();

        var app = App ?? Dispatcher.AttachedApp;
        if (app is null)
        {
            return;
        }

        app.CloseWindow(this);
    }

    /// <summary>
    /// Gets or sets the dialog title visual.
    /// </summary>
    [Bindable]
    public partial Visual? Title { get; set; }

    /// <summary>
    /// Gets or sets the padding between the border and the content.
    /// </summary>
    [Bindable]
    public partial Thickness Padding { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog can be resized with the mouse.
    /// </summary>
    [Bindable]
    public partial bool IsResizable { get; set; }

    /// <summary>
    /// Gets or sets the optional left position (relative to the layout slot).
    /// </summary>
    [Bindable]
    public partial int? Left { get; set; }

    /// <summary>
    /// Gets or sets the optional top position (relative to the layout slot).
    /// </summary>
    [Bindable]
    public partial int? Top { get; set; }

    /// <summary>
    /// Gets or sets the optional dialog width.
    /// </summary>
    [Bindable]
    public partial int? Width { get; set; }

    /// <summary>
    /// Gets or sets the optional dialog height.
    /// </summary>
    [Bindable]
    public partial int? Height { get; set; }

    /// <inheritdoc/>
    [Bindable]
    public partial bool IsModal { get; set; }

    /// <summary>
    /// Gets or sets the dialog content visual.
    /// </summary>
    [Bindable]
    public partial Visual? Content { get; set; }

    /// <summary>
    /// Gets or sets the optional top-right label visual.
    /// </summary>
    [Bindable]
    public partial Visual? TopRightText { get; set; }

    /// <summary>
    /// Gets or sets the optional bottom-left label visual.
    /// </summary>
    [Bindable]
    public partial Visual? BottomLeftText { get; set; }

    /// <summary>
    /// Gets or sets the optional bottom-right label visual.
    /// </summary>
    [Bindable]
    public partial Visual? BottomRightText { get; set; }

    /// <inheritdoc/>
    protected override int ChildrenCount
        => (_title is null ? 0 : 1)
            + (_topRightText is null ? 0 : 1)
            + (_bottomLeftText is null ? 0 : 1)
            + (_bottomRightText is null ? 0 : 1)
            + (_content is null ? 0 : 1);

    /// <inheritdoc/>
    protected override Visual GetChild(int index)
    {
        if (_title is not null)
        {
            if (index == 0)
            {
                return _title;
            }
            index--;
        }

        if (_topRightText is not null)
        {
            if (index == 0)
            {
                return _topRightText;
            }
            index--;
        }

        if (_bottomLeftText is not null)
        {
            if (index == 0)
            {
                return _bottomLeftText;
            }
            index--;
        }

        if (_bottomRightText is not null)
        {
            if (index == 0)
            {
                return _bottomRightText;
            }
            index--;
        }

        if (_content is not null)
        {
            if (index == 0)
            {
                return _content;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var padding = Padding;
        var availableWidth = Width ?? constraints.MaxWidth;
        var availableHeight = Height ?? constraints.MaxHeight;

        var innerWidth = Math.Max(0, availableWidth - 2 - padding.Horizontal);
        var innerHeight = Math.Max(0, availableHeight - 2 - padding.Vertical);

        var content = Content;
        if (content is not null)
        {
            content.Measure(new LayoutConstraints(0, innerWidth, 0, innerHeight));
        }

        var desiredWidth = Math.Max(GetMinimumDialogWidth(), Width ?? (2 + padding.Horizontal + (content?.DesiredSize.Width ?? 0)));
        var desiredHeight = Math.Max(GetMinimumDialogHeight(), Height ?? (2 + padding.Vertical + (content?.DesiredSize.Height ?? 0)));

        var labelConstraints = new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1);
        var title = Title;
        var topRight = TopRightText;
        var bottomLeft = BottomLeftText;
        var bottomRight = BottomRightText;

        if (title is not null) title.Measure(labelConstraints);
        if (topRight is not null) topRight.Measure(labelConstraints);
        if (bottomLeft is not null) bottomLeft.Measure(labelConstraints);
        if (bottomRight is not null) bottomRight.Measure(labelConstraints);

        var topRequired = GetLabelWidth(title) + GetLabelWidth(topRight);
        var bottomRequired = GetLabelWidth(bottomLeft) + GetLabelWidth(bottomRight);
        var labelRequired = Math.Max(topRequired, bottomRequired);
        if (labelRequired > 0)
        {
            desiredWidth = Math.Max(desiredWidth, 2 + labelRequired);
        }

        return SizeHints.Fixed(constraints.Clamp(new Size(desiredWidth, desiredHeight)));
    }

    /// <inheritdoc/>
    protected override Rectangle PrepareArrangeBounds(in Rectangle finalRect)
    {
        _layoutSlot = finalRect;
        var width = ClampWidthToSlot(Width ?? DesiredSize.Width, finalRect.Width);
        var height = ClampHeightToSlot(Height ?? DesiredSize.Height, finalRect.Height);

        var maxLeft = Math.Max(0, finalRect.Width - width);
        var maxTop = Math.Max(0, finalRect.Height - height);

        var left = Left is null ? maxLeft / 2 : Math.Clamp(Left.Value, 0, maxLeft);
        var top = Top is null ? maxTop / 2 : Math.Clamp(Top.Value, 0, maxTop);

        return new Rectangle(finalRect.X + left, finalRect.Y + top, width, height);
    }

    /// <inheritdoc/>
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        ArrangeLabels(finalRect);

        var content = Content;
        if (content is not null)
        {
            var padding = Padding;
            var inner = new Rectangle(
                finalRect.X + 1 + padding.Left,
                finalRect.Y + 1 + padding.Top,
                Math.Max(0, finalRect.Width - 2 - padding.Horizontal),
                Math.Max(0, finalRect.Height - 2 - padding.Vertical));

            content.Arrange(inner);
        }
    }

    /// <inheritdoc/>
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var focused = HasFocusWithin;
        var theme = GetTheme();
        var dialogStyle = GetStyle<DialogStyle>();
        var glyphs = dialogStyle.Glyphs ?? theme.Lines;
        var surface = dialogStyle.ResolveSurfaceStyle(theme);
        var chromeStyle = dialogStyle.ResolveBorderStyle(theme, focused);

        var left = rect.X;
        var top = rect.Y;
        var right = rect.X + rect.Width - 1;
        var bottom = rect.Y + rect.Height - 1;

        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), surface);
            }
        }

        buffer.SetCell(left, top, glyphs.TopLeft, chromeStyle);
        buffer.SetCell(right, top, glyphs.TopRight, chromeStyle);
        buffer.SetCell(left, bottom, glyphs.BottomLeft, chromeStyle);
        buffer.SetCell(right, bottom, glyphs.BottomRight, chromeStyle);

        for (var x = left + 1; x < right; x++)
        {
            buffer.SetCell(x, top, glyphs.Horizontal, chromeStyle);
            buffer.SetCell(x, bottom, glyphs.Horizontal, chromeStyle);
        }

        for (var y = top + 1; y < bottom; y++)
        {
            buffer.SetCell(left, y, glyphs.Vertical, chromeStyle);
            buffer.SetCell(right, y, glyphs.Vertical, chromeStyle);
        }

        var labelStyle = dialogStyle.ResolveLabelBackgroundStyle();
        var innerWidth = Math.Max(0, rect.Width - 2);
        if (innerWidth > 0)
        {
            var innerLeft = rect.X + 1;
            RenderLabelLeft(buffer, Title, innerLeft, top, innerWidth, labelStyle);
            RenderLabelRight(buffer, TopRightText, innerLeft, top, innerWidth, labelStyle);
            RenderLabelLeft(buffer, BottomLeftText, innerLeft, bottom, innerWidth, labelStyle);
            RenderLabelRight(buffer, BottomRightText, innerLeft, bottom, innerWidth, labelStyle);
        }

        var highlightedHandle = ActiveResizeHandle != DialogResizeHandle.None
            ? ActiveResizeHandle
            : (IsHovered && IsResizable ? HoveredResizeHandle : DialogResizeHandle.None);
        if (highlightedHandle != DialogResizeHandle.None && TryGetResizeHandleRect(highlightedHandle, out var handleRect))
        {
            ApplyResizeHandleHighlight(buffer, handleRect, glyphs, dialogStyle.ResolveResizeHandleHoverStyle(theme, focused), highlightedHandle);
        }

        if ((_draggingMove || (IsHovered && HoveredMoveHandle)) && TryGetMoveHandleRect(out var moveHandleRect))
        {
            ApplyMoveHandleHighlight(buffer, moveHandleRect, glyphs, dialogStyle.ResolveResizeHandleHoverStyle(theme, focused));
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.RoutingPhase != RoutingPhase.Bubble
            || e.Button != TerminalMouseButton.Left
            || !IsEnabled)
        {
            return;
        }

        if (IsResizable)
        {
            var resizeHandle = HitTestResizeHandle(e.UiX, e.UiY);
            if (resizeHandle != DialogResizeHandle.None)
            {
                BeginInteraction(e, resizeHandle);
                e.Handled = true;
                return;
            }
        }

        if (!HitTestMoveHandle(e.UiX, e.UiY))
        {
            return;
        }

        BeginInteraction(e, DialogResizeHandle.None);
        _draggingMove = true;
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (ActiveResizeHandle != DialogResizeHandle.None)
        {
            UpdateResize(e.UiX, e.UiY, ActiveResizeHandle);
            e.Handled = true;
            return;
        }

        if (_draggingMove)
        {
            UpdateMove(e.UiX, e.UiY);
            e.Handled = true;
            return;
        }

        var hoveredHandle = IsResizable ? HitTestResizeHandle(e.UiX, e.UiY) : DialogResizeHandle.None;
        if (HoveredResizeHandle != hoveredHandle)
        {
            HoveredResizeHandle = hoveredHandle;
        }

        var hoveredMoveHandle = HitTestMoveHandle(e.UiX, e.UiY);
        if (HoveredMoveHandle != hoveredMoveHandle)
        {
            HoveredMoveHandle = hoveredMoveHandle;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (_draggingMove || ActiveResizeHandle != DialogResizeHandle.None)
        {
            _draggingMove = false;
            ActiveResizeHandle = DialogResizeHandle.None;
            HoveredResizeHandle = IsResizable ? HitTestResizeHandle(e.UiX, e.UiY) : DialogResizeHandle.None;
            HoveredMoveHandle = HitTestMoveHandle(e.UiX, e.UiY);
            e.Handled = true;
        }
    }

    private void ArrangeLabels(in Rectangle finalRect)
    {
        var innerWidth = Math.Max(0, finalRect.Width - 2);
        if (innerWidth <= 0 || finalRect.Height <= 0)
        {
            return;
        }

        var innerLeft = finalRect.X + 1;
        var topY = finalRect.Y;
        var bottomY = finalRect.Y + finalRect.Height - 1;

        ArrangeLabelLeft(Title, innerLeft, topY, innerWidth);
        ArrangeLabelRight(TopRightText, innerLeft, topY, innerWidth);
        ArrangeLabelLeft(BottomLeftText, innerLeft, bottomY, innerWidth);
        ArrangeLabelRight(BottomRightText, innerLeft, bottomY, innerWidth);
    }

    private static void ArrangeLabelLeft(Visual? label, int innerLeft, int y, int innerWidth)
    {
        if (label is null)
        {
            return;
        }

        var totalWidth = Math.Min(innerWidth, label.DesiredSize.Width + 2);
        if (totalWidth < 2)
        {
            label.Arrange(new Rectangle(innerLeft, y, 0, 1));
            return;
        }

        label.Arrange(new Rectangle(innerLeft + 1, y, Math.Max(0, totalWidth - 2), 1));
    }

    private static void ArrangeLabelRight(Visual? label, int innerLeft, int y, int innerWidth)
    {
        if (label is null)
        {
            return;
        }

        var totalWidth = Math.Min(innerWidth, label.DesiredSize.Width + 2);
        if (totalWidth < 2)
        {
            label.Arrange(new Rectangle(innerLeft, y, 0, 1));
            return;
        }

        var startX = innerLeft + Math.Max(0, innerWidth - totalWidth);
        label.Arrange(new Rectangle(startX + 1, y, Math.Max(0, totalWidth - 2), 1));
    }

    private static void RenderLabelLeft(CellBuffer buffer, Visual? label, int innerLeft, int y, int innerWidth, Style style)
    {
        if (label is null)
        {
            return;
        }

        var totalWidth = Math.Min(innerWidth, label.DesiredSize.Width + 2);
        if (totalWidth <= 0)
        {
            return;
        }

        for (var i = 0; i < totalWidth; i++)
        {
            buffer.SetCell(innerLeft + i, y, new Rune(' '), style);
        }
    }

    private static void RenderLabelRight(CellBuffer buffer, Visual? label, int innerLeft, int y, int innerWidth, Style style)
    {
        if (label is null)
        {
            return;
        }

        var totalWidth = Math.Min(innerWidth, label.DesiredSize.Width + 2);
        if (totalWidth <= 0)
        {
            return;
        }

        var startX = innerLeft + Math.Max(0, innerWidth - totalWidth);
        for (var i = 0; i < totalWidth; i++)
        {
            buffer.SetCell(startX + i, y, new Rune(' '), style);
        }
    }

    private static int GetLabelWidth(Visual? label)
        => label is null ? 0 : label.DesiredSize.Width + 2;

    private DialogResizeHandle HoveredResizeHandle
    {
        get => (DialogResizeHandle)HoveredResizeHandleValue;
        set => HoveredResizeHandleValue = (int)value;
    }

    private DialogResizeHandle ActiveResizeHandle
    {
        get => (DialogResizeHandle)ActiveResizeHandleValue;
        set => ActiveResizeHandleValue = (int)value;
    }

    private bool HoveredMoveHandle
    {
        get => HoveredMoveHandleValue;
        set => HoveredMoveHandleValue = value;
    }

    private void BeginInteraction(PointerEventArgs e, DialogResizeHandle resizeHandle)
    {
        ActiveResizeHandle = resizeHandle;
        HoveredResizeHandle = resizeHandle;

        _interactionStartUiX = e.UiX;
        _interactionStartUiY = e.UiY;
        _interactionStartLeft = Bounds.X - _layoutSlot.X;
        _interactionStartTop = Bounds.Y - _layoutSlot.Y;
        _interactionStartWidth = Bounds.Width;
        _interactionStartHeight = Bounds.Height;

        Left = _interactionStartLeft;
        Top = _interactionStartTop;
        Width = _interactionStartWidth;
        Height = _interactionStartHeight;
    }

    private void UpdateMove(int uiX, int uiY)
    {
        var deltaX = uiX - _interactionStartUiX;
        var deltaY = uiY - _interactionStartUiY;

        var maxLeft = Math.Max(0, _layoutSlot.Width - Bounds.Width);
        var maxTop = Math.Max(0, _layoutSlot.Height - Bounds.Height);

        Left = Math.Clamp(_interactionStartLeft + deltaX, 0, maxLeft);
        Top = Math.Clamp(_interactionStartTop + deltaY, 0, maxTop);
    }

    private void UpdateResize(int uiX, int uiY, DialogResizeHandle handle)
    {
        var deltaX = uiX - _interactionStartUiX;
        var deltaY = uiY - _interactionStartUiY;

        var slotWidth = Math.Max(0, _layoutSlot.Width);
        var slotHeight = Math.Max(0, _layoutSlot.Height);

        var nextLeft = _interactionStartLeft;
        var nextWidth = _interactionStartWidth;
        var nextHeight = _interactionStartHeight;

        switch (handle)
        {
            case DialogResizeHandle.Left:
            {
                var right = _interactionStartLeft + _interactionStartWidth;
                var proposedWidth = right - (_interactionStartLeft + deltaX);
                nextWidth = ClampWidthToSlot(proposedWidth, Math.Min(slotWidth, right));
                nextLeft = Math.Clamp(right - nextWidth, 0, Math.Max(0, slotWidth - nextWidth));
                break;
            }
            case DialogResizeHandle.Right:
                nextWidth = ClampWidthToSlot(_interactionStartWidth + deltaX, slotWidth - _interactionStartLeft);
                break;
            case DialogResizeHandle.Bottom:
                nextHeight = ClampHeightToSlot(_interactionStartHeight + deltaY, slotHeight - _interactionStartTop);
                break;
            case DialogResizeHandle.BottomRight:
                nextWidth = ClampWidthToSlot(_interactionStartWidth + deltaX, slotWidth - _interactionStartLeft);
                nextHeight = ClampHeightToSlot(_interactionStartHeight + deltaY, slotHeight - _interactionStartTop);
                break;
        }

        Left = nextLeft;
        Top = _interactionStartTop;
        Width = nextWidth;
        Height = nextHeight;
    }

    private DialogResizeHandle HitTestResizeHandle(int uiX, int uiY)
    {
        if (!IsResizable || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return DialogResizeHandle.None;
        }

        if (TryGetResizeHandleRect(DialogResizeHandle.BottomRight, out var bottomRight) && bottomRight.Contains(uiX, uiY))
        {
            return DialogResizeHandle.BottomRight;
        }

        if (TryGetResizeHandleRect(DialogResizeHandle.Left, out var left) && left.Contains(uiX, uiY))
        {
            return DialogResizeHandle.Left;
        }

        if (TryGetResizeHandleRect(DialogResizeHandle.Right, out var right) && right.Contains(uiX, uiY))
        {
            return DialogResizeHandle.Right;
        }

        if (TryGetResizeHandleRect(DialogResizeHandle.Bottom, out var bottom) && bottom.Contains(uiX, uiY))
        {
            return DialogResizeHandle.Bottom;
        }

        return DialogResizeHandle.None;
    }

    private bool HitTestMoveHandle(int uiX, int uiY)
        => TryGetMoveHandleRect(out var rect) && rect.Contains(uiX, uiY);

    private bool TryGetMoveHandleRect(out Rectangle rect)
    {
        rect = default;
        var bounds = Bounds;
        if (bounds.Width <= 2 || bounds.Height <= 0)
        {
            return false;
        }

        rect = new Rectangle(bounds.X + 1, bounds.Y, bounds.Width - 2, 1);
        return true;
    }

    private bool TryGetResizeHandleRect(DialogResizeHandle handle, out Rectangle rect)
    {
        rect = default;
        var bounds = Bounds;
        if (!IsResizable || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return false;
        }

        switch (handle)
        {
            case DialogResizeHandle.Left:
                rect = new Rectangle(bounds.X, bounds.Y + 1, 1, Math.Max(1, bounds.Height - 2));
                return true;
            case DialogResizeHandle.Right:
                rect = new Rectangle(bounds.Right - 1, bounds.Y + 1, 1, Math.Max(1, bounds.Height - 2));
                return true;
            case DialogResizeHandle.Bottom:
                rect = new Rectangle(bounds.X + 1, bounds.Bottom - 1, Math.Max(1, bounds.Width - 2), 1);
                return true;
            case DialogResizeHandle.BottomRight:
                rect = new Rectangle(bounds.Right - 1, bounds.Bottom - 1, 1, 1);
                return true;
            default:
                return false;
        }
    }

    private static void ApplyResizeHandleHighlight(CellBuffer buffer, Rectangle rect, LineGlyphs glyphs, Style style, DialogResizeHandle handle)
    {
        for (var y = rect.Y; y < rect.Bottom; y++)
        {
            for (var x = rect.X; x < rect.Right; x++)
            {
                var glyph = handle switch
                {
                    DialogResizeHandle.Left or DialogResizeHandle.Right => glyphs.Vertical,
                    DialogResizeHandle.Bottom => glyphs.Horizontal,
                    DialogResizeHandle.BottomRight => glyphs.BottomRight,
                    _ => new Rune(' '),
                };
                buffer.SetCell(x, y, glyph, style);
            }
        }
    }

    private static void ApplyMoveHandleHighlight(CellBuffer buffer, Rectangle rect, LineGlyphs glyphs, Style style)
    {
        for (var y = rect.Y; y < rect.Bottom; y++)
        {
            for (var x = rect.X; x < rect.Right; x++)
            {
                buffer.SetCell(x, y, glyphs.Horizontal, style);
            }
        }
    }

    private int GetMinimumDialogWidth() => Math.Max(3, MinWidth);

    private int GetMinimumDialogHeight() => Math.Max(3, MinHeight);

    private int GetMaximumDialogWidth()
        => MaxWidth == LayoutConstants.Infinite ? int.MaxValue : Math.Max(GetMinimumDialogWidth(), MaxWidth);

    private int GetMaximumDialogHeight()
        => MaxHeight == LayoutConstants.Infinite ? int.MaxValue : Math.Max(GetMinimumDialogHeight(), MaxHeight);

    private int ClampWidthToSlot(int width, int availableWidth)
    {
        var clampedAvailable = Math.Max(0, availableWidth);
        if (clampedAvailable == 0)
        {
            return 0;
        }

        var min = Math.Min(GetMinimumDialogWidth(), clampedAvailable);
        var max = Math.Min(GetMaximumDialogWidth(), clampedAvailable);
        max = Math.Max(min, max);
        return Math.Clamp(width, min, max);
    }

    private int ClampHeightToSlot(int height, int availableHeight)
    {
        var clampedAvailable = Math.Max(0, availableHeight);
        if (clampedAvailable == 0)
        {
            return 0;
        }

        var min = Math.Min(GetMinimumDialogHeight(), clampedAvailable);
        var max = Math.Min(GetMaximumDialogHeight(), clampedAvailable);
        max = Math.Max(min, max);
        return Math.Clamp(height, min, max);
    }
}
