// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using Markdig;
using XenoAtom.Terminal.UI.Extensions.Markdown;
using XenoAtom.Terminal.UI.Extensions.Markdown.Styling;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Scrolling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Renders a markdown document by converting it to <see cref="DocumentFlow"/> content.
/// </summary>
public sealed partial class MarkdownControl : Visual, IScrollable
{
    private readonly DocumentFlow _flow;

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
        };
        AttachChild(_flow);

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

    /// <inheritdoc />
    public ScrollModel Scroll => _flow.Scroll;

    /// <inheritdoc />
    protected override int ChildrenCount => 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index) => index == 0 ? _flow : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        _flow.Measure(constraints);
        var desired = constraints.Clamp(_flow.DesiredSize);
        return SizeHints.Flex(
            min: constraints.Clamp(new Size(1, 1)),
            natural: desired,
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

    private void RebuildContent()
    {
        VerifyAccess();

        var style = RenderStyle ?? GetStyle<MarkdownStyle>();
        var content = new MarkdownDocumentContent(
            Markdown ?? string.Empty,
            Pipeline,
            BaseUri,
            Options,
            style);

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
    }
}
