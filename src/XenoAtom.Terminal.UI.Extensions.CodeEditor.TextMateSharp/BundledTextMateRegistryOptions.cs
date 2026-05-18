// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;
using TextMateThemeName = TextMateSharp.Grammars.ThemeName;

namespace XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;

internal sealed class BundledTextMateRegistryOptions : IRegistryOptions
{
    public const string TomlLanguageId = "toml";
    public const string TomlScopeName = "source.toml";

    private const string TomlGrammarResourceName = "XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp.Syntaxes.TOML.tmLanguage";

    private readonly Lazy<IRawGrammar?> _tomlGrammar;

    public BundledTextMateRegistryOptions(TextMateThemeName themeName)
    {
        RegistryOptions = new RegistryOptions(themeName);
        _tomlGrammar = new Lazy<IRawGrammar?>(LoadTomlGrammar);
    }

    public RegistryOptions RegistryOptions { get; }

    public IRawTheme? GetTheme(string scopeName)
        => RegistryOptions.GetTheme(scopeName);

    public IRawGrammar? GetGrammar(string scopeName)
    {
        if (string.Equals(scopeName, TomlScopeName, StringComparison.Ordinal))
        {
            return _tomlGrammar.Value;
        }

        return RegistryOptions.GetGrammar(scopeName);
    }

    public ICollection<string>? GetInjections(string scopeName)
        => RegistryOptions.GetInjections(scopeName);

    public IRawTheme? GetDefaultTheme()
        => RegistryOptions.GetDefaultTheme();

    private static IRawGrammar? LoadTomlGrammar()
    {
        var assembly = typeof(BundledTextMateRegistryOptions).GetTypeInfo().Assembly;
        var stream = assembly.GetManifestResourceStream(TomlGrammarResourceName);
        if (stream is null)
        {
            return null;
        }

        using (stream)
        using (var reader = new StreamReader(stream))
        {
            return GrammarReader.ReadGrammarSync(reader);
        }
    }
}
