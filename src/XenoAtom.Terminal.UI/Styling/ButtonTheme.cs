// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public sealed class ButtonTheme
{
    public static ButtonTheme Default { get; } = new();

    public static EnvironmentKey<ButtonTheme> Key { get; } = new("ButtonTheme", Default);

    public CellStyle? Normal { get; init; }
    public CellStyle? Hovered { get; init; }
    public CellStyle? Pressed { get; init; }
    public CellStyle? Focused { get; init; }
    public CellStyle? Disabled { get; init; }

    public CellStyle Resolve(Theme theme, bool enabled, bool focused, bool hovered, bool pressed)
    {
        if (!enabled)
        {
            return Disabled ?? CellStyle.Dim;
        }

        if (pressed)
        {
            return Pressed ?? theme.SelectionStyle();
        }

        if (focused)
        {
            return Focused ?? theme.SelectionStyle();
        }

        if (hovered)
        {
            if (Hovered is { } h)
            {
                return h;
            }

            var style = CellStyle.None;
            if (theme.Selection is { } selection)
            {
                style = style.WithBackground(selection);
            }
            return style;
        }

        return Normal ?? CellStyle.None;
    }
}

