// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Text;
using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Specifies how Enter and Ctrl+J are interpreted by <see cref="PromptEditor"/>.
/// </summary>
public enum PromptEditorEnterMode
{
    /// <summary>
    /// Enter accepts the prompt, and Ctrl+J inserts a newline.
    /// </summary>
    EnterAccepts = 0,

    /// <summary>
    /// Enter inserts a newline, and Ctrl+J accepts the prompt.
    /// </summary>
    EnterInsertsNewLine = 1,
}

/// <summary>
/// Specifies whether <see cref="PromptEditor"/> edits a single line or multiple lines.
/// </summary>
public enum PromptEditorLineMode
{
    /// <summary>
    /// Allow multiple lines of text.
    /// </summary>
    MultiLine = 0,

    /// <summary>
    /// Restrict the editor to a single line and discard any attempted line breaks.
    /// </summary>
    SingleLine = 1,
}

/// <summary>
/// Specifies how <see cref="TerminalKey.Escape"/> is interpreted by <see cref="PromptEditor"/>.
/// </summary>
public enum PromptEditorEscapeBehavior
{
    /// <summary>
    /// Escape cancels completion when active; otherwise it raises <see cref="PromptEditor.CanceledEvent"/>.
    /// </summary>
    CancelPromptOrCompletion = 0,

    /// <summary>
    /// Escape cancels completion when active and otherwise falls through to other bindings.
    /// </summary>
    CancelCompletionOnly = 1,
}

/// <summary>
/// Specifies the completion UI mode used by <see cref="PromptEditor"/>.
/// </summary>
public enum PromptEditorCompletionPresentation
{
    /// <summary>
    /// Do not show built-in UI. The control can still render ghost completion if available.
    /// </summary>
    None = 0,

    /// <summary>
    /// Apply completion candidates inline and cycle through them on repeated triggers.
    /// </summary>
    InlineCycle = 1,

    /// <summary>
    /// Show candidates in a popup list anchored to the caret.
    /// </summary>
    PopupList = 2,
}

/// <summary>
/// Represents the completion result computed for a given request.
/// </summary>
/// <param name="Handled">Whether the completion handler produced a result.</param>
/// <param name="Candidates">The candidate list, if any.</param>
/// <param name="ReplaceStart">The start index to replace within the document.</param>
/// <param name="ReplaceLength">The length to replace within the document.</param>
/// <param name="SelectedIndex">The selected candidate index.</param>
/// <param name="GhostText">Optional ghost completion to render after the caret.</param>
public readonly record struct PromptEditorCompletion(
    bool Handled,
    IReadOnlyList<string>? Candidates,
    int ReplaceStart,
    int ReplaceLength,
    int SelectedIndex = 0,
    string? GhostText = null);

/// <summary>
/// Represents a completion request for a <see cref="PromptEditor"/>.
/// </summary>
public readonly record struct PromptEditorCompletionRequest(
    ITextSnapshot Snapshot,
    int CaretIndex,
    int SelectionStart,
    int SelectionLength,
    TerminalModifiers Modifiers);

/// <summary>
/// Delegate used by <see cref="PromptEditor"/> to compute completions.
/// </summary>
public delegate PromptEditorCompletion PromptEditorCompletionHandler(in PromptEditorCompletionRequest request);

/// <summary>
/// Represents a syntax highlighting request for a <see cref="PromptEditor"/>.
/// </summary>
public readonly record struct PromptEditorHighlightRequest(
    ITextSnapshot Snapshot,
    Theme Theme,
    int CaretIndex,
    int SelectionStart,
    int SelectionLength);

/// <summary>
/// Delegate used by <see cref="PromptEditor"/> to compute syntax highlighting style runs.
/// </summary>
/// <param name="request">The highlighting request.</param>
/// <param name="runs">A list that receives style runs. Runs use UTF-16 indices relative to the snapshot text.</param>
public delegate void PromptEditorHighlighter(in PromptEditorHighlightRequest request, List<StyledRun> runs);

/// <summary>
/// Provides an in-memory history store for prompt inputs.
/// </summary>
public sealed class PromptEditorHistory
{
    private readonly List<string> _entries = new(64);

    /// <summary>
    /// Gets the history entries (oldest first).
    /// </summary>
    public IReadOnlyList<string> Entries => _entries;

    /// <summary>
    /// Gets or sets the maximum number of entries retained.
    /// </summary>
    public int MaxEntries { get; set; } = 200;

    /// <summary>
    /// Adds an entry to history.
    /// </summary>
    /// <param name="text">The text to store.</param>
    public void Add(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _entries.Add(text);
        Trim();
    }

    /// <summary>
    /// Clears the history.
    /// </summary>
    public void Clear() => _entries.Clear();

    private void Trim()
    {
        var max = Math.Max(0, MaxEntries);
        while (_entries.Count > max)
        {
            _entries.RemoveAt(0);
        }
    }
}

/// <summary>
/// Represents a prompt-style text editor with prompt prefixes, completion hooks, and history.
/// </summary>
public partial class PromptEditor : TextEditorBase
{
    private const int PromptTabSize = 4;

    private readonly MarkupTextParser _markupParser;
    private readonly KeyGesture? _completeGesture;
    private Visual? _promptVisual;

    private string? _cachedPromptMarkup;
    private string _cachedPromptText = string.Empty;
    private StyledRun[] _cachedPromptRuns = Array.Empty<StyledRun>();

    private string? _cachedContinuationPromptMarkup;
    private string _cachedContinuationPromptText = string.Empty;
    private StyledRun[] _cachedContinuationPromptRuns = Array.Empty<StyledRun>();

    private int _promptWidthCells;
    private int _promptContentWidthCells;
    private bool _showPromptSeparator;
    private Rectangle _contentRect;
    private Rectangle _promptRect;
    private Rectangle _promptContentRect;
    private Rectangle _editorRect;

    private int _cachedTextVersion = -1;
    private string _cachedText = string.Empty;

    private int _cachedHighlightVersion = -1;
    private Theme? _cachedHighlightTheme;
    private PromptEditorHighlighter? _cachedHighlighter;
    private bool _cachedWordHintsEnabled;
    private int _cachedHighlightCaretIndex;
    private int _cachedHighlightSelectionStart;
    private int _cachedHighlightSelectionLength;
    private readonly List<StyledRun> _highlightRuns = new(64);
    private readonly List<int> _highlightBoundaryPoints = new(128);
    private readonly List<StyledRun> _normalizedHighlightRuns = new(64);
    private Style _activeSelectionStyle;

