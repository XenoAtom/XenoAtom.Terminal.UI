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
/// Specifies the direction in which a <see cref="Collapsible"/> expands.
/// </summary>
public enum ExpandDirection
{
    /// <summary>
    /// Expands downward (header on top).
    /// </summary>
    Down = 0,

    /// <summary>
    /// Expands upward (header on bottom).
    /// </summary>
    Up = 1,
}

/// <summary>
/// Displays a header that can expand/collapse a content region.
/// </summary>
public sealed partial class Collapsible : Visual
{
    private bool _hasExpandedStateForEvent;
    private bool _lastExpandedForEvent;
    private Visual? _attachedContent;

    [Bindable]
    internal partial bool IsHeaderHovered { get; set; }

    [Bindable]
    internal partial bool IsHeaderPressed { get; set; }

    private Rectangle _headerRect;
    private Rectangle _contentRect;

    /// <summary>
    /// Initializes a new instance of the <see cref="Collapsible"/> class.
    /// </summary>
    public Collapsible()
    {
        Focusable = true;
        this.Direction(ExpandDirection.Down);
    }

    /// <summary>
    /// Initializes a new collapsible with a header and content.
    /// </summary>
    /// <param name="header">The header visual.</param>
    /// <param name="content">The content visual.</param>
    public Collapsible(Visual header, Visual content) : this()
    {
        this.Header(header);
        this.Content(content);
    }

    /// <summary>
    /// Gets or sets the header visual.
    /// </summary>
    [Bindable]
    public partial Visual? Header { get; set; }

