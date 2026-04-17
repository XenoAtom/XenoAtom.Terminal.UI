// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars;
using TextMateSharp.Themes;
using TextMateTheme = TextMateSharp.Themes.Theme;
using TextMateThemeName = TextMateSharp.Grammars.ThemeName;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;

internal sealed class TextMateThemePalette
{
    private const string TextMateFallbackForeground = "#000000";
    private const string TextMateFallbackBackground = "#ffffff";

    private readonly TextMateTheme _theme;
    private readonly Dictionary<int, Style> _stylesByMetadata;
    private readonly object _sync;
    private readonly string _defaultTokenForeground;

    public TextMateThemePalette(RegistryOptions registryOptions, TextMateThemeName themeName)
    {
        ArgumentNullException.ThrowIfNull(registryOptions);

        var rawTheme = registryOptions.LoadTheme(themeName)
            ?? throw new InvalidOperationException($"Unable to load the bundled TextMate theme `{themeName}`.");
        _theme = TextMateTheme.CreateFromRawTheme(rawTheme, registryOptions);
        _stylesByMetadata = new Dictionary<int, Style>();
        _sync = new object();
        (_defaultTokenForeground, _) = ResolveDefaultTokenColors(rawTheme, registryOptions);
    }

    public Style GetStyle(int metadata)
    {
        if (metadata == 0)
        {
            return Style.None;
        }

        lock (_sync)
        {
            if (!_stylesByMetadata.TryGetValue(metadata, out var style))
            {
                style = ResolveStyle(metadata);
                _stylesByMetadata.Add(metadata, style);
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

    private Style ResolveStyle(int metadata)
    {
        var style = Style.None;

        var foregroundId = EncodedTokenAttributes.GetForeground(metadata);
        if (foregroundId != 0
            && !IsDefaultTokenForeground(foregroundId)
            && TryParseColor(_theme.GetColor(foregroundId), out var foreground))
        {
            style = style.WithForeground(foreground);
        }

        // Binary TextMate metadata bakes an inherited default background into every token, but the host
        // CodeEditor/Markdown surface owns the actual background fill. Applying token backgrounds here causes
        // large opaque blocks that regress rendering, so only the foreground/font-style decorations are used.
        var fontStyle = EncodedTokenAttributes.GetFontStyle(metadata);
        if ((fontStyle & FontStyle.Bold) != 0)
        {
            style |= TextStyle.Bold;
        }

        if ((fontStyle & FontStyle.Italic) != 0)
        {
            style |= TextStyle.Italic;
        }

        if ((fontStyle & FontStyle.Underline) != 0)
        {
            style |= TextStyle.Underline;
        }

        if ((fontStyle & FontStyle.Strikethrough) != 0)
        {
            style |= TextStyle.Strikethrough;
        }

        return style;
    }

    private bool IsDefaultTokenForeground(int foregroundId)
        => string.Equals(_theme.GetColor(foregroundId), _defaultTokenForeground, StringComparison.OrdinalIgnoreCase);

    private static (string Foreground, string Background) ResolveDefaultTokenColors(IRawTheme rawTheme, RegistryOptions registryOptions)
    {
        var foreground = TextMateFallbackForeground;
        var background = TextMateFallbackBackground;
        var visitedThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ApplyDefaultTokenColors(rawTheme, registryOptions, visitedThemes, ref foreground, ref background);
        return (foreground, background);
    }

    private static void ApplyDefaultTokenColors(IRawTheme rawTheme, RegistryOptions registryOptions, HashSet<string> visitedThemes, ref string foreground, ref string background)
    {
        ArgumentNullException.ThrowIfNull(rawTheme);
        ArgumentNullException.ThrowIfNull(registryOptions);
        ArgumentNullException.ThrowIfNull(visitedThemes);

        var include = rawTheme.GetInclude();
        if (!string.IsNullOrWhiteSpace(include) && visitedThemes.Add(include))
        {
            var includedTheme = registryOptions.GetTheme(include);
            if (includedTheme is not null)
            {
                ApplyDefaultTokenColors(includedTheme, registryOptions, visitedThemes, ref foreground, ref background);
            }
        }

        ApplyDefaultTokenColors(rawTheme.GetSettings(), ref foreground, ref background);
        ApplyDefaultTokenColors(rawTheme.GetTokenColors(), ref foreground, ref background);
    }

    private static void ApplyDefaultTokenColors(ICollection<IRawThemeSetting>? settings, ref string foreground, ref string background)
    {
        if (settings is null)
        {
            return;
        }

        foreach (var entry in settings)
        {
            if (entry is null || !HasDefaultScope(entry.GetScope()))
            {
                continue;
            }

            var themeSetting = entry.GetSetting();
            if (themeSetting is null)
            {
                continue;
            }

            var entryForeground = themeSetting.GetForeground();
            if (IsValidHexColor(entryForeground))
            {
                foreground = entryForeground!;
            }

            var entryBackground = themeSetting.GetBackground();
            if (IsValidHexColor(entryBackground))
            {
                background = entryBackground!;
            }
        }
    }

    private static bool HasDefaultScope(object? scope)
    {
        return scope switch
        {
            null => true,
            string text => string.IsNullOrWhiteSpace(text),
            _ => false,
        };
    }

    private static bool IsValidHexColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value[0] != '#')
        {
            return false;
        }

        return value.Length is 7 or 9;
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
