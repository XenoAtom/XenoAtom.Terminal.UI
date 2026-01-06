// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI;

public sealed partial class Dialog : Visual, IModalVisual
{
    private Visual? _child;
    private CellRect _layoutSlot;

    private bool _dragging;
    private int _dragStartUiX;
    private int _dragStartUiY;
    private int _dragStartLeft;
    private int _dragStartTop;

    [Bindable]
    public partial string? Title { get; set; }

    [Bindable]
    public partial Thickness Padding { get; set; }

    [Bindable]
    public partial int? Left { get; set; }

    [Bindable]
    public partial int? Top { get; set; }

    [Bindable]
    public partial int? Width { get; set; }

    [Bindable]
    public partial int? Height { get; set; }

    [Bindable]
    public partial bool IsModal { get; set; }

    public Visual? Child
    {
        get => _child;
        set
        {
            if (ReferenceEquals(_child, value))
            {
                return;
            }

            if (_child is not null)
            {
                throw new InvalidOperationException("Dialog currently only supports setting Child once.");
            }

            _child = value;
            if (value is not null)
            {
                AddChild(value);
            }

            App?.RequestRender();
        }
    }

    protected override CellSize MeasureOverride(CellSize availableSize)
    {
        var padding = Padding;
        var availableWidth = Width ?? availableSize.Width;
        var availableHeight = Height ?? availableSize.Height;

        var innerWidth = Math.Max(0, availableWidth - 2 - padding.Horizontal);
        var innerHeight = Math.Max(0, availableHeight - 2 - padding.Vertical);

        _child?.Measure(new CellSize(innerWidth, innerHeight));

        var desiredWidth = Width ?? Math.Min(availableSize.Width, Math.Max(3, 2 + padding.Horizontal + (_child?.DesiredSize.Width ?? 0)));
        var desiredHeight = Height ?? Math.Min(availableSize.Height, Math.Max(3, 2 + padding.Vertical + (_child?.DesiredSize.Height ?? 0)));

        var title = Title;
        if (!string.IsNullOrEmpty(title))
        {
            var titleCells = TerminalTextUtility.GetWidth(title.AsSpan());
            desiredWidth = Math.Max(desiredWidth, Math.Min(availableSize.Width, Math.Max(3, titleCells + 4)));
        }

        return new CellSize(desiredWidth, desiredHeight);
    }

    protected override void ArrangeOverride(CellRect finalRect)
    {
        _layoutSlot = finalRect;

        var width = Math.Max(3, Math.Min(finalRect.Width, Width ?? DesiredSize.Width));
        var height = Math.Max(3, Math.Min(finalRect.Height, Height ?? DesiredSize.Height));

        var maxLeft = Math.Max(0, finalRect.Width - width);
        var maxTop = Math.Max(0, finalRect.Height - height);

        var left = Left is null ? maxLeft / 2 : Math.Clamp(Left.Value, 0, maxLeft);
        var top = Top is null ? maxTop / 2 : Math.Clamp(Top.Value, 0, maxTop);

        Bounds = new CellRect(finalRect.X + left, finalRect.Y + top, width, height);

        if (_child is not null)
        {
            var padding = Padding;
            var inner = new CellRect(
                Bounds.X + 1 + padding.Left,
                Bounds.Y + 1 + padding.Top,
                Math.Max(0, Bounds.Width - 2 - padding.Horizontal),
                Math.Max(0, Bounds.Height - 2 - padding.Vertical));

            _child.Arrange(inner);
        }
    }

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
        var surface = theme.SurfaceStyle();

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

        buffer.SetCell(left, top, new Rune(glyphs.TopLeft), borderStyle);
        buffer.SetCell(right, top, new Rune(glyphs.TopRight), borderStyle);
        buffer.SetCell(left, bottom, new Rune(glyphs.BottomLeft), borderStyle);
        buffer.SetCell(right, bottom, new Rune(glyphs.BottomRight), borderStyle);

        for (var x = left + 1; x < right; x++)
        {
            buffer.SetCell(x, top, new Rune(glyphs.Horizontal), borderStyle);
            buffer.SetCell(x, bottom, new Rune(glyphs.Horizontal), borderStyle);
        }

        for (var y = top + 1; y < bottom; y++)
        {
            buffer.SetCell(left, y, new Rune(glyphs.Vertical), borderStyle);
            buffer.SetCell(right, y, new Rune(glyphs.Vertical), borderStyle);
        }

        var title = Title;
        if (!string.IsNullOrEmpty(title) && rect.Width >= 4)
        {
            var maxTitleCells = rect.Width - 4;
            var titleSpan = title.AsSpan();
            if (TerminalTextUtility.TryGetIndexAtCell(titleSpan, maxTitleCells, out var titleEnd))
            {
                titleSpan = titleSpan[..titleEnd];
            }

            buffer.WriteText(rect.X + 2, rect.Y, titleSpan, borderStyle | CellStyle.Bold);
        }
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (e.LocalY != 0)
        {
            return;
        }

        _dragging = true;

        var uiX = Bounds.X + e.LocalX;
        var uiY = Bounds.Y + e.LocalY;
        _dragStartUiX = uiX;
        _dragStartUiY = uiY;

        var currentLeft = Bounds.X - _layoutSlot.X;
        var currentTop = Bounds.Y - _layoutSlot.Y;
        _dragStartLeft = currentLeft;
        _dragStartTop = currentTop;

        Left = currentLeft;
        Top = currentTop;

        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        var uiX = Bounds.X + e.LocalX;
        var uiY = Bounds.Y + e.LocalY;

        var deltaX = uiX - _dragStartUiX;
        var deltaY = uiY - _dragStartUiY;

        var maxLeft = Math.Max(0, _layoutSlot.Width - Bounds.Width);
        var maxTop = Math.Max(0, _layoutSlot.Height - Bounds.Height);

        Left = Math.Clamp(_dragStartLeft + deltaX, 0, maxLeft);
        Top = Math.Clamp(_dragStartTop + deltaY, 0, maxTop);

        e.Handled = true;
    }

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
