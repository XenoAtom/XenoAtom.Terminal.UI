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
/// Specifies the severity of a validation message.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message.
    /// </summary>
    Info,

    /// <summary>
    /// Warning message.
    /// </summary>
    Warning,

    /// <summary>
    /// Error message.
    /// </summary>
    Error,
}

/// <summary>
/// Specifies where a validation message is displayed relative to the wrapped control.
/// </summary>
public enum ValidationPlacement
{
    /// <summary>
    /// Displays the message above the wrapped content.
    /// </summary>
    Above,

    /// <summary>
    /// Displays the message below the wrapped content.
    /// </summary>
    Below,
}

/// <summary>
/// Represents a validation message with severity and visual content.
/// </summary>
/// <param name="Severity">The severity.</param>
/// <param name="Content">The message content.</param>
public readonly record struct ValidationMessage(ValidationSeverity Severity, Visual Content);

/// <summary>
/// Wraps a visual and displays an optional validation message above or below it.
/// </summary>
public sealed partial class ValidationPresenter : ContentVisual
{
    private readonly ValidationMessageHost _messageHost;
    private Rectangle _contentRect;
    private Rectangle _messageRect;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationPresenter"/> class.
    /// </summary>
    public ValidationPresenter()
    {
        Focusable = false;
        this.Placement(ValidationPlacement.Below);
        _messageHost = new ValidationMessageHost();
        AttachChild(_messageHost);
    }

    /// <summary>
    /// Gets or sets the message displayed by the presenter, or <see langword="null"/> to hide it.
    /// </summary>
    [Bindable]
    public partial ValidationMessage? Message { get; set; }

    /// <summary>
    /// Gets or sets where the message is displayed relative to the wrapped content.
    /// </summary>
    [Bindable]
    public partial ValidationPlacement Placement { get; set; }

    partial void OnMessageChanged(ValidationMessage? value) => _messageHost.SetMessage(value);

    /// <inheritdoc />
    protected override int ChildrenCount
    {
        get
        {
            var count = Content is null ? 0 : 1;
            // The message host is always part of the tree, but it collapses itself when Message is null.
            return count + 1;
        }
    }

