// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public sealed class TextBoxStyle
{
    public static TextBoxStyle Default { get; } = new();

    public static EnvironmentKey<TextBoxStyle> Key { get; } = new("TextBoxStyle", Default);

    public Rgb24? Border { get; init; }
    public Rgb24? FocusBorder { get; init; }
    public Rgb24? Selection { get; init; }

    public CellStyle BorderStyle(Theme theme, bool focused)
    {
        var color = focused ? (FocusBorder ?? theme.FocusBorder) : (Border ?? theme.Border);
        var style = CellStyle.None;
        if (color is { } c)
        {
            style = style.WithForeground(c);
        }
        return style;
    }

    public CellStyle SelectionStyle(Theme theme)
    {
        var style = CellStyle.None;
        var color = Selection ?? theme.Selection;
        if (color is { } c)
        {
            style = style.WithBackground(c);
        }
        style |= CellStyle.Bold;
        return style;
    }
}
