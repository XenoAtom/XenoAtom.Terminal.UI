// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
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
/// Specifies the text inserted when the Tab key is pressed in a <see cref="CodeEditor"/>.
/// </summary>
public enum CodeEditorIndentationStyle
{
    /// <summary>
    /// Insert spaces. The number of spaces is controlled by <see cref="CodeEditor.IndentationSize"/>.
    /// </summary>
    Spaces,

    /// <summary>
    /// Insert a tab character (<c>\t</c>).
    /// </summary>
    Tabs,
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
/// Represents a visible logical-line range that should be prepared for fast non-blocking syntax rendering.
/// </summary>
public readonly record struct CodeEditorSyntaxVisibleRangeContext(
    ITextSnapshot Snapshot,
    Theme Theme,
    int FirstVisibleLineIndex,
    int LastVisibleLineIndex,
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

    /// <summary>
    /// Gets a compatibility stamp for auxiliary rendering context such as theme-dependent token metadata.
    /// </summary>
    public virtual long CompatibilityStamp => 0;

    /// <summary>
    /// Gets a value indicating whether syntax processing for the current snapshot is complete.
    /// </summary>
    public virtual bool IsComplete => true;
}

/// <summary>
/// Internal contract for progressive syntax states that advance exact token coverage monotonically by logical line.
/// </summary>
internal interface IProgressiveCodeEditorSyntaxState
{
    /// <summary>
    /// Gets the number of logical lines for which exact syntax state is currently available from the start of the document.
    /// </summary>
    int CompletedLineCount { get; }
}

/// <summary>
/// Internal contract for syntax states that can report whether a logical line already has usable syntax coverage,
/// even if that line produces no styled runs because it only contains punctuation/default-colored tokens.
/// </summary>
internal interface ICodeEditorSyntaxCoverageState
{
    /// <summary>
    /// Returns <see langword="true"/> when the specified logical line has syntax information available for rendering.
    /// </summary>
    bool HasLineCoverage(int lineIndex);
}

/// <summary>
/// Base class for advanced code-editor syntax highlighters.
/// </summary>
public abstract class CodeEditorSyntaxHighlighter
{
    /// <summary>
    /// Gets a value indicating whether line syntax runs can change when only the caret or selection moves without a
    /// document/theme/syntax-state change.
    /// </summary>
    public virtual bool DependsOnCaretOrSelection => true;

    /// <summary>
    /// Gets a compatibility stamp for syntax state created under the specified theme.
    /// </summary>
    public virtual long GetCompatibilityStamp(Theme theme)
    {
        _ = theme;
        return 0;
    }

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

    /// <summary>
    /// Prepares a visible logical-line range for fast non-blocking rendering.
    /// </summary>
    ValueTask<CodeEditorSyntaxState> PrepareVisibleRangeAsync(CodeEditorSyntaxState state, in CodeEditorSyntaxVisibleRangeContext context, CancellationToken cancellationToken = default)
        => new(state);
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
    private readonly CodeEditorGoToLinePopup? _goToLinePopup;
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
    private readonly List<StyledRun> _composedHighlightRuns = new(64);

    private Rectangle _contentRect;
    private Rectangle _editorRect;
    private Rectangle _leftMarginsRect;
    private Rectangle _rightMarginsRect;
    private int _leftMarginWidth;
    private int _rightMarginWidth;
    private int _lineNumberDigits;
    private int _cachedHighlightSnapshotVersion = -1;
    private Theme? _cachedHighlightTheme;
    private CodeEditorLineHighlighter? _cachedHighlighter;
    private CodeEditorSyntaxHighlighter? _cachedSyntaxHighlighter;
    private int _cachedHighlightCaretIndex;
    private int _cachedHighlightSelectionStart;
    private int _cachedHighlightSelectionLength;
    private int _cachedHighlightScrollOffsetY = -1;
    private int _cachedHighlightVisibleLineCount = -1;
    private int _cachedFirstVisibleLineIndex = -1;
    private int _cachedLastVisibleLineIndex = -1;
    private int _cachedSyntaxVisualVersion = -1;
    private SearchQuery _cachedSearchQuery;
    private int _cachedSearchActiveMatchIndex = -1;
    private int _cachedSearchMatchCount = -1;
    private TextDocumentChangedEventArgs? _lastDocumentChange;
    private CodeEditorSyntaxState? _syntaxState;
    private int _syntaxRequestId;
    private int _pendingSyntaxSnapshotVersion = -1;
    private long _pendingSyntaxCompatibilityStamp;
    private CancellationTokenSource? _syntaxUpdateCts;
    private int _visibleSyntaxRequestId;
    private int _pendingVisibleSyntaxSnapshotVersion = -1;
    private long _pendingVisibleSyntaxCompatibilityStamp;
    private int _pendingVisibleSyntaxFirstLine = -1;
    private int _pendingVisibleSyntaxLastLine = -1;
    private CancellationTokenSource? _visibleSyntaxUpdateCts;
    private int _lastVisibleLineRequestCount;
    private int _preservedHighlightCacheSnapshotVersion = -1;
    private Rectangle _lastArrangeRect;
    private bool _hasArrangedOnce;
    private int _line = 1;
    private int _column = 1;
    private string _indentationText = "    ";

