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

    /// <summary>
    /// Gets the document-size threshold, in UTF-16 code units, after which the highlighter switches to a large-document
    /// strategy that prefers immediate approximate visible-range coloring over exact synchronous retokenization.
    /// </summary>
    public int LargeDocumentCharacterThreshold { get; init; } = 64 * 1024;

    /// <summary>
    /// Gets the document-size threshold, in logical lines, after which the highlighter switches to a large-document strategy.
    /// </summary>
    public int LargeDocumentLineThreshold { get; init; } = 4_096;

    /// <summary>
    /// Gets the number of exact lines processed per background pass while progressively building the full TextMate state.
    /// </summary>
    public int BackgroundTokenizationLineBudget { get; init; } = 2_048;

    /// <summary>
    /// Gets the interval, in lines, between exact TextMate rule-stack checkpoints used to resume full tokenization efficiently.
    /// </summary>
    public int CheckpointLineInterval { get; init; } = 1_024;

    /// <summary>
    /// Gets the number of lines to look behind when generating an approximate visible-range tokenization window for large files.
    /// </summary>
    public int SpeculativeLookBehindLineCount { get; init; } = 48;

    /// <summary>
    /// Gets the maximum number of lines to tokenize synchronously for one approximate visible-range request in large files.
    /// </summary>
    public int SpeculativeWindowLineCount { get; init; } = 96;

    /// <summary>
    /// Gets the maximum distance from an existing exact checkpoint that the highlighter will reuse when generating
    /// approximate visible-range tokenization in large files.
    /// </summary>
    public int SpeculativeCheckpointSearchLineCount { get; init; } = 256;
}
