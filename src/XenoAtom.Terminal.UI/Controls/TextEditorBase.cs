// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

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
    private ITextDocument _textDocument;
    private readonly ScrollModel _scroll;
    private readonly TextEditorCore _core;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextEditorBase"/> class.
    /// </summary>
    protected TextEditorBase()
    {
        Focusable = true;
        _textDocument = new TextDocument();
        _scroll = new ScrollModel();
        _core = new TextEditorCore(this, _textDocument, _scroll);
        _scroll.Changed += OnScrollChanged;
        _textDocument.Changed += OnDocumentChanged;
    }

    /// <summary>
    /// Gets or sets the text document backing this editor.
    /// </summary>
    public ITextDocument TextDocument
    {
        get => _textDocument;
        set => SetTextDocument(value);
    }

    /// <summary>
    /// Gets the scroll model for this editor.
    /// </summary>
    public ScrollModel Scroll => _scroll;

    [Bindable]
    public partial string? Placeholder { get; set; }

    [Bindable]
    public partial bool AcceptTab { get; set; }

    [Bindable]
    public partial bool WordWrap { get; set; }

    private void SetTextDocument(ITextDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (ReferenceEquals(_textDocument, document))
        {
            return;
        }

        _textDocument.Changed -= OnDocumentChanged;
        _textDocument = document;
        _textDocument.Changed += OnDocumentChanged;
        _core.SetDocument(_textDocument);

        MarkArrangeDirty();
        Invalidate();
    }

    /// <summary>
    /// Gets or sets the caret index in the document.
    /// </summary>
    public int CaretIndex
    {
        get => _core.CaretIndex;
        set => _core.SetCaretIndex(value, BuildEditorOptions());
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
        var options = BuildEditorOptions();
        var context = BuildRenderContext(buffer, contentRect, textStyle, selectionStyle, placeholderStyle);
        _core.Render(context, options);
    }

    /// <summary>
    /// Updates the editor layout using the specified content rectangle.
    /// </summary>
    protected void UpdateEditorLayout(Rectangle contentRect)
    {
        _core.UpdateLayout(contentRect, BuildEditorOptions());
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        _core.OnKeyDown(e, BuildEditorOptions());
    }

    /// <inheritdoc />
    protected override void OnTextInput(TextInputEventArgs e)
    {
        _core.OnTextInput(e, BuildEditorOptions());
    }

    /// <inheritdoc />
    protected override void OnPaste(PasteEventArgs e)
    {
        _core.OnPaste(e, BuildEditorOptions());
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        _core.OnPointerPressed(e, BuildEditorOptions());
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        _core.OnPointerMoved(e, BuildEditorOptions());
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerEventArgs e)
    {
        _core.OnPointerReleased(e);
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

    private void OnScrollChanged()
    {
        MarkArrangeDirty();
        App?.RequestRender();
    }

    private void OnDocumentChanged(object? sender, TextDocumentChangedEventArgs e)
    {
        _core.OnDocumentChanged();
        MarkArrangeDirty();
        App?.RequestRender();
    }

    partial void OnWordWrapChanged(bool value) => MarkArrangeDirty();

    bool ITextEditorHost.IsFocused => ReferenceEquals(App?.FocusedElement, this);

    void ITextEditorHost.InvalidateEditor() => App?.RequestRender();

    void ITextEditorHost.MarkEditorArrangeDirty()
    {
        MarkArrangeDirty();
        App?.RequestRender();
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

    /// <summary>
    /// Tries to get the desired terminal cursor position for this editor.
    /// </summary>
    /// <param name="x">When this method returns, contains the cursor x coordinate.</param>
    /// <param name="y">When this method returns, contains the cursor y coordinate.</param>
    /// <returns><c>true</c> if a cursor position is available; otherwise <c>false</c>.</returns>
    public bool TryGetCursorCell(out int x, out int y)
        => _core.TryGetCursorCell(BuildEditorOptions(), out x, out y);

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
