// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using Markdig;
using XenoAtom.Terminal.UI.Extensions.Markdown.Styling;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Extensions.Markdown;

internal static class MarkdownDefaults
{
    public static MarkdownPipeline Pipeline { get; } = new MarkdownPipelineBuilder()
        .Configure("common+pipetables+alerts")
        .Build();

    public static MarkdownPipeline PreciseSourcePipeline { get; } = new MarkdownPipelineBuilder()
        .Configure("common+pipetables+alerts")
        .UsePreciseSourceLocation()
        .Build();

    public static MarkdownStyle ResolveStyle(Theme theme, MarkdownStyle sourceStyle)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(sourceStyle);

        var defaults = MarkdownStyle.Default;

        var headingColor = ResolveBrightYellow(theme);
        var strongColor = (theme.Accent ?? theme.Primary ?? theme.Warning ?? Colors.TerminalBrightCyan).ToRgb();
        var linkColor = (theme.Accent ?? theme.Primary ?? Colors.TerminalBrightBlue).ToRgb();
        var quoteColor = (theme.Muted ?? theme.Border ?? theme.Foreground ?? Colors.TerminalBrightBlack).ToRgb();

        var inlineCodeForeground = ResolveBrightRed(theme);
        var inlineCodeBaseBackground = ResolveInlineCodeBaseBackground(theme);
        var inlineCodeBackgroundTone = IsLightTheme(theme)
            ? inlineCodeBaseBackground.Darken(0.04f)
            : inlineCodeBaseBackground.Lighten(0.04f);
        var inlineCodeBackground = inlineCodeBackgroundTone.WithAlpha(0x66);