    /// <inheritdoc />
    protected override Visual GetChild(int index)
    {
        var content = Content;
        if (content is not null)
        {
            if (index == 0)
            {
                return content;
            }

            if (index == 1)
            {
                return _messageHost;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return index == 0 ? _messageHost : throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var content = Content;
        var contentHints = content?.Measure(constraints) ?? SizeHints.Fixed(Size.Zero);

        var validationStyle = GetStyle<ValidationStyle>();
        var gap = validationStyle.Gap;

        var messageHints = _messageHost.Measure(constraints);
        var hasMessage = messageHints.Natural.Height > 0 && messageHints.Natural.Width > 0;

        var minW = Math.Max(contentHints.Min.Width, messageHints.Min.Width);
        var natW = Math.Max(contentHints.Natural.Width, messageHints.Natural.Width);
        var maxW = MaxWidthHint(contentHints.Max.Width, messageHints.Max.Width);

        var minH = contentHints.Min.Height + messageHints.Min.Height + (hasMessage ? Math.Max(0, gap) : 0);
        var natH = contentHints.Natural.Height + messageHints.Natural.Height + (hasMessage ? Math.Max(0, gap) : 0);
        var maxH = AddMaxHeight(contentHints.Max.Height, messageHints.Max.Height, hasMessage ? Math.Max(0, gap) : 0);

        var result = SizeHints.Flex(
            new Size(minW, minH),
            new Size(natW, natH),
            new Size(maxW, maxH),
            contentHints.FlexGrowX + messageHints.FlexGrowX,
            contentHints.FlexGrowY + messageHints.FlexGrowY,
            contentHints.FlexShrinkX + messageHints.FlexShrinkX,
            contentHints.FlexShrinkY + messageHints.FlexShrinkY);

        return result.Normalize();
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var validationStyle = GetStyle<ValidationStyle>();
        var gap = Math.Max(0, validationStyle.Gap);

        var content = Content;
        var messageHints = _messageHost.MeasureHints;
        var hasMessage = messageHints.Natural.Height > 0 && messageHints.Natural.Width > 0;
        var gapHeight = hasMessage ? gap : 0;

        var items = content is null ? 1 : 2;
        var mins = new int[items];
        var nats = new int[items];
        var maxs = new int[items];
        var grows = new int[items];
        var shrinks = new int[items];
        var heights = new int[items];

        if (content is null)
        {
            mins[0] = messageHints.Min.Height;
            nats[0] = messageHints.Natural.Height;
            maxs[0] = messageHints.Max.Height;
            grows[0] = messageHints.FlexGrowY;
            shrinks[0] = messageHints.FlexShrinkY;
        }
        else
        {
            var contentHints = content.MeasureHints;
            if (Placement == ValidationPlacement.Above)
            {
                mins[0] = messageHints.Min.Height + gapHeight;
                nats[0] = messageHints.Natural.Height + gapHeight;
                maxs[0] = AddMaxHeight(messageHints.Max.Height, gapHeight);
                grows[0] = messageHints.FlexGrowY;
                shrinks[0] = messageHints.FlexShrinkY;

                mins[1] = contentHints.Min.Height;
                nats[1] = contentHints.Natural.Height;
                maxs[1] = contentHints.Max.Height;
                grows[1] = contentHints.FlexGrowY;
                shrinks[1] = contentHints.FlexShrinkY;
            }
            else
            {
                mins[0] = contentHints.Min.Height;
                nats[0] = contentHints.Natural.Height;
                maxs[0] = contentHints.Max.Height;
                grows[0] = contentHints.FlexGrowY;
                shrinks[0] = contentHints.FlexShrinkY;

                mins[1] = messageHints.Min.Height + gapHeight;
                nats[1] = messageHints.Natural.Height + gapHeight;
                maxs[1] = AddMaxHeight(messageHints.Max.Height, gapHeight);
                grows[1] = messageHints.FlexGrowY;
                shrinks[1] = messageHints.FlexShrinkY;
            }
        }

        FlexAllocator.Allocate(finalRect.Height, mins, nats, maxs, grows, shrinks, heights);

        _contentRect = default;
        _messageRect = default;

        var y = finalRect.Y;

        if (content is null)
        {
            _messageRect = new Rectangle(finalRect.X, y, finalRect.Width, heights[0]);
            _messageHost.Arrange(_messageRect);
            return;
        }

        if (Placement == ValidationPlacement.Above)
        {
            var messageHeight = Math.Max(0, heights[0] - gapHeight);
            _messageRect = new Rectangle(finalRect.X, y, finalRect.Width, messageHeight);
            _messageHost.Arrange(_messageRect);
            y += heights[0];

            _contentRect = new Rectangle(finalRect.X, y, finalRect.Width, heights[1]);
            content.Arrange(_contentRect);
        }
        else
        {
            _contentRect = new Rectangle(finalRect.X, y, finalRect.Width, heights[0]);
            content.Arrange(_contentRect);
            y += heights[0];

            var messageHeight = Math.Max(0, heights[1] - gapHeight);
            _messageRect = new Rectangle(finalRect.X, y + gapHeight, finalRect.Width, messageHeight);
            _messageHost.Arrange(_messageRect);
        }
    }

    private static int MaxWidthHint(int a, int b)
    {
        if (LayoutConstants.IsInfinite(a) || LayoutConstants.IsInfinite(b))
        {
            return LayoutConstants.Infinite;
        }

        return Math.Max(a, b);
    }

    private static int AddMaxHeight(int a, int b)
    {
        if (LayoutConstants.IsInfinite(a))
        {
            return LayoutConstants.Infinite;
        }

        return LayoutConstants.ClampFinite(Math.Max(0, a + b));
    }

    private static int AddMaxHeight(int a, int b, int extra)
        => AddMaxHeight(AddMaxHeight(a, b), extra);
}

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

        // Fill the message area so the severity line is visually distinct and consistent across wrapped content.
        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), lineStyle);
            }
        }

        // Render the prefix glyph (if any) at the first line.
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
