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
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<CodeEditorSyntaxState>(BuildState(context.Snapshot, cancellationToken));
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
            return new TextMateCodeEditorSyntaxState(
                snapshot.Version,
                _session,
                string.Empty,
                Array.Empty<IStateStack?>(),
                Array.Empty<IStateStack?>(),
                Array.Empty<TextMateTokenizedLine?>(),
                validLineCount: 0);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new TextMateCodeEditorSyntaxState(
            snapshot.Version,
            _session,
            GetSnapshotText(snapshot),
            new IStateStack?[lineCount],
            new IStateStack?[lineCount],
            new TextMateTokenizedLine?[lineCount],
            validLineCount: 0);
    }

    private TextMateCodeEditorSyntaxState UpdateState(TextMateCodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(previousState);

        var snapshot = context.Snapshot;
        if (snapshot.LineCount == 0)
        {
            return new TextMateCodeEditorSyntaxState(
                snapshot.Version,
                _session,
                string.Empty,
                Array.Empty<IStateStack?>(),
                Array.Empty<IStateStack?>(),
                Array.Empty<TextMateTokenizedLine?>(),
                validLineCount: 0);
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
        var prefixCount = Math.Min(startLine, Math.Min(previousState.ValidLineCount, Math.Min(previousState.LineCount, lineCount)));
        if (prefixCount > 0)
        {
            Array.Copy(previousState.LineStartStates, 0, lineStartStates, 0, prefixCount);
            Array.Copy(previousState.LineEndStates, 0, lineEndStates, 0, prefixCount);
            Array.Copy(previousState.TokenizedLines, 0, tokenizedLines, 0, prefixCount);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new TextMateCodeEditorSyntaxState(
            snapshot.Version,
            _session,
            GetSnapshotText(snapshot),
            lineStartStates,
            lineEndStates,
            tokenizedLines,
            prefixCount);
    }

    private static string GetSnapshotText(ITextSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.Length == 0)
        {
            return string.Empty;
        }

        return string.Create(snapshot.Length, snapshot, static (destination, currentSnapshot) =>
        {
            currentSnapshot.CopyTo(0, destination);
        });
    }

    private sealed class TextMateCodeEditorSyntaxState : CodeEditorSyntaxState
    {
        private readonly object _syncRoot = new();
        private readonly string _snapshotText;
        private int _validLineCount;

        public TextMateCodeEditorSyntaxState(
            int snapshotVersion,
            TextMateTokenizationSession session,
            string snapshotText,
            IStateStack?[] lineStartStates,
            IStateStack?[] lineEndStates,
            TextMateTokenizedLine?[] tokenizedLines,
            int validLineCount)
        {
            SnapshotVersion = snapshotVersion;
            Session = session;
            _snapshotText = snapshotText ?? string.Empty;
            LineStartStates = lineStartStates;
            LineEndStates = lineEndStates;
            TokenizedLines = tokenizedLines;
            _validLineCount = Math.Clamp(validLineCount, 0, lineStartStates.Length);
        }

        public override int SnapshotVersion { get; }

        internal TextMateTokenizationSession Session { get; }

        internal IStateStack?[] LineStartStates { get; }

        internal IStateStack?[] LineEndStates { get; }

        internal TextMateTokenizedLine?[] TokenizedLines { get; }

        internal int LineCount => LineStartStates.Length;

        internal int ValidLineCount => Volatile.Read(ref _validLineCount);

        internal TextMateTokenizedLine GetOrCreateLineTokens(ITextSnapshot snapshot, int lineIndex)
        {
            if (TokenizedLines[lineIndex] is { } existing)
            {
                return existing;
            }

            lock (_syncRoot)
            {
                if (TokenizedLines[lineIndex] is { } cached)
                {
                    return cached;
                }

                EnsureTokenizedThrough(snapshot, lineIndex);
                if (TokenizedLines[lineIndex] is { } tokenized)
                {
                    return tokenized;
                }

                var startState = lineIndex == 0 ? null : LineEndStates[lineIndex - 1];
                var line = snapshot.GetLine(lineIndex);
                var result = Session.TokenizeLine(GetLineText(line, includeLineBreak: true), startState);
                tokenized = TextMateTokenizedLine.Create(line.Length, result.Tokens);
                TokenizedLines[lineIndex] = tokenized;
                LineStartStates[lineIndex] = startState;
                LineEndStates[lineIndex] = result.RuleStack;
                if (lineIndex == _validLineCount)
                {
                    _validLineCount = lineIndex + 1;
                }

                return tokenized;
            }
        }

        private void EnsureTokenizedThrough(ITextSnapshot snapshot, int lineIndex)
        {
            if (lineIndex < _validLineCount)
            {
                return;
            }

            var currentState = _validLineCount == 0 ? null : LineEndStates[_validLineCount - 1];
            for (var currentLine = _validLineCount; currentLine <= lineIndex; currentLine++)
            {
                var line = snapshot.GetLine(currentLine);
                LineStartStates[currentLine] = currentState;
                var result = Session.TokenizeLine(GetLineText(line, includeLineBreak: true), currentState);
                currentState = result.RuleStack;
                LineEndStates[currentLine] = currentState;
                TokenizedLines[currentLine] = TextMateTokenizedLine.Create(line.Length, result.Tokens);
            }

            _validLineCount = lineIndex + 1;
        }

        private LineText GetLineText(TextLine line, bool includeLineBreak)
        {
            var length = line.Length + (includeLineBreak ? line.LineBreakLength : 0);
            return length <= 0
                ? ReadOnlyMemory<char>.Empty
                : _snapshotText.AsMemory(line.Start, length);
        }
    }

    internal int GetTokenizeLineCallCountForTests() => _session.TokenizeLineCallCount;
}
