// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

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
public abstract partial class TextEditorBase : Visual, ICursorProvider, IScrollable, ITextEditorHost
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
        _document = new TextDocument();
        _scroll = new ScrollModel();
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

    [Bindable]
    public partial string? Placeholder { get; set; }

    [Bindable]
    public partial bool AcceptTab { get; set; }

    [Bindable]
    public partial bool WordWrap { get; set; }

    /// <summary>
    /// Gets a bindable version number used to invalidate layout/render when the underlying document or editor view changes.
    /// </summary>
    [Bindable]
    internal partial int EditorVersion { get; set; }

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
    protected bool IsFocused => ReferenceEquals(App?.FocusedElement, this);

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
    protected virtual void WriteTextSegment(CellBuffer buffer, int x, int y, ReadOnlySpan<char> text, Style style, bool isPlaceholder, int textIndexStart)
    {
        _ = textIndexStart;
        buffer.WriteText(x, y, text, style);
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

    /// <summary>
    /// Renders the editor content into the provided buffer.
    /// </summary>
    protected void RenderEditor(CellBuffer buffer, Rectangle contentRect, Style textStyle, Style selectionStyle, Style placeholderStyle)
    {
        _ = EditorVersion;
        var options = BuildEditorOptions();
        var context = BuildRenderContext(buffer, contentRect, textStyle, selectionStyle, placeholderStyle);
        _core.Render(context, options);
    }

    /// <summary>
    /// Updates the editor layout using the specified content rectangle.
    /// </summary>
    protected void UpdateEditorLayout(Rectangle contentRect)
    {
        _ = EditorVersion;
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
        _core.OnDocumentChanged();
        EditorVersion++;
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

    bool ITextEditorHost.IsFocused => ReferenceEquals(App?.FocusedElement, this);

    void ITextEditorHost.InvalidateEditor() => EditorVersion++;

    void ITextEditorHost.MarkEditorArrangeDirty()
    {
        EditorVersion++;
    }

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