    private sealed record LineHighlightCacheEntry(int LineStart, int LineLength, StyledRun[] Runs);

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeEditor"/> class.
    /// </summary>
    public CodeEditor() : this((CodeEditorConfig?)null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeEditor"/> class with optional init-time configuration.
    /// </summary>
    /// <param name="config">
    /// Optional immutable configuration used to register commands and optional popup features.
    /// When <see langword="null"/>, <see cref="CodeEditorConfig.Default"/> is used.
    /// </param>
    public CodeEditor(CodeEditorConfig? config)
    {
        var resolvedConfig = config ?? CodeEditorConfig.Default;
        var goToLineConfig = resolvedConfig.GoToLine;

        this.AcceptTab(true);
        this.WordWrap(true);
        this.HorizontalAlignment(Align.Stretch);
        this.VerticalAlignment(Align.Stretch);
        this.ShowLineNumbers(true);
        this.HighlightCurrentLine(true);
        this.MinLineNumberDigits(2);
        this.IndentationSize(4);

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

        if (goToLineConfig.IsEnabled)
        {
            _goToLinePopup = new CodeEditorGoToLinePopup(this, goToLineConfig);

            AddCommand(new Command
            {
                Id = "TextEditor.GoToLine",
                LabelMarkup = goToLineConfig.Command.LabelMarkup,
                DescriptionMarkup = goToLineConfig.Command.DescriptionMarkup,
                Gesture = goToLineConfig.Command.Gesture,
                Importance = CommandImportance.Secondary,
                Presentation = CommandPresentation.CommandBar,
                Execute = static v => _ = ((CodeEditor)v).OpenGoToLine(),
            });
        }

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
    public CodeEditor(string? text) : this(text, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeEditor"/> class with initial text and optional init-time configuration.
    /// </summary>
    /// <param name="text">The initial text content.</param>
    /// <param name="config">The optional init-time configuration.</param>
    public CodeEditor(string? text, CodeEditorConfig? config) : this(config)
    {
        this.Text(text);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeEditor"/> class with dynamic text.
    /// </summary>
    public CodeEditor(Func<string?> text) : this(text, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeEditor"/> class with dynamic text and optional init-time configuration.
    /// </summary>
    /// <param name="text">A delegate returning the current text content.</param>
    /// <param name="config">The optional init-time configuration.</param>
    public CodeEditor(Func<string?> text, CodeEditorConfig? config) : this(config)
    {
        this.Text(text);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeEditor"/> class with bound text.
    /// </summary>
    public CodeEditor(Binding<string?> text) : this(text, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeEditor"/> class with bound text and optional init-time configuration.
    /// </summary>
    /// <param name="text">A binding that supplies the text content.</param>
    /// <param name="config">The optional init-time configuration.</param>
    public CodeEditor(Binding<string?> text, CodeEditorConfig? config) : this(config)
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
    /// Gets or sets whether pressing Tab inserts spaces or a tab character.
    /// </summary>
    /// <remarks>
    /// The default is <see cref="CodeEditorIndentationStyle.Spaces"/> so Tab inserts spaces instead of a real tab character.
    /// </remarks>
    [Bindable]
    public partial CodeEditorIndentationStyle IndentationStyle { get; set; }

    /// <summary>
    /// Gets or sets the indentation size, in spaces.
    /// </summary>
    /// <remarks>
    /// When <see cref="IndentationStyle"/> is <see cref="CodeEditorIndentationStyle.Spaces"/>, this controls how many spaces
    /// Tab inserts. When <see cref="IndentationStyle"/> is <see cref="CodeEditorIndentationStyle.Tabs"/>, this controls the
    /// rendered width of tab characters. Values less than 1 are treated as 1.
    /// </remarks>
    [Bindable]
    public partial int IndentationSize { get; set; }

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

    [Bindable]
    private partial int SyntaxVisualVersion { get; set; }

    /// <summary>
    /// Gets the current caret line using a one-based line number suitable for display in editor status bars.
    /// </summary>
    [Bindable]
    public int Line
    {
        get => BindingManager.Current.GetValue(this, ref _line, __Line__BindingAccessor.Instance);
        private set => BindingManager.Current.SetValue(this, ref _line, value, __Line__BindingAccessor.Instance);
    }

    /// <summary>
    /// Gets the current caret column using a one-based column number suitable for display in editor status bars.
    /// </summary>
    [Bindable]
    public int Column
    {
        get => BindingManager.Current.GetValue(this, ref _column, __Column__BindingAccessor.Instance);
        private set => BindingManager.Current.SetValue(this, ref _column, value, __Column__BindingAccessor.Instance);
    }

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
    protected override int TabSize => Math.Max(1, IndentationSize);

    /// <inheritdoc />
    protected override string TabText => IndentationStyle == CodeEditorIndentationStyle.Tabs ? "\t" : _indentationText;

    /// <inheritdoc />
    protected override bool ShowPlaceholderWhenUnfocusedOnly => false;

    /// <summary>
    /// Moves the caret to the specified one-based line number.
    /// </summary>
    /// <remarks>
    /// The requested line is clamped to the current document range. The caret moves to column 1 of the resolved line.
    /// </remarks>
    /// <param name="line">The one-based line number to navigate to.</param>
    public void GoToLine(int line)
        => GoToLine(line, 1);

    /// <summary>
    /// Moves the caret to the specified one-based column on the current caret line.
    /// </summary>
    /// <remarks>
    /// The requested column is clamped to the current logical line length.
    /// </remarks>
    /// <param name="column">The one-based column number to navigate to.</param>
    public void GoToColumn(int column)
    {
        var snapshot = TextDocument.CurrentSnapshot;
        if (snapshot.LineCount == 0)
        {
            CaretIndex = 0;
            return;
        }

        var line = snapshot.GetLineIndexFromPosition(Math.Clamp(CaretIndex, 0, snapshot.Length)) + 1;
        GoToLine(line, column);
    }

    /// <summary>
    /// Moves the caret to the specified one-based line and column.
    /// </summary>
    /// <remarks>
    /// The requested line and column are clamped to the current document bounds.
    /// </remarks>
    /// <param name="line">The one-based line number to navigate to.</param>
    /// <param name="column">The one-based column number to navigate to.</param>
    public void GoToLine(int line, int column)
    {
        var snapshot = TextDocument.CurrentSnapshot;
        if (snapshot.LineCount == 0)
        {
            CaretIndex = 0;
            return;
        }

        var lineIndex = Math.Clamp(line - 1, 0, snapshot.LineCount - 1);
        var textLine = snapshot.GetLine(lineIndex);
        var columnIndex = Math.Clamp(column - 1, 0, textLine.Length);
        CaretIndex = textLine.Start + columnIndex;
    }

    /// <summary>
    /// Moves the caret to the specified zero-based UTF-16 document position.
    /// </summary>
    /// <remarks>
    /// The requested position is clamped to the current document length.
    /// </remarks>
    /// <param name="position">The zero-based UTF-16 document position.</param>
    public void GoToPosition(int position) => CaretIndex = position;

    /// <summary>
    /// Moves the caret to the specified zero-based UTF-16 document position.
    /// </summary>
    /// <param name="position">The document position to navigate to.</param>
    public void GoToPosition(TextPosition position) => GoToPosition(position.Index);

    /// <summary>
    /// Attempts to open the Go To Line popup for this editor.
    /// </summary>
    /// <returns><see langword="true"/> if the popup was opened; otherwise <see langword="false"/>.</returns>
    public bool OpenGoToLine() => _goToLinePopup?.Open() ?? false;

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
        var preserveScrollOffset = _hasArrangedOnce && _lastArrangeRect.Equals(finalRect);

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

        UpdateEditorLayoutPreservingScrollOffset(_editorRect, preserveScrollOffset);
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
            UpdateEditorLayoutPreservingScrollOffset(_editorRect, preserveScrollOffset);
            BuildVisibleLines(_editorRect, includeScrollOffset: false);
            _ = MeasureMargins(_leftMargins, style, _contentRect);
            _ = MeasureMargins(_rightMargins, style, _contentRect);
        }

        _searchPopup.ArrangeWithin(_editorRect);
        _goToLinePopup?.ArrangeWithin(_editorRect);
        _lastArrangeRect = finalRect;
        _hasArrangedOnce = true;
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
        var preservedHighlightCache = TryPreserveVisibleHighlightCacheForDocumentChange(e);
        InvalidateHighlightCache(preserveLineCache: preservedHighlightCache);
        if (preservedHighlightCache)
        {
            _preservedHighlightCacheSnapshotVersion = e.NewVersion;
        }

        base.OnDocumentChanged(e);
    }

    /// <inheritdoc />
    protected override void OnEditorStateChanged()
    {
        SyncCaretLocation();
        base.OnEditorStateChanged();
    }

    partial void OnMinLineNumberDigitsChanged(int value)
    {
        _ = value;
        _lineNumberDigits = 0;
    }

    partial void OnShowLineNumbersChanged(bool value)
    {
        _ = value;
    }

    partial void OnHighlightCurrentLineChanged(bool value)
    {
        _ = value;
    }

    partial void OnIndentationStyleChanged(CodeEditorIndentationStyle value)
    {
        _ = value;
    }

    partial void OnIndentationSizeChanged(int value)
    {
        _indentationText = new string(' ', Math.Max(1, value));
    }

    partial void OnHighlighterChanged(Delegator<CodeEditorLineHighlighter> value)
    {
        _ = value;
        InvalidateHighlightCache();
    }

    partial void OnSyntaxHighlighterChanged(CodeEditorSyntaxHighlighter? value)
    {
        _ = value;
        CancelPendingSyntaxWork();
        CancelPendingVisibleSyntaxWork();
        _syntaxState = null;
        _lastDocumentChange = null;
        InvalidateHighlightCache();
        TriggerSyntaxRefresh();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromApp(TerminalApp app)
    {
        CancelPendingSyntaxWork();
        CancelPendingVisibleSyntaxWork();
        base.OnDetachedFromApp(app);
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

    private void UpdateEditorLayoutPreservingScrollOffset(Rectangle editorRect, bool preserveScrollOffset)
    {
        var previousOffsetX = Scroll.OffsetX;
        var previousOffsetY = Scroll.OffsetY;

        UpdateEditorLayout(editorRect);
        if (!preserveScrollOffset)
        {
            return;
        }

        var maxOffsetX = Math.Max(0, Scroll.ExtentWidth - Scroll.ViewportWidth);
        var maxOffsetY = Math.Max(0, Scroll.ExtentHeight - Scroll.ViewportHeight);
        var targetOffsetX = Math.Clamp(previousOffsetX, 0, maxOffsetX);
        var targetOffsetY = Math.Clamp(previousOffsetY, 0, maxOffsetY);
        if (targetOffsetX == Scroll.OffsetX && targetOffsetY == Scroll.OffsetY)
        {
            return;
        }

        Scroll.SetOffset(targetOffsetX, targetOffsetY);
        RefreshVisibleRows();
    }

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

    private void SyncCaretLocation()
    {
        var snapshot = TextDocument.CurrentSnapshot;
        if (snapshot.LineCount == 0)
        {
            Line = 1;
            Column = 1;
            return;
        }

        var caretIndex = Math.Clamp(CaretIndex, 0, snapshot.Length);
        var lineIndex = snapshot.GetLineIndexFromPosition(caretIndex);
        var textLine = snapshot.GetLine(lineIndex);

        Line = lineIndex + 1;
        Column = (caretIndex - textLine.Start) + 1;
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
        var searchState = GetSearchState();
        var syntaxVisualVersion = SyntaxVisualVersion;
        var dependsOnCaretOrSelection = highlighter is not null || syntaxHighlighter?.DependsOnCaretOrSelection != false;
        var syntaxCompatibilityStamp = syntaxHighlighter?.GetCompatibilityStamp(theme) ?? 0;
        var renderableSyntaxState = GetRenderableSyntaxState(version, syntaxHighlighter, syntaxCompatibilityStamp);

        var snapshotChanged = _cachedHighlightSnapshotVersion != version;
        var themeChanged = !ReferenceEquals(_cachedHighlightTheme, theme);
        var highlighterChanged = !Equals(_cachedHighlighter, highlighter);
        var syntaxHighlighterChanged = !ReferenceEquals(_cachedSyntaxHighlighter, syntaxHighlighter);
        var syntaxVisualChanged = _cachedSyntaxVisualVersion != syntaxVisualVersion;
        var caretChanged = dependsOnCaretOrSelection && _cachedHighlightCaretIndex != caretIndex;
        var selectionStartChanged = dependsOnCaretOrSelection && _cachedHighlightSelectionStart != selectionStart;
        var selectionLengthChanged = dependsOnCaretOrSelection && _cachedHighlightSelectionLength != selectionLength;
        var searchQueryChanged = !_cachedSearchQuery.Equals(searchState.Query);
        var searchActiveMatchChanged = _cachedSearchActiveMatchIndex != searchState.ActiveMatchIndex;
        var searchMatchCountChanged = _cachedSearchMatchCount != searchState.Matches.Count;
        var requiresMetadataRefresh = snapshotChanged
            || themeChanged
            || highlighterChanged
            || syntaxHighlighterChanged
            || syntaxVisualChanged
            || caretChanged
            || selectionStartChanged
            || selectionLengthChanged
            || searchQueryChanged
            || searchActiveMatchChanged
            || searchMatchCountChanged;

        var firstVisibleLineIndex = _visibleLines.Count > 0 ? _visibleLines[0].LineIndex : -1;
        var lastVisibleLineIndex = _visibleLines.Count > 0 ? _visibleLines[^1].LineIndex : -1;
        var visibleLinesChanged = _cachedHighlightScrollOffsetY != Scroll.OffsetY
            || _cachedHighlightVisibleLineCount != _visibleLines.Count
            || _cachedFirstVisibleLineIndex != firstVisibleLineIndex
            || _cachedLastVisibleLineIndex != lastVisibleLineIndex;

        if (requiresMetadataRefresh)
        {
            _cachedHighlightSnapshotVersion = version;
            _cachedHighlightTheme = theme;
            _cachedHighlighter = highlighter;
            _cachedSyntaxHighlighter = syntaxHighlighter;
            _cachedSyntaxVisualVersion = syntaxVisualVersion;
            _cachedHighlightCaretIndex = caretIndex;
            _cachedHighlightSelectionStart = selectionStart;
            _cachedHighlightSelectionLength = selectionLength;
            _cachedSearchQuery = searchState.Query;
            _cachedSearchActiveMatchIndex = searchState.ActiveMatchIndex;
            _cachedSearchMatchCount = searchState.Matches.Count;
            var preserveLineCache = snapshotChanged
                && !themeChanged
                && !highlighterChanged
                && !syntaxHighlighterChanged
                && !syntaxVisualChanged
                && !caretChanged
                && !selectionStartChanged
                && !selectionLengthChanged
                && !searchQueryChanged
                && !searchActiveMatchChanged
                && !searchMatchCountChanged
                && _preservedHighlightCacheSnapshotVersion == version;
            if (!preserveLineCache)
            {
                _lineHighlightCache.Clear();
            }

            _preservedHighlightCacheSnapshotVersion = -1;
            EnsureSyntaxRefresh();
            renderableSyntaxState = GetRenderableSyntaxState(version, syntaxHighlighter, syntaxCompatibilityStamp);
        }

        _cachedHighlightScrollOffsetY = Scroll.OffsetY;
        _cachedHighlightVisibleLineCount = _visibleLines.Count;
        _cachedFirstVisibleLineIndex = firstVisibleLineIndex;
        _cachedLastVisibleLineIndex = lastVisibleLineIndex;

        if (!requiresMetadataRefresh && !visibleLinesChanged)
        {
            return;
        }

        var visibleLineIndices = GetVisibleLogicalLineIndices();
        PruneHighlightCache(visibleLineIndices);
        _lastVisibleLineRequestCount = 0;
        var missingVisibleSyntax = false;

        for (var i = 0; i < visibleLineIndices.Count; i++)
        {
            var lineIndex = visibleLineIndices[i];
            var visible = GetVisibleLineForLogicalLine(lineIndex);

            if (_lineHighlightCache.TryGetValue(lineIndex, out var cachedEntry)
                && cachedEntry.LineStart == visible.LineStart
                && cachedEntry.LineLength == visible.LineLength)
            {
                continue;
            }

            var workingRuns = GetOrCreateWorkingRuns(lineIndex);
            workingRuns.Clear();
            if (renderableSyntaxState is not null
                && syntaxHighlighter is not null)
            {
                syntaxHighlighter.GetLineRuns(
                    renderableSyntaxState,
                    new CodeEditorLineSyntaxRequest(snapshot, theme, visible.LineIndex, visible.LineStart, visible.LineLength, caretIndex, selectionStart, selectionLength),
                    workingRuns);
            }
            else if (highlighter is not null)
            {
                highlighter(new CodeEditorLineHighlightRequest(snapshot, theme, visible.LineIndex, visible.LineStart, visible.LineLength, caretIndex, selectionStart, selectionLength), workingRuns);
            }

            if (syntaxHighlighter is not null && !HasUsableSyntaxCoverage(renderableSyntaxState, lineIndex))
            {
                missingVisibleSyntax = true;
            }

            _lastVisibleLineRequestCount++;
            ComposeSearchHighlightRuns(workingRuns, theme, searchState, visible.LineStart, visible.LineLength);
            NormalizeHighlightRuns(workingRuns, visible.LineLength);
            _lineHighlightCache[lineIndex] = new LineHighlightCacheEntry(visible.LineStart, visible.LineLength, workingRuns.ToArray());
        }

        if (missingVisibleSyntax
            && syntaxHighlighter is IAsyncCodeEditorSyntaxHighlighter asyncHighlighter
            && _syntaxState is ICodeEditorSyntaxCoverageState)
        {
            ScheduleAsyncVisibleSyntaxRefresh(asyncHighlighter, syntaxHighlighter, snapshot, theme);
        }
    }

    private void TriggerSyntaxRefresh()
    {
        var syntaxHighlighter = SyntaxHighlighter;
        if (syntaxHighlighter is null)
        {
            CancelPendingSyntaxWork();
            return;
        }

        var snapshot = TextDocument.CurrentSnapshot;
        var theme = GetTheme();
        var compatibilityStamp = syntaxHighlighter.GetCompatibilityStamp(theme);
        if (_syntaxState is not null
            && _syntaxState.SnapshotVersion == snapshot.Version
            && _syntaxState.CompatibilityStamp == compatibilityStamp
            && _syntaxState.IsComplete)
        {
            return;
        }

        if (_pendingSyntaxSnapshotVersion == snapshot.Version
            && _pendingSyntaxCompatibilityStamp == compatibilityStamp)
        {
            return;
        }

        if (syntaxHighlighter is IAsyncCodeEditorSyntaxHighlighter asyncHighlighter && CheckAccess())
        {
            ScheduleAsyncSyntaxRefresh(asyncHighlighter, syntaxHighlighter, snapshot, theme, compatibilityStamp);
            return;
        }

        CancelPendingSyntaxWork();
        CancelPendingVisibleSyntaxWork();

        var buildContext = new CodeEditorSyntaxBuildContext(snapshot, theme, CaretIndex, SelectionStart, SelectionLength);
        var updateContext = CreateUpdateContext(snapshot, theme, _lastDocumentChange, CaretIndex, SelectionStart, SelectionLength);
        if (_syntaxState is not null
            && _syntaxState.SnapshotVersion == snapshot.Version
            && _syntaxState.CompatibilityStamp == compatibilityStamp)
        {
            _syntaxState = syntaxHighlighter.Update(_syntaxState, updateContext);
        }
        else if (_syntaxState is not null && _lastDocumentChange is not null)
        {
            _syntaxState = syntaxHighlighter.Update(_syntaxState, updateContext);
        }
        else
        {
            _syntaxState = syntaxHighlighter.Build(buildContext);
        }

        _lastDocumentChange = null;
    }

    private void EnsureSyntaxRefresh()
    {
        if (!CheckAccess())
        {
            Dispatcher.InvokeAsync(TriggerSyntaxRefresh).GetAwaiter().GetResult();
            return;
        }

        TriggerSyntaxRefresh();
    }

    private void ScheduleAsyncSyntaxRefresh(
        IAsyncCodeEditorSyntaxHighlighter asyncHighlighter,
        CodeEditorSyntaxHighlighter syntaxHighlighter,
        ITextSnapshot snapshot,
        Theme theme,
        long compatibilityStamp)
    {
        var requestId = ++_syntaxRequestId;
        var buildContext = new CodeEditorSyntaxBuildContext(snapshot, theme, CaretIndex, SelectionStart, SelectionLength);
        var previousState = _syntaxState;
        var change = _lastDocumentChange;
        var canReuseState = previousState is not null
            && previousState.SnapshotVersion == snapshot.Version
            && previousState.CompatibilityStamp == compatibilityStamp;
        var useUpdate = previousState is not null && (change is not null || canReuseState);
        var previousCompletedLineCount = previousState is IProgressiveCodeEditorSyntaxState progressiveState ? progressiveState.CompletedLineCount : -1;
        var invalidatesVisiblePreparation = change is not null || !canReuseState;
        var updateContext = CreateUpdateContext(snapshot, theme, change, buildContext.CaretIndex, buildContext.SelectionStart, buildContext.SelectionLength);

        CancelPendingSyntaxWork();
        if (invalidatesVisiblePreparation)
        {
            CancelPendingVisibleSyntaxWork();
        }
        var cts = new CancellationTokenSource();
        _syntaxUpdateCts = cts;
        _pendingSyntaxSnapshotVersion = snapshot.Version;
        _pendingSyntaxCompatibilityStamp = compatibilityStamp;
        var operation = useUpdate && previousState is not null
            ? asyncHighlighter.UpdateAsync(previousState, updateContext, cts.Token)
            : asyncHighlighter.BuildAsync(buildContext, cts.Token);
        var task = operation.AsTask();

        if (TryApplyCompletedAsyncSyntaxOperation(task, syntaxHighlighter, buildContext, compatibilityStamp, requestId, cts, previousState, previousCompletedLineCount))
        {
            return;
        }

        task.ContinueWith(
            task => HandleAsyncSyntaxCompletion(task, syntaxHighlighter, buildContext, compatibilityStamp, requestId, cts, previousState, previousCompletedLineCount),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private void ScheduleAsyncVisibleSyntaxRefresh(
        IAsyncCodeEditorSyntaxHighlighter asyncHighlighter,
        CodeEditorSyntaxHighlighter syntaxHighlighter,
        ITextSnapshot snapshot,
        Theme theme)
    {
        if (_visibleLines.Count == 0 || _syntaxState is null)
        {
            return;
        }

        var firstVisibleLineIndex = _visibleLines[0].LineIndex;
        var lastVisibleLineIndex = _visibleLines[^1].LineIndex;
        var compatibilityStamp = syntaxHighlighter.GetCompatibilityStamp(theme);
        if (_pendingVisibleSyntaxSnapshotVersion == snapshot.Version
            && _pendingVisibleSyntaxCompatibilityStamp == compatibilityStamp
            && _pendingVisibleSyntaxFirstLine == firstVisibleLineIndex
            && _pendingVisibleSyntaxLastLine == lastVisibleLineIndex)
        {
            return;
        }

        var requestId = ++_visibleSyntaxRequestId;
        var context = new CodeEditorSyntaxVisibleRangeContext(
            snapshot,
            theme,
            firstVisibleLineIndex,
            lastVisibleLineIndex,
            CaretIndex,
            SelectionStart,
            SelectionLength);

        CancelPendingVisibleSyntaxWork();
        var cts = new CancellationTokenSource();
        _visibleSyntaxUpdateCts = cts;
        _pendingVisibleSyntaxSnapshotVersion = snapshot.Version;
        _pendingVisibleSyntaxCompatibilityStamp = compatibilityStamp;
        _pendingVisibleSyntaxFirstLine = firstVisibleLineIndex;
        _pendingVisibleSyntaxLastLine = lastVisibleLineIndex;
        var task = asyncHighlighter.PrepareVisibleRangeAsync(_syntaxState, context, cts.Token).AsTask();

        if (TryApplyCompletedVisibleSyntaxOperation(task, syntaxHighlighter, context, compatibilityStamp, requestId, cts))
        {
            return;
        }

        task.ContinueWith(
            task => HandleAsyncVisibleSyntaxCompletion(task, syntaxHighlighter, context, compatibilityStamp, requestId, cts),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private bool TryApplyCompletedAsyncSyntaxOperation(
        Task<CodeEditorSyntaxState> task,
        CodeEditorSyntaxHighlighter syntaxHighlighter,
        CodeEditorSyntaxBuildContext buildContext,
        long compatibilityStamp,
        int requestId,
        CancellationTokenSource cts,
        CodeEditorSyntaxState? previousState,
        int previousCompletedLineCount)
    {
        if (!task.IsCompleted)
        {
            return false;
        }

        try
        {
            if (!task.IsCompletedSuccessfully
                || !ReferenceEquals(_syntaxUpdateCts, cts)
                || cts.IsCancellationRequested
                || !ReferenceEquals(SyntaxHighlighter, syntaxHighlighter)
                || TextDocument.CurrentSnapshot.Version != buildContext.Snapshot.Version
                || syntaxHighlighter.GetCompatibilityStamp(GetTheme()) != compatibilityStamp
                || requestId < _syntaxRequestId)
            {
                CleanupFailedAsyncSyntaxOperation(cts);
                return true;
            }

            _syntaxState = task.Result;
            _lastDocumentChange = null;
            _pendingSyntaxSnapshotVersion = -1;
            _pendingSyntaxCompatibilityStamp = 0;
            _syntaxUpdateCts = null;
            cts.Dispose();
            if (ShouldRefreshVisibleSyntaxAfterAsyncCompletion(previousState, _syntaxState, previousCompletedLineCount))
            {
                NotifySyntaxVisualStateChanged();
            }

            if (!_syntaxState.IsComplete)
            {
                App?.Post(TriggerSyntaxRefresh);
            }

            return true;
        }
        catch (Exception ex)
        {
            _ = ex;
            _pendingSyntaxSnapshotVersion = -1;
            _pendingSyntaxCompatibilityStamp = 0;
            if (ReferenceEquals(_syntaxUpdateCts, cts))
            {
                _syntaxUpdateCts = null;
            }

            cts.Dispose();
            return true;
        }
    }

    private void HandleAsyncSyntaxCompletion(
        Task<CodeEditorSyntaxState> task,
        CodeEditorSyntaxHighlighter syntaxHighlighter,
        CodeEditorSyntaxBuildContext buildContext,
        long compatibilityStamp,
        int requestId,
        CancellationTokenSource cts,
        CodeEditorSyntaxState? previousState,
        int previousCompletedLineCount)
    {
        if (task.IsCanceled || cts.IsCancellationRequested)
        {
            App?.Post(() => CleanupFailedAsyncSyntaxOperation(cts));
            return;
        }

        if (task.IsFaulted)
        {
            App?.Post(() => CleanupFailedAsyncSyntaxOperation(cts));
            return;
        }

        CodeEditorSyntaxState? state;
        try
        {
            state = task.Result;
        }
        catch (Exception ex)
        {
            _ = ex;
            return;
        }

        if (state is null)
        {
            return;
        }

        var app = App;
        if (app is null)
        {
            return;
        }

        app.Post(() =>
        {
            if (!ReferenceEquals(_syntaxUpdateCts, cts)
                || cts.IsCancellationRequested
                || !ReferenceEquals(SyntaxHighlighter, syntaxHighlighter)
                || TextDocument.CurrentSnapshot.Version != buildContext.Snapshot.Version
                || syntaxHighlighter.GetCompatibilityStamp(GetTheme()) != compatibilityStamp
                || requestId < _syntaxRequestId)
            {
                return;
            }

            _syntaxState = state;
            _lastDocumentChange = null;
            _pendingSyntaxSnapshotVersion = -1;
            _pendingSyntaxCompatibilityStamp = 0;
            _syntaxUpdateCts = null;
            cts.Dispose();
            if (ShouldRefreshVisibleSyntaxAfterAsyncCompletion(previousState, state, previousCompletedLineCount))
            {
                NotifySyntaxVisualStateChanged();
            }

            if (!state.IsComplete)
            {
                app.Post(TriggerSyntaxRefresh);
            }
        });
    }

    private bool TryApplyCompletedVisibleSyntaxOperation(
        Task<CodeEditorSyntaxState> task,
        CodeEditorSyntaxHighlighter syntaxHighlighter,
        CodeEditorSyntaxVisibleRangeContext context,
        long compatibilityStamp,
        int requestId,
        CancellationTokenSource cts)
    {
        if (!task.IsCompleted)
        {
            return false;
        }

        try
        {
            if (!task.IsCompletedSuccessfully
                || !ReferenceEquals(_visibleSyntaxUpdateCts, cts)
                || cts.IsCancellationRequested
                || !ReferenceEquals(SyntaxHighlighter, syntaxHighlighter)
                || TextDocument.CurrentSnapshot.Version != context.Snapshot.Version
                || syntaxHighlighter.GetCompatibilityStamp(GetTheme()) != compatibilityStamp
                || requestId < _visibleSyntaxRequestId)
            {
                CleanupFailedVisibleSyntaxOperation(cts);
                return true;
            }

            _syntaxState = task.Result;
            _pendingVisibleSyntaxSnapshotVersion = -1;
            _pendingVisibleSyntaxCompatibilityStamp = 0;
            _pendingVisibleSyntaxFirstLine = -1;
            _pendingVisibleSyntaxLastLine = -1;
            _visibleSyntaxUpdateCts = null;
            cts.Dispose();
            NotifySyntaxVisualStateChanged();
            return true;
        }
        catch (Exception ex)
        {
            _ = ex;
            _pendingVisibleSyntaxSnapshotVersion = -1;
            _pendingVisibleSyntaxCompatibilityStamp = 0;
            _pendingVisibleSyntaxFirstLine = -1;
            _pendingVisibleSyntaxLastLine = -1;
            if (ReferenceEquals(_visibleSyntaxUpdateCts, cts))
            {
                _visibleSyntaxUpdateCts = null;
            }

            cts.Dispose();
            return true;
        }
    }

    private void HandleAsyncVisibleSyntaxCompletion(
        Task<CodeEditorSyntaxState> task,
        CodeEditorSyntaxHighlighter syntaxHighlighter,
        CodeEditorSyntaxVisibleRangeContext context,
        long compatibilityStamp,
        int requestId,
        CancellationTokenSource cts)
    {
        if (task.IsCanceled || cts.IsCancellationRequested || task.IsFaulted)
        {
            App?.Post(() => CleanupFailedVisibleSyntaxOperation(cts));
            return;
        }

        CodeEditorSyntaxState? state;
        try
        {
            state = task.Result;
        }
        catch (Exception ex)
        {
            _ = ex;
            App?.Post(() => CleanupFailedVisibleSyntaxOperation(cts));
            return;
        }

        if (state is null)
        {
            return;
        }

        var app = App;
        if (app is null)
        {
            return;
        }

        app.Post(() =>
        {
            if (!ReferenceEquals(_visibleSyntaxUpdateCts, cts)
                || cts.IsCancellationRequested
                || !ReferenceEquals(SyntaxHighlighter, syntaxHighlighter)
                || TextDocument.CurrentSnapshot.Version != context.Snapshot.Version
                || syntaxHighlighter.GetCompatibilityStamp(GetTheme()) != compatibilityStamp
                || requestId < _visibleSyntaxRequestId)
            {
                return;
            }

            _syntaxState = state;
            _pendingVisibleSyntaxSnapshotVersion = -1;
            _pendingVisibleSyntaxCompatibilityStamp = 0;
            _pendingVisibleSyntaxFirstLine = -1;
            _pendingVisibleSyntaxLastLine = -1;
            _visibleSyntaxUpdateCts = null;
            cts.Dispose();
            NotifySyntaxVisualStateChanged();
        });
    }

    private void CleanupFailedAsyncSyntaxOperation(CancellationTokenSource cts)
    {
        _pendingSyntaxSnapshotVersion = -1;
        _pendingSyntaxCompatibilityStamp = 0;
        if (ReferenceEquals(_syntaxUpdateCts, cts))
        {
            _syntaxUpdateCts = null;
        }

        cts.Dispose();
    }

    private void CleanupFailedVisibleSyntaxOperation(CancellationTokenSource cts)
    {
        _pendingVisibleSyntaxSnapshotVersion = -1;
        _pendingVisibleSyntaxCompatibilityStamp = 0;
        _pendingVisibleSyntaxFirstLine = -1;
        _pendingVisibleSyntaxLastLine = -1;
        if (ReferenceEquals(_visibleSyntaxUpdateCts, cts))
        {
            _visibleSyntaxUpdateCts = null;
        }

        cts.Dispose();
    }

    private static CodeEditorSyntaxUpdateContext CreateUpdateContext(ITextSnapshot snapshot, Theme theme, TextDocumentChangedEventArgs? change, int caretIndex, int selectionStart, int selectionLength)
    {
        if (change is null)
        {
            return new CodeEditorSyntaxUpdateContext(snapshot, theme, null, 0, Math.Max(0, snapshot.LineCount - 1), caretIndex, selectionStart, selectionLength);
        }

        var startLine = snapshot.GetLineIndexFromPosition(Math.Clamp(change.Position, 0, snapshot.Length));
        var endPosition = Math.Min(snapshot.Length, change.Position + change.InsertedLength);
        var endLine = snapshot.GetLineIndexFromPosition(endPosition);
        return new CodeEditorSyntaxUpdateContext(snapshot, theme, change, startLine, endLine, caretIndex, selectionStart, selectionLength);
    }

    private void CancelPendingSyntaxWork()
    {
        var cts = _syntaxUpdateCts;
        _syntaxUpdateCts = null;
        _pendingSyntaxSnapshotVersion = -1;
        _pendingSyntaxCompatibilityStamp = 0;
        if (cts is null)
        {
            return;
        }

        try
        {
            cts.Cancel();
        }
        finally
        {
            cts.Dispose();
        }
    }

    private void CancelPendingVisibleSyntaxWork()
    {
        var cts = _visibleSyntaxUpdateCts;
        _visibleSyntaxUpdateCts = null;
        _pendingVisibleSyntaxSnapshotVersion = -1;
        _pendingVisibleSyntaxCompatibilityStamp = 0;
        _pendingVisibleSyntaxFirstLine = -1;
        _pendingVisibleSyntaxLastLine = -1;
        if (cts is null)
        {
            return;
        }

        try
        {
            cts.Cancel();
        }
        finally
        {
            cts.Dispose();
        }
    }

    private bool TryPreserveVisibleHighlightCacheForDocumentChange(TextDocumentChangedEventArgs change)
    {
        ArgumentNullException.ThrowIfNull(change);

        if (_lineHighlightCache.Count == 0)
        {
            return false;
        }

        if (!Highlighter.IsEmpty)
        {
            return false;
        }

        var syntaxHighlighter = SyntaxHighlighter;
        if (syntaxHighlighter is null || syntaxHighlighter.DependsOnCaretOrSelection)
        {
            return false;
        }

        var searchState = GetSearchState();
        if (!string.IsNullOrEmpty(searchState.Query.Text) || searchState.Matches.Count != 0)
        {
            return false;
        }

        var snapshot = TextDocument.CurrentSnapshot;
        if (snapshot.Version != change.NewVersion)
        {
            return false;
        }

        Dictionary<int, LineHighlightCacheEntry>? preservedEntries = null;
        foreach (var pair in _lineHighlightCache)
        {
            if (!TryCreatePreservedHighlightCacheEntry(snapshot, pair.Key, pair.Value, change, out var newLineIndex, out var preservedEntry))
            {
                continue;
            }

            preservedEntries ??= new Dictionary<int, LineHighlightCacheEntry>(_lineHighlightCache.Count);
            preservedEntries[newLineIndex] = preservedEntry;
        }

        if (preservedEntries is null || preservedEntries.Count == 0)
        {
            return false;
        }

        _lineHighlightCache.Clear();
        foreach (var pair in preservedEntries)
        {
            _lineHighlightCache[pair.Key] = pair.Value;
        }

        return true;
    }

    private static bool TryCreatePreservedHighlightCacheEntry(
        ITextSnapshot snapshot,
        int oldLineIndex,
        LineHighlightCacheEntry entry,
        TextDocumentChangedEventArgs change,
        out int newLineIndex,
        out LineHighlightCacheEntry preservedEntry)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(change);

        newLineIndex = -1;
        preservedEntry = default!;

        var deltaChars = change.InsertedLength - change.RemovedLength;
        var deltaLines = change.NewLineCount - change.OldLineCount;
        var oldLineStart = entry.LineStart;
        var oldLineEnd = entry.LineStart + entry.LineLength;
        var oldAffectedEnd = change.Position + change.RemovedLength;

        if (oldLineEnd <= change.Position)
        {
            newLineIndex = oldLineIndex;
            return TryCreatePreservedHighlightCacheEntry(snapshot, newLineIndex, entry.Runs, expectedLineStart: oldLineStart, expectedLineLength: entry.LineLength, out preservedEntry);
        }

        if (oldLineStart >= oldAffectedEnd)
        {
            newLineIndex = oldLineIndex + deltaLines;
            var newLineStart = oldLineStart + deltaChars;
            return TryCreatePreservedHighlightCacheEntry(snapshot, newLineIndex, entry.Runs, expectedLineStart: newLineStart, expectedLineLength: entry.LineLength, out preservedEntry);
        }

        if (change.OldLineCount == change.NewLineCount)
        {
            newLineIndex = oldLineIndex;
            var newLineLength = Math.Max(0, entry.LineLength + deltaChars);
            var changeStartInLine = Math.Clamp(change.Position - oldLineStart, 0, entry.LineLength);
            var shiftedRuns = ShiftStyledRunsForIntraLineEdit(entry.Runs, changeStartInLine, change.RemovedLength, change.InsertedLength, newLineLength);
            return TryCreatePreservedHighlightCacheEntry(snapshot, newLineIndex, shiftedRuns, expectedLineStart: oldLineStart, expectedLineLength: newLineLength, out preservedEntry);
        }

        return false;
    }

    private static bool TryCreatePreservedHighlightCacheEntry(
        ITextSnapshot snapshot,
        int lineIndex,
        StyledRun[] runs,
        int expectedLineStart,
        int expectedLineLength,
        out LineHighlightCacheEntry entry)
    {
        entry = default!;
        if ((uint)lineIndex >= (uint)snapshot.LineCount)
        {
            return false;
        }

        var line = snapshot.GetLine(lineIndex);
        if (line.Start != expectedLineStart || line.Length != expectedLineLength)
        {
            return false;
        }

        entry = new LineHighlightCacheEntry(line.Start, line.Length, runs);
        return true;
    }

    private static StyledRun[] ShiftStyledRunsForIntraLineEdit(StyledRun[] runs, int changeStart, int removedLength, int insertedLength, int newLineLength)
    {
        ArgumentNullException.ThrowIfNull(runs);

        if (runs.Length == 0 || newLineLength <= 0)
        {
            return Array.Empty<StyledRun>();
        }

        changeStart = Math.Clamp(changeStart, 0, newLineLength);
        removedLength = Math.Max(0, removedLength);
        insertedLength = Math.Max(0, insertedLength);

        var oldChangeEnd = changeStart + removedLength;
        var delta = insertedLength - removedLength;
        var rebuilt = new List<StyledRun>(runs.Length + 2);
        Style? insertedStyle = null;

        for (var i = 0; i < runs.Length; i++)
        {
            var run = runs[i];
            var runStart = run.Start;
            var runEnd = run.Start + run.Length;

            if (runEnd <= changeStart)
            {
                AddShiftedStyledRun(rebuilt, runStart, runEnd, run.Style, newLineLength);
                if (runEnd == changeStart)
                {
                    insertedStyle = run.Style;
                }

                continue;
            }

            if (runStart >= oldChangeEnd)
            {
                AddShiftedStyledRun(rebuilt, runStart + delta, runEnd + delta, run.Style, newLineLength);
                continue;
            }

            if (runStart < changeStart)
            {
                AddShiftedStyledRun(rebuilt, runStart, changeStart, run.Style, newLineLength);
                insertedStyle = run.Style;
            }
            else if (insertedStyle is null)
            {
                insertedStyle = run.Style;
            }

            if (runEnd > oldChangeEnd)
            {
                AddShiftedStyledRun(rebuilt, changeStart + insertedLength, runEnd + delta, run.Style, newLineLength);
            }
        }

        if (insertedLength > 0)
        {
            if (insertedStyle is null && rebuilt.Count > 0)
            {
                insertedStyle = rebuilt[^1].Style;
            }

            if (insertedStyle is Style style)
            {
                AddShiftedStyledRun(rebuilt, changeStart, changeStart + insertedLength, style, newLineLength);
            }
        }

        return rebuilt.Count == 0 ? Array.Empty<StyledRun>() : rebuilt.ToArray();
    }

    private static void AddShiftedStyledRun(List<StyledRun> destination, int start, int end, Style style, int lineLength)
    {
        ArgumentNullException.ThrowIfNull(destination);

        start = Math.Clamp(start, 0, lineLength);
        end = Math.Clamp(end, 0, lineLength);
        if (end <= start)
        {
            return;
        }

        if (destination.Count > 0)
        {
            var previous = destination[^1];
            if (previous.Start + previous.Length == start && previous.Style == style)
            {
                destination[^1] = new StyledRun(previous.Start, previous.Length + (end - start), previous.Style);
                return;
            }
        }

        destination.Add(new StyledRun(start, end - start, style));
    }

    private void InvalidateHighlightCache(bool preserveLineCache = false)
    {
        _cachedHighlightSnapshotVersion = -1;
        _cachedHighlightTheme = null;
        _cachedHighlighter = null;
        _cachedSyntaxHighlighter = null;
        _cachedSyntaxVisualVersion = -1;
        _cachedHighlightScrollOffsetY = -1;
        _cachedHighlightVisibleLineCount = -1;
        _cachedFirstVisibleLineIndex = -1;
        _cachedLastVisibleLineIndex = -1;
        _cachedSearchQuery = default;
        _cachedSearchActiveMatchIndex = -1;
        _cachedSearchMatchCount = -1;
        _lastVisibleLineRequestCount = 0;
        _preservedHighlightCacheSnapshotVersion = -1;
        if (!preserveLineCache)
        {
            _lineHighlightCache.Clear();
        }
    }

    private void NotifySyntaxVisualStateChanged()
    {
        InvalidateHighlightCache();
        BindingManager.Current.RunAfterTracking(() =>
        {
            SyntaxVisualVersion++;
        });
    }

    private CodeEditorSyntaxState? GetRenderableSyntaxState(int snapshotVersion, CodeEditorSyntaxHighlighter? syntaxHighlighter, long compatibilityStamp)
    {
        if (_syntaxState is null || syntaxHighlighter is null)
        {
            return null;
        }

        return _syntaxState.SnapshotVersion == snapshotVersion
            && _syntaxState.CompatibilityStamp == compatibilityStamp
            ? _syntaxState
            : null;
    }

    private static bool HasUsableSyntaxCoverage(CodeEditorSyntaxState? syntaxState, int lineIndex)
    {
        if (syntaxState is null)
        {
            return false;
        }

        if (syntaxState is ICodeEditorSyntaxCoverageState coverageState)
        {
            return coverageState.HasLineCoverage(lineIndex);
        }

        return syntaxState.IsComplete;
    }

    private bool ShouldRefreshVisibleSyntaxAfterAsyncCompletion(CodeEditorSyntaxState? previousState, CodeEditorSyntaxState newState, int previousCompletedLineCount)
    {
        ArgumentNullException.ThrowIfNull(newState);

        if (previousState is null || !ReferenceEquals(previousState, newState))
        {
            return true;
        }

        if (previousCompletedLineCount < 0 || newState is not IProgressiveCodeEditorSyntaxState progressiveState)
        {
            return true;
        }

        var newCompletedLineCount = progressiveState.CompletedLineCount;
        if (newCompletedLineCount <= previousCompletedLineCount || _visibleLines.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < _visibleLines.Count; i++)
        {
            var lineIndex = _visibleLines[i].LineIndex;
            if (lineIndex >= previousCompletedLineCount && lineIndex < newCompletedLineCount)
            {
                return true;
            }
        }

        return false;
    }

    private void ComposeSearchHighlightRuns(List<StyledRun> workingRuns, Theme theme, TextEditorSearchState searchState, int lineStart, int lineLength)
    {
        if (lineLength <= 0)
        {
            return;
        }

        if (string.IsNullOrEmpty(searchState.Query.Text) || searchState.Matches.Count == 0)
        {
            return;
        }

        var lineEnd = lineStart + lineLength;
        var composed = _composedHighlightRuns;
        composed.Clear();
        composed.AddRange(workingRuns);
        var style = GetStyle<CodeEditorStyle>();

        for (var i = 0; i < searchState.Matches.Count; i++)
        {
            var match = searchState.Matches[i];
            var matchStart = match.Start;
            var matchEnd = match.Start + match.Length;
            if (match.Length <= 0 || matchEnd <= lineStart || matchStart >= lineEnd)
            {
                continue;
            }

            var start = Math.Max(matchStart, lineStart) - lineStart;
            var end = Math.Min(matchEnd, lineEnd) - lineStart;
            if (end <= start)
            {
                continue;
            }

            composed.Add(new StyledRun(start, end - start, style.SearchMatchStyle(theme, match.IsActive)));
        }

        workingRuns.Clear();
        workingRuns.AddRange(composed);
    }

    private List<int> GetVisibleLogicalLineIndices()
    {
        var result = new List<int>(_visibleLines.Count);
        var last = -1;
        for (var i = 0; i < _visibleLines.Count; i++)
        {
            var lineIndex = _visibleLines[i].LineIndex;
            if (lineIndex == last)
            {
                continue;
            }

            result.Add(lineIndex);
            last = lineIndex;
        }

        return result;
    }

    private CodeEditorVisibleLine GetVisibleLineForLogicalLine(int lineIndex)
    {
        for (var i = 0; i < _visibleLines.Count; i++)
        {
            if (_visibleLines[i].LineIndex == lineIndex)
            {
                return _visibleLines[i];
            }
        }

        throw new InvalidOperationException($"Unable to locate visible line {lineIndex}.");
    }

    private void PruneHighlightCache(IReadOnlyCollection<int> visibleLineIndices)
    {
        if (_lineHighlightCache.Count == 0)
        {
            return;
        }

        List<int>? toRemove = null;
        foreach (var key in _lineHighlightCache.Keys)
        {
            if (visibleLineIndices.Contains(key))
            {
                continue;
            }

            toRemove ??= new List<int>();
            toRemove.Add(key);
        }

        if (toRemove is null)
        {
            return;
        }

        for (var i = 0; i < toRemove.Count; i++)
        {
            _lineHighlightCache.Remove(toRemove[i]);
        }
    }

    internal int GetCachedHighlightLineCountForTests() => _lineHighlightCache.Count;

    internal int GetLastVisibleLineRequestCountForTests() => _lastVisibleLineRequestCount;

    internal int[] GetVisibleLogicalLineIndicesForTests() => GetVisibleLogicalLineIndices().ToArray();

    internal StyledRun[]? GetHighlightRunsForTests(int lineIndex)
        => _lineHighlightCache.TryGetValue(lineIndex, out var entry) ? entry.Runs : null;

    internal int GetSyntaxStateSnapshotVersionForTests() => _syntaxState?.SnapshotVersion ?? -1;

    internal TextEditorSearchState GetSearchStateForTests() => GetSearchState();

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
