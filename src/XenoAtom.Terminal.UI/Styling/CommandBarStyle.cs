// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling for the command bar control.
/// </summary>
public sealed record CommandBarStyle : IStyle<CommandBarStyle>
{
    /// <summary>
    /// Gets the default command bar style.
    /// </summary>
    public static CommandBarStyle Default { get; } = new();

    /// <summary>
    /// Gets the style key for command bars.
    /// </summary>
    public static StyleKey<CommandBarStyle> Key { get; } = new("CommandBarStyle", Default);

    /// <summary>
    /// Gets the optional bar background color.
    /// </summary>
    public Color? Background { get; init; }

    /// <summary>
    /// Gets the optional bar foreground color.
    /// </summary>
    public Color? Foreground { get; init; }

    /// <summary>
    /// Gets the optional foreground color for keycaps.
    /// </summary>
    public Color? KeyForeground { get; init; }

    /// <summary>
    /// Gets the optional background color for keycaps.
    /// </summary>
    public Color? KeyBackground { get; init; }

    /// <summary>
    /// Gets the number of spaces inserted between command entries.
    /// </summary>
    public int Gap { get; init; } = 2;

    /// <summary>
    /// Gets the character used to open a keycap.
    /// </summary>
    public Rune KeycapOpen { get; init; } = new('[');

    /// <summary>
    /// Gets the character used to close a keycap.
    /// </summary>
    public Rune KeycapClose { get; init; } = new(']');

    /// <summary>
    /// Resolves the style set used by the command bar for the given theme.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <returns>The resolved styles.</returns>
    public CommandBarResolvedStyle Resolve(Theme theme)
    {
        var barStyle = theme.SurfaceStyle();
        if (Foreground is { } fg) barStyle = barStyle.WithForeground(fg);
        if (Background is { } bg) barStyle = barStyle.WithBackground(bg);

        var keyStyle = Style.None;
        var keyFg = KeyForeground ?? theme.Accent ?? theme.Primary ?? theme.Foreground;
        if (keyFg is { } kfg) keyStyle = keyStyle.WithForeground(kfg);
        if (KeyBackground is { } kbg) keyStyle = keyStyle.WithBackground(kbg);
        keyStyle |= TextStyle.Bold;

        var labelStyle = Style.None;
        if ((Foreground ?? theme.Foreground) is { } labelFg)
        {
            labelStyle = labelStyle.WithForeground(labelFg);
        }

        var disabledStyle = Style.None;
        if (theme.Disabled is { } disabledFg)
        {
            disabledStyle = disabledStyle.WithForeground(disabledFg);
        }

        return new CommandBarResolvedStyle(
            BarStyle: barStyle,
            KeyStyle: keyStyle,
            LabelStyle: labelStyle,
            DisabledLabelStyle: disabledStyle);
    }
}

/// <summary>
/// Represents a resolved set of styles used by a command bar instance.
/// </summary>
/// <param name="BarStyle">The base bar style used for background fill.</param>
/// <param name="KeyStyle">The style used for keycap text.</param>
/// <param name="LabelStyle">The style used for command labels.</param>
/// <param name="DisabledLabelStyle">The style used for disabled command labels.</param>
public readonly record struct CommandBarResolvedStyle(
    Style BarStyle,
    Style KeyStyle,
    Style LabelStyle,
    Style DisabledLabelStyle);
