// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Scrolling;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Hosts a single content visual and provides horizontal/vertical scrolling with scroll bars.
/// </summary>
public sealed partial class ScrollViewer : Visual
{
    private readonly ContentViewportHost _contentHost;
    private readonly ScrollBar _verticalBar;
    private readonly ScrollBar _horizontalBar;
    private readonly ScrollCornerVisual _corner;
    private Visual? _content;
    private IScrollable? _contentScrollable;
    private ScrollModel? _contentScrollModel;

    private int _contentWidth;
    private int _contentHeight;
    private SizeHints _extentHints;

    private bool _showHorizontalBar;
    private bool _showVerticalBar;
    private ScrollBarStyle? _internalScrollBarStyle;
    private bool _syncingOffsets;

    /// <summary>
    /// Initializes a new instance of the ScrollViewer class with default settings, enabling content scrolling and
    /// displaying scroll bars as needed.
    /// </summary>
    /// <remarks>The ScrollViewer is configured to be focusable and stretches to fill its parent container by
    /// default. It automatically creates and attaches vertical and horizontal scroll bars, as well as a content host
    /// and scroll corner visual, to support scrolling functionality. After construction, you can add content to the
    /// ScrollViewer and customize its behavior as required.</remarks>
    public ScrollViewer()
    {
        Focusable = true;
        VerticalAlignment = VerticalAlignment.Stretch;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        this.HorizontalScrollEnabled(true);
        this.VerticalScrollEnabled(true);

        _contentHost = new ContentViewportHost(this);
        // Internal scrollbars may receive their Thickness style during Arrange (when ScrollViewerStyle is bridged to
        // ScrollBarStyle). Stretch the cross axis so they still fill the allocated slot even if their desired size
        // was measured with a different thickness.
        _verticalBar = new VScrollBar(focusable: false).HorizontalAlignment(HorizontalAlignment.Stretch);
        _horizontalBar = new HScrollBar(focusable: false).VerticalAlignment(VerticalAlignment.Stretch);
        _corner = new ScrollCornerVisual(this);

        _verticalBar.ValueChanged(OnVerticalBarValueChanged);
        _horizontalBar.ValueChanged(OnHorizontalBarValueChanged);

        AttachChild(_contentHost);
        AttachChild(_verticalBar);
        AttachChild(_horizontalBar);
        AttachChild(_corner);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScrollViewer"/> with content.
    /// </summary>
    /// <param name="content">The content visual.</param>
    public ScrollViewer(Visual? content) : this()
    {
        Content = content;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScrollViewer"/> with dynamic content.
    /// </summary>
    /// <param name="contentFactory">A factory that produces the content visual.</param>
    public ScrollViewer(Func<Visual?> contentFactory) : this()
    {
        this.Content(contentFactory);
    }

    /// <inheritdoc/>
    protected override int ChildrenCount => 4;

    /// <inheritdoc/>
    protected override Visual GetChild(int index)
        => index switch
        {
            0 => _contentHost,
            1 => _verticalBar,
            2 => _horizontalBar,
            3 => _corner,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };

    /// <summary>
    /// Gets or sets the content visual.
    /// </summary>
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
            UpdateContentScrollable(value as IScrollable);

            BindingManager.Current.NotifyValueChanged(this, __Content__BindingAccessor.Instance);
        }
    }

    /// <summary>
    /// Gets the content scroll interface when the content implements <see cref="IScrollable"/>.
    /// </summary>
    public IScrollable? ContentScrollable => _contentScrollable;

    /// <summary>
    /// Gets or sets a value indicating whether horizontal scrolling is enabled.
    /// </summary>
    /// <remarks>
    /// When disabled, the horizontal scroll bar is never shown and <see cref="HorizontalOffset"/> is forced to <c>0</c>.
    /// This is commonly used for document-like content that should wrap to the viewport width.
    /// </remarks>
    [Bindable]
    public partial bool HorizontalScrollEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether vertical scrolling is enabled.
    /// </summary>
    /// <remarks>
    /// When disabled, the vertical scroll bar is never shown and <see cref="VerticalOffset"/> is forced to <c>0</c>.
    /// </remarks>
    [Bindable]
    public partial bool VerticalScrollEnabled { get; set; }


