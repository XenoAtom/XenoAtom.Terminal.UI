// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using TextMateThemeName = TextMateSharp.Grammars.ThemeName;

namespace XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;

/// <summary>
/// Configures a TextMateSharp-backed <see cref="Controls.CodeEditor"/> syntax highlighter.
/// </summary>
public sealed record TextMateCodeEditorOptions
{
    /// <summary>
    /// Gets an explicit TextMate scope name such as <c>source.cs</c>.
    /// </summary>
    public string? ScopeName { get; init; }

    /// <summary>
    /// Gets a TextMate language identifier or alias such as <c>csharp</c> or <c>cs</c>.
    /// </summary>
    public string? LanguageId { get; init; }

    /// <summary>
    /// Gets an optional file name used to resolve the grammar from its extension.
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// Gets the bundled TextMate theme used when the host terminal theme is dark.
    /// </summary>
    public TextMateThemeName DarkTheme { get; init; } = TextMateThemeName.DarkPlus;

    /// <summary>
    /// Gets the bundled TextMate theme used when the host terminal theme is light.
    /// </summary>
    public TextMateThemeName LightTheme { get; init; } = TextMateThemeName.LightPlus;
}
