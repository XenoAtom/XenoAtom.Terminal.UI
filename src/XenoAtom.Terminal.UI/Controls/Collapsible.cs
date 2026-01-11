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

public enum ExpandDirection
{
    Down = 0,
    Up = 1,
}

public sealed partial class Collapsible : Visual
{
    private bool _pressedHeader;
    private bool _headerHovered;
    private bool _oldExpandedForEvent;

    private Rectangle _headerRect;
    private Rectangle _contentRect;

    public Collapsible()
    {
        Focusable = true;
        this.Direction(ExpandDirection.Down);
    }

    public Collapsible(Visual header, Visual content) : this()
    {
        this.Header(header);
        this.Content(content);
    }

    [Bindable]
    public partial Visual? Header { get; set; }

    [Bindable]
    public partial Visual? Content { get; set; }

    [Bindable]
    public partial bool IsExpanded { get; set; }

    [Bindable]
    public partial ExpandDirection Direction { get; set; }

    protected override int ChildrenCount
        => (Header is null ? 0 : 1) + (IsExpanded && Content is not null ? 1 : 0);

    protected override Visual GetChild(int index)
    {
        if (Header is null)
        {
            if (IsExpanded && Content is not null && index == 0)
            {
                return Content;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (index == 0)
        {
            return Header;
        }

        if (IsExpanded && Content is not null && index == 1)
        {
            return Content;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = Get<CollapsibleStyle>();
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
        try
        {
            checked
            {
                minW = prefixWidth + inner.Min.Width;
                natW = prefixWidth + inner.Natural.Width;
            }
        }
        catch (OverflowException ex)
        {
            throw new LayoutException("Overflow while computing Collapsible header widths.", ex);
        }

        if (LayoutConstants.IsInfinite(inner.Max.Width))
        {
            maxW = LayoutConstants.Infinite;
        }
        else
        {
            try
            {
                maxW = checked(prefixWidth + inner.Max.Width);
                maxW = LayoutConstants.ClampOrInfinite(maxW);
            }
            catch (OverflowException ex)
            {
                throw new LayoutException("Overflow while computing Collapsible header Max.Width.", ex);
            }
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
        var style = Get<CollapsibleStyle>();
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

        int minH, natH;
        try
        {
            checked
            {
                minH = headerHints.Min.Height + spacing + contentHints.Min.Height;
                natH = headerHints.Natural.Height + spacing + contentHints.Natural.Height;
            }
        }
        catch (OverflowException ex)
        {
            throw new LayoutException("Overflow while computing Collapsible heights.", ex);
        }

        int maxH;
        if (LayoutConstants.IsInfinite(headerHints.Max.Height) || LayoutConstants.IsInfinite(contentHints.Max.Height))
        {
            maxH = LayoutConstants.Infinite;
        }
        else
        {
            try
            {
                maxH = checked(headerHints.Max.Height + spacing + contentHints.Max.Height);
            }
            catch (OverflowException ex)
            {
                throw new LayoutException("Overflow while computing Collapsible Max.Height.", ex);
            }
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

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var style = Get<CollapsibleStyle>();
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

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = Get<CollapsibleStyle>();
        var isFocused = IsFocusWithin();
        var headerStyle = style.ResolveHeader(theme, IsEnabled, isFocused, _headerHovered, _pressedHeader);

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

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var hover = _headerRect.Contains(e.UiX, e.UiY);
        if (_headerHovered != hover)
        {
            _headerHovered = hover;
            Invalidate();
        }
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (!IsEnabled || e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (_headerRect.Contains(e.UiX, e.UiY))
        {
            _pressedHeader = true;
            e.Handled = true;
            Invalidate();
        }
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var wasPressed = _pressedHeader;
        _pressedHeader = false;

        if (wasPressed && IsEnabled && _headerRect.Contains(e.UiX, e.UiY))
        {
            IsExpanded = !IsExpanded;
        }

        if (wasPressed)
        {
            e.Handled = true;
            Invalidate();
        }
    }

    partial void OnIsExpandedChanging(ref bool value)
    {
        _oldExpandedForEvent = _isExpanded;
    }

    partial void OnIsExpandedChanged(bool value)
    {
        if (_oldExpandedForEvent != value)
        {
            RaiseEvent(ExpandedChangedEvent, new ExpandedChangedEventArgs { OldValue = _oldExpandedForEvent, NewValue = value });
        }

        var app = App;
        var content = Content;
        if (app is null || content is null)
        {
            return;
        }

        if (value && content.App is null)
        {
            content.AttachToApp(app);
        }
        else if (!value && content.App is not null)
        {
            if (app.FocusedElement is { } focused && IsDescendantOf(focused, content))
            {
                app.Focus(this);
            }

            content.DetachFromApp();
        }
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