    /// <summary>
    /// Gets or sets the vertical scroll offset.
    /// </summary>
    [Bindable]
    public partial int VerticalOffset { get; set; }

    /// <summary>
    /// Gets or sets the horizontal scroll offset.
    /// </summary>
    [Bindable]
    public partial int HorizontalOffset { get; set; }

    partial void OnHorizontalScrollEnabledChanged(bool value)
    {
        if (!value)
        {
            HorizontalOffset = 0;
        }

        MarkArrangeDirty();
    }

    partial void OnVerticalScrollEnabledChanged(bool value)
    {
        if (!value)
        {
            VerticalOffset = 0;
        }

        MarkArrangeDirty();
    }

    partial void OnVerticalOffsetChanged(int value)
    {
        if (_syncingOffsets)
        {
            return;
        }

        if (!VerticalScrollEnabled)
        {
            if (value != 0)
            {
                VerticalOffset = 0;
            }
            return;
        }

        if (_contentScrollModel is not null)
        {
            _contentScrollModel.SetOffset(_contentScrollModel.OffsetX, value);
        }

        MarkArrangeDirty();
    }

    partial void OnHorizontalOffsetChanged(int value)
    {
        if (_syncingOffsets)
        {
            return;
        }

        if (!HorizontalScrollEnabled)
        {
            if (value != 0)
            {
                HorizontalOffset = 0;
            }
            return;
        }

        if (_contentScrollModel is not null)
        {
            _contentScrollModel.SetOffset(value, _contentScrollModel.OffsetY);
        }

        MarkArrangeDirty();
    }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var content = Content;
        if (content is not null)
        {
            SizeHints hints;
            if (_contentScrollModel is not null)
            {
                hints = content.Measure(constraints);
            }
            else
            {
                var maxWidth = HorizontalScrollEnabled ? LayoutConstants.Infinite : constraints.MaxWidth;
                var maxHeight = VerticalScrollEnabled ? LayoutConstants.Infinite : constraints.MaxHeight;
                var childConstraints = new LayoutConstraints(0, maxWidth, 0, maxHeight);
                hints = content.Measure(childConstraints);
            }

            _contentWidth = hints.Natural.Width;
            _contentHeight = hints.Natural.Height;
            _extentHints = hints;
        }
        else
        {
            _contentWidth = 0;
            _contentHeight = 0;
            _extentHints = SizeHints.Fixed(Size.Zero);
        }

        var desiredWidth = constraints.IsWidthBounded ? Math.Min(_contentWidth, constraints.MaxWidth) : _contentWidth;
        var desiredHeight = constraints.IsHeightBounded ? Math.Min(_contentHeight, constraints.MaxHeight) : _contentHeight;

        if (content is not null)
        {
            desiredWidth = Math.Max(1, desiredWidth);
            desiredHeight = Math.Max(1, desiredHeight);
        }