    private bool _completionActive;
    private int _completionReplaceStart;
    private int _completionReplaceLength;
    private IReadOnlyList<string>? _completionCandidates;
    private int _completionSelectedIndex;
    private string? _ghostText;
    private int _ghostTextVersion = -1;
    private int _ghostTextCaretIndex = -1;
    private Popup? _completionPopup;

    private int _historyIndex = -1;
    private string? _historyOriginalText;
    private bool _normalizingTextForLineMode;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptEditor"/> class.
    /// </summary>
    public PromptEditor() : this((PromptEditorConfig?)null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptEditor"/> class with command metadata configuration.
    /// </summary>
    /// <param name="config">
    /// The command metadata configuration used for prompt-specific commands. If <see langword="null"/>,
    /// <see cref="PromptEditorConfig.Default"/> is used.
    /// </param>
    public PromptEditor(PromptEditorConfig? config)
    {
        var defaultConfig = PromptEditorConfig.Default;
        var effectiveConfig = config ?? defaultConfig;
        var completeCommandConfig = effectiveConfig.CompleteCommand ?? defaultConfig.CompleteCommand;

        _markupParser = new MarkupTextParser();
        _completeGesture = completeCommandConfig.Gesture;

        Focusable = true;
        this.WordWrap(true);
        this.AcceptTab(false);
        this.HorizontalAlignment(Align.Stretch);
        this.VerticalAlignment(Align.Stretch);

        this.PromptMarkup("[primary]>[/] ");
        this.ContinuationPromptMarkup("[muted]·[/] ");
        this.LineMode(PromptEditorLineMode.MultiLine);
        this.EnterMode(PromptEditorEnterMode.EnterAccepts);
        this.CompletionPresentation(PromptEditorCompletionPresentation.PopupList);
        this.EnableGhostCompletion(true);
        this.EnableWordHints(false);
        this.History(new PromptEditorHistory());

        TextDocument = new DynamicTextDocument(
            getter: () => Text ?? string.Empty,
            setter: value => Text = value);

        AddCommand(CreateAcceptCommand(effectiveConfig.AcceptCommand ?? defaultConfig.AcceptCommand));
        AddCommand(CreateCancelCommand(effectiveConfig.CancelCommand ?? defaultConfig.CancelCommand));
        AddCommand(CreateInsertNewLineCommand(effectiveConfig.InsertNewLineCommand ?? defaultConfig.InsertNewLineCommand));
        AddCommand(CreateCompleteCommand(completeCommandConfig));
        AddCommand(CreateHistoryPreviousCommand(effectiveConfig.HistoryPreviousCommand ?? defaultConfig.HistoryPreviousCommand));
        AddCommand(CreateHistoryNextCommand(effectiveConfig.HistoryNextCommand ?? defaultConfig.HistoryNextCommand));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptEditor"/> class with initial text.
    /// </summary>
    /// <param name="text">The initial text.</param>
    public PromptEditor(string? text) : this()
    {
        this.Text(text);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptEditor"/> class with dynamic text.
    /// </summary>
    /// <param name="text">A delegate that supplies the text content.</param>
    public PromptEditor(Func<string?> text) : this()
    {
        this.Text(text);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptEditor"/> class with bound text.
    /// </summary>
    /// <param name="text">A binding that supplies the text content.</param>
    public PromptEditor(Binding<string?> text) : this()
    {
        this.BindText(text);
    }

    private static Command CreateAcceptCommand(PromptEditorCommandConfig config)
    {
        return new Command
        {
            Id = "PromptEditor.Accept",
            LabelMarkup = config.LabelMarkup,
            DescriptionMarkup = config.DescriptionMarkup,
            Gesture = config.Gesture,
            Importance = CommandImportance.Primary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v =>
            {
                var editor = (PromptEditor)v;
                if (editor.EnterMode == PromptEditorEnterMode.EnterAccepts)
                {
                    editor.Accept();
                }
                else
                {
                    editor.InsertNewLine();
                }
            },
        };
    }

    private static Command CreateCancelCommand(PromptEditorCommandConfig config)
    {
        return new Command
        {
            Id = "PromptEditor.Cancel",
            LabelMarkup = config.LabelMarkup,
            DescriptionMarkup = config.DescriptionMarkup,
            Gesture = config.Gesture,
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            IsVisible = static v => ((PromptEditor)v).IsCancelCommandVisible,
            CanExecute = static v => ((PromptEditor)v).CanExecuteCancelCommand,
            ConsumesGestureWhenUnavailable = false,
            Execute = static v => ((PromptEditor)v).Cancel(),
        };
    }

    private static Command CreateInsertNewLineCommand(PromptEditorCommandConfig config)
    {
        return new Command
        {
            Id = "PromptEditor.InsertNewLine",
            LabelMarkup = config.LabelMarkup,
            DescriptionMarkup = config.DescriptionMarkup,
            Gesture = config.Gesture,
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            IsVisible = static v => ((PromptEditor)v).IsInsertNewLineCommandVisible,
            CanExecute = static v => ((PromptEditor)v).CanExecuteInsertNewLineCommand,
            ConsumesGestureWhenUnavailable = false,
            Execute = static v =>
            {
                var editor = (PromptEditor)v;
                if (editor.EnterMode == PromptEditorEnterMode.EnterAccepts)
                {
                    editor.InsertNewLine();
                }
                else
                {
                    editor.Accept();
                }
            },
        };
    }

    private static Command CreateCompleteCommand(PromptEditorCommandConfig config)
    {
        return new Command
        {
            Id = "PromptEditor.Complete",
            LabelMarkup = config.LabelMarkup,
            DescriptionMarkup = config.DescriptionMarkup,
            Gesture = config.Gesture,
            Importance = CommandImportance.Primary,
            Presentation = CommandPresentation.CommandBar,
            IsVisible = static v => ((PromptEditor)v).CompletionHandler.Invoke is not null,
            ConsumesGestureWhenUnavailable = false,
            RouteGesture = false,
            Execute = static v => ((PromptEditor)v).RequestCompletion(TerminalModifiers.None),
        };
    }

    private static Command CreateHistoryPreviousCommand(PromptEditorCommandConfig config)
    {
        return new Command
        {
            Id = "PromptEditor.HistoryPrevious",
            LabelMarkup = config.LabelMarkup,
            DescriptionMarkup = config.DescriptionMarkup,
            Gesture = config.Gesture,
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            IsVisible = static v => ((PromptEditor)v).History is { Entries.Count: > 0 },
            Execute = static v => ((PromptEditor)v).HistoryPrevious(),
            CanExecute = static v => ((PromptEditor)v).CanNavigateHistory,
        };
    }

    private static Command CreateHistoryNextCommand(PromptEditorCommandConfig config)
    {
        return new Command
        {
            Id = "PromptEditor.HistoryNext",
            LabelMarkup = config.LabelMarkup,
            DescriptionMarkup = config.DescriptionMarkup,
            Gesture = config.Gesture,
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            IsVisible = static v => ((PromptEditor)v).History is { Entries.Count: > 0 },
            Execute = static v => ((PromptEditor)v).HistoryNext(),
            CanExecute = static v => ((PromptEditor)v).CanNavigateHistory,
        };
    }

    /// <summary>
    /// Gets or sets the editor text content.
    /// </summary>
    [Bindable]
    public partial string? Text { get; set; }

    /// <summary>
    /// Gets or sets an optional prompt visual rendered in the left prompt column on the first visual row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set, the prompt visual takes precedence over <see cref="PromptMarkup"/> for the first row. Continuation
    /// rows still use <see cref="ContinuationPromptMarkup"/> (or indentation if empty).
    /// </para>
    /// <para>
    /// This allows composing rich prompts (e.g. a spinner, icons, or additional visuals) while preserving the prompt
    /// column layout.
    /// </para>
    /// </remarks>
    [Bindable(NoVisualAttach = true)]
    public partial Visual? Prompt { get; set; }

    /// <summary>
    /// Gets or sets the prompt prefix markup displayed on the first visual line.
    /// </summary>
    [Bindable]
    public partial string? PromptMarkup { get; set; }

    /// <summary>
    /// Gets or sets the prompt prefix markup displayed on continuation visual lines.
    /// </summary>
    [Bindable]
    public partial string? ContinuationPromptMarkup { get; set; }

    /// <summary>
    /// Gets or sets whether the editor accepts a single line or multiple lines.
    /// </summary>
    [Bindable]
    public partial PromptEditorLineMode LineMode { get; set; }

    /// <summary>
    /// Gets or sets how Enter and Ctrl+J are interpreted.
    /// </summary>
    [Bindable]
    public partial PromptEditorEnterMode EnterMode { get; set; }

    /// <summary>
    /// Gets or sets how <see cref="TerminalKey.Escape"/> is interpreted.
    /// </summary>
    [Bindable]
    public partial PromptEditorEscapeBehavior EscapeBehavior { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether ghost completion is rendered when available.
    /// </summary>
    [Bindable]
    public partial bool EnableGhostCompletion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether word hints (underline) are enabled.
    /// </summary>
    [Bindable]
    public partial bool EnableWordHints { get; set; }

    /// <summary>
    /// Gets or sets the completion UI mode.
    /// </summary>
    [Bindable]
    public partial PromptEditorCompletionPresentation CompletionPresentation { get; set; }

    /// <summary>
    /// Gets or sets the completion handler used to compute candidates.
    /// </summary>
    [Bindable]
    public partial Delegator<PromptEditorCompletionHandler> CompletionHandler { get; set; }

    /// <summary>
    /// Gets or sets the optional syntax highlighter used to style the editor content.
    /// </summary>
    [Bindable]
    public partial Delegator<PromptEditorHighlighter> Highlighter { get; set; }

    /// <summary>
    /// Gets or sets the history store used by this editor.
    /// </summary>
    [Bindable]
    public partial PromptEditorHistory? History { get; set; }

    /// <inheritdoc />
    protected override int ChildrenCount => _promptVisual is null ? 0 : 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index)
        => index == 0 && _promptVisual is not null ? _promptVisual : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc />
    protected override void PrepareChildren()
    {
        base.PrepareChildren();

        if (!ReferenceEquals(_promptVisual, Prompt))
        {
            if (_promptVisual is not null)
            {
                DetachChild(_promptVisual);
            }

            _promptVisual = Prompt;
            if (_promptVisual is not null)
            {
                _promptVisual.IsHitTestVisible = false;
                AttachChild(_promptVisual);
            }
        }
    }

    /// <summary>
    /// Called when the prompt is accepted.
    /// </summary>
    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnAccepted(PromptEditorAcceptedEventArgs e) { }

    /// <summary>
    /// Called when the prompt is canceled.
    /// </summary>
    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnCanceled(PromptEditorCanceledEventArgs e) { }

    /// <inheritdoc />
    protected override bool IsSingleLine => LineMode == PromptEditorLineMode.SingleLine;

    /// <inheritdoc />
    protected override bool AcceptsReturn => false;

    /// <inheritdoc />
    protected override bool ShowPlaceholderWhenUnfocusedOnly => false;

    private bool HasActiveCompletion => _completionActive || _completionPopup is not null;

    private bool CanExecuteCancelCommand => EscapeBehavior == PromptEditorEscapeBehavior.CancelPromptOrCompletion || HasActiveCompletion;

    private bool IsCancelCommandVisible => EscapeBehavior == PromptEditorEscapeBehavior.CancelPromptOrCompletion || HasActiveCompletion;

    private bool CanExecuteInsertNewLineCommand => LineMode == PromptEditorLineMode.MultiLine;

    private bool IsInsertNewLineCommandVisible => LineMode == PromptEditorLineMode.MultiLine;

    partial void OnLineModeChanged(PromptEditorLineMode value)
    {
        _ = value;
        NormalizeTextForCurrentLineMode();
    }

    partial void OnTextChanged(string? value)
    {
        if (_normalizingTextForLineMode)
        {
            return;
        }

        _ = value;
        NormalizeTextForCurrentLineMode();
    }

    /// <summary>
    /// Accepts the current text and raises <see cref="AcceptedEvent"/>.
    /// </summary>
    public void Accept()
    {
        CancelCompletion();

        var text = GetCachedText();
        History?.Add(text);
        _historyIndex = -1;
        _historyOriginalText = null;

        RaiseEvent(AcceptedEvent, new PromptEditorAcceptedEventArgs(text));
    }

    /// <summary>
    /// Cancels completion if active, otherwise raises <see cref="CanceledEvent"/>.
    /// </summary>
    public void Cancel()
    {
        if (_completionActive || _completionPopup is not null)
        {
            CancelCompletion();
            return;
        }

        RaiseEvent(CanceledEvent, new PromptEditorCanceledEventArgs());
    }

    /// <summary>
    /// Inserts a newline (LF, <c>\n</c>) at the caret.
    /// </summary>
    public void InsertNewLine()
    {
        if (LineMode == PromptEditorLineMode.SingleLine)
        {
            return;
        }

        var args = new TextInputEventArgs { Text = "\n" };
        base.OnTextInput(args);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        var hadActiveCompletion = HasActiveCompletion;
        if (hadActiveCompletion && e.Key != TerminalKey.Tab)
        {
            CancelCompletion();
        }

        if (TryHandleAcceptOrNewLine(e))
        {
            return;
        }

        if (TryHandleCompletionGesture(e))
        {
            return;
        }

        if (e.Key == TerminalKey.Escape)
        {
            if (hadActiveCompletion)
            {
                e.Handled = true;
                return;
            }

            if (EscapeBehavior == PromptEditorEscapeBehavior.CancelPromptOrCompletion)
            {
                Cancel();
                e.Handled = true;
                return;
            }
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnTextInput(TextInputEventArgs e)
    {
        var normalizedText = NormalizeTextForCurrentLineMode(e.Text);
        if (string.IsNullOrEmpty(normalizedText))
        {
            e.Handled = true;
            return;
        }

        if (!string.Equals(normalizedText, e.Text, StringComparison.Ordinal))
        {
            base.OnTextInput(new TextInputEventArgs { Text = normalizedText });
            e.Handled = true;
            return;
        }

        base.OnTextInput(e);
    }

    /// <inheritdoc />
    protected override void OnPaste(PasteEventArgs e)
    {
        var normalizedText = NormalizeTextForCurrentLineMode(e.Text);
        if (string.IsNullOrEmpty(normalizedText))
        {
            e.Handled = true;
            return;
        }

        if (!string.Equals(normalizedText, e.Text, StringComparison.Ordinal))
        {
            base.OnPaste(new PasteEventArgs { Text = normalizedText });
            e.Handled = true;
            return;
        }

        base.OnPaste(e);
    }

    private bool TryHandleAcceptOrNewLine(KeyEventArgs e)
    {
        // We interpret Enter/Ctrl+J outside TextEditorCore so prompt-like accept behavior does not require
        // setting AcceptsReturn=true.
        var isEnter = e.Key == TerminalKey.Enter || e.Char == TerminalChar.CtrlM;
        // Some terminal backends may report Ctrl+J as a control character without setting the Ctrl modifier.
        var isCtrlJ = e.Char == TerminalChar.CtrlJ;

        if (!isEnter && !isCtrlJ)
        {
            return false;
        }

        var enterMode = EnterMode;
        var accept =
            (enterMode == PromptEditorEnterMode.EnterAccepts && isEnter) ||
            (enterMode == PromptEditorEnterMode.EnterInsertsNewLine && isCtrlJ);

        if (accept)
        {
            Accept();
        }
        else
        {
            InsertNewLine();
        }

        e.Handled = true;
        return true;
    }

    private bool TryHandleCompletionGesture(KeyEventArgs e)
    {
        if (_completeGesture is not { } completeGesture || !completeGesture.Matches(e.RawEvent))
        {
            return false;
        }

        if (AcceptTab && e.Key == TerminalKey.Tab)
        {
            return false;
        }

        if (!RequestCompletion(e.Modifiers))
        {
            return false;
        }

        e.Handled = true;
        return true;
    }

    private bool CanNavigateHistory => History is { Entries.Count: > 0 };

    private void NormalizeTextForCurrentLineMode()
    {
        var normalizedText = NormalizeTextForCurrentLineMode(Text);
        if (string.Equals(normalizedText, Text, StringComparison.Ordinal))
        {
            return;
        }

        _normalizingTextForLineMode = true;
        try
        {
            Text = normalizedText;
        }
        finally
        {
            _normalizingTextForLineMode = false;
        }
    }

    private string? NormalizeTextForCurrentLineMode(string? text)
        => LineMode == PromptEditorLineMode.SingleLine ? RemoveLineBreaks(text) : text;

    private static string? RemoveLineBreaks(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (text.IndexOfAny(['\r', '\n']) < 0)
        {
            return text;
        }

        var builder = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (ch is not ('\r' or '\n'))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    private void HistoryPrevious()
    {
        var history = History;
        if (history is null || history.Entries.Count == 0)
        {
            return;
        }

        if (_historyIndex < 0)
        {
            _historyOriginalText = GetCachedText();
            _historyIndex = history.Entries.Count;
        }

        _historyIndex = Math.Max(0, _historyIndex - 1);
        SetTextFromHistory(history.Entries[_historyIndex]);
    }

    private void HistoryNext()
    {
        var history = History;
        if (history is null || history.Entries.Count == 0 || _historyIndex < 0)
        {
            return;
        }

        _historyIndex = Math.Min(history.Entries.Count, _historyIndex + 1);
        if (_historyIndex == history.Entries.Count)
        {
            SetTextFromHistory(_historyOriginalText ?? string.Empty);
            _historyIndex = -1;
            _historyOriginalText = null;
        }
        else
        {
            SetTextFromHistory(history.Entries[_historyIndex]);
        }
    }

    private void SetTextFromHistory(string text)
    {
        var current = GetCachedText();
        ReplaceTextWithUndo(0, current.Length, text, TextUndoRedoManager.TextUndoKind.Replace);
        CaretIndex = text.Length;
    }

    private bool RequestCompletion(TerminalModifiers modifiers)
    {
        var handler = CompletionHandler.Invoke;
        if (handler is null)
        {
            return false;
        }

        var snapshot = TextDocument.CurrentSnapshot;
        var request = new PromptEditorCompletionRequest(
            Snapshot: snapshot,
            CaretIndex: CaretIndex,
            SelectionStart: CaretIndex,
            SelectionLength: 0,
            Modifiers: modifiers);

        var result = handler(in request);
        if (!result.Handled || result.Candidates is not { Count: > 0 })
        {
            CancelCompletion();
            SetGhostText(result.Handled ? result.GhostText : null);
            return result.Handled;
        }

        _completionCandidates = result.Candidates;
        _completionReplaceStart = Math.Clamp(result.ReplaceStart, 0, GetCachedText().Length);
        _completionReplaceLength = Math.Max(0, result.ReplaceLength);
        SetGhostText(result.GhostText);

        var candidatesCount = result.Candidates.Count;
        var initialIndex = Math.Clamp(result.SelectedIndex, 0, candidatesCount - 1);

        if (CompletionPresentation == PromptEditorCompletionPresentation.InlineCycle)
        {
            var nextIndex = _completionActive ? (_completionSelectedIndex + 1) % candidatesCount : initialIndex;
            _completionSelectedIndex = nextIndex;
            ApplyCompletionCandidate(result.Candidates[nextIndex]);
            _completionActive = true;
            return true;
        }

        if (CompletionPresentation == PromptEditorCompletionPresentation.PopupList)
        {
            _completionSelectedIndex = initialIndex;
            if (candidatesCount == 1)
            {
                ApplyCompletionCandidate(result.Candidates[initialIndex]);
                CancelCompletion();
                return true;
            }

            OpenCompletionPopup(result.Candidates, initialIndex);
            _completionActive = true;
            return true;
        }

        _completionSelectedIndex = initialIndex;
        _completionActive = true;
        return true;
    }

    private void ApplyCompletionCandidate(string candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var text = GetCachedText();
        var start = Math.Clamp(_completionReplaceStart, 0, text.Length);
        var length = Math.Clamp(_completionReplaceLength, 0, text.Length - start);

        ReplaceTextWithUndo(start, length, candidate, TextUndoRedoManager.TextUndoKind.Replace);

        _completionReplaceStart = start;
        _completionReplaceLength = candidate.Length;
        CaretIndex = start + candidate.Length;
    }

    private void OpenCompletionPopup(IReadOnlyList<string> candidates, int selectedIndex)
    {
        VerifyAccess();

        if (_completionPopup is not null)
        {
            _completionPopup.Close();
            _completionPopup = null;
        }

        var list = new OptionList<string>()
            .ActivateOnClick(true);

        foreach (var c in candidates)
        {
            list.Items.Add(c);
        }

        list.ItemTemplate = new DataTemplate<string>(
            Display: static (DataTemplateValue<string> value, in DataTemplateContext _) => new TextBlock(() => value.GetValue()),
            Editor: null);

        list.SelectedIndex = Math.Clamp(selectedIndex, 0, Math.Max(0, list.Items.Count - 1));

        list.PointerPressed((s, e) =>
        {
            if (e.Button != TerminalMouseButton.Left)
            {
                return;
            }

            if (!TryFindCompletionPopupParent(s as Visual, out var lb, out var owner))
            {
                return;
            }

            var selection = lb.SelectedIndex;
            if (selection >= 0 && selection < candidates.Count)
            {
                owner._completionSelectedIndex = selection;
                owner.ApplyCompletionCandidate(candidates[selection]);
            }

            owner.CancelCompletion();
        });

        list.KeyDown((s, e) =>
        {
            if (e.Key is not (TerminalKey.Enter or TerminalKey.Space))
            {
                return;
            }

            if (!TryFindCompletionPopupParent(s as Visual, out var lb, out var owner))
            {
                return;
            }

            var selection = lb.SelectedIndex;
            if (selection >= 0 && selection < candidates.Count)
            {
                owner._completionSelectedIndex = selection;
                owner.ApplyCompletionCandidate(candidates[selection]);
            }

            owner.CancelCompletion();
            e.Handled = true;
        });

        Rectangle? anchorRect = null;
        if (TryGetCursorCell(out var caretX, out var caretY))
        {
            anchorRect = new Rectangle(caretX, caretY, 1, 1);
        }

        var popup = new Popup
        {
            Anchor = this,
            AnchorRect = anchorRect,
            Content = list,
            MatchAnchorWidth = false,
            Placement = PopupPlacement.Below,
            CloseOnTab = true,
        };

        popup.Closed(static (sender, _) =>
        {
            if (sender is not Popup p || p.Anchor is not PromptEditor owner)
            {
                return;
            }

            owner._completionPopup = null;
            owner._completionActive = false;
        });

        popup.Show();
        _completionPopup = popup;
    }

    private static bool TryFindCompletionPopupParent(Visual? visual, [MaybeNullWhen(false)] out OptionList<string> list, [MaybeNullWhen(false)] out PromptEditor owner)
    {
        list = null;
        owner = null;

        if (visual is not OptionList<string> listInstance)
        {
            return false;
        }

        list = listInstance;
        var parent = list.Parent;
        while (parent is not null)
        {
            if (parent is Popup popup && popup.Anchor is PromptEditor editor)
            {
                owner = editor;
                return true;
            }
            parent = parent.Parent;
        }

        return false;
    }

    private void CancelCompletion()
    {
        _completionActive = false;
        _completionCandidates = null;
        _completionSelectedIndex = 0;
        _completionReplaceStart = 0;
        _completionReplaceLength = 0;
        SetGhostText(null);

        if (_completionPopup is not null)
        {
            _completionPopup.Close();
            _completionPopup = null;
        }
    }

    private void ReplaceTextWithUndo(int position, int length, string insertedText, TextUndoRedoManager.TextUndoKind kind)
    {
        var document = TextDocument;
        var beforeText = GetCachedText();

        position = Math.Clamp(position, 0, beforeText.Length);
        length = Math.Clamp(length, 0, beforeText.Length - position);
        insertedText ??= string.Empty;

        var removed = length == 0 ? string.Empty : beforeText.Substring(position, length);

        var before = new TextUndoRedoManager.TextEditorStateSnapshot(
            CaretIndex: CaretIndex,
            SelectionAnchor: -1,
            SelectionEnd: -1,
            ScrollX: Scroll.OffsetX,
            ScrollY: Scroll.OffsetY,
            PreferredColumn: -1);

        using var _ = UndoManager.BeginRecording();
        using (document.BeginUpdate())
        {
            document.Replace(position, length, insertedText.AsSpan());
        }

        var after = before with
        {
            CaretIndex = position + insertedText.Length,
            ScrollX = Scroll.OffsetX,
            ScrollY = Scroll.OffsetY,
        };

        UndoManager.RecordSingle(
            kind,
            new TextUndoRedoManager.TextChange(position, removed, insertedText),
            before,
            after,
            allowCoalesce: false);
    }

    private string GetCachedText()
    {
        var version = TextDocument.Version;
        if (version != _cachedTextVersion)
        {
            _cachedText = TextDocumentUtility.GetText(TextDocument);
            _cachedTextVersion = version;
        }

        return _cachedText;
    }

    private void EnsurePromptCache(Theme theme)
    {
        var styles = theme.GetMarkupStyles();

        var promptMarkup = PromptMarkup;
        if (!string.Equals(_cachedPromptMarkup, promptMarkup, StringComparison.Ordinal))
        {
            _cachedPromptMarkup = promptMarkup;
            _cachedPromptText = _markupParser.Parse(promptMarkup, out _cachedPromptRuns, styles);
        }

        var contMarkup = ContinuationPromptMarkup;
        if (!string.Equals(_cachedContinuationPromptMarkup, contMarkup, StringComparison.Ordinal))
        {
            _cachedContinuationPromptMarkup = contMarkup;
            _cachedContinuationPromptText = _markupParser.Parse(contMarkup, out _cachedContinuationPromptRuns, styles);
        }

        var promptWidth = TerminalTextUtility.GetWidth(_cachedPromptText.AsSpan());
        var contWidth = TerminalTextUtility.GetWidth(_cachedContinuationPromptText.AsSpan());
        _promptWidthCells = Math.Max(promptWidth, contWidth);
    }

    private int MeasurePromptColumnWidth(Theme theme, PromptEditorStyle style, int availableContentWidth)
    {
        _ = theme;
        var separatorWidth = style.ShowPromptSeparator ? 1 : 0;
        var maxPromptWidth = Math.Max(0, availableContentWidth - separatorWidth);
        var promptContentWidthCells = Math.Clamp(_promptWidthCells, 0, maxPromptWidth);

        if (_promptVisual is not null && maxPromptWidth > 0)
        {
            _promptVisual.Measure(new LayoutConstraints(0, maxPromptWidth, 0, 1));
            promptContentWidthCells = Math.Max(promptContentWidthCells, _promptVisual.DesiredSize.Width);
        }

        return Math.Clamp(promptContentWidthCells + separatorWidth, 0, availableContentWidth);
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var theme = GetTheme();
        var style = GetStyle<PromptEditorStyle>();
        var width = Math.Max(0, Math.Min(constraints.MaxWidth, 48));
        var height = IsSingleLine ? 1 : 5;

        if (AutoSizeHeight)
        {
            EnsurePromptCache(theme);

            var contentWidth = Math.Max(0, width - style.Padding.Horizontal);
            var promptWidth = MeasurePromptColumnWidth(theme, style, contentWidth);
            var editorWidth = Math.Max(0, contentWidth - promptWidth);
            height = Math.Max(1, style.Padding.Vertical + MeasureContentRowsForWidth(editorWidth));
        }

        var size = new Size(width, height);
        return SizeHints.Fixed(constraints.Clamp(size));
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var theme = GetTheme();
        var style = GetStyle<PromptEditorStyle>();
        var padding = style.Padding;

        _contentRect = new Rectangle(
            finalRect.X + padding.Left,
            finalRect.Y + padding.Top,
            Math.Max(0, finalRect.Width - padding.Horizontal),
            Math.Max(0, finalRect.Height - padding.Vertical));

        EnsurePromptCache(theme);

        _showPromptSeparator = style.ShowPromptSeparator;
        var separatorWidth = _showPromptSeparator ? 1 : 0;
        var promptWidth = MeasurePromptColumnWidth(theme, style, _contentRect.Width);
        _promptContentWidthCells = Math.Max(0, promptWidth - separatorWidth);
        _promptRect = new Rectangle(_contentRect.X, _contentRect.Y, promptWidth, _contentRect.Height);
        _promptContentRect = new Rectangle(_promptRect.X, _promptRect.Y, Math.Max(0, _promptRect.Width - separatorWidth), _promptRect.Height);
        _editorRect = new Rectangle(_contentRect.X + promptWidth, _contentRect.Y, Math.Max(0, _contentRect.Width - promptWidth), _contentRect.Height);

        UpdateEditorLayout(_editorRect);

        if (_promptVisual is not null && _promptContentRect.Width > 0)
        {
            _promptVisual.Arrange(new Rectangle(_promptContentRect.X, _promptContentRect.Y, _promptContentRect.Width, 1));
        }
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var isFocused = HasFocus;
        var style = GetStyle<PromptEditorStyle>();

        EnsurePromptCache(theme);
        EnsureHighlightRuns(theme);

        var backgroundStyle = style.BackgroundStyle(theme, isFocused);
        var promptSidebarBackgroundStyle = style.PromptSidebarBackgroundStyle(theme, isFocused);
        var selectionStyle = style.SelectionStyle(theme);
        _activeSelectionStyle = selectionStyle;
        var placeholderStyle = style.PlaceholderStyle(theme, isFocused);

        if (_promptRect.Width > 0 && _promptRect.Height > 0)
        {
            for (var y = _promptRect.Y; y < _promptRect.Y + _promptRect.Height; y++)
            {
                for (var x = _promptRect.X; x < _promptRect.X + _promptRect.Width; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), backgroundStyle);
                }
            }

            // Apply a subtle sidebar tint on top of the editor background.
            if (promptSidebarBackgroundStyle != Style.None)
            {
                for (var y = _promptRect.Y; y < _promptRect.Y + _promptRect.Height; y++)
                {
                    for (var x = _promptRect.X; x < _promptRect.X + _promptRect.Width; x++)
                    {
                        buffer.SetCell(x, y, new Rune(' '), promptSidebarBackgroundStyle);
                    }
                }
            }
        }

        if (_editorRect.Width > 0 && _editorRect.Height > 0)
        {
            for (var y = _editorRect.Y; y < _editorRect.Y + _editorRect.Height; y++)
            {
                for (var x = _editorRect.X; x < _editorRect.X + _editorRect.Width; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), backgroundStyle);
                }
            }
        }

        if (_promptContentRect.Width > 0 && _promptContentRect.Height > 0)
        {
            buffer.PushClip(_promptContentRect);
            try
            {
                RenderPrompt(buffer, theme, style, isFocused);
            }
            finally
            {
                buffer.PopClip();
            }
        }

        if (_showPromptSeparator && _promptRect.Width > 0 && _promptRect.Height > 0 && _promptContentRect.Width < _promptRect.Width)
        {
            var separatorX = _promptContentRect.X + _promptContentRect.Width;
            if (separatorX >= _promptRect.X && separatorX < _promptRect.Right)
            {
                var separatorStyle = style.PromptSeparatorStyle(theme, isFocused);
                var separatorGlyph = theme.Lines.Vertical;

                for (var y = _promptRect.Y; y < _promptRect.Bottom; y++)
                {
                    buffer.SetCell(separatorX, y, separatorGlyph, separatorStyle);
                }
            }
        }

        if (_editorRect.Width > 0 && _editorRect.Height > 0)
        {
            buffer.PushClip(_editorRect);
            try
            {
                RenderEditor(buffer, _editorRect, backgroundStyle, selectionStyle, placeholderStyle);
                RenderGhostCompletion(buffer, theme, style, isFocused);
            }
            finally
            {
                buffer.PopClip();
            }
        }
    }

    private void RenderPrompt(CellBuffer buffer, Theme theme, PromptEditorStyle style, bool focused)
    {
        var promptBaseStyle = style.PromptStyle(theme, focused);
        var continuationBaseStyle = style.ContinuationPromptStyle(theme, focused);

        for (var row = 0; row < _promptContentRect.Height; row++)
        {
            var visualRow = Scroll.OffsetY + row;
            var y = _promptContentRect.Y + row;

            if (visualRow == 0)
            {
                if (_promptVisual is not null)
                {
                    continue;
                }

                WriteMarkup(buffer, _promptContentRect.X, y, _cachedPromptText, _cachedPromptRuns, promptBaseStyle, _promptContentRect.Width);
            }
            else if (!string.IsNullOrEmpty(_cachedContinuationPromptText))
            {
                WriteMarkup(buffer, _promptContentRect.X, y, _cachedContinuationPromptText, _cachedContinuationPromptRuns, continuationBaseStyle, _promptContentRect.Width);
            }
        }
    }

    private void RenderGhostCompletion(CellBuffer buffer, Theme theme, PromptEditorStyle style, bool focused)
    {
        if (!EnableGhostCompletion || !focused)
        {
            return;
        }

        var ghostText = _ghostText;
        if (string.IsNullOrEmpty(ghostText))
        {
            return;
        }

        var text = GetCachedText();
        if (CaretIndex != text.Length)
        {
            return;
        }

        // Ensure ghost text doesn't "stick" after edits or caret movement.
        if (_ghostTextVersion != TextDocument.Version || _ghostTextCaretIndex != CaretIndex)
        {
            return;
        }

        if (!TryGetCursorCell(out var caretX, out var caretY))
        {
            return;
        }

        var ghostStyle = style.GhostStyle(theme, focused);
        buffer.WriteText(caretX, caretY, ghostText.AsSpan(), ghostStyle);
    }

    private void SetGhostText(string? ghostText)
    {
        _ghostText = ghostText;
        if (string.IsNullOrEmpty(ghostText))
        {
            _ghostTextVersion = -1;
            _ghostTextCaretIndex = -1;
            return;
        }

        _ghostTextVersion = TextDocument.Version;
        _ghostTextCaretIndex = CaretIndex;
    }

    private void EnsureHighlightRuns(Theme theme)
    {
        var snapshot = TextDocument.CurrentSnapshot;
        var version = snapshot.Version;
        var highlighter = (PromptEditorHighlighter?)Highlighter;
        var wordHintsEnabled = EnableWordHints && HasFocus;
        var caretIndex = CaretIndex;
        var selectionStart = SelectionStart;
        var selectionLength = SelectionLength;

        if (_cachedHighlightVersion == version &&
            ReferenceEquals(_cachedHighlightTheme, theme) &&
            Equals(_cachedHighlighter, highlighter) &&
            _cachedWordHintsEnabled == wordHintsEnabled &&
            _cachedHighlightCaretIndex == caretIndex &&
            _cachedHighlightSelectionStart == selectionStart &&
            _cachedHighlightSelectionLength == selectionLength)
        {
            return;
        }

        _cachedHighlightVersion = version;
        _cachedHighlightTheme = theme;
        _cachedHighlighter = highlighter;
        _cachedWordHintsEnabled = wordHintsEnabled;
        _cachedHighlightCaretIndex = caretIndex;
        _cachedHighlightSelectionStart = selectionStart;
        _cachedHighlightSelectionLength = selectionLength;

        _highlightRuns.Clear();

        if (highlighter is not null)
        {
            highlighter(new PromptEditorHighlightRequest(snapshot, theme, caretIndex, selectionStart, selectionLength), _highlightRuns);
        }

        if (wordHintsEnabled)
        {
            var text = GetCachedText().AsSpan();
            if (!text.IsEmpty)
            {
                var index = Math.Clamp(caretIndex, 0, text.Length);
                var start = TerminalTextUtility.GetWordStart(text, index);
                var end = TerminalTextUtility.GetWordEnd(text, index);
                if (end > start)
                {
                    _highlightRuns.Add(new StyledRun(start, end - start, GetStyle<PromptEditorStyle>().WordHintStyle(theme)));
                }
            }
        }

        NormalizeHighlightRuns(textLength: GetCachedText().Length);
    }

    private void NormalizeHighlightRuns(int textLength)
    {
        if (_highlightRuns.Count == 0 || textLength <= 0)
        {
            return;
        }

        // Normalize potentially overlapping runs into non-overlapping segments with combined styles.
        // This allows, for example, "keyword color" + "current word underline" to apply simultaneously.
        var boundaries = _highlightBoundaryPoints;
        boundaries.Clear();
        boundaries.EnsureCapacity(_highlightRuns.Count * 2 + 2);
        boundaries.Add(0);
        boundaries.Add(textLength);

        for (var i = 0; i < _highlightRuns.Count; i++)
        {
            var run = _highlightRuns[i];
            if (run.Length <= 0)
            {
                continue;
            }

            var start = Math.Clamp(run.Start, 0, textLength);
            var end = Math.Clamp(run.Start + run.Length, 0, textLength);
            if (end <= start)
            {
                continue;
            }

            boundaries.Add(start);
            boundaries.Add(end);
        }

        boundaries.Sort();
        for (var i = boundaries.Count - 2; i >= 0; i--)
        {
            if (boundaries[i] == boundaries[i + 1])
            {
                boundaries.RemoveAt(i + 1);
            }
        }

        var normalized = _normalizedHighlightRuns;
        normalized.Clear();
        normalized.EnsureCapacity(boundaries.Count);

        for (var i = 0; i + 1 < boundaries.Count; i++)
        {
            var start = boundaries[i];
            var end = boundaries[i + 1];
            if (end <= start)
            {
                continue;
            }

            var style = Style.None;
            for (var j = 0; j < _highlightRuns.Count; j++)
            {
                var run = _highlightRuns[j];
                if (run.Length <= 0)
                {
                    continue;
                }

                var runStart = run.Start;
                var runEnd = run.Start + run.Length;

                if (runStart <= start && runEnd >= end)
                {
                    style |= run.Style;
                }
            }

            if (style == Style.None)
            {
                continue;
            }

            if (normalized.Count > 0)
            {
                var prev = normalized[^1];
                if (prev.Start + prev.Length == start && prev.Style == style)
                {
                    normalized[^1] = new StyledRun(prev.Start, prev.Length + (end - start), style);
                    continue;
                }
            }

            normalized.Add(new StyledRun(start, end - start, style));
        }

        _highlightRuns.Clear();
        _highlightRuns.AddRange(normalized);
    }

    private void WriteMarkup(CellBuffer buffer, int x, int y, string plainText, StyledRun[] runs, Style baseStyle, int maxWidthCells)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return;
        }

        var col = 0;
        if (runs.Length == 0)
        {
            buffer.WriteText(x, y, plainText.AsSpan(), baseStyle);
            return;
        }

        foreach (var run in runs)
        {
            if (run.Length <= 0)
            {
                continue;
            }

            var slice = plainText.AsSpan(run.Start, run.Length);
            buffer.WriteText(x + col, y, slice, baseStyle | run.Style);
            col += GetTextCells(slice, col, PromptTabSize);
            if (col >= maxWidthCells)
            {
                break;
            }
        }
    }

    /// <inheritdoc />
    protected override void WriteTextSegment(CellBuffer buffer, int x, int y, ReadOnlySpan<char> text, Style style, bool isPlaceholder, int textIndexStart, int startColumn)
    {
        if (isPlaceholder || _highlightRuns.Count == 0 || textIndexStart < 0)
        {
            base.WriteTextSegment(buffer, x, y, text, style, isPlaceholder, textIndexStart, startColumn);
            return;
        }

        var segmentStart = textIndexStart;
        var segmentEnd = segmentStart + text.Length;

        var runIndex = FindFirstRunIndex(segmentStart);
        var localIndex = 0;
        var col = startColumn;
        var cellX = x;

        while (localIndex < text.Length)
        {
            if (runIndex >= _highlightRuns.Count)
            {
                var rest = text.Slice(localIndex);
                base.WriteTextSegment(buffer, cellX, y, rest, style, isPlaceholder, segmentStart + localIndex, col);
                return;
            }

            var run = _highlightRuns[runIndex];
            var runStart = run.Start;
            var runEnd = run.Start + run.Length;

            if (runEnd <= segmentStart + localIndex)
            {
                runIndex++;
                continue;
            }

            if (runStart > segmentStart + localIndex)
            {
                var len = Math.Min(text.Length - localIndex, runStart - (segmentStart + localIndex));
                var slice = text.Slice(localIndex, len);
                base.WriteTextSegment(buffer, cellX, y, slice, style, isPlaceholder, segmentStart + localIndex, col);
                var width = GetTextCells(slice, col, TabSize);
                col += width;
                cellX += width;
                localIndex += len;
                continue;
            }

            var overlapEnd = Math.Min(segmentEnd, runEnd);
            var len2 = overlapEnd - (segmentStart + localIndex);
            if (len2 <= 0)
            {
                runIndex++;
                continue;
            }

            var slice2 = text.Slice(localIndex, len2);
            base.WriteTextSegment(buffer, cellX, y, slice2, ComposeHighlightStyle(style, run.Style), isPlaceholder, segmentStart + localIndex, col);
            var width2 = GetTextCells(slice2, col, TabSize);
            col += width2;
            cellX += width2;
            localIndex += len2;

            if (runEnd <= overlapEnd)
            {
                runIndex++;
            }
        }
    }

    private int FindFirstRunIndex(int index)
    {
        var lo = 0;
        var hi = _highlightRuns.Count - 1;
        var result = _highlightRuns.Count;
        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) / 2);
            var run = _highlightRuns[mid];
            if (run.Start + run.Length > index)
            {
                result = mid;
                hi = mid - 1;
            }
            else
            {
                lo = mid + 1;
            }
        }

        return result;
    }

    private Style ComposeHighlightStyle(Style baseStyle, Style highlightStyle)
    {
        if (baseStyle != _activeSelectionStyle)
        {
            return baseStyle | highlightStyle;
        }

        if (baseStyle.TryGetBackground(out _))
        {
            highlightStyle = highlightStyle.ClearBackground();
        }

        if (baseStyle.TryGetForeground(out _))
        {
            highlightStyle = highlightStyle.ClearForeground();
        }

        return baseStyle | highlightStyle;
    }

    private static int GetTextCells(ReadOnlySpan<char> text, int startColumn, int tabSize)
    {
        var col = startColumn;
        var i = 0;
        while (i < text.Length)
        {
            var next = TerminalTextUtility.GetNextTextElementIndex(text, i);
            if (next <= i)
            {
                break;
            }

            col += GetTextElementCellWidth(text.Slice(i, next - i), col, tabSize);
            i = next;
        }

        return col - startColumn;
    }

    private static int GetTextElementCellWidth(ReadOnlySpan<char> element, int column, int tabSize)
    {
        if (element.Length == 1 && element[0] == '\t')
        {
            var size = Math.Max(1, tabSize);
            return size - (column % size);
        }

        return Math.Max(1, TerminalTextUtility.GetWidth(element));
    }
}

/// <summary>
/// Provides data for <see cref="PromptEditor.AcceptedEvent"/>.
/// </summary>
public sealed class PromptEditorAcceptedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PromptEditorAcceptedEventArgs"/> class.
    /// </summary>
    /// <param name="text">The accepted text.</param>
    public PromptEditorAcceptedEventArgs(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
    }

    /// <summary>
    /// Gets the accepted text.
    /// </summary>
    public string Text { get; }
}

/// <summary>
/// Provides data for <see cref="PromptEditor.CanceledEvent"/>.
/// </summary>
public sealed class PromptEditorCanceledEventArgs : RoutedEventArgs
{
}