    /// <summary>
    /// Gets or sets the content visual.
    /// </summary>
    [Bindable(NoVisualAttach = true)]
    public partial Visual? Content { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the content is expanded.
    /// </summary>
    [Bindable]
    public partial bool IsExpanded { get; set; }

    /// <summary>
    /// Gets or sets the expansion direction.
    /// </summary>
    [Bindable]
    public partial ExpandDirection Direction { get; set; }

    /// <inheritdoc/>
    protected override int ChildrenCount
        => (_header is null ? 0 : 1) + (_attachedContent is null ? 0 : 1);

    /// <inheritdoc/>
    protected override Visual GetChild(int index)
    {
        if (_header is null)
        {
            if (_attachedContent is not null && index == 0)
            {
                return _attachedContent;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (index == 0)
        {
            return _header;
        }

        if (_attachedContent is not null && index == 1)
        {
            return _attachedContent;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = GetStyle<CollapsibleStyle>();
        var glyph = IsExpanded ? style.ExpandedGlyph : style.CollapsedGlyph;
        var glyphWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(glyph));
        var gap = Math.Max(0, style.SpaceBetweenGlyphAndHeader);
        var prefixWidth = glyphWidth + gap;

        var header = Header;

        var headerHints = header is null
            ? SizeHints.Fixed(new Size(0, 1))
            : MeasureHeader(header, constraints, prefixWidth);

        if (IsExpanded && Content is not null)
        {
            return MeasureExpanded(constraints, prefixWidth, headerHints);
        }

        // Collapsed: only header is visible.
        return headerHints;
    }

    private SizeHints MeasureHeader(Visual header, in LayoutConstraints constraints, int prefixWidth)
    {
        var headerMaxW = constraints.MaxWidth == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxWidth - prefixWidth);

        var headerConstraints = new LayoutConstraints(0, headerMaxW, 0, constraints.MaxHeight);
        var inner = header.Measure(headerConstraints);

        int minW, natW, maxW;
        minW = prefixWidth + inner.Min.Width;
        natW = prefixWidth + inner.Natural.Width;

        if (LayoutConstants.IsInfinite(inner.Max.Width))
        {
            maxW = LayoutConstants.Infinite;
        }
        else
        {
            maxW = prefixWidth + inner.Max.Width;
            maxW = LayoutConstants.ClampOrInfinite(maxW);
        }

        return SizeHints.Flex(
            new Size(LayoutConstants.ClampFinite(minW), LayoutConstants.ClampFinite(inner.Min.Height)),
            new Size(LayoutConstants.ClampFinite(natW), LayoutConstants.ClampFinite(Math.Max(1, inner.Natural.Height))),
            new Size(maxW, inner.Max.Height),
            inner.FlexGrowX,
            inner.FlexGrowY,
            inner.FlexShrinkX,
            inner.FlexShrinkY).Normalize();
    }

    private SizeHints MeasureExpanded(in LayoutConstraints constraints, int prefixWidth, SizeHints headerHints)
    {
        var style = GetStyle<CollapsibleStyle>();
        var spacing = Math.Max(0, style.ContentSpacing);

        var content = Content!;

        var contentMaxH = constraints.MaxHeight == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxHeight - headerHints.Natural.Height - spacing);

        var contentConstraints = new LayoutConstraints(0, constraints.MaxWidth, 0, contentMaxH);
        var contentHints = content.Measure(contentConstraints);

        var minW = Math.Max(headerHints.Min.Width, contentHints.Min.Width);
        var natW = Math.Max(headerHints.Natural.Width, contentHints.Natural.Width);

        var maxW = LayoutConstants.IsInfinite(headerHints.Max.Width) || LayoutConstants.IsInfinite(contentHints.Max.Width)
            ? LayoutConstants.Infinite
            : Math.Max(headerHints.Max.Width, contentHints.Max.Width);

        var minH = headerHints.Min.Height + spacing + contentHints.Min.Height;
        var natH = headerHints.Natural.Height + spacing + contentHints.Natural.Height;

        int maxH;
        if (LayoutConstants.IsInfinite(headerHints.Max.Height) || LayoutConstants.IsInfinite(contentHints.Max.Height))
        {
            maxH = LayoutConstants.Infinite;
        }
        else
        {
            maxH = LayoutConstants.ClampOrInfinite(headerHints.Max.Height + spacing + contentHints.Max.Height);
        }

        return SizeHints.Flex(
            new Size(LayoutConstants.ClampFinite(minW), LayoutConstants.ClampFinite(minH)),
            new Size(LayoutConstants.ClampFinite(natW), LayoutConstants.ClampFinite(natH)),
            new Size(maxW, LayoutConstants.IsInfinite(maxH) ? LayoutConstants.Infinite : LayoutConstants.ClampFinite(maxH)),
            growX: Math.Max(headerHints.FlexGrowX, contentHints.FlexGrowX),
            growY: Math.Max(headerHints.FlexGrowY, contentHints.FlexGrowY),
            shrinkX: Math.Max(headerHints.FlexShrinkX, contentHints.FlexShrinkX),
            shrinkY: Math.Max(headerHints.FlexShrinkY, contentHints.FlexShrinkY)).Normalize();
    }

    /// <inheritdoc/>
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var style = GetStyle<CollapsibleStyle>();
        var glyph = IsExpanded ? style.ExpandedGlyph : style.CollapsedGlyph;
        var glyphWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(glyph));
        var gap = Math.Max(0, style.SpaceBetweenGlyphAndHeader);
        var prefixWidth = glyphWidth + gap;
        var spacing = Math.Max(0, style.ContentSpacing);

        var header = Header;
        var headerHeight = 1;
        if (header is not null)
        {
            headerHeight = Math.Max(1, Math.Min(finalRect.Height, header.DesiredSize.Height));
        }

        var content = Content;
        var contentHeight = IsExpanded && content is not null ? Math.Max(0, finalRect.Height - headerHeight - spacing) : 0;

        if (Direction == ExpandDirection.Down)
        {
            _headerRect = new Rectangle(finalRect.X, finalRect.Y, finalRect.Width, headerHeight);
            _contentRect = new Rectangle(finalRect.X, finalRect.Y + headerHeight + spacing, finalRect.Width, contentHeight);
        }
        else
        {
            _contentRect = new Rectangle(finalRect.X, finalRect.Y, finalRect.Width, contentHeight);
            _headerRect = new Rectangle(finalRect.X, finalRect.Bottom - headerHeight, finalRect.Width, headerHeight);
        }

