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

public sealed partial class Switch : ContentVisual
{
    private const int TrackWidth = 4;

    private bool _pressed;
    private bool _oldValueForEvent;

    public Switch()
    {
        Focusable = true;
    }

    public Switch(Visual content) : this()
    {
        this.Content(content);
    }

    [Bindable]
    public partial bool IsOn { get; set; }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = Get<SwitchStyle>();
        var gap = Math.Max(0, style.SpaceBetweenGlyphAndText);

        var content = Content;
        if (content is not null)
        {
            var maxW = constraints.MaxWidth == LayoutConstants.Infinite ? LayoutConstants.Infinite : constraints.MaxWidth;
            var maxH = constraints.MaxHeight == LayoutConstants.Infinite ? LayoutConstants.Infinite : constraints.MaxHeight;
            content.Measure(new Size(Math.Max(0, maxW - TrackWidth - gap), maxH));
            var natural = new Size(TrackWidth + gap + content.DesiredSize.Width, Math.Max(1, content.DesiredSize.Height));
            return SizeHints.Fixed(constraints.Clamp(natural));
        }

        return SizeHints.Fixed(constraints.Clamp(new Size(TrackWidth, 1)));
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var style = Get<SwitchStyle>();
        var gap = Math.Max(0, style.SpaceBetweenGlyphAndText);

        var contentLeft = finalRect.X + Math.Min(finalRect.Width, TrackWidth + gap);
        var contentWidth = Math.Max(0, finalRect.Right - contentLeft);
        var contentRect = new Rectangle(contentLeft, finalRect.Y, contentWidth, finalRect.Height);

        Content?.Arrange(contentRect);
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var style = Get<SwitchStyle>();
        var theme = GetTheme();
        var focused = ReferenceEquals(App?.FocusedElement, this);

        var trackStyle = style.ResolveTrack(theme, IsEnabled, focused, IsHovered, _pressed, IsOn);
        var thumbStyle = style.ResolveThumb(theme, IsEnabled, focused, IsHovered, _pressed, IsOn);

        var x = rect.X;
        var y = rect.Y + Math.Max(0, (rect.Height - 1) / 2);
        var trackCells = Math.Min(TrackWidth, rect.Width);

        // Track background.
        for (var i = 0; i < trackCells; i++)
        {
            buffer.SetCell(x + i, y, new Rune(' '), trackStyle);
        }

        if (trackCells >= 1)
        {
            buffer.SetCell(x, y, style.TrackLeft, trackStyle);
        }
        if (trackCells >= 2)
        {
            buffer.SetCell(x + trackCells - 1, y, style.TrackRight, trackStyle);
        }

        if (trackCells >= 4)
        {
            var thumbX = IsOn ? x + 2 : x + 1;
            if (thumbX >= x && thumbX < x + trackCells - 1)
            {
                buffer.SetCell(thumbX, y, style.ThumbGlyph, thumbStyle);
            }
        }
        else if (trackCells >= 2)
        {
            // Narrow fallback.
            var thumbX = x + Math.Min(1, trackCells - 2);
            buffer.SetCell(thumbX, y, style.ThumbGlyph, thumbStyle);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        switch (e.Key)
        {
            case TerminalKey.Space:
            case TerminalKey.Enter:
                IsOn = !IsOn;
                e.Handled = true;
                break;
            case TerminalKey.Left:
                IsOn = false;
                e.Handled = true;
                break;
            case TerminalKey.Right:
                IsOn = true;
                e.Handled = true;
                break;
        }
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (!IsEnabled || e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        _pressed = true;
        e.Handled = true;
        Invalidate();
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var wasPressed = _pressed;
        _pressed = false;

        if (wasPressed && IsEnabled && Bounds.Contains(e.UiX, e.UiY))
        {
            IsOn = !IsOn;
        }

        e.Handled = true;
        Invalidate();
    }

    partial void OnIsOnChanging(ref bool value)
    {
        _oldValueForEvent = _isOn;
    }

    partial void OnIsOnChanged(bool value)
    {
        if (_oldValueForEvent != value)
        {
            RaiseEvent(ToggledEvent, new ToggleChangedEventArgs { OldValue = _oldValueForEvent, NewValue = value });
        }
    }

    [RoutedEvent(RoutingStrategy.Bubble)]
    private void OnToggled(ToggleChangedEventArgs e) { }
}
