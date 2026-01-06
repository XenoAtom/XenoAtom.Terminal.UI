// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class ScrollViewer : Visuals.Visual
{
    private readonly ContentViewportHost _contentHost;
    private readonly VerticalScrollBarVisual _verticalBar;
    private readonly HorizontalScrollBarVisual _horizontalBar;
    private readonly ScrollCornerVisual _corner;

    private Visuals.Visual? _child;
    private int _contentWidth;
    private int _contentHeight;

    private bool _showHorizontalBar;
    private bool _showVerticalBar;
    private int _scrollBarThickness;

    private bool _draggingVertical;
    private bool _draggingHorizontal;
    private int _dragStartUiX;
    private int _dragStartUiY;
    private int _dragStartHorizontalOffset;
    private int _dragStartVerticalOffset;

    public ScrollViewer()
    {
        Focusable = true;
        Height = 6;

        _contentHost = new ContentViewportHost(this);
        _verticalBar = new VerticalScrollBarVisual(this);
        _horizontalBar = new HorizontalScrollBarVisual(this);
        _corner = new ScrollCornerVisual(this);

        AddChild(_contentHost);
        AddChild(_verticalBar);
        AddChild(_horizontalBar);
        AddChild(_corner);
    }

    public Visuals.Visual? Child
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
                throw new InvalidOperationException("ScrollViewer currently only supports setting Child once.");
            }

            _child = value;
            if (value is not null)
            {
                _contentHost.SetContent(value);
            }

            App?.RequestRender();
        }
    }

    [Bindable]
    public partial int VerticalOffset { get; set; }

    [Bindable]
    public partial int HorizontalOffset { get; set; }

    [Bindable]
    public partial int Height { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var height = Math.Max(1, Height);
        var child = _child;
        if (child is not null)
        {
            child.Measure(new Size(int.MaxValue / 4, int.MaxValue / 4));
            _contentWidth = child.DesiredSize.Width;
            _contentHeight = child.DesiredSize.Height;
        }
        else
        {
            _contentWidth = 0;
            _contentHeight = 0;
        }

        var desiredHeight = Math.Min(height, availableSize.Height);
        var desiredWidth = Math.Min(availableSize.Width, Math.Max(1, Math.Min(availableSize.Width, _contentWidth)));
        return new Size(desiredWidth, desiredHeight);
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        if (_child is null)
        {
            return;
        }

        var style = GetEnvironmentValue(ScrollViewerStyle.Key);
        var thickness = Math.Max(1, style.ScrollBarThickness);
        _scrollBarThickness = thickness;

        var viewportWidth = Math.Max(1, finalRect.Width);
        var viewportHeight = Math.Max(1, finalRect.Height);

        var showV = _contentHeight > viewportHeight;
        var showH = _contentWidth > viewportWidth;

        // account for bars and re-evaluate.
        for (var i = 0; i < 2; i++)
        {
            var w = viewportWidth - (showV ? thickness : 0);
            var hViewport = viewportHeight - (showH ? thickness : 0);
            showV = _contentHeight > Math.Max(1, hViewport);
            showH = _contentWidth > Math.Max(1, w);
        }

        _showVerticalBar = showV;
        _showHorizontalBar = showH;

        var contentViewportWidth = Math.Max(1, viewportWidth - (showV ? thickness : 0));
        var contentViewportHeight = Math.Max(1, viewportHeight - (showH ? thickness : 0));

        var maxVerticalOffset = Math.Max(0, _contentHeight - contentViewportHeight);
        var maxHorizontalOffset = Math.Max(0, _contentWidth - contentViewportWidth);

        var v = Math.Clamp(VerticalOffset, 0, maxVerticalOffset);
        var hOffset = Math.Clamp(HorizontalOffset, 0, maxHorizontalOffset);
        if (v != VerticalOffset) VerticalOffset = v;
        if (hOffset != HorizontalOffset) HorizontalOffset = hOffset;

        _contentHost.UpdateLayout(_contentWidth, _contentHeight, hOffset, v);
        _contentHost.Arrange(new Rectangle(finalRect.X, finalRect.Y, contentViewportWidth, contentViewportHeight));

        if (_showVerticalBar)
        {
            _verticalBar.Arrange(new Rectangle(finalRect.X + finalRect.Width - thickness, finalRect.Y, thickness, contentViewportHeight));
        }
        else
        {
            _verticalBar.Arrange(new Rectangle(finalRect.X + finalRect.Width, finalRect.Y, 0, 0));
        }

        if (_showHorizontalBar)
        {
            _horizontalBar.Arrange(new Rectangle(finalRect.X, finalRect.Y + finalRect.Height - thickness, contentViewportWidth, thickness));
        }
        else
        {
            _horizontalBar.Arrange(new Rectangle(finalRect.X, finalRect.Y + finalRect.Height, 0, 0));
        }

        if (_showVerticalBar && _showHorizontalBar)
        {
            _corner.Arrange(new Rectangle(finalRect.X + finalRect.Width - thickness, finalRect.Y + finalRect.Height - thickness, thickness, thickness));
        }
        else
        {
            _corner.Arrange(new Rectangle(finalRect.X + finalRect.Width, finalRect.Y + finalRect.Height, 0, 0));
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var viewportWidth = Math.Max(1, _contentHost.Bounds.Width);
        var viewportHeight = Math.Max(1, _contentHost.Bounds.Height);

        var maxVerticalOffset = Math.Max(0, _contentHeight - viewportHeight);
        var maxHorizontalOffset = Math.Max(0, _contentWidth - viewportWidth);

        switch (e.Key)
        {
            case TerminalKey.Up:
                VerticalOffset = Math.Max(0, VerticalOffset - 1);
                e.Handled = true;
                return;
            case TerminalKey.Down:
                VerticalOffset = Math.Min(maxVerticalOffset, VerticalOffset + 1);
                e.Handled = true;
                return;
            case TerminalKey.PageUp:
                VerticalOffset = Math.Max(0, VerticalOffset - viewportHeight);
                e.Handled = true;
                return;
            case TerminalKey.PageDown:
                VerticalOffset = Math.Min(maxVerticalOffset, VerticalOffset + viewportHeight);
                e.Handled = true;
                return;
            case TerminalKey.Home:
                VerticalOffset = 0;
                e.Handled = true;
                return;
            case TerminalKey.End:
                VerticalOffset = maxVerticalOffset;
                e.Handled = true;
                return;
            case TerminalKey.Left:
                HorizontalOffset = Math.Max(0, HorizontalOffset - 1);
                e.Handled = true;
                return;
            case TerminalKey.Right:
                HorizontalOffset = Math.Min(maxHorizontalOffset, HorizontalOffset + 1);
                e.Handled = true;
                return;
        }
    }

    protected override void OnPointerWheel(PointerEventArgs e)
    {
        if (e.WheelDelta == 0)
        {
            return;
        }

        if ((e.Modifiers & TerminalModifiers.Shift) != 0)
        {
            var viewportWidth = Math.Max(1, _contentHost.Bounds.Width);
            var maxOffset = Math.Max(0, _contentWidth - viewportWidth);
            if (maxOffset == 0)
            {
                return;
            }

            HorizontalOffset = e.WheelDelta > 0 ? Math.Max(0, HorizontalOffset - 1) : Math.Min(maxOffset, HorizontalOffset + 1);
        }
        else
        {
            var viewportHeight = Math.Max(1, _contentHost.Bounds.Height);
            var maxOffset = Math.Max(0, _contentHeight - viewportHeight);
            if (maxOffset == 0)
            {
                return;
            }

            VerticalOffset = e.WheelDelta > 0 ? Math.Max(0, VerticalOffset - 1) : Math.Min(maxOffset, VerticalOffset + 1);
        }

        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var uiX = e.UiX;
        var uiY = e.UiY;

        var vRect = _verticalBar.Bounds;
        if (vRect.Width > 0 && vRect.Height > 0 && vRect.Contains(uiX, uiY))
        {
            var viewportHeight = vRect.Height;
            var maxOffset = Math.Max(0, _contentHeight - viewportHeight);
            if (maxOffset > 0)
            {
                var local = uiY - vRect.Y;
                if (TryGetVerticalThumb(viewportHeight, out var thumbStart, out var thumbLen))
                {
                    if (local >= thumbStart && local < thumbStart + thumbLen)
                    {
                        _draggingVertical = true;
                        _dragStartUiY = uiY;
                        _dragStartVerticalOffset = VerticalOffset;
                    }
                    else
                    {
                        var page = viewportHeight;
                        VerticalOffset = local < thumbStart ? Math.Max(0, VerticalOffset - page) : Math.Min(maxOffset, VerticalOffset + page);
                    }
                }

                e.Handled = true;
                return;
            }
        }

        var hRect = _horizontalBar.Bounds;
        if (hRect.Width > 0 && hRect.Height > 0 && hRect.Contains(uiX, uiY))
        {
            var viewportWidth = hRect.Width;
            var maxOffset = Math.Max(0, _contentWidth - viewportWidth);
            if (maxOffset > 0)
            {
                var local = uiX - hRect.X;
                if (TryGetHorizontalThumb(viewportWidth, out var thumbStart, out var thumbLen))
                {
                    if (local >= thumbStart && local < thumbStart + thumbLen)
                    {
                        _draggingHorizontal = true;
                        _dragStartUiX = uiX;
                        _dragStartHorizontalOffset = HorizontalOffset;
                    }
                    else
                    {
                        var page = viewportWidth;
                        HorizontalOffset = local < thumbStart ? Math.Max(0, HorizontalOffset - page) : Math.Min(maxOffset, HorizontalOffset + page);
                    }
                }

                e.Handled = true;
                return;
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_draggingVertical && !_draggingHorizontal)
        {
            return;
        }

        if (_draggingVertical)
        {
            var viewportHeight = Math.Max(1, _verticalBar.Bounds.Height);
            var maxOffset = Math.Max(0, _contentHeight - viewportHeight);
            if (maxOffset > 0)
            {
                var trackLen = Math.Max(1, viewportHeight - GetThumbLength(viewportHeight, _contentHeight));
                var delta = e.UiY - _dragStartUiY;
                var deltaOffset = (int)Math.Round((double)delta * maxOffset / trackLen);
                VerticalOffset = Math.Clamp(_dragStartVerticalOffset + deltaOffset, 0, maxOffset);
                e.Handled = true;
            }
        }

        if (_draggingHorizontal)
        {
            var viewportWidth = Math.Max(1, _horizontalBar.Bounds.Width);
            var maxOffset = Math.Max(0, _contentWidth - viewportWidth);
            if (maxOffset > 0)
            {
                var trackLen = Math.Max(1, viewportWidth - GetThumbLength(viewportWidth, _contentWidth));
                var delta = e.UiX - _dragStartUiX;
                var deltaOffset = (int)Math.Round((double)delta * maxOffset / trackLen);
                HorizontalOffset = Math.Clamp(_dragStartHorizontalOffset + deltaOffset, 0, maxOffset);
                e.Handled = true;
            }
        }
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (_draggingVertical || _draggingHorizontal)
        {
            _draggingVertical = false;
            _draggingHorizontal = false;
            e.Handled = true;
        }
    }

    private bool TryGetVerticalThumb(Rectangle rect, int viewportHeight, out int thumbStart, out int thumbLen)
    {
        var maxOffset = Math.Max(0, _contentHeight - viewportHeight);
        thumbLen = GetThumbLength(viewportHeight, _contentHeight);
        var trackLen = Math.Max(1, viewportHeight - thumbLen);
        thumbStart = maxOffset == 0 ? 0 : (int)Math.Round((double)VerticalOffset * trackLen / maxOffset);
        thumbStart = Math.Clamp(thumbStart, 0, trackLen);
        return true;
    }

    private bool TryGetHorizontalThumb(Rectangle rect, int viewportWidth, out int thumbStart, out int thumbLen)
    {
        var maxOffset = Math.Max(0, _contentWidth - viewportWidth);
        thumbLen = GetThumbLength(viewportWidth, _contentWidth);
        var trackLen = Math.Max(1, viewportWidth - thumbLen);
        thumbStart = maxOffset == 0 ? 0 : (int)Math.Round((double)HorizontalOffset * trackLen / maxOffset);
        thumbStart = Math.Clamp(thumbStart, 0, trackLen);
        return true;
    }

    private bool TryGetVerticalThumb(int viewportHeight, out int thumbStart, out int thumbLen) => TryGetVerticalThumb(Bounds, viewportHeight, out thumbStart, out thumbLen);

    private bool TryGetHorizontalThumb(int viewportWidth, out int thumbStart, out int thumbLen) => TryGetHorizontalThumb(Bounds, viewportWidth, out thumbStart, out thumbLen);

    private static int GetThumbLength(int viewport, int content)
    {
        if (content <= 0)
        {
            return 1;
        }

        return Math.Clamp(Math.Max(1, (int)Math.Round((double)viewport * viewport / content)), 1, viewport);
    }

    private bool IsFocusWithin()
    {
        var focused = App?.FocusedElement;
        for (var v = focused; v is not null; v = v.Parent)
        {
            if (ReferenceEquals(v, this))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class ContentViewportHost : Visuals.Visual
    {
        private readonly ScrollViewer _owner;
        private Visuals.Visual? _child;

        private int _contentWidth;
        private int _contentHeight;
        private int _horizontalOffset;
        private int _verticalOffset;

        public ContentViewportHost(ScrollViewer owner)
        {
            _owner = owner;
        }

        public void SetContent(Visuals.Visual child)
        {
            if (_child is not null)
            {
                throw new InvalidOperationException("ScrollViewer content host currently only supports setting content once.");
            }

            _child = child;
            AddChild(child);
        }

        public void UpdateLayout(int contentWidth, int contentHeight, int horizontalOffset, int verticalOffset)
        {
            _contentWidth = contentWidth;
            _contentHeight = contentHeight;
            _horizontalOffset = horizontalOffset;
            _verticalOffset = verticalOffset;
        }

        protected override void ArrangeOverride(Rectangle finalRect)
        {
            Bounds = finalRect;

            if (_child is null)
            {
                return;
            }

            _child.Arrange(new Rectangle(finalRect.X - _horizontalOffset, finalRect.Y - _verticalOffset, _contentWidth, _contentHeight));
        }
    }

    private abstract class ScrollBarVisualBase : Visuals.Visual
    {
        protected readonly ScrollViewer Owner;

        protected ScrollBarVisualBase(ScrollViewer owner)
        {
            Owner = owner;
        }

        protected (Cell Track, Cell Thumb, ScrollBarGlyphs Glyphs) GetStyles()
        {
            var theme = GetTheme();
            var focused = Owner.IsFocusWithin();
            var style = GetEnvironmentValue(ScrollViewerStyle.Key);
            return (style.ResolveTrackStyle(theme), style.ResolveThumbStyle(theme, focused), theme.ScrollBars);
        }
    }

    private sealed class VerticalScrollBarVisual : ScrollBarVisualBase
    {
        public VerticalScrollBarVisual(ScrollViewer owner) : base(owner)
        {
        }

        protected override void RenderOverride(CellBuffer buffer)
        {
            var rect = Bounds;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            var viewportHeight = rect.Height;
            var thickness = rect.Width;

            var maxOffset = Math.Max(0, Owner._contentHeight - viewportHeight);
            var thumbLen = GetThumbLength(viewportHeight, Owner._contentHeight);
            var trackLen = Math.Max(1, viewportHeight - thumbLen);
            var thumbStart = maxOffset == 0 ? 0 : (int)Math.Round((double)Owner.VerticalOffset * trackLen / maxOffset);
            thumbStart = Math.Clamp(thumbStart, 0, trackLen);

            var (trackStyle, thumbStyle, glyphs) = GetStyles();
            for (var y = 0; y < viewportHeight; y++)
            {
                var isThumb = y >= thumbStart && y < thumbStart + thumbLen;
                var ch = isThumb ? glyphs.Thumb : glyphs.Track;
                var st = isThumb ? thumbStyle : trackStyle;
                for (var dx = 0; dx < thickness; dx++)
                {
                    buffer.SetCell(rect.X + dx, rect.Y + y, new Rune(ch), st);
                }
            }
        }
    }

    private sealed class HorizontalScrollBarVisual : ScrollBarVisualBase
    {
        public HorizontalScrollBarVisual(ScrollViewer owner) : base(owner)
        {
        }

        protected override void RenderOverride(CellBuffer buffer)
        {
            var rect = Bounds;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            var viewportWidth = rect.Width;
            var thickness = rect.Height;

            var maxOffset = Math.Max(0, Owner._contentWidth - viewportWidth);
            var thumbLen = GetThumbLength(viewportWidth, Owner._contentWidth);
            var trackLen = Math.Max(1, viewportWidth - thumbLen);
            var thumbStart = maxOffset == 0 ? 0 : (int)Math.Round((double)Owner.HorizontalOffset * trackLen / maxOffset);
            thumbStart = Math.Clamp(thumbStart, 0, trackLen);

            var (trackStyle, thumbStyle, glyphs) = GetStyles();
            for (var x = 0; x < viewportWidth; x++)
            {
                var isThumb = x >= thumbStart && x < thumbStart + thumbLen;
                var ch = isThumb ? glyphs.Thumb : glyphs.Track;
                var st = isThumb ? thumbStyle : trackStyle;
                for (var dy = 0; dy < thickness; dy++)
                {
                    buffer.SetCell(rect.X + x, rect.Y + dy, new Rune(ch), st);
                }
            }
        }
    }

    private sealed class ScrollCornerVisual : ScrollBarVisualBase
    {
        public ScrollCornerVisual(ScrollViewer owner) : base(owner)
        {
        }

        protected override void RenderOverride(CellBuffer buffer)
        {
            var rect = Bounds;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            var (trackStyle, _, glyphs) = GetStyles();
            for (var y = 0; y < rect.Height; y++)
            {
                for (var x = 0; x < rect.Width; x++)
                {
                    buffer.SetCell(rect.X + x, rect.Y + y, new Rune(glyphs.Track), trackStyle);
                }
            }
        }
    }
}
