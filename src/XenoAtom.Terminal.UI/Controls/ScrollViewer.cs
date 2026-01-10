// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class ScrollViewer : Visual
{
    private readonly ContentViewportHost _contentHost;
    private readonly ScrollBar _verticalBar;
    private readonly ScrollBar _horizontalBar;
    private readonly ScrollCornerVisual _corner;
    private Visual? _content;

    private int _contentWidth;
    private int _contentHeight;

    private bool _showHorizontalBar;
    private bool _showVerticalBar;
    private ScrollBarStyle? _internalScrollBarStyle;

    public ScrollViewer()
    {
        Focusable = true;
        VerticalAlignment = VerticalAlignment.Stretch;
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _contentHost = new ContentViewportHost(this);
        _verticalBar = new ScrollBar(focusable: false).Orientation(Orientation.Vertical);
        _horizontalBar = new ScrollBar(focusable: false).Orientation(Orientation.Horizontal);
        _corner = new ScrollCornerVisual(this);

        _verticalBar.ValueChanged(static (s, e) =>
        {
            if (s is ScrollBar { Parent: ScrollViewer owner })
            {
                owner.VerticalOffset = e.NewValue;
            }
        });

        _horizontalBar.ValueChanged(static (s, e) =>
        {
            if (s is ScrollBar { Parent: ScrollViewer owner })
            {
                owner.HorizontalOffset = e.NewValue;
            }
        });

        AttachChild(_contentHost);
        AttachChild(_verticalBar);
        AttachChild(_horizontalBar);
        AttachChild(_corner);
    }

    protected override int ChildrenCount => 4;

    protected override Visual GetChild(int index)
        => index switch
        {
            0 => _contentHost,
            1 => _verticalBar,
            2 => _horizontalBar,
            3 => _corner,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };

    [Bindable]
    public Visual? Content
    {
        get
        {
            VerifyAccess();
            BindingManager.Current.RegisterRead(this, __Content__BindingAccessor.Instance);
            return _content;
        }
        set
        {
            VerifyAccess();
            if (ReferenceEquals(_content, value))
            {
                return;
            }

            _content = value;
            _contentHost.SetContent(value);

            BindingManager.Current.NotifyValueChanged(this, __Content__BindingAccessor.Instance);
        }
    }


    [Bindable]
    public partial int VerticalOffset { get; set; }

    [Bindable]
    public partial int HorizontalOffset { get; set; }

    [Bindable]
    public partial int Height { get; set; }

    partial void OnVerticalOffsetChanged(int value) => MarkArrangeDirty();

    partial void OnHorizontalOffsetChanged(int value) => MarkArrangeDirty();

    protected override Size MeasureOverride(Size availableSize)
    {
        var height = Math.Max(1, Height);
        var content = Content;
        if (content is not null)
        {
            content.Measure(new Size(int.MaxValue / 4, int.MaxValue / 4));
            _contentWidth = content.DesiredSize.Width;
            _contentHeight = content.DesiredSize.Height;
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

        if (Content is null)
        {
            return;
        }

        var style = Get<ScrollViewerStyle>();
        var thickness = Math.Max(1, style.ScrollBarThickness);
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

        // Keep scrollbars in sync with the viewport/content model (two-way via ValueChanged).
        _verticalBar.Minimum(0);
        _verticalBar.Maximum(maxVerticalOffset);
        _verticalBar.ViewportSize(contentViewportHeight);
        _verticalBar.Value(v);
        _verticalBar.IsVisible = _showVerticalBar;

        _horizontalBar.Minimum(0);
        _horizontalBar.Maximum(maxHorizontalOffset);
        _horizontalBar.ViewportSize(contentViewportWidth);
        _horizontalBar.Value(hOffset);
        _horizontalBar.IsVisible = _showHorizontalBar;

        // Bridge ScrollViewerStyle to ScrollBarStyle for internal bars.
        var scrollBarStyle = new ScrollBarStyle
        {
            Thickness = thickness,
            TrackStyle = style.TrackStyle,
            ThumbStyle = style.ThumbStyle,
        };
        if (!Equals(_internalScrollBarStyle, scrollBarStyle))
        {
            _internalScrollBarStyle = scrollBarStyle;
            _verticalBar.Set(scrollBarStyle);
            _horizontalBar.Set(scrollBarStyle);
        }

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

    private sealed class ContentViewportHost : Visual
    {
        private readonly ScrollViewer _owner;
        private Visual? _child;

        private int _contentWidth;
        private int _contentHeight;
        private int _horizontalOffset;
        private int _verticalOffset;

        public ContentViewportHost(ScrollViewer owner)
        {
            _owner = owner;
            this.HorizontalAlignment(HorizontalAlignment.Stretch);
            this.VerticalAlignment(VerticalAlignment.Stretch);
        }

        public void SetContent(Visual? child)
        {
            if (_child is not null)
            {
                DetachChild(_child);
            }
            _child = child;
            if (child is not null)
            {
                AttachChild(child);
            }
            MarkMeasureDirty();
        }

        protected override int ChildrenCount => _child is null ? 0 : 1;

        protected override Visual GetChild(int index)
            => index == 0 && _child is not null ? _child : throw new ArgumentOutOfRangeException(nameof(index));

        public void UpdateLayout(int contentWidth, int contentHeight, int horizontalOffset, int verticalOffset)
        {
            if (_contentWidth == contentWidth
                && _contentHeight == contentHeight
                && _horizontalOffset == horizontalOffset
                && _verticalOffset == verticalOffset)
            {
                return;
            }

            _contentWidth = contentWidth;
            _contentHeight = contentHeight;
            _horizontalOffset = horizontalOffset;
            _verticalOffset = verticalOffset;

            MarkArrangeDirty();
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

    private sealed class ScrollCornerVisual : Visual
    {
        private readonly ScrollViewer _owner;

        public ScrollCornerVisual(ScrollViewer owner)
        {
            _owner = owner;
            this.HorizontalAlignment(HorizontalAlignment.Stretch);
            this.VerticalAlignment(VerticalAlignment.Stretch);
        }

        protected override void RenderOverride(CellBuffer buffer)
        {
            var rect = Bounds;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            var theme = GetTheme();
            var style = _owner.Get<ScrollViewerStyle>();
            var trackStyle = style.ResolveTrackStyle(theme);
            var glyphs = theme.ScrollBars;

            for (var y = 0; y < rect.Height; y++)
            {
                for (var x = 0; x < rect.Width; x++)
                {
                    buffer.SetCell(rect.X + x, rect.Y + y, glyphs.Track, trackStyle);
                }
            }
        }
    }
}
