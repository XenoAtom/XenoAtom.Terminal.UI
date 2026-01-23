// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Renders a single validation message line (or wrapped block) using <see cref="ValidationStyle"/>.
/// </summary>
/// <remarks>
/// This is an internal building block used by <see cref="ValidationPresenter"/> and controls that surface
/// validation messages as part of their own layout (e.g. <see cref="NumberBox{T}"/>).
/// </remarks>
internal sealed class ValidationMessageHost : Visual
{
    private ValidationMessage? _message;
    private Visual? _content;
    private TextBlockStyle? _resolvedTextBlockStyle;

    public ValidationMessageHost()
    {
        Focusable = false;
    }

    protected override int ChildrenCount => _content is null ? 0 : 1;

    protected override Visual GetChild(int index)
        => index == 0 && _content is not null ? _content : throw new ArgumentOutOfRangeException(nameof(index));

    public bool HasMessage => _message is not null;

    public void SetMessage(ValidationMessage? message)
    {
        VerifyAccess();

        if (message is null)
        {
            if (_content is not null)
            {
                DetachChild(_content);
                _content = null;
            }

            _message = null;
            _resolvedTextBlockStyle = null;
            Invalidate();
            return;
        }

        var newContent = message.Value.Content;
        if (_content is not null && !ReferenceEquals(_content, newContent))
        {
            DetachChild(_content);
            _content = null;
        }

        if (_content is null)
        {
            _content = newContent;
            AttachChild(_content);
            EnsureWrappedText(_content);
        }

        _message = message;
        Invalidate();
    }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        if (_message is null || _content is null)
        {
            return SizeHints.Fixed(Size.Zero);
        }

        var style = GetStyle<ValidationStyle>();
        var padding = style.Padding;
        var prefix = style.BuildPrefix(_message.Value.Severity);

        var prefixWidth = TerminalTextUtility.GetWidth(prefix.AsSpan());
        var inner = new LayoutConstraints(
            0,
            Math.Max(0, constraints.MaxWidth - padding.Horizontal - prefixWidth),
            0,
            Math.Max(0, constraints.MaxHeight - padding.Vertical));

        var contentHints = _content.Measure(inner);

        var min = new Size(
            LayoutConstants.ClampFinite(padding.Horizontal + prefixWidth + contentHints.Min.Width),
            LayoutConstants.ClampFinite(padding.Vertical + contentHints.Min.Height));

        var natural = new Size(
            LayoutConstants.ClampFinite(padding.Horizontal + prefixWidth + contentHints.Natural.Width),
            LayoutConstants.ClampFinite(padding.Vertical + contentHints.Natural.Height));

        var maxW = LayoutConstants.IsInfinite(contentHints.Max.Width)
            ? LayoutConstants.Infinite
            : LayoutConstants.ClampFinite(padding.Horizontal + prefixWidth + contentHints.Max.Width);

        var maxH = LayoutConstants.IsInfinite(contentHints.Max.Height)
            ? LayoutConstants.Infinite
            : LayoutConstants.ClampFinite(padding.Vertical + contentHints.Max.Height);

        return SizeHints.Flex(min, natural, new Size(maxW, maxH), 0, contentHints.FlexGrowY, 0, contentHints.FlexShrinkY).Normalize();
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        if (_message is null || _content is null || finalRect.Width <= 0 || finalRect.Height <= 0)
        {
            return;
        }

        var style = GetStyle<ValidationStyle>();
        var padding = style.Padding;
        var prefix = style.BuildPrefix(_message.Value.Severity);

        var prefixWidth = TerminalTextUtility.GetWidth(prefix.AsSpan());
        var inner = new Rectangle(
            finalRect.X + padding.Left + prefixWidth,
            finalRect.Y + padding.Top,
            Math.Max(0, finalRect.Width - padding.Horizontal - prefixWidth),
            Math.Max(0, finalRect.Height - padding.Vertical));

        _content.Arrange(inner);
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        if (_message is null || _content is null)
        {
            return;
        }

        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = GetStyle<ValidationStyle>();
        var lineStyle = style.ResolveLineStyle(theme, _message.Value.Severity);
        var padding = style.Padding;
        var prefix = style.BuildPrefix(_message.Value.Severity);

        // Ensure TextBlock children inherit a severity-appropriate foreground by default.
        var textBlockStyle = style.ResolveTextBlockStyle(theme, _message.Value.Severity);
        if (_resolvedTextBlockStyle != textBlockStyle)
        {
            StyleEnvironment ??= new Dictionary<object, object?>();
            StyleEnvironment[TextBlockStyle.Key] = textBlockStyle;
            _resolvedTextBlockStyle = textBlockStyle;
        }

        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), lineStyle);
            }
        }

        if (prefix.Length > 0 && rect.Height > 0)
        {
            var x = rect.X + padding.Left;
            var y = rect.Y + padding.Top;
            buffer.WriteText(x, y, prefix.AsSpan(), lineStyle);
        }
    }

    private static void EnsureWrappedText(Visual content)
    {
        switch (content)
        {
            case TextBlock tb:
                tb.Wrap(true);
                break;
            case Markup markup:
                markup.Wrap(true);
                break;
        }
    }
}

