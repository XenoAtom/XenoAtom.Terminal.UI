// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using TextMateSharp.Grammars;
using TextMateSharp.Themes;
using TextMateThemeName = TextMateSharp.Grammars.ThemeName;
using TextMateFontStyle = TextMateSharp.Themes.FontStyle;
using XenoAtom.Terminal.UI.Styling;
using TextMateTheme = TextMateSharp.Themes.Theme;

namespace XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;

internal sealed class TextMateThemePalette
{
    private readonly TextMateTheme _theme;
    private readonly Dictionary<string, Style> _stylesByScopeKey;
    private readonly object _sync;

    public TextMateThemePalette(RegistryOptions registryOptions, TextMateThemeName themeName)
    {
        ArgumentNullException.ThrowIfNull(registryOptions);

        var rawTheme = registryOptions.LoadTheme(themeName)
            ?? throw new InvalidOperationException($"Unable to load the bundled TextMate theme `{themeName}`.");
        _theme = TextMateTheme.CreateFromRawTheme(rawTheme, registryOptions);
        _stylesByScopeKey = new Dictionary<string, Style>(StringComparer.Ordinal);
        _sync = new object();
    }

    public Style GetStyle(TextMateTokenizedSegment segment)
    {
        lock (_sync)
        {
            if (!_stylesByScopeKey.TryGetValue(segment.ScopeKey, out var style))
            {
                style = ResolveStyle(segment.Scopes);
                _stylesByScopeKey.Add(segment.ScopeKey, style);
            }

            return style;
        }
    }

    public static bool IsLightTheme(XenoAtom.Terminal.UI.Styling.Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

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

    private Style ResolveStyle(string[] scopes)
    {
        var matches = _theme.Match(scopes);
        if (matches.Count == 0)
        {
            return Style.None;
        }

        var fontStyle = TextMateFontStyle.NotSet;
        var foregroundId = 0;
        var backgroundId = 0;

        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            if (match.fontStyle != TextMateFontStyle.NotSet)
            {
                fontStyle = match.fontStyle;
            }

            if (match.foreground != 0)
            {
                foregroundId = match.foreground;
            }

            if (match.background != 0)
            {
                backgroundId = match.background;
            }
        }

        if (fontStyle == TextMateFontStyle.NotSet)
        {
            fontStyle = TextMateFontStyle.None;
        }

        var style = Style.None;
        if (foregroundId != 0 && TryParseColor(_theme.GetColor(foregroundId), out var foreground))
        {
            style = style.WithForeground(foreground);
        }

        if (backgroundId != 0 && TryParseColor(_theme.GetColor(backgroundId), out var background))
        {
            style = style.WithBackground(background);
        }

        if ((fontStyle & TextMateFontStyle.Bold) != 0)
        {
            style |= TextStyle.Bold;
        }

        if ((fontStyle & TextMateFontStyle.Italic) != 0)
        {
            style |= TextStyle.Italic;
        }

        if ((fontStyle & TextMateFontStyle.Underline) != 0)
        {
            style |= TextStyle.Underline;
        }

        if ((fontStyle & TextMateFontStyle.Strikethrough) != 0)
        {
            style |= TextStyle.Strikethrough;
        }

        return style;
    }

    private static bool TryParseColor(string? value, out Color color)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            color = default;
            return false;
        }

        value = value.Trim();
        if (value[0] != '#')
        {
            color = default;
            return false;
        }

        if (value.Length == 7
            && byte.TryParse(value.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, null, out var r)
            && byte.TryParse(value.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, null, out var g)
            && byte.TryParse(value.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            color = Color.Rgb(r, g, b);
            return true;
        }

        if (value.Length == 9
            && byte.TryParse(value.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, null, out r)
            && byte.TryParse(value.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, null, out g)
            && byte.TryParse(value.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, null, out b)
            && byte.TryParse(value.AsSpan(7, 2), System.Globalization.NumberStyles.HexNumber, null, out var a))
        {
            color = Color.RgbA(r, g, b, a);
            return true;
        }

        color = default;
        return false;
    }
}