        var min = content is null ? Size.Zero : new Size(1, 1);
        var natural = new Size(desiredWidth, desiredHeight);
        var max = new Size(LayoutConstants.Infinite, LayoutConstants.Infinite);
        return SizeHints.Flex(
            min,
            natural,
            max,
            growX: HorizontalAlignment == HorizontalAlignment.Stretch ? 1 : 0,
            growY: VerticalAlignment == VerticalAlignment.Stretch ? 1 : 0,
            shrinkX: 1,
            shrinkY: 1);
    }

    /// <inheritdoc/>
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        if (Content is null)
        {
            return;
        }

        var verticalScrollEnabled = VerticalScrollEnabled;
        var horizontalScrollEnabled = HorizontalScrollEnabled;

        var style = Get<ScrollViewerStyle>();
        var thickness = Math.Max(1, style.ScrollBarThickness);
        var viewportWidth = Math.Max(1, finalRect.Width);
        var viewportHeight = Math.Max(1, finalRect.Height);

        var useContentScroll = _contentScrollModel is not null;
        var extentHints = _extentHints;
        var extentWidth = 0;
        var extentHeight = 0;

        bool CanShrinkToWidth(int width)
            => !useContentScroll && extentHints.FlexShrinkX > 0 && extentHints.Min.Width <= Math.Max(0, width);

        // Start from the previous visibility state to avoid oscillating between
        // "no bars" and "bars" on every Arrange() call for content-provided scroll models.
        var showV = _showVerticalBar;
        var showH = _showHorizontalBar;
        var contentViewportWidth = viewportWidth;
        var contentViewportHeight = viewportHeight;

        var modelViewportWidth = 0;
        var modelViewportHeight = 0;

        var v = 0;
        var hOffset = 0;

        if (useContentScroll)
        {
            var scrollModel = _contentScrollModel;
            if (scrollModel is null)
            {
                return;
            }

            // When a content provides a ScrollModel (e.g. TextArea), the content owns its viewport size
            // (it depends on padding/borders and any internal chrome). ScrollViewer must not call
            // ScrollModel.SetViewport(), otherwise it will fight with the content and cause oscillations
            // that can pin the caret and prevent scrolling.
            for (var pass = 0; pass < 3; pass++)
            {
                contentViewportWidth = Math.Max(1, viewportWidth - (showV ? thickness : 0));
                contentViewportHeight = Math.Max(1, viewportHeight - (showH ? thickness : 0));

                _contentHost.UpdateLayout(contentViewportWidth, contentViewportHeight, 0, 0);
                _contentHost.Arrange(new Rectangle(finalRect.X, finalRect.Y, contentViewportWidth, contentViewportHeight));

                extentWidth = Math.Max(0, scrollModel.ExtentWidth);
                extentHeight = Math.Max(0, scrollModel.ExtentHeight);
                modelViewportWidth = Math.Max(0, scrollModel.ViewportWidth);
                modelViewportHeight = Math.Max(0, scrollModel.ViewportHeight);

                var nextShowV = verticalScrollEnabled && extentHeight > modelViewportHeight;
                var nextShowH = horizontalScrollEnabled && extentWidth > modelViewportWidth;
                if (nextShowV == showV && nextShowH == showH)
                {
                    break;
                }

                showV = nextShowV;
                showH = nextShowH;
            }

            _showVerticalBar = showV;
            _showHorizontalBar = showH;

            var maxVerticalOffset = Math.Max(0, extentHeight - modelViewportHeight);
            var maxHorizontalOffset = Math.Max(0, extentWidth - modelViewportWidth);

            v = verticalScrollEnabled ? Math.Clamp(scrollModel.OffsetY, 0, maxVerticalOffset) : 0;
            hOffset = horizontalScrollEnabled ? Math.Clamp(scrollModel.OffsetX, 0, maxHorizontalOffset) : 0;
            if (v != scrollModel.OffsetY || hOffset != scrollModel.OffsetX)
            {
                scrollModel.SetOffset(hOffset, v);
            }

            SyncOffsetsFromContent();

            _verticalBar.Minimum = 0;
            _verticalBar.Maximum = maxVerticalOffset;
            _verticalBar.ViewportSize = modelViewportHeight;
            SetBarValue(_verticalBar, v);
            _verticalBar.IsVisible = _showVerticalBar;

            _horizontalBar.Minimum = 0;
            _horizontalBar.Maximum = maxHorizontalOffset;
            _horizontalBar.ViewportSize = modelViewportWidth;
            SetBarValue(_horizontalBar, hOffset);
            _horizontalBar.IsVisible = _showHorizontalBar;
        }
        else
        {
            extentWidth = extentHints.Natural.Width;
            extentHeight = extentHints.Natural.Height;

            var lastMeasuredViewportWidth = -1;

            showV = verticalScrollEnabled && extentHeight > viewportHeight;
            showH = horizontalScrollEnabled && extentWidth > viewportWidth && !CanShrinkToWidth(viewportWidth);

            // Determine which bars to show. If horizontal scrolling isn't needed, re-measure the content
            // using the final viewport width so width-dependent layout (e.g. wrapping) can report a correct height.
            for (var pass = 0; pass < 4; pass++)
            {
                // account for bars and re-evaluate.
                for (var i = 0; i < 2; i++)
                {
                    var w = viewportWidth - (showV ? thickness : 0);
                    var hViewport = viewportHeight - (showH ? thickness : 0);
                    showV = verticalScrollEnabled && extentHeight > Math.Max(1, hViewport);
                    showH = horizontalScrollEnabled && extentWidth > Math.Max(1, w) && !CanShrinkToWidth(Math.Max(1, w));
                }

                contentViewportWidth = Math.Max(1, viewportWidth - (showV ? thickness : 0));
                contentViewportHeight = Math.Max(1, viewportHeight - (showH ? thickness : 0));

                if (showH)
                {
                    break;
                }

                if (lastMeasuredViewportWidth == contentViewportWidth)
                {
                    break;
                }

                lastMeasuredViewportWidth = contentViewportWidth;

                var content = Content;
                if (content is null)
                {
                    break;
                }

                var forWidthHints = content.Measure(new LayoutConstraints(0, contentViewportWidth, 0, LayoutConstants.Infinite));
                extentWidth = forWidthHints.Natural.Width;
                extentHeight = forWidthHints.Natural.Height;

                _contentWidth = extentWidth;
                _contentHeight = extentHeight;

                // Continue loop to re-evaluate vertical bar visibility (height may have changed due to wrapping).
                showV = verticalScrollEnabled && extentHeight > viewportHeight;
                showH = horizontalScrollEnabled && extentWidth > viewportWidth && !CanShrinkToWidth(viewportWidth);
            }

            _showVerticalBar = showV;
            _showHorizontalBar = showH;

            var maxVerticalOffset = Math.Max(0, extentHeight - contentViewportHeight);
            var maxHorizontalOffset = Math.Max(0, extentWidth - contentViewportWidth);

            v = verticalScrollEnabled ? Math.Clamp(VerticalOffset, 0, maxVerticalOffset) : 0;
            hOffset = horizontalScrollEnabled ? Math.Clamp(HorizontalOffset, 0, maxHorizontalOffset) : 0;
            if (v != VerticalOffset) VerticalOffset = v;
            if (hOffset != HorizontalOffset) HorizontalOffset = hOffset;

            _verticalBar.Minimum = 0;
            _verticalBar.Maximum = maxVerticalOffset;
            _verticalBar.ViewportSize = contentViewportHeight;
            SetBarValue(_verticalBar, v);
            _verticalBar.IsVisible = _showVerticalBar;

            _horizontalBar.Minimum = 0;
            _horizontalBar.Maximum = maxHorizontalOffset;
            _horizontalBar.ViewportSize = contentViewportWidth;
            SetBarValue(_horizontalBar, hOffset);
            _horizontalBar.IsVisible = _showHorizontalBar;

            var contentArrangeWidth = showH ? extentWidth : contentViewportWidth;
            var contentArrangeHeight = showV ? extentHeight : contentViewportHeight;
            _contentHost.UpdateLayout(contentArrangeWidth, contentArrangeHeight, hOffset, v);
            _contentHost.Arrange(new Rectangle(finalRect.X, finalRect.Y, contentViewportWidth, contentViewportHeight));
        }

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

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!VerticalScrollEnabled && !HorizontalScrollEnabled)
        {
            return;
        }

        var useContentScroll = _contentScrollModel is not null;
        var viewportWidth = useContentScroll ? Math.Max(1, _contentScrollModel!.ViewportWidth) : Math.Max(1, _contentHost.Bounds.Width);
        var viewportHeight = useContentScroll ? Math.Max(1, _contentScrollModel!.ViewportHeight) : Math.Max(1, _contentHost.Bounds.Height);
        var extentHeight = useContentScroll ? _contentScrollModel!.ExtentHeight : _contentHeight;
        var extentWidth = useContentScroll ? _contentScrollModel!.ExtentWidth : _contentWidth;

        var maxVerticalOffset = Math.Max(0, extentHeight - viewportHeight);
        var maxHorizontalOffset = Math.Max(0, extentWidth - viewportWidth);

        switch (e.Key)
        {
            case TerminalKey.Up:
                if (!VerticalScrollEnabled) return;
                VerticalOffset = Math.Max(0, VerticalOffset - 1);
                e.Handled = true;
                return;
            case TerminalKey.Down:
                if (!VerticalScrollEnabled) return;
                VerticalOffset = Math.Min(maxVerticalOffset, VerticalOffset + 1);
                e.Handled = true;
                return;
            case TerminalKey.PageUp:
                if (!VerticalScrollEnabled) return;
                VerticalOffset = Math.Max(0, VerticalOffset - viewportHeight);
                e.Handled = true;
                return;
            case TerminalKey.PageDown:
                if (!VerticalScrollEnabled) return;
                VerticalOffset = Math.Min(maxVerticalOffset, VerticalOffset + viewportHeight);
                e.Handled = true;
                return;
            case TerminalKey.Home:
                if (!VerticalScrollEnabled) return;
                VerticalOffset = 0;
                e.Handled = true;
                return;
            case TerminalKey.End:
                if (!VerticalScrollEnabled) return;
                VerticalOffset = maxVerticalOffset;
                e.Handled = true;
                return;
            case TerminalKey.Left:
                if (!HorizontalScrollEnabled) return;
                HorizontalOffset = Math.Max(0, HorizontalOffset - 1);
                e.Handled = true;
                return;
            case TerminalKey.Right:
                if (!HorizontalScrollEnabled) return;
                HorizontalOffset = Math.Min(maxHorizontalOffset, HorizontalOffset + 1);
                e.Handled = true;
                return;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerWheel(PointerEventArgs e)
    {
        if (e.WheelDelta == 0)
        {
            return;
        }

        var useContentScroll = _contentScrollModel is not null;

        if ((e.Modifiers & TerminalModifiers.Shift) != 0)
        {
            if (!HorizontalScrollEnabled) return;
            var viewportWidth = useContentScroll ? Math.Max(1, _contentScrollModel!.ViewportWidth) : Math.Max(1, _contentHost.Bounds.Width);
            var extentWidth = useContentScroll ? _contentScrollModel!.ExtentWidth : _contentWidth;
            var maxOffset = Math.Max(0, extentWidth - viewportWidth);
            if (maxOffset == 0)
            {
                return;
            }

            HorizontalOffset = e.WheelDelta > 0 ? Math.Max(0, HorizontalOffset - 1) : Math.Min(maxOffset, HorizontalOffset + 1);
        }
        else
        {
            if (!VerticalScrollEnabled) return;
            var viewportHeight = useContentScroll ? Math.Max(1, _contentScrollModel!.ViewportHeight) : Math.Max(1, _contentHost.Bounds.Height);
            var extentHeight = useContentScroll ? _contentScrollModel!.ExtentHeight : _contentHeight;
            var maxOffset = Math.Max(0, extentHeight - viewportHeight);
            if (maxOffset == 0)
            {
                return;
            }

            VerticalOffset = e.WheelDelta > 0 ? Math.Max(0, VerticalOffset - 1) : Math.Min(maxOffset, VerticalOffset + 1);
        }

        e.Handled = true;
    }

    private void UpdateContentScrollable(IScrollable? scrollable)
    {
        if (_contentScrollModel is not null)
        {
            _contentScrollModel.Changed -= OnContentScrollChanged;
        }

        _contentScrollable = scrollable;
        _contentScrollModel = scrollable?.Scroll;

        if (_contentScrollModel is not null)
        {
            _contentScrollModel.Changed += OnContentScrollChanged;
        }
    }

    private void OnContentScrollChanged()
    {
        if (_contentScrollModel is null)
        {
            return;
        }

        SyncOffsetsFromContent();
        MarkArrangeDirty();
    }

    private void SyncOffsetsFromContent()
    {
        if (_contentScrollModel is null)
        {
            return;
        }

        _syncingOffsets = true;
        try
        {
            VerticalOffset = _contentScrollModel.OffsetY;
            HorizontalOffset = _contentScrollModel.OffsetX;
        }
        finally
        {
            _syncingOffsets = false;
        }
    }

    private void OnVerticalBarValueChanged(object? sender, ScrollValueChangedEventArgs e)
    {
        if (_syncingOffsets)
        {
            return;
        }

        VerticalOffset = e.NewValue;
    }

    private void OnHorizontalBarValueChanged(object? sender, ScrollValueChangedEventArgs e)
    {
        if (_syncingOffsets)
        {
            return;
        }

        HorizontalOffset = e.NewValue;
    }

    private void SetBarValue(ScrollBar bar, int value)
    {
        if (bar.Value == value)
        {
            return;
        }

        _syncingOffsets = true;
        try
        {
            bar.Value = value;
        }
        finally
        {
            _syncingOffsets = false;
        }
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

            MarkArrangeDirtyLocal();
        }

        protected override void ArrangeCore(in Rectangle finalRect)
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
