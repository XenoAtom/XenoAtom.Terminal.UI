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
    private Rectangle _layoutSlot;

    private bool _dragging;
    private int _dragStartUiX;
    private int _dragStartUiY;
    private int _dragStartLeft;
    private int _dragStartTop;

    /// <summary>
    /// Initializes a new instance of the <see cref="Dialog"/> class.
    /// </summary>
    public Dialog()
    {
        this.HorizontalAlignment(HorizontalAlignment.Stretch);
        this.VerticalAlignment(VerticalAlignment.Stretch);
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

    /// <inheritdoc/>
    protected override int ChildrenCount
        => (_title is null ? 0 : 1) + (_content is null ? 0 : 1);

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

        var desiredWidth = Width ?? Math.Max(3, 2 + padding.Horizontal + (content?.DesiredSize.Width ?? 0));
        var desiredHeight = Height ?? Math.Max(3, 2 + padding.Vertical + (content?.DesiredSize.Height ?? 0));

        var title = Title;
        if (title is not null)
        {
            title.Measure(new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1));
            desiredWidth = Math.Max(desiredWidth, Math.Max(3, title.DesiredSize.Width + 4));
        }

        return SizeHints.Fixed(constraints.Clamp(new Size(desiredWidth, desiredHeight)));
    }

    /// <inheritdoc/>
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        _layoutSlot = finalRect;

        var width = Math.Max(3, Math.Min(finalRect.Width, Width ?? DesiredSize.Width));
        var height = Math.Max(3, Math.Min(finalRect.Height, Height ?? DesiredSize.Height));

        var maxLeft = Math.Max(0, finalRect.Width - width);
        var maxTop = Math.Max(0, finalRect.Height - height);

        var left = Left is null ? maxLeft / 2 : Math.Clamp(Left.Value, 0, maxLeft);
        var top = Top is null ? maxTop / 2 : Math.Clamp(Top.Value, 0, maxTop);

        Bounds = new Rectangle(finalRect.X + left, finalRect.Y + top, width, height);

        var title = Title;
        if (title is not null && Bounds.Width >= 4)
        {
            var titleMaxWidth = Math.Max(0, Bounds.Width - 4);
            var titleWidth = Math.Min(titleMaxWidth, title.DesiredSize.Width);
            title.Arrange(new Rectangle(Bounds.X + 2, Bounds.Y, titleWidth, 1));
        }

        var content = Content;
        if (content is not null)
        {
            var padding = Padding;
            var inner = new Rectangle(
                Bounds.X + 1 + padding.Left,
                Bounds.Y + 1 + padding.Top,
                Math.Max(0, Bounds.Width - 2 - padding.Horizontal),
                Math.Max(0, Bounds.Height - 2 - padding.Vertical));

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

        var focused = false;
        var focusedElement = App?.FocusedElement;
        for (var v = focusedElement; v is not null; v = v.Parent)
        {
            if (ReferenceEquals(v, this))
            {
                focused = true;
                break;
            }
        }

        var theme = GetTheme();
        var glyphs = theme.Lines;
        var borderStyle = theme.BorderStyle(focused);

        var dialogBackground = theme.PopupSurface ?? theme.SurfaceAlt ?? theme.Surface;
        var surface = theme.ForegroundTextStyle();
        if (dialogBackground is { } bg)
        {
            surface = surface.WithBackground(bg);
        }

        var chromeStyle = borderStyle;
        if (dialogBackground is { } chromeBg)
        {
            chromeStyle = chromeStyle.WithBackground(chromeBg);
        }

        var left = rect.X;
        var top = rect.Y;
        var right = rect.X + rect.Width - 1;
        var bottom = rect.Y + rect.Height - 1;

        // Fill background.
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

        var title = Title;
        if (title is not null && rect.Width >= 4 && title.Bounds.Width > 0)
        {
            for (var x = title.Bounds.X; x < title.Bounds.X + title.Bounds.Width && x < rect.Right - 2; x++)
            {
                buffer.SetCell(x, rect.Y, new Rune(' '), chromeStyle | TextStyle.Bold);
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (e.UiY != Bounds.Y)
        {
            return;
        }

        _dragging = true;

        _dragStartUiX = e.UiX;
        _dragStartUiY = e.UiY;

        var currentLeft = Bounds.X - _layoutSlot.X;
        var currentTop = Bounds.Y - _layoutSlot.Y;
        _dragStartLeft = currentLeft;
        _dragStartTop = currentTop;

        Left = currentLeft;
        Top = currentTop;

        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        var deltaX = e.UiX - _dragStartUiX;
        var deltaY = e.UiY - _dragStartUiY;

        var maxLeft = Math.Max(0, _layoutSlot.Width - Bounds.Width);
        var maxTop = Math.Max(0, _layoutSlot.Height - Bounds.Height);

        Left = Math.Clamp(_dragStartLeft + deltaX, 0, maxLeft);
        Top = Math.Clamp(_dragStartTop + deltaY, 0, maxTop);

        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (_dragging)
        {
            _dragging = false;
            e.Handled = true;
        }
    }
}
