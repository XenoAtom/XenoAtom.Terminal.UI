// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Geometry;
using Input = XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Specifies which side of the editor hosts a margin.
/// </summary>
public enum CodeEditorMarginSide
{
    /// <summary>
    /// The margin is rendered before the text surface.
    /// </summary>
    Left,

    /// <summary>
    /// The margin is rendered after the text surface.
    /// </summary>
    Right,
}

/// <summary>
/// Describes a visible row mapped from the wrapped editor layout.
/// </summary>
/// <param name="LineIndex">The zero-based logical line index.</param>
/// <param name="LineStart">The UTF-16 document start index of the logical line.</param>
/// <param name="LineLength">The UTF-16 length of the logical line excluding the line break.</param>
/// <param name="RowInLine">The wrapped-row index within the logical line.</param>
/// <param name="VisualRow">The viewport-relative row within the current editor viewport.</param>
/// <param name="ScreenY">The absolute screen Y coordinate.</param>
/// <param name="SegmentStart">The UTF-16 start index within the logical line for the visible wrapped segment.</param>
/// <param name="SegmentLength">The UTF-16 length of the visible wrapped segment.</param>
public readonly record struct CodeEditorVisibleLine(
    int LineIndex,
    int LineStart,
    int LineLength,
    int RowInLine,
    int VisualRow,
    int ScreenY,
    int SegmentStart,
    int SegmentLength)
{
    /// <summary>
    /// Gets a value indicating whether this row is the first wrapped row of the logical line.
    /// </summary>
    public bool IsFirstRowOfLine => RowInLine == 0;
}

/// <summary>
/// Represents a highlighting request for a single logical line.
/// </summary>
public readonly record struct CodeEditorLineHighlightRequest(
    ITextSnapshot Snapshot,
    Theme Theme,
    int LineIndex,
    int LineStart,
    int LineLength,
    int CaretIndex,
    int SelectionStart,
    int SelectionLength);

/// <summary>
/// Delegate used to compute syntax highlighting runs for a single logical line.
/// </summary>
/// <param name="request">The current line highlighting request.</param>
/// <param name="runs">The destination list that receives UTF-16 runs relative to the line text.</param>
public delegate void CodeEditorLineHighlighter(in CodeEditorLineHighlightRequest request, List<StyledRun> runs);

/// <summary>
/// Represents a request to fetch syntax runs for a single logical line.
/// </summary>
public readonly record struct CodeEditorLineSyntaxRequest(
    ITextSnapshot Snapshot,
    Theme Theme,
    int LineIndex,
    int LineStart,
    int LineLength,
    int CaretIndex,
    int SelectionStart,
    int SelectionLength);

/// <summary>
/// Represents the initial syntax-build context for a snapshot.
/// </summary>
public readonly record struct CodeEditorSyntaxBuildContext(
    ITextSnapshot Snapshot,
    Theme Theme,
    int CaretIndex,
    int SelectionStart,
    int SelectionLength);

/// <summary>
/// Represents an incremental syntax-update request.
/// </summary>
public readonly record struct CodeEditorSyntaxUpdateContext(
    ITextSnapshot Snapshot,
    Theme Theme,
    TextDocumentChangedEventArgs? Change,
    int AffectedStartLine,
    int AffectedEndLine,
    int CaretIndex,
    int SelectionStart,
    int SelectionLength);

/// <summary>
/// Base type for syntax state associated with a snapshot version.
/// </summary>
public abstract class CodeEditorSyntaxState
{
    /// <summary>
    /// Gets the snapshot version associated with the syntax state.
    /// </summary>
    public abstract int SnapshotVersion { get; }
}

/// <summary>
/// Base class for advanced code-editor syntax highlighters.
/// </summary>
public abstract class CodeEditorSyntaxHighlighter
{
    /// <summary>
    /// Builds syntax state for a snapshot from scratch.
    /// </summary>
    public abstract CodeEditorSyntaxState Build(in CodeEditorSyntaxBuildContext context);

    /// <summary>
    /// Updates syntax state after a document change.
    /// </summary>
    public abstract CodeEditorSyntaxState Update(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context);

    /// <summary>
    /// Gets syntax runs for a single logical line.
    /// </summary>
    public abstract void GetLineRuns(CodeEditorSyntaxState state, in CodeEditorLineSyntaxRequest request, List<StyledRun> runs);
}

