// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Scrolling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Base class for text editor controls (TextBox, TextArea).
/// </summary>
/// <remarks>
/// This type wires together:
/// <list type="bullet">
/// <item><description>A <see cref="ITextDocument"/> for content storage.</description></item>
/// <item><description>A <see cref="ScrollModel"/> for viewport/extent and scrolling.</description></item>
/// <item><description><see cref="TextEditorCore"/> for input handling, layout, selection, and rendering.</description></item>
/// </list>
/// Derived controls typically override editor options (single-line vs multi-line, wrapping, alignment) and style rendering.
/// </remarks>
public abstract partial class TextEditorBase : Visual, ICursorProvider, IScrollable, ITextEditorHost, ISelectionOwner
{
    private ITextDocument _document;
    private readonly ScrollModel _scroll;
    private readonly TextEditorCore _core;
    private readonly TextUndoRedoManager _undoRedo;
    private bool _canUndo;
    private bool _canRedo;
    private int _requestedCaretIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextEditorBase"/> class.
    /// </summary>
    protected TextEditorBase()
    {
        Focusable = true;
        IsSelectable = true;
        _document = new TextDocument();
        _scroll = new ScrollModel(this);
        _undoRedo = new TextUndoRedoManager();
        _undoRedo.Attach(_document);
        _undoRedo.StateChanged += OnUndoRedoStateChanged;

        this.EnableUndo(true);
        this.MaxUndoEntries(200);

        _core = new TextEditorCore(this, _document, _scroll, _undoRedo);
        _document.Changed += OnDocumentChanged;
        OnUndoRedoStateChanged();

        // Expose the document through a bindable property so controls/templates can bind to it.
        // Any later replacement is bridged to the editor core from PrepareChildren().
        this.TextDocument(_document);

        AddCommand(new Command
        {
            Id = "TextEditor.Undo",
            LabelMarkup = "Undo",
            DescriptionMarkup = "Undo the last change.",
            Gesture = new Input.KeyGesture(TerminalChar.CtrlZ, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Primary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((TextEditorBase)v).Undo(),
            CanExecute = static v => ((TextEditorBase)v).CanUndo,
        });

        AddCommand(new Command
        {
            Id = "TextEditor.Redo",
            LabelMarkup = "Redo",
            DescriptionMarkup = "Redo the last undone change.",
            Gesture = new Input.KeyGesture(TerminalChar.CtrlR, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Primary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((TextEditorBase)v).Redo(),
            CanExecute = static v => ((TextEditorBase)v).CanRedo,
        });

        // Text editing shortcuts are handled by TextEditorCore, but registering them as commands ensures
        // command routing prefers the focused editor over ancestor visuals (e.g. a DataGridControl).
        AddCommand(new Command
        {
            Id = "TextEditor.SelectAll",
            LabelMarkup = string.Empty,
            Gesture = new Input.KeyGesture(TerminalChar.CtrlA, TerminalModifiers.Ctrl),
            Presentation = CommandPresentation.None,
            Execute = static v => ((TextEditorBase)v).ExecuteEditorShortcut(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlA, Modifiers = TerminalModifiers.Ctrl }),
        });

        AddCommand(new Command
        {
            Id = "TextEditor.Copy",
            LabelMarkup = string.Empty,
            Gesture = new Input.KeyGesture(TerminalChar.CtrlC, TerminalModifiers.Ctrl),
            Presentation = CommandPresentation.None,
            Execute = static v => ((TextEditorBase)v).ExecuteEditorShortcut(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlC, Modifiers = TerminalModifiers.Ctrl }),
        });

        AddCommand(new Command
        {
            Id = "TextEditor.Paste",
            LabelMarkup = string.Empty,
            Gesture = new Input.KeyGesture(TerminalChar.CtrlV, TerminalModifiers.Ctrl),
            Presentation = CommandPresentation.None,
            Execute = static v => ((TextEditorBase)v).ExecuteEditorShortcut(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlV, Modifiers = TerminalModifiers.Ctrl }),
        });

