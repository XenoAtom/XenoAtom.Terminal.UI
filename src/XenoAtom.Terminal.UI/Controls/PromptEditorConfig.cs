// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Input;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Configures the user-facing command metadata registered by <see cref="PromptEditor"/>.
/// </summary>
public sealed record PromptEditorConfig
{
    /// <summary>
    /// Gets the default prompt editor command configuration.
    /// </summary>
    public static PromptEditorConfig Default { get; } = new();

    /// <summary>
    /// Gets the configuration for the <c>PromptEditor.Accept</c> command.
    /// </summary>
    public PromptEditorCommandConfig AcceptCommand { get; init; } = new(
        "Accept",
        "Accept the current prompt text.",
        new KeyGesture(TerminalKey.Enter));

    /// <summary>
    /// Gets the configuration for the <c>PromptEditor.Cancel</c> command.
    /// </summary>
    public PromptEditorCommandConfig CancelCommand { get; init; } = new(
        "Cancel",
        "Cancel completion or cancel the prompt.",
        new KeyGesture(TerminalKey.Escape));

    /// <summary>
    /// Gets the configuration for the <c>PromptEditor.InsertNewLine</c> command.
    /// </summary>
    public PromptEditorCommandConfig InsertNewLineCommand { get; init; } = new(
        "New line",
        "Insert a newline in the prompt editor (LF).",
        new KeyGesture(TerminalKey.Enter, TerminalModifiers.Shift));

    /// <summary>
    /// Gets the fallback shortcut gesture used for the <c>PromptEditor.InsertNewLine</c> command when the preferred
    /// gesture requires extended keyboard input and the current terminal does not support it.
    /// </summary>
    /// <remarks>
    /// The default <see cref="InsertNewLineCommand"/> gesture is Shift+Enter. Terminals that cannot report modifiers on
    /// Enter use this fallback instead. Set this property to another gesture to customize the fallback shortcut, or
    /// <see langword="null"/> to keep the preferred gesture even when extended keys are unavailable.
    /// </remarks>
    public KeyGesture? InsertNewLineFallbackGesture { get; init; } =
        new KeyGesture(TerminalChar.CtrlN, TerminalModifiers.Ctrl);

    /// <summary>
    /// Gets the configuration for the <c>PromptEditor.Complete</c> command.
    /// </summary>
    public PromptEditorCommandConfig CompleteCommand { get; init; } = new(
        "Complete",
        "Request completion at the caret.",
        new KeyGesture(TerminalKey.Tab));

    /// <summary>
    /// Gets the configuration for the <c>PromptEditor.HistoryPrevious</c> command.
    /// </summary>
    public PromptEditorCommandConfig HistoryPreviousCommand { get; init; } = new(
        "History (previous)",
        "Load the previous history entry.",
        new KeyGesture(TerminalKey.Up, TerminalModifiers.Alt));

    /// <summary>
    /// Gets the configuration for the <c>PromptEditor.HistoryNext</c> command.
    /// </summary>
    public PromptEditorCommandConfig HistoryNextCommand { get; init; } = new(
        "History (next)",
        "Load the next history entry.",
        new KeyGesture(TerminalKey.Down, TerminalModifiers.Alt));
}

/// <summary>
/// Configures the label, description, and shortcut metadata for a <see cref="PromptEditor"/> command.
/// </summary>
public sealed record PromptEditorCommandConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PromptEditorCommandConfig"/> class.
    /// </summary>
    /// <param name="labelMarkup">The command label as markup text.</param>
    /// <param name="descriptionMarkup">The optional command description as markup text.</param>
    /// <param name="gesture">The optional shortcut gesture exposed by command surfaces.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="labelMarkup"/> is null or empty.</exception>
    public PromptEditorCommandConfig(string labelMarkup, string? descriptionMarkup, KeyGesture? gesture)
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
