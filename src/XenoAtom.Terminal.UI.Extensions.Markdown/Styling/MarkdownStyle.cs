// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Extensions.Markdown.Styling;

/// <summary>
/// Defines style values used by <c>MarkdownControl</c> and Markdown document rendering.
/// </summary>
public sealed record MarkdownStyle : IStyle<MarkdownStyle>
{
    /// <summary>
    /// Gets the default markdown style.
    /// </summary>
    public static MarkdownStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key for <see cref="MarkdownStyle"/>.
    /// </summary>
    public static StyleKey<MarkdownStyle> Key { get; } = new("MarkdownStyle", Default);

    /// <summary>
    /// Gets the style applied to normal paragraph text.
    /// </summary>
    public Style ParagraphStyle { get; init; } = Style.None;

    /// <summary>
    /// Gets the style applied to level 1 headings.
    /// </summary>
    public Style Heading1Style { get; init; } = Style.None | TextStyle.Bold | TextStyle.Underline;

    /// <summary>
    /// Gets the style applied to level 2 headings.
    /// </summary>
    public Style Heading2Style { get; init; } = Style.None | TextStyle.Bold;

    /// <summary>
    /// Gets the style applied to level 3 headings.
    /// </summary>
    public Style Heading3Style { get; init; } = Style.None | TextStyle.Bold;

    /// <summary>
    /// Gets the style applied to level 4 headings.
    /// </summary>
    public Style Heading4Style { get; init; } = Style.None | TextStyle.Bold;

    /// <summary>
    /// Gets the style applied to level 5 headings.
    /// </summary>
    public Style Heading5Style { get; init; } = Style.None | TextStyle.Bold;

    /// <summary>
    /// Gets the style applied to level 6 headings.
    /// </summary>
    public Style Heading6Style { get; init; } = Style.None | TextStyle.Bold;

    /// <summary>
    /// Gets the style applied to emphasized text.
    /// </summary>
    public Style EmphasisStyle { get; init; } = Style.None | TextStyle.Italic;

    /// <summary>
    /// Gets the style applied to strong-emphasis text.
    /// </summary>
    public Style StrongStyle { get; init; } = Style.None | TextStyle.Bold;

    /// <summary>
    /// Gets the style applied to inline code spans.
    /// </summary>
    public Style InlineCodeStyle { get; init; } = Style.None;

    /// <summary>
    /// Gets the style applied to links.
    /// </summary>
    public Style LinkStyle { get; init; } = Style.None | TextStyle.Underline;

    /// <summary>
    /// Gets the style applied to HTML text fallbacks.
    /// </summary>
    public Style HtmlStyle { get; init; } = Style.None | TextStyle.Dim;

    /// <summary>
    /// Gets the prefix used for unordered list items.
    /// </summary>
    public string UnorderedListBullet { get; init; } = "•";

    /// <summary>
    /// Gets the prefix used for quote lines.
    /// </summary>
    public string QuotePrefix { get; init; } = "│ ";

    /// <summary>
    /// Gets the style applied to quote prefixes.
    /// </summary>
    public Style QuotePrefixStyle { get; init; } = Style.None;

    /// <summary>
    /// Gets style for NOTE alerts.
    /// </summary>
    public MarkdownAlertStyle NoteAlert { get; init; } = MarkdownAlertStyle.Default;

    /// <summary>
    /// Gets style for TIP alerts.
    /// </summary>
    public MarkdownAlertStyle TipAlert { get; init; } = MarkdownAlertStyle.Default;

    /// <summary>
    /// Gets style for IMPORTANT alerts.
    /// </summary>
    public MarkdownAlertStyle ImportantAlert { get; init; } = MarkdownAlertStyle.Default;

    /// <summary>
    /// Gets style for WARNING alerts.
    /// </summary>
    public MarkdownAlertStyle WarningAlert { get; init; } = MarkdownAlertStyle.Default;

    /// <summary>
    /// Gets style for CAUTION alerts.
    /// </summary>
    public MarkdownAlertStyle CautionAlert { get; init; } = MarkdownAlertStyle.Default;

    /// <summary>
    /// Resolves the style for a heading level.
    /// </summary>
    /// <param name="level">The heading level in the range [1..6].</param>
    /// <returns>The heading style.</returns>
    public Style ResolveHeadingStyle(int level)
    {
        return level switch
        {
            1 => Heading1Style,
            2 => Heading2Style,
            3 => Heading3Style,
            4 => Heading4Style,
            5 => Heading5Style,
            _ => Heading6Style,
        };
    }

    /// <summary>
    /// Resolves alert style for an alert kind (e.g. NOTE, TIP).
    /// </summary>
    /// <param name="kind">The alert kind.</param>
    /// <returns>The resolved alert style.</returns>
    public MarkdownAlertStyle ResolveAlertStyle(string kind)
    {
        return kind.ToUpperInvariant() switch
        {
            "TIP" => TipAlert,
            "IMPORTANT" => ImportantAlert,
            "WARNING" => WarningAlert,
            "CAUTION" => CautionAlert,
            _ => NoteAlert,
        };
    }
}

/// <summary>
/// Defines visual styles for a markdown alert block.
/// </summary>
public sealed record MarkdownAlertStyle
{
    /// <summary>
    /// Gets the default alert style.
    /// </summary>
    public static MarkdownAlertStyle Default { get; } = new();

    /// <summary>
    /// Gets the border style.
    /// </summary>
    public Style BorderStyle { get; init; } = Style.None;

    /// <summary>
    /// Gets the surface background style.
    /// </summary>
    public Style BackgroundStyle { get; init; } = Style.None;

    /// <summary>
    /// Gets the title style.
    /// </summary>
    public Style TitleStyle { get; init; } = Style.None | TextStyle.Bold;
}

