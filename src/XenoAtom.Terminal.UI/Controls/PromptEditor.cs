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
    Theme Theme);

/// <summary>
/// Provides syntax highlighting runs for a <see cref="PromptEditor"/>.
/// </summary>
public interface IPromptEditorHighlighter
{
    /// <summary>
    /// Populates style runs for the snapshot. Runs use UTF-16 indices relative to the snapshot text.
    /// </summary>
    /// <param name="request">The highlighting request.</param>
    /// <param name="runs">A list that receives styled runs.</param>
    void Highlight(in PromptEditorHighlightRequest request, List<StyledRun> runs);
}

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

    private string? _cachedPromptMarkup;
    private string _cachedPromptText = string.Empty;
    private StyledRun[] _cachedPromptRuns = Array.Empty<StyledRun>();

    private string? _cachedContinuationPromptMarkup;
    private string _cachedContinuationPromptText = string.Empty;
    private StyledRun[] _cachedContinuationPromptRuns = Array.Empty<StyledRun>();

    private int _promptWidthCells;
    private Rectangle _contentRect;
    private Rectangle _promptRect;
    private Rectangle _editorRect;

    private int _cachedTextVersion = -1;
    private string _cachedText = string.Empty;

    private int _cachedHighlightVersion = -1;
    private Theme? _cachedHighlightTheme;
    private IPromptEditorHighlighter? _cachedHighlighter;
    private bool _cachedWordHints;
    private readonly List<StyledRun> _highlightRuns = new(64);

    private bool _completionActive;
    private int _completionReplaceStart;
    private int _completionReplaceLength;
    private IReadOnlyList<string>? _completionCandidates;
    private int _completionSelectedIndex;
    private string? _ghostText;
    private Popup? _completionPopup;

    private int _historyIndex = -1;
    private string? _historyOriginalText;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptEditor"/> class.
    /// </summary>
    public PromptEditor()
    {
        _markupParser = new MarkupTextParser();

        Focusable = true;
        this.WordWrap(true);
        this.AcceptTab(false);
        this.HorizontalAlignment(Align.Stretch);
        this.VerticalAlignment(Align.Stretch);

        this.PromptMarkup("[primary]>[/] ");
        this.ContinuationPromptMarkup("[muted]·[/] ");
        this.EnterMode(PromptEditorEnterMode.EnterAccepts);
        this.CompletionPresentation(PromptEditorCompletionPresentation.PopupList);
        this.EnableGhostCompletion(true);
        this.EnableWordHints(false);
        this.History(new PromptEditorHistory());

        TextDocument = new DynamicTextDocument(
            getter: () => Text ?? string.Empty,
            setter: value => Text = value);

        AddCommand(new Command
        {
            Id = "PromptEditor.Accept",
            LabelMarkup = "Accept",
            DescriptionMarkup = "Accept the current prompt text.",
            Gesture = new KeyGesture(TerminalKey.Enter),
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
        });

        AddCommand(new Command
        {
            Id = "PromptEditor.Cancel",
            LabelMarkup = "Cancel",
            DescriptionMarkup = "Cancel completion or cancel the prompt.",
            Gesture = new KeyGesture(TerminalKey.Escape),
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((PromptEditor)v).Cancel(),
        });

        AddCommand(new Command
        {
            Id = "PromptEditor.InsertNewLine",
            LabelMarkup = "New line",
            DescriptionMarkup = "Insert a newline in the prompt editor (LF).",
            Gesture = new KeyGesture(TerminalChar.CtrlJ, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
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
        });

        AddCommand(new Command
        {
            Id = "PromptEditor.Complete",
            LabelMarkup = "Complete",
            DescriptionMarkup = "Request completion at the caret.",
            Gesture = new KeyGesture(TerminalKey.Tab),
            Importance = CommandImportance.Primary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((PromptEditor)v).RequestCompletion(TerminalModifiers.None),
        });

        AddCommand(new Command
        {
            Id = "PromptEditor.HistoryPrevious",
            LabelMarkup = "History (previous)",
            DescriptionMarkup = "Load the previous history entry.",
            Gesture = new KeyGesture(TerminalKey.Up, TerminalModifiers.Alt),
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((PromptEditor)v).HistoryPrevious(),
            CanExecute = static v => ((PromptEditor)v).CanNavigateHistory,
        });

        AddCommand(new Command
        {
            Id = "PromptEditor.HistoryNext",
            LabelMarkup = "History (next)",
            DescriptionMarkup = "Load the next history entry.",
            Gesture = new KeyGesture(TerminalKey.Down, TerminalModifiers.Alt),
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((PromptEditor)v).HistoryNext(),
            CanExecute = static v => ((PromptEditor)v).CanNavigateHistory,
        });
    }

    /// <summary>
    /// Gets or sets the editor text content.
    /// </summary>
    [Bindable]
    public partial string? Text { get; set; }

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
    /// Gets or sets how Enter and Ctrl+J are interpreted.
    /// </summary>
    [Bindable]
    public partial PromptEditorEnterMode EnterMode { get; set; }

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
    public partial IPromptEditorHighlighter? Highlighter { get; set; }

    /// <summary>
    /// Gets or sets the history store used by this editor.
    /// </summary>
    [Bindable]
    public partial PromptEditorHistory? History { get; set; }

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
    protected override bool IsSingleLine => false;

    /// <inheritdoc />
    protected override bool AcceptsReturn => false;

    /// <inheritdoc />
    protected override bool ShowPlaceholderWhenUnfocusedOnly => false;

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
        var args = new TextInputEventArgs { Text = "\n" };
        base.OnTextInput(args);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_completionActive && e.Key != TerminalKey.Tab)
        {
            CancelCompletion();
        }

        if (TryHandleAcceptOrNewLine(e))
        {
            return;
        }

        if (e.Key == TerminalKey.Tab && !AcceptTab)
        {
            RequestCompletion(e.Modifiers);
            e.Handled = true;
            return;
        }

        if (e.Key == TerminalKey.Escape)
        {
            Cancel();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
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

    private bool CanNavigateHistory => History is { Entries.Count: > 0 };

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

    private void RequestCompletion(TerminalModifiers modifiers)
    {
        var handler = CompletionHandler.Invoke;
        if (handler is null)
        {
            return;
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
            _ghostText = result.Handled ? result.GhostText : null;
            return;
        }

        _completionCandidates = result.Candidates;
        _completionReplaceStart = Math.Clamp(result.ReplaceStart, 0, GetCachedText().Length);
        _completionReplaceLength = Math.Max(0, result.ReplaceLength);
        _ghostText = result.GhostText;

        var candidatesCount = result.Candidates.Count;
        var initialIndex = Math.Clamp(result.SelectedIndex, 0, candidatesCount - 1);

        if (CompletionPresentation == PromptEditorCompletionPresentation.InlineCycle)
        {
            var nextIndex = _completionActive ? (_completionSelectedIndex + 1) % candidatesCount : initialIndex;
            _completionSelectedIndex = nextIndex;
            ApplyCompletionCandidate(result.Candidates[nextIndex]);
            _completionActive = true;
            return;
        }

        if (CompletionPresentation == PromptEditorCompletionPresentation.PopupList)
        {
            _completionSelectedIndex = initialIndex;
            OpenCompletionPopup(result.Candidates, initialIndex);
            _completionActive = true;
            return;
        }

        _completionSelectedIndex = initialIndex;
        _completionActive = true;
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
            owner.App?.Focus(owner);
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
        _ghostText = null;

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

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var size = new Size(48, 5);
        return SizeHints.Fixed(constraints.Clamp(size));
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var theme = GetTheme();
        var style = GetStyle<PromptEditorStyle>();
        var padding = style.Padding;

        EnsurePromptCache(theme);

        _contentRect = new Rectangle(
            finalRect.X + padding.Left,
            finalRect.Y + padding.Top,
            Math.Max(0, finalRect.Width - padding.Horizontal),
            Math.Max(0, finalRect.Height - padding.Vertical));

        var promptWidth = Math.Clamp(_promptWidthCells, 0, _contentRect.Width);
        _promptRect = new Rectangle(_contentRect.X, _contentRect.Y, promptWidth, _contentRect.Height);
        _editorRect = new Rectangle(_contentRect.X + promptWidth, _contentRect.Y, Math.Max(0, _contentRect.Width - promptWidth), _contentRect.Height);

        UpdateEditorLayout(_editorRect);
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
        var selectionStyle = style.SelectionStyle(theme);
        var placeholderStyle = style.PlaceholderStyle(theme, isFocused);

        if (_contentRect.Width > 0 && _contentRect.Height > 0)
        {
            for (var y = _contentRect.Y; y < _contentRect.Y + _contentRect.Height; y++)
            {
                for (var x = _contentRect.X; x < _contentRect.X + _contentRect.Width; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), backgroundStyle);
                }
            }
        }

        if (_promptRect.Width > 0 && _promptRect.Height > 0)
        {
            buffer.PushClip(_promptRect);
            try
            {
                RenderPrompt(buffer, theme, style, isFocused);
            }
            finally
            {
                buffer.PopClip();
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

        for (var row = 0; row < _promptRect.Height; row++)
        {
            var visualRow = Scroll.OffsetY + row;
            var y = _promptRect.Y + row;

            if (visualRow == 0)
            {
                WriteMarkup(buffer, _promptRect.X, y, _cachedPromptText, _cachedPromptRuns, promptBaseStyle);
            }
            else if (!string.IsNullOrEmpty(_cachedContinuationPromptText))
            {
                WriteMarkup(buffer, _promptRect.X, y, _cachedContinuationPromptText, _cachedContinuationPromptRuns, continuationBaseStyle);
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

        if (!TryGetCursorCell(out var caretX, out var caretY))
        {
            return;
        }

        var ghostStyle = style.GhostStyle(theme, focused);
        buffer.WriteText(caretX, caretY, ghostText.AsSpan(), ghostStyle);
    }

    private void EnsureHighlightRuns(Theme theme)
    {
        var snapshot = TextDocument.CurrentSnapshot;
        var version = snapshot.Version;
        var highlighter = Highlighter;
        var enableWordHints = EnableWordHints;

        if (_cachedHighlightVersion == version &&
            ReferenceEquals(_cachedHighlightTheme, theme) &&
            ReferenceEquals(_cachedHighlighter, highlighter) &&
            _cachedWordHints == enableWordHints)
        {
            return;
        }

        _cachedHighlightVersion = version;
        _cachedHighlightTheme = theme;
        _cachedHighlighter = highlighter;
        _cachedWordHints = enableWordHints;

        _highlightRuns.Clear();

        if (highlighter is not null)
        {
            highlighter.Highlight(new PromptEditorHighlightRequest(snapshot, theme), _highlightRuns);
        }

        if (enableWordHints && HasFocus)
        {
            var text = GetCachedText().AsSpan();
            if (!text.IsEmpty)
            {
                var index = Math.Clamp(CaretIndex, 0, text.Length);
                var start = TerminalTextUtility.GetWordStart(text, index);
                var end = TerminalTextUtility.GetWordEnd(text, index);
                if (end > start)
                {
                    _highlightRuns.Add(new StyledRun(start, end - start, GetStyle<PromptEditorStyle>().WordHintStyle(theme)));
                }
            }
        }

        _highlightRuns.Sort(static (a, b) => a.Start.CompareTo(b.Start));
    }

    private void WriteMarkup(CellBuffer buffer, int x, int y, string plainText, StyledRun[] runs, Style baseStyle)
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
            if (col >= _promptRect.Width)
            {
                break;
            }
        }
    }

    /// <inheritdoc />
    protected override void WriteTextSegment(CellBuffer buffer, int x, int y, ReadOnlySpan<char> text, Style style, bool isPlaceholder, int textIndexStart)
    {
        if (isPlaceholder || _highlightRuns.Count == 0 || textIndexStart < 0)
        {
            base.WriteTextSegment(buffer, x, y, text, style, isPlaceholder, textIndexStart);
            return;
        }

        var segmentStart = textIndexStart;
        var segmentEnd = segmentStart + text.Length;

        var runIndex = FindFirstRunIndex(segmentStart);
        var localIndex = 0;
        var col = 0;

        while (localIndex < text.Length)
        {
            if (runIndex >= _highlightRuns.Count)
            {
                var rest = text.Slice(localIndex);
                base.WriteTextSegment(buffer, x + col, y, rest, style, isPlaceholder, segmentStart + localIndex);
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
                base.WriteTextSegment(buffer, x + col, y, slice, style, isPlaceholder, segmentStart + localIndex);
                col += GetTextCells(slice, col, TabSize);
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
            base.WriteTextSegment(buffer, x + col, y, slice2, style | run.Style, isPlaceholder, segmentStart + localIndex);
            col += GetTextCells(slice2, col, TabSize);
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
