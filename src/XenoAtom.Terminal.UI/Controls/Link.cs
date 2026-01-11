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

public sealed partial class Link : Visual
{
    private static readonly Rune Ellipsis = new(0x2026);
    private bool _pressed;

    public Link()
    {
        Focusable = true;
    }

    public Link(string uri, string? text = null)
        : this()
    {
        this.Uri(uri);
        if (!string.IsNullOrEmpty(text))
        {
            this.Text(text);
        }
    }

    [Bindable]
    public partial string? Text { get; set; }

    [Bindable]
    public partial string? Uri { get; set; }

    [Bindable]
    public partial TextTrimming Trimming { get; set; }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var text = Text ?? Uri ?? string.Empty;
        var width = TerminalTextUtility.GetWidth(text.AsSpan());
        var natural = constraints.Clamp(new Size(Math.Max(0, width), 1));
        return SizeHints.Fixed(natural);
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = Get<LinkStyle>();
        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var resolved = style.Resolve(theme, IsEnabled, focused: isFocused, hovered: IsHovered);

        var uri = Uri;
        var hyperlinkToken = !string.IsNullOrEmpty(uri) ? buffer.RegisterHyperlink(uri) : 0;

        var text = Text ?? uri ?? string.Empty;
        var span = text.AsSpan();

        var maxCells = rect.Width;
        if (maxCells <= 0)
        {
            return;
        }

        var fullWidth = TerminalTextUtility.GetWidth(span);
        if (fullWidth <= maxCells)
        {
            buffer.WriteText(rect.X, rect.Y, span, resolved, hyperlinkToken);
            return;
        }

        if (Trimming == TextTrimming.EndEllipsis && maxCells > 0)
        {
            var ellipsisWidth = TerminalTextUtility.GetRuneWidth(Ellipsis);
            var prefixCells = Math.Max(0, maxCells - Math.Max(1, ellipsisWidth));

            if (prefixCells > 0 && TerminalTextUtility.TryGetIndexAtCell(span, prefixCells, out var endIndex))
            {
                var prefix = span[..endIndex];
                buffer.WriteText(rect.X, rect.Y, prefix, resolved, hyperlinkToken);
                var writtenCells = TerminalTextUtility.GetWidth(prefix);
                if (writtenCells < maxCells)
                {
                    buffer.SetCell(rect.X + writtenCells, rect.Y, Ellipsis, resolved, hyperlinkToken);
                }
            }
            else
            {
                buffer.SetCell(rect.X, rect.Y, Ellipsis, resolved, hyperlinkToken);
            }

            return;
        }

        if (TerminalTextUtility.TryGetIndexAtCell(span, maxCells, out var clippedIndex))
        {
            buffer.WriteText(rect.X, rect.Y, span[..clippedIndex], resolved, hyperlinkToken);
        }
        else
        {
            buffer.WriteText(rect.X, rect.Y, span, resolved, hyperlinkToken);
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
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (!IsEnabled || e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var wasPressed = _pressed;
        _pressed = false;
        if (wasPressed && Bounds.Contains(e.UiX, e.UiY))
        {
            Open();
        }

        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (e.Key is TerminalKey.Enter or TerminalKey.Space)
        {
            Open();
            e.Handled = true;
        }
    }

    private void Open()
    {
        var uri = Uri;
        if (string.IsNullOrEmpty(uri))
        {
            return;
        }

        RaiseEvent(OpenedEvent, new LinkOpenedEventArgs(uri));
    }

    [RoutedEvent(RoutingStrategy.Bubble)]
    private void OnOpened(LinkOpenedEventArgs e) { }
}

public sealed class LinkOpenedEventArgs : RoutedEventArgs
{
    public LinkOpenedEventArgs(string uri) => Uri = uri;

    public string Uri { get; }
}