        return sourceStyle with
        {
            Heading1Style = ResolveDefaultStyle(
                sourceStyle.Heading1Style,
                defaults.Heading1Style,
                Style.None.WithForeground(headingColor) | TextStyle.Bold | TextStyle.Underline),
            Heading2Style = ResolveDefaultStyle(
                sourceStyle.Heading2Style,
                defaults.Heading2Style,
                Style.None.WithForeground(headingColor) | TextStyle.Bold),
            Heading3Style = ResolveDefaultStyle(
                sourceStyle.Heading3Style,
                defaults.Heading3Style,
                Style.None.WithForeground(headingColor) | TextStyle.Bold),
            Heading4Style = ResolveDefaultStyle(
                sourceStyle.Heading4Style,
                defaults.Heading4Style,
                Style.None.WithForeground(headingColor) | TextStyle.Bold),
            Heading5Style = ResolveDefaultStyle(
                sourceStyle.Heading5Style,
                defaults.Heading5Style,
                Style.None.WithForeground(headingColor) | TextStyle.Bold),
            Heading6Style = ResolveDefaultStyle(
                sourceStyle.Heading6Style,
                defaults.Heading6Style,
                Style.None.WithForeground(headingColor) | TextStyle.Bold),
            StrongStyle = ResolveDefaultStyle(
                sourceStyle.StrongStyle,
                defaults.StrongStyle,
                Style.None.WithForeground(strongColor) | TextStyle.Bold),
            InlineCodeStyle = ResolveDefaultStyle(
                sourceStyle.InlineCodeStyle,
                defaults.InlineCodeStyle,
                Style.None.WithForeground(inlineCodeForeground).WithBackground(inlineCodeBackground)),
            LinkStyle = ResolveDefaultStyle(
                sourceStyle.LinkStyle,
                defaults.LinkStyle,
                Style.None.WithForeground(linkColor) | TextStyle.Underline),
            QuotePrefixStyle = ResolveDefaultStyle(
                sourceStyle.QuotePrefixStyle,
                defaults.QuotePrefixStyle,
                Style.None.WithForeground(quoteColor)),
            NoteAlert = ResolveDefaultAlert(
                sourceStyle.NoteAlert,
                defaults.NoteAlert,
                BuildAlertStyle(theme.Primary ?? theme.Accent ?? Colors.CornflowerBlue)),
            TipAlert = ResolveDefaultAlert(
                sourceStyle.TipAlert,
                defaults.TipAlert,
                BuildAlertStyle(theme.Success ?? Colors.MediumSeaGreen)),
            ImportantAlert = ResolveDefaultAlert(
                sourceStyle.ImportantAlert,
                defaults.ImportantAlert,
                BuildAlertStyle(theme.Accent ?? theme.Primary ?? Colors.MediumPurple)),
            WarningAlert = ResolveDefaultAlert(
                sourceStyle.WarningAlert,
                defaults.WarningAlert,
                BuildAlertStyle(theme.Warning ?? Colors.Goldenrod)),
            CautionAlert = ResolveDefaultAlert(
                sourceStyle.CautionAlert,
                defaults.CautionAlert,
                BuildAlertStyle(theme.Error ?? Colors.IndianRed)),
        };
    }

    private static Style ResolveDefaultStyle(Style style, Style defaultStyle, Style themedStyle)
        => style == defaultStyle ? themedStyle : style;

    private static MarkdownAlertStyle ResolveDefaultAlert(MarkdownAlertStyle style, MarkdownAlertStyle defaultStyle, MarkdownAlertStyle themedStyle)
        => style == defaultStyle ? themedStyle : style;

    private static MarkdownAlertStyle BuildAlertStyle(Color tone)
    {
        tone = tone.ToRgb();
        return MarkdownAlertStyle.Default with
        {
            BorderStyle = Style.None.WithForeground(tone),
            BackgroundStyle = Style.None.WithBackground(tone.WithAlpha(0x22)),
            TitleStyle = Style.None.WithForeground(tone) | TextStyle.Bold,
        };
    }

    private static Color ResolveBrightYellow(Theme theme)
        => theme.Scheme?.BrightYellow ?? Colors.TerminalBrightYellow;

    private static Color ResolveBrightRed(Theme theme)
        => theme.Scheme?.BrightRed ?? Colors.TerminalBrightRed;

    private static Color ResolveInlineCodeBaseBackground(Theme theme)
    {
        var candidate =
            theme.InputFill ??
            theme.SurfaceAlt ??
            theme.InputFillFocused ??
            theme.Selection ??
            Colors.TerminalBrightBlack;
        return ResolveColorAgainstThemeBackground(candidate, theme.Background ?? Color.Default);
    }

    private static Color ResolveColorAgainstThemeBackground(Color color, Color themeBackground)
    {
        color = color.Kind is ColorKind.Basic16 or ColorKind.Indexed256 ? color.ToRgb() : color;
        if (color.Kind == ColorKind.Rgb)
        {
            return color;
        }

        if (color.Kind != ColorKind.RgbA)
        {
            return Colors.TerminalBrightBlack.ToRgb();
        }

        if (themeBackground.Kind is ColorKind.Basic16 or ColorKind.Indexed256)
        {
            themeBackground = themeBackground.ToRgb();
        }
        else if (themeBackground.Kind == ColorKind.RgbA)
        {
            themeBackground = Color.Rgb(themeBackground.R, themeBackground.G, themeBackground.B);
        }

        if (themeBackground.Kind != ColorKind.Rgb)
        {
            return Color.Rgb(color.R, color.G, color.B);
        }

        if (color.A == 0)
        {
            return themeBackground;
        }

        if (color.A >= byte.MaxValue)
        {
            return Color.Rgb(color.R, color.G, color.B);
        }

        var alpha = color.A / 255f;
        return themeBackground.Mix(Color.Rgb(color.R, color.G, color.B), alpha, ColorMixSpace.LinearRgb);
    }

    private static bool IsLightTheme(Theme theme)
    {
        var background = theme.Background?.ToRgb() ?? Color.Default;
        var foreground = theme.Foreground?.ToRgb() ?? Color.Default;
        if (background.Kind == ColorKind.Default || foreground.Kind == ColorKind.Default)
        {
            return false;
        }

        var backgroundLuminance = background.GetRelativeLuminance();
        var foregroundLuminance = foreground.GetRelativeLuminance();
        return backgroundLuminance > foregroundLuminance && backgroundLuminance >= 0.55f;
    }
}
