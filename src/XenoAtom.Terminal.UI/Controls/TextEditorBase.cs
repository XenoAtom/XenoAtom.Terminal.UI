// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Scrolling;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

public abstract partial class TextEditorBase : Visual, ICursorProvider, IScrollable, ITextEditorHost
{
    private ITextDocument _textDocument;
    private readonly ScrollModel _scroll;
    private readonly TextEditorCore _core;

    protected TextEditorBase()
    {
        Focusable = true;
        _textDocument = new TextDocument();
        _scroll = new ScrollModel();
        _core = new TextEditorCore(this, _textDocument, _scroll);
        _scroll.Changed += OnScrollChanged;
        _textDocument.Changed += OnDocumentChanged;
    }

    public ITextDocument TextDocument
    {
        get => _textDocument;
        set => SetTextDocument(value);
    }

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

    public int CaretIndex
    {
        get => _core.CaretIndex;
        set => _core.SetCaretIndex(value, BuildEditorOptions());
    }

    protected bool IsFocused => ReferenceEquals(App?.FocusedElement, this);

    protected abstract bool IsSingleLine { get; }

    protected virtual bool AcceptsReturn => false;

    protected virtual int TabSize => 4;

    protected virtual TextAlignment Alignment => TextAlignment.Left;

    protected virtual bool ShowPlaceholderWhenUnfocusedOnly => true;

    protected virtual void WriteTextSegment(CellBuffer buffer, int x, int y, ReadOnlySpan<char> text, CellStyle style, bool isPlaceholder)
    {
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

    private TextEditorRenderContext BuildRenderContext(CellBuffer buffer, Rectangle contentRect, CellStyle textStyle, CellStyle selectionStyle, CellStyle placeholderStyle)
        => new(buffer, contentRect, textStyle, selectionStyle, placeholderStyle, Placeholder, IsFocused, WriteTextSegment);

    protected void RenderEditor(CellBuffer buffer, Rectangle contentRect, CellStyle textStyle, CellStyle selectionStyle, CellStyle placeholderStyle)
    {
        var options = BuildEditorOptions();
        var context = BuildRenderContext(buffer, contentRect, textStyle, selectionStyle, placeholderStyle);
        _core.Render(context, options);
    }

    protected void UpdateEditorLayout(Rectangle contentRect)
    {
        _core.UpdateLayout(contentRect, BuildEditorOptions());
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        _core.OnKeyDown(e, BuildEditorOptions());
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        _core.OnTextInput(e, BuildEditorOptions());
    }

    protected override void OnPaste(PasteEventArgs e)
    {
        _core.OnPaste(e, BuildEditorOptions());
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        _core.OnPointerPressed(e, BuildEditorOptions());
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        _core.OnPointerMoved(e, BuildEditorOptions());
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        _core.OnPointerReleased(e);
    }

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

    public bool TryGetCursorCell(out int x, out int y)
        => _core.TryGetCursorCell(BuildEditorOptions(), out x, out y);
}