        if (header is not null)
        {
            var headerInner = new Rectangle(
                _headerRect.X + Math.Min(_headerRect.Width, prefixWidth),
                _headerRect.Y,
                Math.Max(0, _headerRect.Width - prefixWidth),
                _headerRect.Height);

            header.Arrange(headerInner);
        }

        if (IsExpanded && content is not null)
        {
            content.Arrange(_contentRect);
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

        var theme = GetTheme();
        var style = GetStyle<CollapsibleStyle>();
        var isFocused = HasFocusWithin;
        var headerStyle = style.ResolveHeader(theme, IsEnabled, isFocused, IsHeaderHovered, IsHeaderPressed);

        // Header surface.
        for (var y = _headerRect.Y; y < _headerRect.Y + _headerRect.Height; y++)
        {
            for (var x = _headerRect.X; x < _headerRect.X + _headerRect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), headerStyle);
            }
        }

        // Expand/collapse glyph (first line only).
        var glyph = IsExpanded ? style.ExpandedGlyph : style.CollapsedGlyph;
        if (_headerRect.Width > 0)
        {
            buffer.SetCell(_headerRect.X, _headerRect.Y, glyph, headerStyle);
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, this) || !IsEnabled)
        {
            return;
        }

        if (e.Key is TerminalKey.Space or TerminalKey.Enter)
        {
            IsExpanded = !IsExpanded;
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var hover = _headerRect.Contains(e.UiX, e.UiY);
        if (IsHeaderHovered != hover)
        {
            IsHeaderHovered = hover;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (!IsEnabled || e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (_headerRect.Contains(e.UiX, e.UiY))
        {
            IsHeaderPressed = true;
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var wasPressed = IsHeaderPressed;
        IsHeaderPressed = false;

        if (wasPressed && IsEnabled && _headerRect.Contains(e.UiX, e.UiY))
        {
            IsExpanded = !IsExpanded;
        }

        if (wasPressed)
        {
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void PrepareChildren()
    {
        var isExpanded = IsExpanded;
        var content = Content;

        if (isExpanded && content is not null)
        {
            if (_attachedContent is not null && !ReferenceEquals(_attachedContent, content))
            {
                DetachChild(_attachedContent);
                _attachedContent = null;
            }

            if (_attachedContent is null)
            {
                AttachChild(content);
                _attachedContent = content;
            }
        }
        else if (_attachedContent is not null)
        {
            var app = App;
            if (app is not null && HasFocusWithin)
            {
                app.Focus(this);
            }

            DetachChild(_attachedContent);
            _attachedContent = null;
        }
    }

    partial void OnIsExpandedChanging(ref bool value)
    {
        if (!_hasExpandedStateForEvent)
        {
            _hasExpandedStateForEvent = true;
            _lastExpandedForEvent = _isExpanded;
            return;
        }

        _lastExpandedForEvent = _isExpanded;
    }

    partial void OnIsExpandedChanged(bool value)
    {
        if (!_hasExpandedStateForEvent)
        {
            _hasExpandedStateForEvent = true;
            _lastExpandedForEvent = value;
            return;
        }

        if (_lastExpandedForEvent != value)
        {
            RaiseEvent(ExpandedChangedEvent, new ExpandedChangedEventArgs { OldValue = _lastExpandedForEvent, NewValue = value });
        }

        _lastExpandedForEvent = value;
    }

    private static bool IsDescendantOf(Visual visual, Visual ancestor)
    {
        for (var v = visual; v is not null; v = v.Parent)
        {
            if (ReferenceEquals(v, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    [RoutedEvent(RoutingStrategy.Bubble)]
    private void OnExpandedChanged(ExpandedChangedEventArgs e) { }
}
