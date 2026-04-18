// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Configures init-time optional features for <see cref="CodeEditor"/>.
/// </summary>
/// <remarks>
/// This configuration is intentionally immutable and constructor-only so command registration and optional popup
/// composition stay stable for the lifetime of the editor.
/// </remarks>
public sealed record CodeEditorConfig
{
    /// <summary>
    /// Gets the default code editor configuration.
    /// </summary>
    public static CodeEditorConfig Default { get; } = new();

    /// <summary>
    /// Gets the configuration for the Go To Line popup feature.
    /// </summary>
    public CodeEditorGoToLineConfig GoToLine { get; init; } = new();
}

/// <summary>
/// Configures the Go To Line popup hosted by <see cref="CodeEditor"/>.
/// </summary>
public sealed record CodeEditorGoToLineConfig
{
    /// <summary>
    /// Gets a disabled Go To Line configuration.
    /// </summary>
    public static CodeEditorGoToLineConfig Disabled { get; } = new()
    {
        IsEnabled = false,
    };

    /// <summary>
    /// Gets a value indicating whether the Go To Line popup feature is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Gets the command metadata registered for the Go To Line feature.
    /// </summary>
    public CodeEditorCommandConfig Command { get; init; } = new(
        "Go to line",
        "Open a popup to navigate to a line number.",
        new KeyGesture(TerminalChar.CtrlG, TerminalModifiers.Ctrl));

    /// <summary>
    /// Gets the text displayed before the line number editor.
    /// </summary>
    public string PromptText { get; init; } = "Go to line:";

    /// <summary>
    /// Gets the horizontal alignment used to place the popup inside the editor surface.
    /// </summary>
    public Align PopupHorizontalAlignment { get; init; } = Align.Center;

    /// <summary>
    /// Gets the vertical alignment used to place the popup inside the editor surface.
    /// </summary>
    public Align PopupVerticalAlignment { get; init; } = Align.Center;

    /// <summary>
    /// Gets the horizontal offset applied after popup alignment.
    /// </summary>
    public int PopupOffsetX { get; init; }

    /// <summary>
    /// Gets the vertical offset applied after popup alignment.
    /// </summary>
    public int PopupOffsetY { get; init; }
}

/// <summary>
/// Configures the label, description, and shortcut metadata for a <see cref="CodeEditor"/> command.
/// </summary>
public sealed record CodeEditorCommandConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeEditorCommandConfig"/> class.
    /// </summary>
    /// <param name="labelMarkup">The command label as markup text.</param>
    /// <param name="descriptionMarkup">The optional command description as markup text.</param>
    /// <param name="gesture">The optional shortcut gesture exposed by command surfaces.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="labelMarkup"/> is null or empty.</exception>
    public CodeEditorCommandConfig(string labelMarkup, string? descriptionMarkup, KeyGesture? gesture)
    {
        ArgumentException.ThrowIfNullOrEmpty(labelMarkup);
        LabelMarkup = labelMarkup;
        DescriptionMarkup = descriptionMarkup;
        Gesture = gesture;
    }

    /// <summary>
    /// Gets the command label as markup text.
    /// </summary>
    public string LabelMarkup { get; init; }

    /// <summary>
    /// Gets the optional command description as markup text.
    /// </summary>
    public string? DescriptionMarkup { get; init; }

    /// <summary>
    /// Gets the optional shortcut gesture exposed by command surfaces.
    /// </summary>
    public KeyGesture? Gesture { get; init; }
}