        AddCommand(new Command
        {
            Id = "TextEditor.Cut",
            LabelMarkup = string.Empty,
            Gesture = new Input.KeyGesture(TerminalChar.CtrlX, TerminalModifiers.Ctrl),
            Presentation = CommandPresentation.None,
            Execute = static v => ((TextEditorBase)v).ExecuteEditorShortcut(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlX, Modifiers = TerminalModifiers.Ctrl }),
        });

        AddCommand(new Command
        {
            Id = "TextEditor.CtrlHome",
            LabelMarkup = string.Empty,
            Gesture = new Input.KeyGesture(TerminalKey.Home, TerminalModifiers.Ctrl),
            Presentation = CommandPresentation.None,
            Execute = static v => ((TextEditorBase)v).ExecuteEditorShortcut(new TerminalKeyEvent { Key = TerminalKey.Home, Modifiers = TerminalModifiers.Ctrl }),
        });

        AddCommand(new Command
        {
            Id = "TextEditor.CtrlEnd",
            LabelMarkup = string.Empty,
            Gesture = new Input.KeyGesture(TerminalKey.End, TerminalModifiers.Ctrl),
            Presentation = CommandPresentation.None,
            Execute = static v => ((TextEditorBase)v).ExecuteEditorShortcut(new TerminalKeyEvent { Key = TerminalKey.End, Modifiers = TerminalModifiers.Ctrl }),
        });
    }

    private void ExecuteEditorShortcut(TerminalKeyEvent keyEvent)
    {
        var args = new KeyEventArgs { RawEvent = keyEvent };
        _core.OnKeyDown(args, BuildEditorOptions());
        _requestedCaretIndex = _core.CaretIndex;
    }

    /// <summary>
    /// Gets or sets the text document backing this editor.
    /// </summary>
    [Bindable]
    public partial ITextDocument TextDocument { get; set; }

    /// <inheritdoc/>
    protected override void PrepareChildren()
    {
        var desired = TextDocument;
        if (ReferenceEquals(_document, desired))
        {
            return;
        }

        _document.Changed -= OnDocumentChanged;
        _document = desired;

        _undoRedo.Attach(_document);
        _document.Changed += OnDocumentChanged;
        _core.SetDocument(_document);

        OnUndoRedoStateChanged();

        // Re-apply the last requested caret position after swapping documents. This makes object initializers work
        // (e.g. new TextBox("...") { CaretIndex = 6 }) and keeps the caret stable when a bound document changes.
        _core.SetCaretIndex(_requestedCaretIndex, BuildEditorOptions());
        _requestedCaretIndex = _core.CaretIndex;
    }

    /// <summary>
    /// Gets the scroll model for this editor.
    /// </summary>
    public ScrollModel Scroll => _scroll;

    /// <summary>
    /// Gets a value indicating whether an undo operation is currently available.
    /// </summary>
    [Bindable]
    public bool CanUndo
    {
        get => BindingManager.Current.GetValue(this, ref _canUndo, __CanUndo__BindingAccessor.Instance);
        private set => BindingManager.Current.SetValue(this, ref _canUndo, value, __CanUndo__BindingAccessor.Instance);
    }

    /// <summary>
    /// Gets a value indicating whether a redo operation is currently available.
    /// </summary>
    [Bindable]
    public bool CanRedo
    {
        get => BindingManager.Current.GetValue(this, ref _canRedo, __CanRedo__BindingAccessor.Instance);
        private set => BindingManager.Current.SetValue(this, ref _canRedo, value, __CanRedo__BindingAccessor.Instance);
    }

    /// <summary>
    /// Gets or sets a value indicating whether undo/redo tracking is enabled.
    /// </summary>
    [Bindable]
    public partial bool EnableUndo { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of undo entries retained by this editor.
    /// </summary>
    [Bindable]
    public partial int MaxUndoEntries { get; set; }

    /// <summary>
    /// Gets or sets the placeholder text displayed when the editor is empty.
    /// </summary>
    [Bindable]
    public partial string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the editor accepts the Tab key as input.
    /// </summary>
    /// <remarks>
    /// When enabled, pressing Tab inserts a tab character (or triggers indentation behavior) instead of moving focus to the next control.
    /// </remarks>
    [Bindable]
    public partial bool AcceptTab { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether word-wrapping is enabled.
    /// </summary>
    /// <remarks>
    /// When enabled, long lines wrap within the editor viewport rather than scrolling horizontally.
    /// </remarks>
    [Bindable]
    public partial bool WordWrap { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the editor participates in selection ownership.
    /// </summary>
    [Bindable]
    public partial bool IsSelectable { get; set; }

    // NOTE: Text document replacement is handled by PrepareChildren() to avoid ad-hoc invalidation.

    /// <summary>
    /// Gets or sets the caret index in the document.
    /// </summary>
    public int CaretIndex
    {
        get => _core.CaretIndex;
        set
        {
            _requestedCaretIndex = value;
            _core.SetCaretIndex(value, BuildEditorOptions());
        }
    }

    /// <summary>
    /// Gets a value indicating whether this editor is the focused element in the application.
    /// </summary>
    protected bool IsFocused => HasFocus;

    /// <summary>
    /// Gets the selection start index in the document.
    /// </summary>
    /// <remarks>
    /// When there is no selection, this value is equal to <see cref="CaretIndex"/>.
    /// </remarks>
    protected int SelectionStart => _core.SelectionStart;

    /// <summary>
    /// Gets the selection length in the document.
    /// </summary>
    protected int SelectionLength => _core.SelectionLength;

    /// <inheritdoc />
    public bool HasSelection => _core.HasSelectionForSelectionOwner;

    void ISelectionOwner.ClearSelection()
    {
        _core.ClearSelectionForSelectionOwner();
        App?.RequestRender();
    }

    /// <inheritdoc />
    public bool TryCopySelection(out string text) => _core.TryGetSelectionText(out text);

    /// <summary>
    /// Gets a value indicating whether this editor is single-line.
    /// </summary>
    protected abstract bool IsSingleLine { get; }

    /// <summary>
    /// Gets a value indicating whether the editor accepts the Return key to insert a newline.
    /// </summary>
    protected virtual bool AcceptsReturn => false;

    /// <summary>
    /// Gets the tab size (in spaces) used when inserting or rendering tabs.
    /// </summary>
    protected virtual int TabSize => 4;

    /// <summary>
    /// Gets the text alignment used by the editor.
    /// </summary>
    protected virtual TextAlignment Alignment => TextAlignment.Left;

    /// <summary>
    /// Gets a value indicating whether placeholder text is shown only when the editor is not focused.
    /// </summary>
    protected virtual bool ShowPlaceholderWhenUnfocusedOnly => true;

    /// <summary>
    /// Writes a segment of text into the buffer.
    /// </summary>
    /// <remarks>
    /// Derived controls can override this to customize how characters are rendered (e.g. masking).
    /// </remarks>
    protected virtual void WriteTextSegment(CellBuffer buffer, int x, int y, ReadOnlySpan<char> text, Style style, bool isPlaceholder, int textIndexStart, int startColumn)
    {
        _ = textIndexStart;
        if (text.IsEmpty)
        {
            return;
        }

        if (text.IndexOf('\t') < 0)
        {
            buffer.WriteText(x, y, text, style);
            return;
        }

        var column = Math.Max(0, startColumn);
        var cellX = x;
        var index = 0;
        var tabSize = Math.Max(1, TabSize);
        while (index < text.Length)
        {
            var tabOffset = text[index..].IndexOf('\t');
            if (tabOffset < 0)
            {
                var slice = text[index..];
                buffer.WriteText(cellX, y, slice, style);
                var width = Math.Max(0, TerminalTextUtility.GetWidth(slice));
                column += width;
                cellX += width;
                break;
            }

            if (tabOffset > 0)
            {
                var slice = text.Slice(index, tabOffset);
                buffer.WriteText(cellX, y, slice, style);
                var width = Math.Max(0, TerminalTextUtility.GetWidth(slice));
                column += width;
                cellX += width;
                index += tabOffset;
            }

            var tabWidth = tabSize - (column % tabSize);
            tabWidth = Math.Max(1, tabWidth);
            for (var i = 0; i < tabWidth; i++)
            {
                buffer.SetCell(cellX + i, y, new Rune(' '), style);
            }

            column += tabWidth;
            cellX += tabWidth;
            index++;
        }
    }

    private TextEditorOptions BuildEditorOptions()
        => new(
            SingleLine: IsSingleLine,
            AcceptsReturn: AcceptsReturn,
            AcceptsTab: AcceptTab,
            WordWrap: WordWrap,
            TabSize: TabSize,
            Alignment: Alignment,
            ShowPlaceholderWhenUnfocusedOnly: ShowPlaceholderWhenUnfocusedOnly);

    private TextEditorRenderContext BuildRenderContext(CellBuffer buffer, Rectangle contentRect, Style textStyle, Style selectionStyle, Style placeholderStyle)
        => new(buffer, contentRect, textStyle, selectionStyle, placeholderStyle, Placeholder, IsFocused, WriteTextSegment);

    internal TextEditorCore.TextEditorLineLayoutDiagnostics GetLineLayoutDiagnostics(int lineIndex)
        => _core.GetLineLayoutDiagnostics(lineIndex, BuildEditorOptions());

    /// <summary>
    /// Renders the editor content into the provided buffer.
    /// </summary>
    protected void RenderEditor(CellBuffer buffer, Rectangle contentRect, Style textStyle, Style selectionStyle, Style placeholderStyle)
    {
        _ = _core.Version;
        var options = BuildEditorOptions();
        var context = BuildRenderContext(buffer, contentRect, textStyle, selectionStyle, placeholderStyle);
        _core.Render(context, options);
    }

    /// <summary>
    /// Updates the editor layout using the specified content rectangle.
    /// </summary>
    protected void UpdateEditorLayout(Rectangle contentRect)
    {
        _ = _core.Version;
        _core.UpdateLayout(contentRect, BuildEditorOptions());
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        _core.OnKeyDown(e, BuildEditorOptions());
        _requestedCaretIndex = _core.CaretIndex;
    }

    /// <inheritdoc />
    protected override void OnTextInput(TextInputEventArgs e)
    {
        _core.OnTextInput(e, BuildEditorOptions());
        _requestedCaretIndex = _core.CaretIndex;
    }

    /// <inheritdoc />
    protected override void OnPaste(PasteEventArgs e)
    {
        _core.OnPaste(e, BuildEditorOptions());
        _requestedCaretIndex = _core.CaretIndex;
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        _core.OnPointerPressed(e, BuildEditorOptions());
        _requestedCaretIndex = _core.CaretIndex;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        _core.OnPointerMoved(e, BuildEditorOptions());
        _requestedCaretIndex = _core.CaretIndex;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerEventArgs e)
    {
        _core.OnPointerReleased(e);
        _requestedCaretIndex = _core.CaretIndex;
    }

    /// <inheritdoc />
    protected override void OnPointerWheel(PointerEventArgs e)
    {
        if (IsSingleLine || e.WheelDelta == 0)
        {
            return;
        }

        var delta = e.WheelDelta > 0 ? -1 : 1;
        _scroll.ScrollBy(0, delta);
        e.Handled = true;
    }

    private void OnDocumentChanged(object? sender, TextDocumentChangedEventArgs e)
    {
        _undoRedo.EnsureSynchronized();
        _core.OnDocumentChanged(e);
    }

    partial void OnEnableUndoChanged(bool value)
    {
        _undoRedo.Enabled = value;
        if (!value)
        {
            _undoRedo.Clear();
        }
    }

    partial void OnMaxUndoEntriesChanged(int value) => _undoRedo.MaxEntries = Math.Max(0, value);

    bool ITextEditorHost.IsFocused => HasFocus;

    bool ITextEditorHost.TryOpenSearchReplacePopup(SearchReplaceMode mode, string? initialSearchText)
        => TryOpenSearchReplacePopup(mode, initialSearchText);

    /// <summary>
    /// Attempts to open a search/replace popup for this editor.
    /// </summary>
    /// <remarks>
    /// The base implementation returns <see langword="false"/>. Multi-line editors can override this to
    /// provide an integrated find/replace UI.
    /// </remarks>
    /// <param name="mode">The requested mode.</param>
    /// <param name="initialSearchText">An optional initial search text (typically the current selection).</param>
    /// <returns><see langword="true"/> if a popup was opened; otherwise <see langword="false"/>.</returns>
    protected virtual bool TryOpenSearchReplacePopup(SearchReplaceMode mode, string? initialSearchText)
    {
        _ = mode;
        _ = initialSearchText;
        return false;
    }

    internal ISearchReplaceTarget CreateSearchReplaceTarget() => new TextEditorSearchTarget(this);

    internal TextUndoRedoManager UndoManager => _undoRedo;

    /// <summary>
    /// Clears undo and redo history for this editor.
    /// </summary>
    public void ClearUndoHistory() => _undoRedo.Clear();

    /// <summary>
    /// Attempts to undo the last edit.
    /// </summary>
    public void Undo() => _core.Undo(BuildEditorOptions());

    /// <summary>
    /// Attempts to redo the last undone edit.
    /// </summary>
    public void Redo() => _core.Redo(BuildEditorOptions());

    /// <summary>
    /// Attempts to open the integrated find UI for this editor.
    /// </summary>
    /// <param name="initialSearchText">An optional initial search text (typically the current selection).</param>
    /// <returns><see langword="true"/> if a popup was opened; otherwise <see langword="false"/>.</returns>
    public bool OpenFind(string? initialSearchText = null)
        => TryOpenSearchReplacePopup(SearchReplaceMode.Find, initialSearchText);

    /// <summary>
    /// Attempts to open the integrated find/replace UI for this editor.
    /// </summary>
    /// <param name="initialSearchText">An optional initial search text (typically the current selection).</param>
    /// <returns><see langword="true"/> if a popup was opened; otherwise <see langword="false"/>.</returns>
    public bool OpenReplace(string? initialSearchText = null)
        => TryOpenSearchReplacePopup(SearchReplaceMode.Replace, initialSearchText);

    /// <summary>
    /// Tries to get the desired terminal cursor position for this editor.
    /// </summary>
    /// <param name="x">When this method returns, contains the cursor x coordinate.</param>
    /// <param name="y">When this method returns, contains the cursor y coordinate.</param>
    /// <returns><c>true</c> if a cursor position is available; otherwise <c>false</c>.</returns>
    public bool TryGetCursorCell(out int x, out int y)
        => _core.TryGetCursorCell(BuildEditorOptions(), out x, out y);

    private void OnUndoRedoStateChanged()
    {
        CanUndo = _undoRedo.CanUndo;
        CanRedo = _undoRedo.CanRedo;
        App?.RequestRender();
    }

    private sealed class TextEditorSearchTarget : ISearchReplaceTarget
    {
        private readonly TextEditorBase _owner;

        public TextEditorSearchTarget(TextEditorBase owner)
        {
            _owner = owner;
        }

        public string Title => "Find";

        public bool SupportsReplace => !_owner.IsSingleLine;

        public void SetQuery(in SearchQuery query)
            => _owner._core.SetSearchQuery(query, _owner.BuildEditorOptions());

        public void NextMatch()
            => _owner._core.GoToNextSearchMatch(_owner.BuildEditorOptions());

        public void PreviousMatch()
            => _owner._core.GoToPreviousSearchMatch(_owner.BuildEditorOptions());

        public int ReplaceCurrent(string replacement)
            => _owner._core.ReplaceCurrentSearchMatch(replacement, _owner.BuildEditorOptions());

        public int ReplaceAll(string replacement)
            => _owner._core.ReplaceAllSearchMatches(replacement, _owner.BuildEditorOptions());

        public string GetStatusText() => _owner._core.GetSearchStatusText();

        public string? GetErrorText() => _owner._core.GetSearchErrorText();
    }
}
