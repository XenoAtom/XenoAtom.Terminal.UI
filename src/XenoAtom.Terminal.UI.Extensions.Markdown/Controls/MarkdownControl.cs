// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using Markdig;
using XenoAtom.Terminal.UI.Extensions.Markdown;
using XenoAtom.Terminal.UI.Extensions.Markdown.Styling;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Scrolling;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Renders a markdown document by converting it to <see cref="DocumentFlow"/> content.
/// </summary>
public sealed partial class MarkdownControl : Visual, IScrollable
{
    private readonly DocumentFlow _flow;
    private Theme? _lastResolvedTheme;
    private MarkdownStyle? _lastResolvedSourceStyle;
    private int _lastNaturalContentWidthVersion = -1;
    private int _lastNaturalContentWidth = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownControl"/> class.
    /// </summary>
    public MarkdownControl()
    {
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;

        _flow = new DocumentFlow
        {
            HorizontalAlignment = Align.Stretch,
            VerticalAlignment = Align.Stretch,
            ItemPadding = new Thickness(0),
            ItemSpacing = 0,
            FollowTail = false,
        };
        AttachChild(_flow);

        this.HorizontalScrollEnabled(true);
        this.VerticalScrollEnabled(true);
        Options = MarkdownRenderOptions.Default;
        RebuildContent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownControl"/> class with markdown text.
    /// </summary>
    /// <param name="markdown">The markdown text.</param>
    public MarkdownControl(string markdown) : this()
    {
        Markdown = markdown;
        RebuildContent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownControl"/> class with a markdown provider.
    /// </summary>
    /// <param name="markdown">The markdown provider.</param>
    public MarkdownControl(Func<string> markdown) : this()
    {
        this.Markdown(markdown);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownControl"/> class with a markdown binding.
    /// </summary>
    /// <param name="markdown">The markdown binding.</param>
    public MarkdownControl(Binding<string?> markdown) : this()
    {
        this.Markdown(markdown);
    }

    /// <summary>
    /// Gets or sets the markdown source text.
    /// </summary>
    [Bindable]
    public partial string? Markdown { get; set; }

    /// <summary>
    /// Gets or sets an optional markdown pipeline. When null, the default pipeline is used.
    /// </summary>
    [Bindable]
    public partial MarkdownPipeline? Pipeline { get; set; }

    /// <summary>
    /// Gets or sets an optional base URI used to resolve relative links.
    /// </summary>
    [Bindable]
    public partial Uri? BaseUri { get; set; }

    /// <summary>
    /// Gets or sets markdown render options.
    /// </summary>
    [Bindable]
    public partial MarkdownRenderOptions Options { get; set; }

    /// <summary>
    /// Gets or sets optional markdown style overrides. When null, style is resolved from <see cref="MarkdownStyle.Key"/>.
    /// </summary>
    [Bindable]
    public partial MarkdownStyle? RenderStyle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether horizontal scrolling is enabled for the rendered document.
    /// </summary>
    [Bindable]
    public partial bool HorizontalScrollEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether vertical scrolling is enabled for the rendered document.
    /// </summary>
    /// <remarks>
    /// Disable vertical scrolling when an ancestor provides scrolling and the markdown should grow to its natural height.
    /// </remarks>
    [Bindable]
    public partial bool VerticalScrollEnabled { get; set; }

    /// <inheritdoc />
    public ScrollModel Scroll => _flow.Scroll;

    /// <inheritdoc />
    protected override int ChildrenCount => 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index) => index == 0 ? _flow : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc />
    protected override void PrepareChildren()
    {
        var sourceStyle = RenderStyle ?? GetStyle<MarkdownStyle>();
        var theme = GetTheme();

        if (!ReferenceEquals(theme, _lastResolvedTheme) || !Equals(sourceStyle, _lastResolvedSourceStyle))
        {
            RebuildContent(sourceStyle, theme);
        }
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var naturalContentWidth = GetNaturalContentWidth();
        _flow.Measure(constraints);
        var desired = constraints.Clamp(_flow.DesiredSize);
        var naturalWidth = Math.Min(desired.Width, naturalContentWidth);
        var natural = constraints.Clamp(new Size(Math.Max(1, naturalWidth), desired.Height));
        return SizeHints.Flex(
            min: constraints.Clamp(new Size(1, 1)),
            natural: natural,
            max: new Size(int.MaxValue, int.MaxValue),
            growX: HorizontalAlignment == Align.Stretch ? 1 : 0,
            growY: VerticalAlignment == Align.Stretch ? 1 : 0,
            shrinkX: 1,
            shrinkY: 1);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect) => _flow.Arrange(finalRect);

    partial void OnMarkdownChanged(string? value)
    {
        _ = value;
        RebuildContent();
    }

    partial void OnPipelineChanged(MarkdownPipeline? value)
    {
        _ = value;
        RebuildContent();
    }

    partial void OnBaseUriChanged(Uri? value)
    {
        _ = value;
        RebuildContent();
    }

    partial void OnOptionsChanging(ref MarkdownRenderOptions value)
        => ArgumentNullException.ThrowIfNull(value);

    partial void OnOptionsChanged(MarkdownRenderOptions value)
    {
        _ = value;
        RebuildContent();
    }

    partial void OnRenderStyleChanged(MarkdownStyle? value)
    {
        _ = value;
        RebuildContent();
    }

    partial void OnHorizontalScrollEnabledChanged(bool value) => _flow.HorizontalScrollEnabled = value;

    partial void OnVerticalScrollEnabledChanged(bool value) => _flow.VerticalScrollEnabled = value;

    private void RebuildContent()
    {
        VerifyAccess();
        var sourceStyle = RenderStyle ?? GetStyle<MarkdownStyle>();
        var theme = GetTheme();
        RebuildContent(sourceStyle, theme);
    }

    private void RebuildContent(MarkdownStyle sourceStyle, Theme theme)
    {
        VerifyAccess();

        var style = MarkdownDefaults.ResolveStyle(theme, sourceStyle);
        var content = new MarkdownDocumentContent(
            Markdown ?? string.Empty,
            Pipeline,
            BaseUri,
            Options,
            style,
            theme);

        var item = new DocumentFlowItem
        {
            Content = content,
            Alignment = DocumentFlowAlignment.Stretch,
            Padding = new Thickness(0),
        };

        if (_flow.Items.Count == 0)
        {
            _flow.Items.Add(item);
        }
        else
        {
            _flow.Items[0] = item;
        }

        _lastResolvedTheme = theme;
        _lastResolvedSourceStyle = sourceStyle;
        _lastNaturalContentWidthVersion = -1;
        _lastNaturalContentWidth = -1;
    }

    private int GetNaturalContentWidth()
    {
        if (_flow.Items.Count == 0)
        {
            return 1;
        }

        var item = _flow.Items[0];
        var content = item.Content;
        var version = content.Version;
        if (_lastNaturalContentWidthVersion == version && _lastNaturalContentWidth > 0)
        {
            return _lastNaturalContentWidth;
        }

        var padding = item.Padding ?? _flow.ItemPadding;
        var paddingHorizontal = Math.Max(0, padding.Horizontal);
        var width = paddingHorizontal;
        for (var index = 0; index < content.BlockCount; index++)
        {
            var block = content.GetBlock(index);
            var visual = block.CreateVisual();
            try
            {
                visual.Measure(LayoutConstraints.Unbounded);
                width = Math.Max(width, visual.MeasureHints.Natural.Width + paddingHorizontal);
            }
            finally
            {
                block.Release(visual);
            }
        }

        _lastNaturalContentWidthVersion = version;
        _lastNaturalContentWidth = Math.Max(1, LayoutConstants.ClampFinite(width));
        return _lastNaturalContentWidth;
    }
}
