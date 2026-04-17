// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using TextMateSharp.Grammars;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;

/// <summary>
/// Provides a TextMateSharp-backed syntax highlighter for <see cref="CodeEditor"/>.
/// </summary>
/// <remarks>
/// <para>
/// The highlighter keeps persistent per-line tokenizer state so edits only need to recompute the affected suffix of the
/// document.
/// </para>
/// <para>
/// Rendering colors are chosen from bundled light/dark TextMate themes based on the host terminal UI theme.
/// </para>
/// </remarks>
public sealed class TextMateCodeEditorSyntaxHighlighter : CodeEditorSyntaxHighlighter, IAsyncCodeEditorSyntaxHighlighter
{
    private readonly TextMateLanguageCatalog _catalog;
    private readonly TextMateTokenizationSession _session;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextMateCodeEditorSyntaxHighlighter"/> class for an explicit TextMate scope name.
    /// </summary>
    /// <param name="scopeName">A TextMate scope name such as <c>source.cs</c>.</param>
    public TextMateCodeEditorSyntaxHighlighter(string scopeName)
        : this(new TextMateCodeEditorOptions { ScopeName = scopeName })
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextMateCodeEditorSyntaxHighlighter"/> class.
    /// </summary>
    /// <param name="options">The TextMate resolution and theme options.</param>
    public TextMateCodeEditorSyntaxHighlighter(TextMateCodeEditorOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        _catalog = TextMateLanguageCatalog.Default;
        ScopeName = _catalog.ResolveScopeName(Options);
        _session = _catalog.CreateSession(ScopeName);
    }

    /// <summary>
    /// Gets the options used by this highlighter instance.
    /// </summary>
    public TextMateCodeEditorOptions Options { get; }

    /// <summary>
    /// Gets the resolved TextMate scope name.
    /// </summary>
    public string ScopeName { get; }

    /// <inheritdoc />
    public override CodeEditorSyntaxState Build(in CodeEditorSyntaxBuildContext context)
        => BuildState(context.Snapshot, CancellationToken.None);

    /// <inheritdoc />
    public override CodeEditorSyntaxState Update(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(previousState);
        return previousState is TextMateCodeEditorSyntaxState textMateState
            ? UpdateState(textMateState, context, CancellationToken.None)
            : BuildState(context.Snapshot, CancellationToken.None);
    }

    /// <inheritdoc />
    public override void GetLineRuns(CodeEditorSyntaxState state, in CodeEditorLineSyntaxRequest request, List<StyledRun> runs)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(runs);

        if (state is not TextMateCodeEditorSyntaxState textMateState)
        {
            throw new ArgumentException("The syntax state does not belong to TextMateCodeEditorSyntaxHighlighter.", nameof(state));
        }

        if ((uint)request.LineIndex >= (uint)textMateState.LineCount)
        {
            return;
        }