/// <summary>
/// Optional asynchronous syntax-highlighter contract for background processing.
/// </summary>
public interface IAsyncCodeEditorSyntaxHighlighter
{
    /// <summary>
    /// Builds syntax state asynchronously.
    /// </summary>
    ValueTask<CodeEditorSyntaxState> BuildAsync(in CodeEditorSyntaxBuildContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates syntax state asynchronously.
    /// </summary>
    ValueTask<CodeEditorSyntaxState> UpdateAsync(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides measurement information to a code-editor margin.
/// </summary>
public readonly record struct CodeEditorMarginMeasureContext(
    CodeEditor Editor,
    Theme Theme,
    CodeEditorStyle Style,
    IReadOnlyList<CodeEditorVisibleLine> VisibleLines,
    Rectangle Bounds,
    bool IsFocused,
    int CaretLineIndex,
    int MinLineNumberDigits);

/// <summary>
/// Provides rendering information to a code-editor margin.
/// </summary>
public readonly record struct CodeEditorMarginRenderContext(
    CodeEditor Editor,
    CellBuffer Buffer,
    Theme Theme,
    CodeEditorStyle Style,
    IReadOnlyList<CodeEditorVisibleLine> VisibleLines,
    Rectangle Bounds,
    bool IsFocused,
    int CaretLineIndex,
    int MinLineNumberDigits);

/// <summary>
/// Provides pointer-routing information to a code-editor margin.
/// </summary>
public readonly record struct CodeEditorMarginPointerContext(
    CodeEditor Editor,
    Input.PointerEventArgs EventArgs,
    IReadOnlyList<CodeEditorVisibleLine> VisibleLines,
    Rectangle Bounds,
    int CaretLineIndex,
    int MinLineNumberDigits)
{
    /// <summary>
    /// Tries to locate the visible line under the pointer.
    /// </summary>
    public bool TryGetVisibleLine(out CodeEditorVisibleLine visibleLine)
    {
        var localY = EventArgs.UiY - Bounds.Y;
        if ((uint)localY >= (uint)Bounds.Height || localY < 0 || localY >= VisibleLines.Count)
        {
            visibleLine = default;
            return false;
        }

        visibleLine = VisibleLines[localY];
        return true;
    }
}

/// <summary>
/// Base class for pluggable code-editor margins.
/// </summary>
public abstract class CodeEditorMargin : IVisualElement
{
    private CodeEditor? _owner;

    /// <summary>
    /// Gets the owning application when attached.
    /// </summary>
    public TerminalApp? App => _owner?.App;

    /// <summary>
    /// Gets the side on which the margin is rendered.
    /// </summary>
    public abstract CodeEditorMarginSide Side { get; }

    /// <summary>
    /// Measures the width of the margin for the current viewport.
    /// </summary>
    public abstract int MeasureWidth(in CodeEditorMarginMeasureContext context);

    /// <summary>
    /// Renders the margin.
    /// </summary>
    public abstract void Render(in CodeEditorMarginRenderContext context);

    /// <summary>
    /// Handles pointer presses routed to the margin.
    /// </summary>
    public virtual bool OnPointerPressed(in CodeEditorMarginPointerContext context)
    {
        _ = context;
        return false;
    }

    internal void Attach(CodeEditor owner) => _owner = owner;

    internal void Detach(CodeEditor owner)
    {
        if (ReferenceEquals(_owner, owner))
        {
            _owner = null;
        }
    }
}

internal sealed class CodeEditorLineNumberMargin : CodeEditorMargin
{
    public static CodeEditorLineNumberMargin Instance { get; } = new();

    public override CodeEditorMarginSide Side => CodeEditorMarginSide.Left;

    public override int MeasureWidth(in CodeEditorMarginMeasureContext context)
    {
        if (!context.Editor.ShowLineNumbers)
        {
            return 0;
        }

        var digits = Math.Max(1, context.MinLineNumberDigits);
        var lines = context.VisibleLines;
        for (var i = 0; i < lines.Count; i++)
        {
            var lineNumber = lines[i].LineIndex + 1;
            digits = Math.Max(digits, CountDigits(lineNumber));
        }

        return digits + 1;
    }

    public override void Render(in CodeEditorMarginRenderContext context)
    {
        if (!context.Editor.ShowLineNumbers || context.Bounds.Width <= 0 || context.Bounds.Height <= 0)
        {
            return;
        }

        var lines = context.VisibleLines;
        for (var i = 0; i < lines.Count; i++)
        {
            var visible = lines[i];
            if (!visible.IsFirstRowOfLine)
            {
                continue;
            }

            var y = visible.ScreenY;
            if (y < context.Bounds.Y || y >= context.Bounds.Bottom)
            {
                continue;
            }

            var style = visible.LineIndex == context.CaretLineIndex
                ? context.Style.ActiveLineNumberStyle(context.Theme, context.IsFocused)
                : context.Style.LineNumberStyle(context.Theme, context.IsFocused);

            var numberText = (visible.LineIndex + 1).ToString();
            var textX = Math.Max(context.Bounds.X, context.Bounds.Right - 1 - numberText.Length);
            context.Buffer.WriteText(textX, y, numberText.AsSpan(), style);
        }
    }

    private static int CountDigits(int value)
    {
        value = Math.Abs(value);
        if (value < 10) return 1;
        if (value < 100) return 2;
        if (value < 1000) return 3;
        if (value < 10000) return 4;
        var digits = 1;
        while (value >= 10)
        {
            value /= 10;
            digits++;
        }

        return digits;
    }
}

/// <summary>
/// Represents a code-oriented multi-line text editor with pluggable margins and syntax highlighting.
/// </summary>
public sealed partial class CodeEditor : TextEditorBase
{
    private readonly SearchReplacePopup _searchPopup;
    private readonly BindableList<CodeEditorMargin> _leftMargins;
    private readonly BindableList<CodeEditorMargin> _rightMargins;
    private readonly Dictionary<CodeEditorMargin, int> _marginWidths = new();
    private readonly Dictionary<CodeEditorMargin, Rectangle> _marginBounds = new();
    private readonly List<CodeEditorVisibleLine> _visibleLines = new();
    private readonly Dictionary<int, List<StyledRun>> _workingHighlightRunsByLine = new();
    private readonly List<int> _highlightBoundaryPoints = new(128);
    private readonly List<StyledRun> _normalizedHighlightRuns = new(64);
    private readonly Dictionary<int, LineHighlightCacheEntry> _lineHighlightCache = new();
    private readonly List<(CodeEditorMargin Margin, Rectangle Bounds)> _orderedMargins = new();
    private readonly CancellationTokenSource _syntaxUpdateCts = new();

    private Rectangle _contentRect;
    private Rectangle _editorRect;
    private Rectangle _leftMarginsRect;
    private Rectangle _rightMarginsRect;
    private int _leftMarginWidth;
    private int _rightMarginWidth;
    private int _lineNumberDigits;
    private int _lastMeasuredViewportHeight;
    private int _cachedHighlightSnapshotVersion = -1;
    private Theme? _cachedHighlightTheme;
    private CodeEditorLineHighlighter? _cachedHighlighter;
    private CodeEditorSyntaxHighlighter? _cachedSyntaxHighlighter;
    private int _cachedHighlightCaretIndex;
    private int _cachedHighlightSelectionStart;
    private int _cachedHighlightSelectionLength;
    private TextDocumentChangedEventArgs? _lastDocumentChange;
    private CodeEditorSyntaxState? _syntaxState;
    private int _pendingSyntaxVersion = -1;
    private int _asyncBuildVersion = -1;

    private sealed record LineHighlightCacheEntry(int LineStart, int LineLength, StyledRun[] Runs);

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeEditor"/> class.
    /// </summary>
    public CodeEditor()
    {
        this.AcceptTab(true);
        this.WordWrap(true);
        this.HorizontalAlignment(Align.Stretch);
        this.VerticalAlignment(Align.Stretch);
        this.ShowLineNumbers(true);
        this.HighlightCurrentLine(true);
        this.MinLineNumberDigits(2);

        TextDocument = new DynamicTextDocument(
            getter: () => Text ?? string.Empty,
            setter: value => Text = value);

        _leftMargins = new BindableList<CodeEditorMargin>(this, $"{nameof(CodeEditor)}.{nameof(LeftMargins)}", AttachMargin, DetachMargin);
        _rightMargins = new BindableList<CodeEditorMargin>(this, $"{nameof(CodeEditor)}.{nameof(RightMargins)}", AttachMargin, DetachMargin);
        _leftMargins.Add(CodeEditorLineNumberMargin.Instance);

        _searchPopup = new SearchReplacePopup(CreateSearchReplaceTarget())
        {
            ClearQueryOnClose = true,
        };
        AttachChild(_searchPopup);

        AddCommand(new Command
        {
            Id = "TextEditor.Find",
            LabelMarkup = "Find",
            DescriptionMarkup = "Search within the current document.",
            Gesture = new Input.KeyGesture(TerminalChar.CtrlF, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((CodeEditor)v).OpenFind(),
        });

        AddCommand(new Command
        {
            Id = "TextEditor.Replace",
            LabelMarkup = "Replace",
            DescriptionMarkup = "Search and replace within the current document.",
            Gesture = new Input.KeyGesture(TerminalChar.CtrlH, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((CodeEditor)v).OpenReplace(),
        });
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeEditor"/> class with initial text.
    /// </summary>
    public CodeEditor(string? text) : this()
    {
        this.Text(text);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeEditor"/> class with dynamic text.
    /// </summary>
    public CodeEditor(Func<string?> text) : this()
    {
        this.Text(text);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeEditor"/> class with bound text.
    /// </summary>
    public CodeEditor(Binding<string?> text) : this()
    {
        this.BindText(text);
    }

    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    [Bindable]
    public partial string? Text { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether logical line numbers are shown.
    /// </summary>
    [Bindable]
    public partial bool ShowLineNumbers { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of digits reserved by the adaptive line-number gutter.
    /// </summary>
    [Bindable]
    public partial int MinLineNumberDigits { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current caret line receives an editor-surface highlight.
    /// </summary>
    [Bindable]
    public partial bool HighlightCurrentLine { get; set; }

    /// <summary>
    /// Gets or sets the optional simple line-based syntax highlighter.
    /// </summary>
    [Bindable]
    public partial Delegator<CodeEditorLineHighlighter> Highlighter { get; set; }

    /// <summary>
    /// Gets or sets the optional advanced syntax highlighter.
    /// </summary>
    [Bindable(NoVisualAttach = true)]
    public partial CodeEditorSyntaxHighlighter? SyntaxHighlighter { get; set; }

    /// <summary>
    /// Gets the ordered left-side margins.
    /// </summary>
    public BindableList<CodeEditorMargin> LeftMargins => _leftMargins;

    /// <summary>
    /// Gets the ordered right-side margins.
    /// </summary>
    public BindableList<CodeEditorMargin> RightMargins => _rightMargins;

    /// <inheritdoc />
    protected override bool IsSingleLine => false;

    /// <inheritdoc />
    protected override bool AcceptsReturn => true;

    /// <inheritdoc />
    protected override bool ShowPlaceholderWhenUnfocusedOnly => false;

    /// <inheritdoc/>
    protected override int ChildrenCount => 1;

    /// <inheritdoc/>
    protected override Visual GetChild(int index)
        => index == 0 ? _searchPopup : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc />
    protected override void PrepareChildren()
    {
        _ = Scroll.Version;
        base.PrepareChildren();
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var width = 40;
        var height = 10;
        return SizeHints.Fixed(constraints.Clamp(new Size(width, height)));
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var style = GetStyle<CodeEditorStyle>();
        var padding = style.Padding;

        _contentRect = new Rectangle(
            finalRect.X + padding.Left,
            finalRect.Y + padding.Top,
            Math.Max(0, finalRect.Width - padding.Horizontal),
            Math.Max(0, finalRect.Height - padding.Vertical));

        BuildVisibleLines(_contentRect, includeScrollOffset: true);

        _leftMarginWidth = MeasureMargins(_leftMargins, style, _contentRect);
        _rightMarginWidth = MeasureMargins(_rightMargins, style, _contentRect);

        _leftMarginsRect = new Rectangle(_contentRect.X, _contentRect.Y, _leftMarginWidth, _contentRect.Height);
        _rightMarginsRect = new Rectangle(Math.Max(_contentRect.X, _contentRect.Right - _rightMarginWidth), _contentRect.Y, _rightMarginWidth, _contentRect.Height);
        _editorRect = new Rectangle(
            _contentRect.X + _leftMarginWidth,
            _contentRect.Y,
            Math.Max(0, _contentRect.Width - _leftMarginWidth - _rightMarginWidth),
            _contentRect.Height);

        UpdateEditorLayout(_editorRect);
        BuildVisibleLines(_editorRect, includeScrollOffset: false);

        var measuredLeft = MeasureMargins(_leftMargins, style, _contentRect);
        var measuredRight = MeasureMargins(_rightMargins, style, _contentRect);
        if (measuredLeft != _leftMarginWidth || measuredRight != _rightMarginWidth)
        {
            _leftMarginWidth = measuredLeft;
            _rightMarginWidth = measuredRight;
            _leftMarginsRect = new Rectangle(_contentRect.X, _contentRect.Y, _leftMarginWidth, _contentRect.Height);
            _rightMarginsRect = new Rectangle(Math.Max(_contentRect.X, _contentRect.Right - _rightMarginWidth), _contentRect.Y, _rightMarginWidth, _contentRect.Height);
            _editorRect = new Rectangle(
                _contentRect.X + _leftMarginWidth,
                _contentRect.Y,
                Math.Max(0, _contentRect.Width - _leftMarginWidth - _rightMarginWidth),
                _contentRect.Height);
            UpdateEditorLayout(_editorRect);
            BuildVisibleLines(_editorRect, includeScrollOffset: false);
            _ = MeasureMargins(_leftMargins, style, _contentRect);
            _ = MeasureMargins(_rightMargins, style, _contentRect);
        }

        _searchPopup.ArrangeWithin(_editorRect);
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
        var style = GetStyle<CodeEditorStyle>();
        var focused = HasFocus;
        var backgroundStyle = style.BackgroundStyle(theme, focused);
        var selectionStyle = style.SelectionStyle(theme);
        var placeholderStyle = style.PlaceholderStyle(theme, focused);
        var marginBackgroundStyle = style.MarginBackgroundStyle(theme, focused);

        FillRect(buffer, _contentRect, backgroundStyle);
        FillRect(buffer, _leftMarginsRect, marginBackgroundStyle);
        FillRect(buffer, _rightMarginsRect, marginBackgroundStyle);

        if (_editorRect.Width > 0 && _editorRect.Height > 0)
        {
            RenderCurrentLineBackground(buffer, theme, style, focused);

            buffer.PushClip(_editorRect);
            try
            {
                EnsureHighlightRuns(theme);
                RenderEditor(buffer, _editorRect, backgroundStyle, selectionStyle, placeholderStyle);
            }
            finally
            {
                buffer.PopClip();
            }
        }

        RenderMargins(buffer, theme, style, focused);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(Input.PointerEventArgs e)
    {
        if (RoutePointerPressedToMargins(e))
        {
            return;
        }

        base.OnPointerPressed(e);
    }

    /// <inheritdoc />
    protected override bool TryOpenSearchReplacePopup(SearchReplaceMode mode, string? initialSearchText)
        => mode == SearchReplaceMode.Replace
            ? _searchPopup.OpenReplace(initialSearchText)
            : _searchPopup.OpenFind(initialSearchText);

    /// <inheritdoc />
    protected override void WriteTextSegment(CellBuffer buffer, int x, int y, ReadOnlySpan<char> text, Style style, bool isPlaceholder, int textIndexStart, int startColumn)
    {
        if (isPlaceholder || textIndexStart < 0)
        {
            base.WriteTextSegment(buffer, x, y, text, style, isPlaceholder, textIndexStart, startColumn);
            return;
        }

        var line = TryGetVisibleLineForScreenY(y, out var visibleLine)
            ? visibleLine
            : default;
        if (line.LineLength <= 0 || !_lineHighlightCache.TryGetValue(line.LineIndex, out var cacheEntry) || cacheEntry.Runs.Length == 0)
        {
            base.WriteTextSegment(buffer, x, y, text, style, isPlaceholder, textIndexStart, startColumn);
            return;
        }

        var runs = cacheEntry.Runs;

        var lineTextStart = line.LineStart + line.SegmentStart;
        var lineRelativeSegmentStart = textIndexStart - lineTextStart + line.SegmentStart;
        if (lineRelativeSegmentStart < 0)
        {
            base.WriteTextSegment(buffer, x, y, text, style, isPlaceholder, textIndexStart, startColumn);
            return;
        }

        var segmentStart = lineRelativeSegmentStart;
        var segmentEnd = segmentStart + text.Length;

        var runIndex = FindFirstRunIndex(runs, segmentStart);
        var localIndex = 0;
        var col = startColumn;
        var cellX = x;

        while (localIndex < text.Length)
        {
            if (runIndex >= runs.Length)
            {
                var rest = text.Slice(localIndex);
                base.WriteTextSegment(buffer, cellX, y, rest, style, isPlaceholder, textIndexStart + localIndex, col);
                return;
            }

            var run = runs[runIndex];
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
                base.WriteTextSegment(buffer, cellX, y, slice, style, isPlaceholder, textIndexStart + localIndex, col);
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
            base.WriteTextSegment(buffer, cellX, y, slice2, style | run.Style, isPlaceholder, textIndexStart + localIndex, col);
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

    /// <inheritdoc />
    protected override void OnDocumentChanged(TextDocumentChangedEventArgs e)
    {
        _lastDocumentChange = e;
        InvalidateHighlightCache();
        base.OnDocumentChanged(e);
    }

    partial void OnMinLineNumberDigitsChanged(int value)
    {
        _ = value;
        _lineNumberDigits = 0;
        App?.RequestRender();
    }

    partial void OnShowLineNumbersChanged(bool value)
    {
        _ = value;
        App?.RequestRender();
    }

    partial void OnHighlightCurrentLineChanged(bool value)
    {
        _ = value;
        App?.RequestRender();
    }

    partial void OnHighlighterChanged(Delegator<CodeEditorLineHighlighter> value)
    {
        _ = value;
        InvalidateHighlightCache();
        App?.RequestRender();
    }

    partial void OnSyntaxHighlighterChanged(CodeEditorSyntaxHighlighter? value)
    {
        _ = value;
        _syntaxState = null;
        _pendingSyntaxVersion = -1;
        _asyncBuildVersion = -1;
        InvalidateHighlightCache();
        TriggerSyntaxRefresh();
    }

    private void AttachMargin(CodeEditorMargin margin) => margin.Attach(this);

    private void DetachMargin(CodeEditorMargin margin)
    {
        margin.Detach(this);
        _marginWidths.Remove(margin);
        _marginBounds.Remove(margin);
    }

    private void BuildVisibleLines()
        => BuildVisibleLines(_editorRect, includeScrollOffset: false);

    private void BuildVisibleLines(Rectangle sourceRect, bool includeScrollOffset)
    {
        RefreshVisibleRows();
        _visibleLines.Clear();
        var rows = VisibleRows;
        var viewportHeight = Math.Max(1, sourceRect.Height);
        if (includeScrollOffset && Scroll.ViewportHeight > 0)
        {
            viewportHeight = Scroll.ViewportHeight;
        }

        var maxVisibleRows = Math.Min(rows.Count, viewportHeight);
        var rowOffsetBase = includeScrollOffset ? 0 : Scroll.OffsetY;
        for (var i = 0; i < rows.Count; i++)
        {
            if (i >= maxVisibleRows)
            {
                break;
            }

            var row = rows[i];
            _visibleLines.Add(new CodeEditorVisibleLine(
                row.LineIndex,
                row.LineStart,
                row.LineLength,
                row.RowInLine,
                row.VisualRow - rowOffsetBase,
                sourceRect.Y + i,
                row.SegmentStart,
                row.SegmentLength));
        }

        var digits = Math.Max(1, MinLineNumberDigits);
        for (var i = 0; i < _visibleLines.Count; i++)
        {
            digits = Math.Max(digits, CountDigits(_visibleLines[i].LineIndex + 1));
        }

        _lineNumberDigits = digits;
        _lastMeasuredViewportHeight = viewportHeight;
    }

    private int MeasureMargins(BindableList<CodeEditorMargin> margins, CodeEditorStyle style, Rectangle bounds)
    {
        var width = 0;
        for (var i = 0; i < margins.Count; i++)
        {
            var margin = margins[i];
            var measureContext = new CodeEditorMarginMeasureContext(
                this,
                GetTheme(),
                style,
                _visibleLines,
                bounds,
                HasFocus,
                GetCaretLineIndex(),
                _lineNumberDigits);

            var marginWidth = Math.Max(0, margin.MeasureWidth(measureContext));
            _marginWidths[margin] = marginWidth;
            width += marginWidth;
        }

        return width;
    }

    private void RenderMargins(CellBuffer buffer, Theme theme, CodeEditorStyle style, bool focused)
    {
        _orderedMargins.Clear();

        var x = _leftMarginsRect.X;
        for (var i = 0; i < _leftMargins.Count; i++)
        {
            var margin = _leftMargins[i];
            var width = _marginWidths.GetValueOrDefault(margin);
            if (width <= 0)
            {
                continue;
            }

            var rect = new Rectangle(x, _leftMarginsRect.Y, width, _leftMarginsRect.Height);
            _marginBounds[margin] = rect;
            _orderedMargins.Add((margin, rect));
            RenderMargin(buffer, theme, style, focused, margin, rect);
            x += width;
        }

        x = _editorRect.Right;
        for (var i = 0; i < _rightMargins.Count; i++)
        {
            var margin = _rightMargins[i];
            var width = _marginWidths.GetValueOrDefault(margin);
            if (width <= 0)
            {
                continue;
            }

            var rect = new Rectangle(x, _rightMarginsRect.Y, width, _rightMarginsRect.Height);
            _marginBounds[margin] = rect;
            _orderedMargins.Add((margin, rect));
            RenderMargin(buffer, theme, style, focused, margin, rect);
            x += width;
        }

        if (style.ShowMarginSeparators)
        {
            var separatorStyle = style.MarginSeparatorStyle(theme, focused);
            if (_leftMarginWidth > 0 && _editorRect.X > _contentRect.X)
            {
                DrawVerticalLine(buffer, _editorRect.X - 1, _contentRect.Y, _contentRect.Height, separatorStyle, theme.Lines.Vertical);
            }

            if (_rightMarginWidth > 0 && _editorRect.Right < _contentRect.Right)
            {
                DrawVerticalLine(buffer, _editorRect.Right, _contentRect.Y, _contentRect.Height, separatorStyle, theme.Lines.Vertical);
            }
        }
    }

    private void RenderMargin(CellBuffer buffer, Theme theme, CodeEditorStyle style, bool focused, CodeEditorMargin margin, Rectangle rect)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        buffer.PushClip(rect);
        try
        {
            margin.Render(new CodeEditorMarginRenderContext(
                this,
                buffer,
                theme,
                style,
                _visibleLines,
                rect,
                focused,
                GetCaretLineIndex(),
                _lineNumberDigits));
        }
        finally
        {
            buffer.PopClip();
        }
    }

    private void RenderCurrentLineBackground(CellBuffer buffer, Theme theme, CodeEditorStyle style, bool focused)
    {
        if (!HighlightCurrentLine || _editorRect.Width <= 0 || _editorRect.Height <= 0)
        {
            return;
        }

        var currentLine = GetCaretLineIndex();
        if (currentLine < 0)
        {
            return;
        }

        var currentLineStyle = style.CurrentLineStyle(theme, focused);
        if (currentLineStyle == Style.None)
        {
            return;
        }

        for (var i = 0; i < _visibleLines.Count; i++)
        {
            var visibleLine = _visibleLines[i];
            if (visibleLine.LineIndex != currentLine)
            {
                continue;
            }

            var y = visibleLine.ScreenY;
            for (var x = _editorRect.X; x < _editorRect.Right; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), currentLineStyle);
            }
        }
    }

    private bool RoutePointerPressedToMargins(Input.PointerEventArgs e)
    {
        for (var i = 0; i < _orderedMargins.Count; i++)
        {
            var (margin, bounds) = _orderedMargins[i];
            if (e.UiX < bounds.X || e.UiX >= bounds.Right || e.UiY < bounds.Y || e.UiY >= bounds.Bottom)
            {
                continue;
            }

            var context = new CodeEditorMarginPointerContext(this, e, _visibleLines, bounds, GetCaretLineIndex(), _lineNumberDigits);
            if (margin.OnPointerPressed(context))
            {
                e.Handled = true;
                return true;
            }
        }

        return false;
    }

    private int GetCaretLineIndex()
    {
        var snapshot = TextDocument.CurrentSnapshot;
        if (snapshot.LineCount == 0)
        {
            return 0;
        }

        return snapshot.GetLineIndexFromPosition(Math.Clamp(CaretIndex, 0, snapshot.Length));
    }

    private bool TryGetVisibleLineForScreenY(int y, out CodeEditorVisibleLine visibleLine)
    {
        var index = y - _editorRect.Y;
        if ((uint)index >= (uint)_visibleLines.Count)
        {
            visibleLine = default;
            return false;
        }

        visibleLine = _visibleLines[index];
        return true;
    }

    private void EnsureHighlightRuns(Theme theme)
    {
        _ = Scroll.Version;
        var snapshot = TextDocument.CurrentSnapshot;
        var version = snapshot.Version;
        var highlighter = (CodeEditorLineHighlighter?)Highlighter;
        var syntaxHighlighter = SyntaxHighlighter;
        var caretIndex = CaretIndex;
        var selectionStart = SelectionStart;
        var selectionLength = SelectionLength;

        if (_cachedHighlightSnapshotVersion == version
            && ReferenceEquals(_cachedHighlightTheme, theme)
            && Equals(_cachedHighlighter, highlighter)
            && ReferenceEquals(_cachedSyntaxHighlighter, syntaxHighlighter)
            && _cachedHighlightCaretIndex == caretIndex
            && _cachedHighlightSelectionStart == selectionStart
            && _cachedHighlightSelectionLength == selectionLength)
        {
            return;
        }

        _cachedHighlightSnapshotVersion = version;
        _cachedHighlightTheme = theme;
        _cachedHighlighter = highlighter;
        _cachedSyntaxHighlighter = syntaxHighlighter;
        _cachedHighlightCaretIndex = caretIndex;
        _cachedHighlightSelectionStart = selectionStart;
        _cachedHighlightSelectionLength = selectionLength;
        TriggerSyntaxRefresh();

        for (var i = 0; i < _visibleLines.Count; i++)
        {
            var visible = _visibleLines[i];
            if (_lineHighlightCache.ContainsKey(visible.LineIndex))
            {
                continue;
            }

            var workingRuns = GetOrCreateWorkingRuns(visible.LineIndex);
            workingRuns.Clear();
            if (_syntaxState is not null && syntaxHighlighter is not null)
            {
                syntaxHighlighter.GetLineRuns(
                    _syntaxState,
                    new CodeEditorLineSyntaxRequest(snapshot, theme, visible.LineIndex, visible.LineStart, visible.LineLength, caretIndex, selectionStart, selectionLength),
                    workingRuns);
            }
            else if (highlighter is not null)
            {
                highlighter(new CodeEditorLineHighlightRequest(snapshot, theme, visible.LineIndex, visible.LineStart, visible.LineLength, caretIndex, selectionStart, selectionLength), workingRuns);
            }

            NormalizeHighlightRuns(workingRuns, visible.LineLength);
            _lineHighlightCache[visible.LineIndex] = new LineHighlightCacheEntry(visible.LineStart, visible.LineLength, workingRuns.ToArray());
        }
    }

    private void TriggerSyntaxRefresh()
    {
        var syntaxHighlighter = SyntaxHighlighter;
        if (syntaxHighlighter is null)
        {
            return;
        }

        var snapshot = TextDocument.CurrentSnapshot;
        if (_syntaxState is not null && _syntaxState.SnapshotVersion == snapshot.Version)
        {
            return;
        }

        var theme = GetTheme();
        var buildContext = new CodeEditorSyntaxBuildContext(snapshot, theme, CaretIndex, SelectionStart, SelectionLength);
        if (syntaxHighlighter is IAsyncCodeEditorSyntaxHighlighter asyncHighlighter && CheckAccess())
        {
            _pendingSyntaxVersion = snapshot.Version;
            _ = ApplySyntaxStateAsync(asyncHighlighter, syntaxHighlighter, buildContext, snapshot.Version, _lastDocumentChange, _syntaxState);
            return;
        }

        if (_syntaxState is not null && _lastDocumentChange is not null)
        {
            var startLine = snapshot.GetLineIndexFromPosition(Math.Clamp(_lastDocumentChange.Position, 0, snapshot.Length));
            var endPosition = Math.Min(snapshot.Length, _lastDocumentChange.Position + _lastDocumentChange.InsertedLength);
            var endLine = snapshot.GetLineIndexFromPosition(endPosition);
            _syntaxState = syntaxHighlighter.Update(_syntaxState, new CodeEditorSyntaxUpdateContext(snapshot, theme, _lastDocumentChange, startLine, endLine, CaretIndex, SelectionStart, SelectionLength));
            _lastDocumentChange = null;
        }
        else
        {
            _syntaxState = syntaxHighlighter.Build(buildContext);
        }
    }

    private async Task ApplySyntaxStateAsync(
        IAsyncCodeEditorSyntaxHighlighter asyncHighlighter,
        CodeEditorSyntaxHighlighter syntaxHighlighter,
        CodeEditorSyntaxBuildContext buildContext,
        int version,
        TextDocumentChangedEventArgs? change,
        CodeEditorSyntaxState? previousState)
    {
        _asyncBuildVersion = version;
        CodeEditorSyntaxState? state = null;
        try
        {
            if (previousState is not null && change is not null)
            {
                var snapshot = buildContext.Snapshot;
                var startLine = snapshot.GetLineIndexFromPosition(Math.Clamp(change.Position, 0, snapshot.Length));
                var endPosition = Math.Min(snapshot.Length, change.Position + change.InsertedLength);
                var endLine = snapshot.GetLineIndexFromPosition(endPosition);
                state = await asyncHighlighter.UpdateAsync(previousState, new CodeEditorSyntaxUpdateContext(snapshot, buildContext.Theme, change, startLine, endLine, buildContext.CaretIndex, buildContext.SelectionStart, buildContext.SelectionLength), _syntaxUpdateCts.Token).ConfigureAwait(false);
            }
            else
            {
                state = await asyncHighlighter.BuildAsync(buildContext, _syntaxUpdateCts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (state is null)
        {
            return;
        }

        await Dispatcher.InvokeAsync(() =>
        {
            if (TextDocument.CurrentSnapshot.Version != version || _asyncBuildVersion != version)
            {
                return;
            }

            _syntaxState = state;
            _lastDocumentChange = null;
            InvalidateHighlightCache();
            App?.RequestRender();
        }).ConfigureAwait(false);
    }

    private void InvalidateHighlightCache()
    {
        _cachedHighlightSnapshotVersion = -1;
        _cachedHighlightTheme = null;
        _cachedHighlighter = null;
        _cachedSyntaxHighlighter = null;
        _lineHighlightCache.Clear();
    }

    private List<StyledRun> GetOrCreateWorkingRuns(int lineIndex)
    {
        if (_workingHighlightRunsByLine.TryGetValue(lineIndex, out var list))
        {
            return list;
        }

        list = new List<StyledRun>(16);
        _workingHighlightRunsByLine.Add(lineIndex, list);
        return list;
    }

    private void NormalizeHighlightRuns(List<StyledRun> workingRuns, int textLength)
    {
        if (workingRuns.Count == 0 || textLength <= 0)
        {
            return;
        }

        var boundaries = _highlightBoundaryPoints;
        boundaries.Clear();
        boundaries.EnsureCapacity(workingRuns.Count * 2 + 2);
        boundaries.Add(0);
        boundaries.Add(textLength);

        for (var i = 0; i < workingRuns.Count; i++)
        {
            var run = workingRuns[i];
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
            for (var j = 0; j < workingRuns.Count; j++)
            {
                var run = workingRuns[j];
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

        workingRuns.Clear();
        workingRuns.AddRange(normalized);
    }

    private static int FindFirstRunIndex(IReadOnlyList<StyledRun> runs, int index)
    {
        var lo = 0;
        var hi = runs.Count - 1;
        var result = runs.Count;
        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) / 2);
            var run = runs[mid];
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

    private static void FillRect(CellBuffer buffer, Rectangle rect, Style style)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        for (var y = rect.Y; y < rect.Bottom; y++)
        {
            for (var x = rect.X; x < rect.Right; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), style);
            }
        }
    }

    private static void DrawVerticalLine(CellBuffer buffer, int x, int y, int height, Style style, Rune glyph)
    {
        if (height <= 0)
        {
            return;
        }

        for (var row = 0; row < height; row++)
        {
            buffer.SetCell(x, y + row, glyph, style);
        }
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

    private static int CountDigits(int value)
    {
        value = Math.Abs(value);
        if (value < 10) return 1;
        if (value < 100) return 2;
        if (value < 1000) return 3;
        if (value < 10000) return 4;
        var digits = 1;
        while (value >= 10)
        {
            value /= 10;
            digits++;
        }

        return digits;
    }

    /// <summary>
    /// Creates a simple built-in diff indicator margin using per-line glyph callbacks.
    /// </summary>
    /// <param name="glyphProvider">Provides a glyph for a logical line, or <see langword="null"/> for no marker.</param>
    /// <param name="styleProvider">Optionally provides a style for the marker glyph.</param>
    /// <returns>A margin instance that renders one marker cell on the left side.</returns>
    public static CodeEditorMargin CreateDiffIndicatorMargin(Func<int, Rune?> glyphProvider, Func<int, Style>? styleProvider = null)
        => new DiffIndicatorMargin(glyphProvider, styleProvider);

    private sealed class DiffIndicatorMargin : CodeEditorMargin
    {
        private readonly Func<int, Rune?> _glyphProvider;
        private readonly Func<int, Style>? _styleProvider;

        public DiffIndicatorMargin(Func<int, Rune?> glyphProvider, Func<int, Style>? styleProvider = null)
        {
            _glyphProvider = glyphProvider ?? throw new ArgumentNullException(nameof(glyphProvider));
            _styleProvider = styleProvider;
        }

        public override CodeEditorMarginSide Side => CodeEditorMarginSide.Left;

        public override int MeasureWidth(in CodeEditorMarginMeasureContext context)
        {
            _ = context;
            return 1;
        }

        public override void Render(in CodeEditorMarginRenderContext context)
        {
            for (var i = 0; i < context.VisibleLines.Count; i++)
            {
                var visible = context.VisibleLines[i];
                if (!visible.IsFirstRowOfLine)
                {
                    continue;
                }

                var glyph = _glyphProvider(visible.LineIndex);
                if (glyph is null)
                {
                    continue;
                }

                var style = _styleProvider?.Invoke(visible.LineIndex) ?? context.Style.LineNumberStyle(context.Theme, context.IsFocused);
                context.Buffer.SetCell(context.Bounds.X, visible.ScreenY, glyph.Value, style);
            }
        }
    }
}
