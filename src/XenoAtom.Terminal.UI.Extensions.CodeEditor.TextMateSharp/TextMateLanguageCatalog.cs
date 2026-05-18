// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.IO;
using TextMateSharp.Grammars;
using TextMateThemeName = TextMateSharp.Grammars.ThemeName;

namespace XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;

internal sealed class TextMateLanguageCatalog
{
    private readonly BundledTextMateRegistryOptions _discoveryRegistryOptions;
    private readonly Dictionary<string, string> _scopesByLanguage;
    private readonly Dictionary<string, string> _scopesByExtension;
    private readonly Dictionary<TextMateThemeName, BundledTextMateRegistryOptions> _registryOptionsByTheme;
    private readonly Dictionary<TextMateThemeName, TextMateThemePalette> _palettes;

    public static TextMateLanguageCatalog Default { get; } = new();

    private TextMateLanguageCatalog()
    {
        _discoveryRegistryOptions = new BundledTextMateRegistryOptions(TextMateThemeName.DarkPlus);
        _scopesByLanguage = new Dictionary<string, string>(StringComparer.Ordinal);
        _scopesByExtension = new Dictionary<string, string>(StringComparer.Ordinal);
        _registryOptionsByTheme = new Dictionary<TextMateThemeName, BundledTextMateRegistryOptions>();
        _palettes = new Dictionary<TextMateThemeName, TextMateThemePalette>();

        foreach (var language in _discoveryRegistryOptions.RegistryOptions.GetAvailableLanguages())
        {
            if (string.IsNullOrWhiteSpace(language.Id))
            {
                continue;
            }

            var scopeName = _discoveryRegistryOptions.RegistryOptions.GetScopeByLanguageId(language.Id);
            if (string.IsNullOrWhiteSpace(scopeName))
            {
                continue;
            }

            AddLanguageKey(language.Id, scopeName);
            if (language.Aliases is not null)
            {
                foreach (var alias in language.Aliases)
                {
                    AddLanguageKey(alias, scopeName);
                }
            }

            if (language.Extensions is not null)
            {
                foreach (var extension in language.Extensions)
                {
                    AddExtensionKey(extension, scopeName);
                }
            }
        }

        AddBundledTomlLanguage();
    }

    public TextMateTokenizationSession CreateSession(string scopeName, TextMateThemeName themeName)
        => new(scopeName, GetRegistryOptions(themeName));

    public TextMateThemePalette GetPalette(TextMateThemeName themeName)
    {
        lock (_palettes)
        {
            if (!_palettes.TryGetValue(themeName, out var palette))
            {
                palette = new TextMateThemePalette(GetRegistryOptions(themeName).RegistryOptions, themeName);
                _palettes.Add(themeName, palette);
            }

            return palette;
        }
    }

    public string ResolveScopeName(TextMateCodeEditorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(options.ScopeName))
        {
            return options.ScopeName.Trim();
        }

        if (TryResolveScope(options.LanguageId, out var scopeName))
        {
            return scopeName;
        }

        if (!string.IsNullOrWhiteSpace(options.FileName)
            && TryResolveScopeByExtension(Path.GetExtension(options.FileName), out scopeName))
        {
            return scopeName;
        }

        throw new ArgumentException(
            "Unable to resolve a TextMate scope name. Set ScopeName, LanguageId, or FileName to a supported language.",
            nameof(options));
    }

    public bool TryResolveMarkdownScope(string? language, TextMateMarkdownRendererOptions options, out string scopeName)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (TryResolveScope(language, out scopeName))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(options.DefaultScopeName))
        {
            scopeName = options.DefaultScopeName.Trim();
            return true;
        }

        return TryResolveScope(options.DefaultLanguageId, out scopeName);
    }

    private bool TryResolveScope(string? key, out string scopeName)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            if (TryResolveScopeByLanguage(key, out scopeName))
            {
                return true;
            }

            if (TryResolveScopeByExtension(key, out scopeName))
            {
                return true;
            }
        }

        scopeName = string.Empty;
        return false;
    }

    private bool TryResolveScopeByLanguage(string? language, out string scopeName)
    {
        if (!string.IsNullOrWhiteSpace(language)
            && _scopesByLanguage.TryGetValue(NormalizeLanguageKey(language), out scopeName!))
        {
            return true;
        }

        scopeName = string.Empty;
        return false;
    }

    private bool TryResolveScopeByExtension(string? extension, out string scopeName)
    {
        if (!string.IsNullOrWhiteSpace(extension)
            && _scopesByExtension.TryGetValue(NormalizeExtensionKey(extension), out scopeName!))
        {
            return true;
        }

        scopeName = string.Empty;
        return false;
    }

    private void AddLanguageKey(string? language, string scopeName)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return;
        }

        var key = NormalizeLanguageKey(language);
        _scopesByLanguage.TryAdd(key, scopeName);
    }

    private void AddExtensionKey(string? extension, string scopeName)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return;
        }

        var key = NormalizeExtensionKey(extension);
        _scopesByExtension.TryAdd(key, scopeName);
    }

    private static string NormalizeLanguageKey(string value)
        => value.Trim().ToLowerInvariant();

    private static string NormalizeExtensionKey(string value)
    {
        value = value.Trim();
        if (value[0] != '.')
        {
            value = string.Concat('.', value);
        }

        return value.ToLowerInvariant();
    }

    private BundledTextMateRegistryOptions GetRegistryOptions(TextMateThemeName themeName)
    {
        lock (_registryOptionsByTheme)
        {
            if (!_registryOptionsByTheme.TryGetValue(themeName, out var registryOptions))
            {
                registryOptions = new BundledTextMateRegistryOptions(themeName);
                _registryOptionsByTheme.Add(themeName, registryOptions);
            }

            return registryOptions;
        }
    }

    private void AddBundledTomlLanguage()
    {
        AddLanguageKey(BundledTextMateRegistryOptions.TomlLanguageId, BundledTextMateRegistryOptions.TomlScopeName);
        AddLanguageKey("tml", BundledTextMateRegistryOptions.TomlScopeName);
        AddExtensionKey(".toml", BundledTextMateRegistryOptions.TomlScopeName);
        AddExtensionKey(".tml", BundledTextMateRegistryOptions.TomlScopeName);
    }
}