        var line = textMateState.GetOrCreateLineTokens(request.Snapshot, request.LineIndex);
        var themeName = TextMateThemePalette.IsLightTheme(request.Theme) ? Options.LightTheme : Options.DarkTheme;
        var palette = _catalog.GetPalette(themeName);
        TextMateRunBuilder.AddStyledRuns(runs, baseOffset: 0, line.Segments, palette);
    }

    /// <inheritdoc />
    public ValueTask<CodeEditorSyntaxState> BuildAsync(in CodeEditorSyntaxBuildContext context, CancellationToken cancellationToken = default)
    {
        var snapshot = context.Snapshot;
        return new(Task.Run<CodeEditorSyntaxState>(() => BuildState(snapshot, cancellationToken), cancellationToken));
    }

    /// <inheritdoc />
    public ValueTask<CodeEditorSyntaxState> UpdateAsync(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(previousState);

        if (previousState is not TextMateCodeEditorSyntaxState textMateState)
        {
            return BuildAsync(new CodeEditorSyntaxBuildContext(context.Snapshot, context.Theme, context.CaretIndex, context.SelectionStart, context.SelectionLength), cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<CodeEditorSyntaxState>(UpdateState(textMateState, context, cancellationToken));
    }

    private TextMateCodeEditorSyntaxState BuildState(ITextSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var lineCount = snapshot.LineCount;
        if (lineCount == 0)
        {
            return new TextMateCodeEditorSyntaxState(snapshot.Version, _session, Array.Empty<IStateStack?>(), Array.Empty<IStateStack?>(), Array.Empty<TextMateTokenizedLine?>());
        }

        var lineStartStates = new IStateStack?[lineCount];
        var lineEndStates = new IStateStack?[lineCount];
        var tokenizedLines = new TextMateTokenizedLine?[lineCount];
        IStateStack? currentState = null;

        for (var lineIndex = 0; lineIndex < lineCount; lineIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lineStartStates[lineIndex] = currentState;
            var result = _session.TokenizeLine(GetLineText(snapshot, lineIndex), currentState);
            currentState = result.RuleStack;
            lineEndStates[lineIndex] = currentState;
        }

        return new TextMateCodeEditorSyntaxState(snapshot.Version, _session, lineStartStates, lineEndStates, tokenizedLines);
    }

    private TextMateCodeEditorSyntaxState UpdateState(TextMateCodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(previousState);

        var snapshot = context.Snapshot;
        if (snapshot.LineCount == 0)
        {
            return new TextMateCodeEditorSyntaxState(snapshot.Version, _session, Array.Empty<IStateStack?>(), Array.Empty<IStateStack?>(), Array.Empty<TextMateTokenizedLine?>());
        }

        var change = context.Change;
        if (change is null || previousState.LineCount == 0)
        {
            return BuildState(snapshot, cancellationToken);
        }

        var lineCount = snapshot.LineCount;
        var lineStartStates = new IStateStack?[lineCount];
        var lineEndStates = new IStateStack?[lineCount];
        var tokenizedLines = new TextMateTokenizedLine?[lineCount];
        var startLine = Math.Clamp(context.AffectedStartLine, 0, lineCount - 1);
        var prefixCount = Math.Min(startLine, Math.Min(previousState.LineCount, lineCount));
        if (prefixCount > 0)
        {
            Array.Copy(previousState.LineStartStates, 0, lineStartStates, 0, prefixCount);
            Array.Copy(previousState.LineEndStates, 0, lineEndStates, 0, prefixCount);
            Array.Copy(previousState.TokenizedLines, 0, tokenizedLines, 0, prefixCount);
        }

        var lineDelta = change.NewLineCount - change.OldLineCount;
        var currentState = startLine == 0 ? null : lineEndStates[startLine - 1];

        for (var lineIndex = startLine; lineIndex < lineCount; lineIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lineStartStates[lineIndex] = currentState;

            var lineText = GetLineText(snapshot, lineIndex);
            var result = _session.TokenizeLine(lineText, currentState);
            currentState = result.RuleStack;
            lineEndStates[lineIndex] = currentState;

            var oldEquivalentLineIndex = lineIndex - lineDelta;
            if (lineIndex > context.AffectedEndLine
                && (uint)oldEquivalentLineIndex < (uint)previousState.LineCount
                && StatesEqual(lineStartStates[lineIndex], previousState.LineStartStates[oldEquivalentLineIndex])
                && previousState.TokenizedLines[oldEquivalentLineIndex] is { } cachedLine
                && string.Equals(cachedLine.Text, lineText, StringComparison.Ordinal))
            {
                tokenizedLines[lineIndex] = cachedLine;
            }

            var oldNextStartIndex = (lineIndex + 1) - lineDelta;
            if (lineIndex >= context.AffectedEndLine
                && (uint)oldNextStartIndex < (uint)previousState.LineCount
                && StatesEqual(currentState, previousState.LineStartStates[oldNextStartIndex]))
            {
                var tailCount = Math.Min(lineCount - (lineIndex + 1), previousState.LineCount - oldNextStartIndex);
                if (tailCount > 0)
                {
                    Array.Copy(previousState.LineStartStates, oldNextStartIndex, lineStartStates, lineIndex + 1, tailCount);
                    Array.Copy(previousState.LineEndStates, oldNextStartIndex, lineEndStates, lineIndex + 1, tailCount);
                    Array.Copy(previousState.TokenizedLines, oldNextStartIndex, tokenizedLines, lineIndex + 1, tailCount);
                }

                break;
            }
        }

        return new TextMateCodeEditorSyntaxState(snapshot.Version, _session, lineStartStates, lineEndStates, tokenizedLines);
    }

    private static bool StatesEqual(IStateStack? left, IStateStack? right)
        => left is null ? right is null : left.Equals(right);

    private static string GetLineText(ITextSnapshot snapshot, int lineIndex)
    {
        var line = snapshot.GetLine(lineIndex);
        if (line.Length == 0)
        {
            return string.Empty;
        }

        return string.Create(
            line.Length,
            (Snapshot: snapshot, Start: line.Start),
            static (span, state) => state.Snapshot.CopyTo(state.Start, span));
    }

    private sealed class TextMateCodeEditorSyntaxState : CodeEditorSyntaxState
    {
        public TextMateCodeEditorSyntaxState(
            int snapshotVersion,
            TextMateTokenizationSession session,
            IStateStack?[] lineStartStates,
            IStateStack?[] lineEndStates,
            TextMateTokenizedLine?[] tokenizedLines)
        {
            SnapshotVersion = snapshotVersion;
            Session = session;
            LineStartStates = lineStartStates;
            LineEndStates = lineEndStates;
            TokenizedLines = tokenizedLines;
        }

        public override int SnapshotVersion { get; }

        internal TextMateTokenizationSession Session { get; }

        internal IStateStack?[] LineStartStates { get; }

        internal IStateStack?[] LineEndStates { get; }

        internal TextMateTokenizedLine?[] TokenizedLines { get; }

        internal int LineCount => LineStartStates.Length;

        internal TextMateTokenizedLine GetOrCreateLineTokens(ITextSnapshot snapshot, int lineIndex)
        {
            if (TokenizedLines[lineIndex] is { } existing)
            {
                return existing;
            }

            var lineText = GetLineText(snapshot, lineIndex);
            var tokenized = TextMateTokenizedLine.Create(lineText, Session.TokenizeLine(lineText, LineStartStates[lineIndex]).Tokens);
            TokenizedLines[lineIndex] = tokenized;
            return tokenized;
        }
    }
}
