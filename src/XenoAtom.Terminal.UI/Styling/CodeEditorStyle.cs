// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.CodeEditor"/>.
/// </summary>
public sealed record CodeEditorStyle : IStyle<CodeEditorStyle>
{
    /// <summary>
    /// Gets the default code editor style.
    /// </summary>
    public static CodeEditorStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="CodeEditorStyle"/>.
    /// </summary>
    public static StyleKey<CodeEditorStyle> Key { get; } = new("CodeEditorStyle", Default);

    /// <summary>
    /// Gets the padding between the control bounds and the combined gutter/editor surface.
    /// </summary>
    public Thickness Padding { get; init; } = new(1, 0, 1, 0);

    /// <summary>
    /// Gets the optional background color for the editor surface.
    /// </summary>
    public Color? Background { get; init; }

    /// <summary>
    /// Gets the optional selection background color.
    /// </summary>
    public Color? Selection { get; init; }

    /// <summary>
    /// Gets the optional background color used for non-active search matches.
    /// </summary>
    public Color? SearchMatchBackground { get; init; }

    /// <summary>
    /// Gets the optional background color used for the active search match.
    /// </summary>
    public Color? ActiveSearchMatchBackground { get; init; }

    /// <summary>
    /// Gets the optional placeholder foreground color.
    /// </summary>
    public Color? PlaceholderForeground { get; init; }

    /// <summary>
    /// Gets the optional background color for the current caret line.
    /// </summary>
    public Color? CurrentLineBackground { get; init; }

    /// <summary>
    /// Gets the optional background color for editor margins.
    /// </summary>
    public Color? MarginBackground { get; init; }

    /// <summary>
    /// Gets the optional foreground color for regular line numbers.
    /// </summary>
    public Color? LineNumberForeground { get; init; }

    /// <summary>
    /// Gets the optional foreground color for the active line number.
    /// </summary>
    public Color? ActiveLineNumberForeground { get; init; }

    /// <summary>
    /// Gets the optional foreground color for vertical margin separators.
    /// </summary>
    public Color? MarginSeparatorForeground { get; init; }

    /// <summary>
    /// Gets a value indicating whether vertical separators are drawn between the gutter strips and the text surface.
    /// </summary>
    public bool ShowMarginSeparators { get; init; } = true;

    /// <summary>
    /// Resolves the background style for the editor text surface.
    /// </summary>
    public Style BackgroundStyle(Theme theme, bool focused)
    {
        var style = Style.None;
        if (theme.Foreground is { } fg)
        {
            style = style.WithForeground(fg);
        }

        var themeFill = focused ? (theme.InputFillFocused ?? theme.InputFill) : theme.InputFill;
        var bg = Background ?? themeFill ?? theme.SurfaceAlt ?? theme.Surface ?? theme.Background;
        if (bg is { } b)
        {
            style = style.WithBackground(b);
        }

        return style;
    }

    /// <summary>
    /// Resolves the selection style.
    /// </summary>
    public Style SelectionStyle(Theme theme)
    {
        var style = Style.None;
        var color = Selection ?? theme.Selection;
        if (color is { } c)
        {
            style = style.WithBackground(c);
        }

        style |= TextStyle.Bold;
        return style;
    }

    /// <summary>
    /// Resolves the style used for search match overlays.
    /// </summary>
    /// <param name="theme">The active theme.</param>
    /// <param name="isActive">Whether the match is the currently active search result.</param>
    public Style SearchMatchStyle(Theme theme, bool isActive)
    {
        var style = Style.None;
        var color = isActive
            ? ActiveSearchMatchBackground ?? theme.Warning ?? theme.Accent ?? theme.Selection
            : SearchMatchBackground ?? theme.Accent?.WithAlpha(0x46) ?? theme.Selection;

        if (color is { } c)
        {
            style = style.WithBackground(c);
        }

        return style;
    }

    /// <summary>
    /// Resolves the placeholder style.
    /// </summary>
    public Style PlaceholderStyle(Theme theme, bool focused)
    {
        var style = BackgroundStyle(theme, focused);
        var fg = PlaceholderForeground ?? theme.Muted ?? theme.Foreground;
        if (fg is { } c)
        {
            style = style.WithForeground(c);
        }

        return style;
    }

    /// <summary>
    /// Resolves the background style used for editor margins.
    /// </summary>
    public Style MarginBackgroundStyle(Theme theme, bool focused)
    {
        var baseStyle = BackgroundStyle(theme, focused);
        var bg = MarginBackground;
        if (bg is null)
        {
            var tint = theme.Accent ?? theme.Primary ?? theme.FocusBorder ?? theme.Selection;
            bg = tint?.WithAlpha(0x08);
        }

        return bg is { } color ? baseStyle.WithBackground(color) : baseStyle;
    }

    /// <summary>
    /// Resolves the style used to render regular line numbers.
    /// </summary>
    public Style LineNumberStyle(Theme theme, bool focused)
    {
        var style = MarginBackgroundStyle(theme, focused);
        var fg = LineNumberForeground ?? theme.Muted ?? theme.Foreground;
        if (fg is { } c)
        {
            style = style.WithForeground(c);
        }

        style |= TextStyle.Dim;
        return style;
    }

    /// <summary>
    /// Resolves the style used to render the active line number.
    /// </summary>
    public Style ActiveLineNumberStyle(Theme theme, bool focused)
    {
        var style = MarginBackgroundStyle(theme, focused);
        var fg = ActiveLineNumberForeground ?? theme.Accent ?? theme.Primary ?? theme.Foreground;
        if (fg is { } c)
        {
            style = style.WithForeground(c);
        }

        style |= TextStyle.Bold;
        return style;
    }

    /// <summary>
    /// Resolves the current-line background style.
    /// </summary>
    public Style CurrentLineStyle(Theme theme, bool focused)
    {
        var style = Style.None;
        var bg = CurrentLineBackground;
        if (bg is null)
        {
            var tint = theme.Accent ?? theme.Primary ?? theme.FocusBorder ?? theme.Selection;
            bg = tint?.WithAlpha(focused ? (byte)0x18 : (byte)0x10);
        }

        if (bg is { } color)
        {
            style = style.WithBackground(color);
        }

        return style;
    }

    /// <summary>
    /// Resolves the style used to render vertical margin separators.
    /// </summary>
    public Style MarginSeparatorStyle(Theme theme, bool focused)
    {
        var style = MarginBackgroundStyle(theme, focused);
        var fg = MarginSeparatorForeground ?? theme.Border ?? theme.Muted ?? theme.Foreground;
        if (fg is { } c)
        {
            style = style.WithForeground(c);
        }

        style |= TextStyle.Dim;
        return style;
    }
}
