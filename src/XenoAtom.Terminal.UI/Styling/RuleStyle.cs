// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling for <see cref="Controls.Rule"/>.
/// </summary>
public sealed record RuleStyle : IStyle<RuleStyle>
{
    /// <summary>
    /// Gets the default rule style.
    /// </summary>
    public static RuleStyle Default { get; } = new();

    /// <summary>
    /// Gets the style key for rules.
    /// </summary>
    public static StyleKey<RuleStyle> Key { get; } = new("RuleStyle", Default);

    /// <summary>
    /// Gets the optional glyph set.
    /// </summary>
    public RuleGlyphs? Glyphs { get; init; }

    /// <summary>
    /// Gets the optional line style.
    /// </summary>
    public Style? LineStyle { get; init; }

    /// <summary>
    /// Gets the padding around labels.
    /// </summary>
    public int LabelPadding { get; init; } = 1;

    /// <summary>
    /// Resolves glyphs for the given theme.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <returns>The resolved glyph set.</returns>
    public RuleGlyphs ResolveGlyphs(Theme theme)
        => Glyphs ?? new RuleGlyphs(theme.Lines.Horizontal, theme.Lines.Vertical);

    /// <summary>
    /// Resolves line style for the given theme.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <returns>The resolved cell style.</returns>
    public Style ResolveLineStyle(Theme theme)
        => LineStyle ?? theme.BorderStyle(focused: false);
}
