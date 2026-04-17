// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using TextMateThemeName = TextMateSharp.Grammars.ThemeName;

namespace XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;

/// <summary>
/// Configures the TextMateSharp-backed Markdown fenced-code renderer.
/// </summary>
public sealed record TextMateMarkdownRendererOptions
{
    /// <summary>
    /// Gets an optional explicit TextMate scope name used when a fenced code block does not declare a language.
    /// </summary>
    public string? DefaultScopeName { get; init; }

    /// <summary>
    /// Gets an optional default language identifier or alias used when a fenced code block does not declare a language.
    /// </summary>
    public string? DefaultLanguageId { get; init; }

    /// <summary>
    /// Gets the bundled TextMate theme used when the host terminal theme is dark.
    /// </summary>
    public TextMateThemeName DarkTheme { get; init; } = TextMateThemeName.DarkPlus;

    /// <summary>
    /// Gets the bundled TextMate theme used when the host terminal theme is light.
    /// </summary>
    public TextMateThemeName LightTheme { get; init; } = TextMateThemeName.LightPlus;
}
