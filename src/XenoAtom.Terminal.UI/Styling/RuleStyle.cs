// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

public sealed record RuleStyle : IStyle<RuleStyle>
{
    public static RuleStyle Default { get; } = new();

    public static StyleKey<RuleStyle> Key { get; } = new("RuleStyle", Default);

    public RuleGlyphs? Glyphs { get; init; }

    public CellStyle? LineStyle { get; init; }

    public int LabelPadding { get; init; } = 1;

    public RuleGlyphs ResolveGlyphs(Theme theme)
        => Glyphs ?? new RuleGlyphs(theme.Lines.Horizontal, theme.Lines.Vertical);

    public CellStyle ResolveLineStyle(Theme theme)
        => LineStyle ?? theme.BorderStyle(focused: false);
}

