// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using TextMateSharp.Grammars;
using TextMateThemeName = TextMateSharp.Grammars.ThemeName;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;
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
    private readonly Dictionary<TextMateThemeName, TextMateTokenizationSession> _sessionsByTheme;
    private readonly object _sessionSync;

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
        _sessionsByTheme = new Dictionary<TextMateThemeName, TextMateTokenizationSession>();
        _sessionSync = new object();
        ScopeName = _catalog.ResolveScopeName(Options);
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
    public override bool DependsOnCaretOrSelection => false;

    /// <inheritdoc />
    public override long GetCompatibilityStamp(Theme theme)
        => (long)GetThemeName(theme);

    /// <inheritdoc />
    public override CodeEditorSyntaxState Build(in CodeEditorSyntaxBuildContext context)
        => CreateInitialState(context.Snapshot, GetThemeName(context.Theme), allowSynchronousTokenization: true);

    /// <inheritdoc />
    public override CodeEditorSyntaxState Update(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(previousState);

        if (previousState is not TextMateCodeEditorSyntaxState textMateState)
        {
            return Build(new CodeEditorSyntaxBuildContext(context.Snapshot, context.Theme, context.CaretIndex, context.SelectionStart, context.SelectionLength));
        }

        return CreateUpdatedState(textMateState, context, allowSynchronousTokenization: true);
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

        var line = textMateState.GetLineTokens(request.Snapshot, request.LineIndex);
        if (line is null)
        {
            return;
        }

        TextMateRunBuilder.AddStyledRuns(runs, baseOffset: 0, line.Segments, _catalog.GetPalette(textMateState.ThemeName));
    }

    /// <inheritdoc />
    public ValueTask<CodeEditorSyntaxState> BuildAsync(in CodeEditorSyntaxBuildContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = CreateInitialState(context.Snapshot, GetThemeName(context.Theme), allowSynchronousTokenization: ShouldAllowSynchronousTokenization(context.Snapshot));
        return new ValueTask<CodeEditorSyntaxState>(state);
    }

    /// <inheritdoc />
    public ValueTask<CodeEditorSyntaxState> UpdateAsync(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(previousState);
        cancellationToken.ThrowIfCancellationRequested();

        if (previousState is not TextMateCodeEditorSyntaxState textMateState)
        {
            return BuildAsync(new CodeEditorSyntaxBuildContext(context.Snapshot, context.Theme, context.CaretIndex, context.SelectionStart, context.SelectionLength), cancellationToken);
        }

        var requestedThemeName = GetThemeName(context.Theme);
        if (textMateState.SnapshotVersion == context.Snapshot.Version
            && textMateState.ThemeName == requestedThemeName
            && context.Change is null)
        {
            return RunBackgroundChunkAsync(textMateState, cancellationToken);
        }

        var updatedState = CreateUpdatedState(textMateState, context, allowSynchronousTokenization: ShouldAllowSynchronousTokenization(context.Snapshot));
        return new ValueTask<CodeEditorSyntaxState>(updatedState);
    }

    /// <inheritdoc />
    public ValueTask<CodeEditorSyntaxState> PrepareVisibleRangeAsync(CodeEditorSyntaxState state, in CodeEditorSyntaxVisibleRangeContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        cancellationToken.ThrowIfCancellationRequested();

        var requestedThemeName = GetThemeName(context.Theme);
        var textMateState = state as TextMateCodeEditorSyntaxState;
        if (textMateState is null
            || textMateState.SnapshotVersion != context.Snapshot.Version
            || textMateState.ThemeName != requestedThemeName)
        {
            textMateState = CreateInitialState(context.Snapshot, requestedThemeName, allowSynchronousTokenization: ShouldAllowSynchronousTokenization(context.Snapshot));
        }

        if (!textMateState.RequiresVisibleRangePreparation(context.FirstVisibleLineIndex, context.LastVisibleLineIndex))
        {
            return new ValueTask<CodeEditorSyntaxState>(textMateState);
        }

        var firstVisibleLineIndex = context.FirstVisibleLineIndex;
        var lastVisibleLineIndex = context.LastVisibleLineIndex;

        return new(Task.Run<CodeEditorSyntaxState>(() =>
        {
            textMateState.PrepareVisibleRange(firstVisibleLineIndex, lastVisibleLineIndex, cancellationToken);
            return textMateState;
        }, cancellationToken));
    }

    private ValueTask<CodeEditorSyntaxState> RunBackgroundChunkAsync(TextMateCodeEditorSyntaxState state, CancellationToken cancellationToken)
        => new(Task.Run<CodeEditorSyntaxState>(() =>
        {
            state.AdvanceBackgroundTokenization(cancellationToken);
            return state;
        }, cancellationToken));

    private TextMateCodeEditorSyntaxState CreateInitialState(ITextSnapshot snapshot, TextMateThemeName themeName, bool allowSynchronousTokenization)
        => new(
            snapshot.Version,
            GetSession(themeName),
            snapshot,
            themeName,
            compatibilityStamp: (long)themeName,
            allowSynchronousTokenization,
            isLargeDocument: IsLargeDocument(snapshot),
            new IStateStack?[snapshot.LineCount],
            new IStateStack?[snapshot.LineCount],
            new TextMateTokenizedLine?[snapshot.LineCount],
            validLineCount: 0,
            checkpointLineInterval: GetCheckpointLineInterval(snapshot),
            backgroundTokenizationLineBudget: GetBackgroundTokenizationLineBudget(snapshot),
            speculativeLookBehindLineCount: Math.Max(1, Options.SpeculativeLookBehindLineCount),
            speculativeWindowLineCount: Math.Max(1, Options.SpeculativeWindowLineCount),
            speculativeCheckpointSearchLineCount: Math.Max(0, Options.SpeculativeCheckpointSearchLineCount));

    private TextMateCodeEditorSyntaxState CreateUpdatedState(TextMateCodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context, bool allowSynchronousTokenization)
    {
        var snapshot = context.Snapshot;
        var themeName = GetThemeName(context.Theme);
        var updatedState = CreateInitialState(snapshot, themeName, allowSynchronousTokenization);
        updatedState.CopyReusableContentFrom(previousState, context.Change, context.AffectedStartLine);
        return updatedState;
    }

    private TextMateTokenizationSession GetSession(TextMateThemeName themeName)
    {
        lock (_sessionSync)
        {
            if (!_sessionsByTheme.TryGetValue(themeName, out var session))
            {
                session = _catalog.CreateSession(ScopeName, themeName);
                _sessionsByTheme.Add(themeName, session);
            }

            return session;
        }
    }

    private TextMateThemeName GetThemeName(Theme theme)
        => TextMateThemePalette.IsLightTheme(theme) ? Options.LightTheme : Options.DarkTheme;

    private bool IsLargeDocument(ITextSnapshot snapshot)
        => snapshot.Length >= Math.Max(1, Options.LargeDocumentCharacterThreshold)
            || snapshot.LineCount >= Math.Max(1, Options.LargeDocumentLineThreshold);

    private bool ShouldAllowSynchronousTokenization(ITextSnapshot snapshot)
        => !IsLargeDocument(snapshot);

    private int GetCheckpointLineInterval(ITextSnapshot snapshot)
    {
        var configured = Math.Max(1, Options.CheckpointLineInterval);
        return IsLargeDocument(snapshot) ? Math.Max(configured, configured * 4) : configured;
    }

    private int GetBackgroundTokenizationLineBudget(ITextSnapshot snapshot)
    {
        var configured = Math.Max(1, Options.BackgroundTokenizationLineBudget);
        return IsLargeDocument(snapshot) ? Math.Max(configured, configured * 2) : configured;
    }

    private sealed class TextMateCodeEditorSyntaxState : CodeEditorSyntaxState, IProgressiveCodeEditorSyntaxState, ICodeEditorSyntaxCoverageState
    {
        private readonly object _syncRoot = new();
        private readonly int _checkpointLineInterval;
        private readonly int _backgroundTokenizationLineBudget;
        private readonly int _speculativeLookBehindLineCount;
        private readonly int _speculativeWindowLineCount;
        private readonly int _speculativeCheckpointSearchLineCount;
        private readonly Dictionary<int, IStateStack?> _checkpointStates;
        private readonly Dictionary<int, TextMateTokenizedLine> _speculativeTokenizedLines;
        private readonly bool _allowSynchronousTokenization;
        private readonly bool _isLargeDocument;
        private char[] _lineTextBuffer;
        private int _validLineCount;

        public TextMateCodeEditorSyntaxState(
            int snapshotVersion,
            TextMateTokenizationSession session,
            ITextSnapshot snapshot,
            TextMateThemeName themeName,
            long compatibilityStamp,
            bool allowSynchronousTokenization,
            bool isLargeDocument,
            IStateStack?[] lineStartStates,
            IStateStack?[] lineEndStates,
            TextMateTokenizedLine?[] tokenizedLines,
            int validLineCount,
            int checkpointLineInterval,
            int backgroundTokenizationLineBudget,
            int speculativeLookBehindLineCount,
            int speculativeWindowLineCount,
            int speculativeCheckpointSearchLineCount)
        {
            SnapshotVersion = snapshotVersion;
            Session = session;
            Snapshot = snapshot;
            ThemeName = themeName;
            CompatibilityStamp = compatibilityStamp;
            _allowSynchronousTokenization = allowSynchronousTokenization;
            _isLargeDocument = isLargeDocument;
            LineStartStates = lineStartStates;
            LineEndStates = lineEndStates;
            TokenizedLines = tokenizedLines;
            _lineTextBuffer = Array.Empty<char>();
            _validLineCount = Math.Clamp(validLineCount, 0, lineStartStates.Length);
            _checkpointLineInterval = Math.Max(1, checkpointLineInterval);
            _backgroundTokenizationLineBudget = Math.Max(1, backgroundTokenizationLineBudget);
            _speculativeLookBehindLineCount = Math.Max(1, speculativeLookBehindLineCount);
            _speculativeWindowLineCount = Math.Max(1, speculativeWindowLineCount);
            _speculativeCheckpointSearchLineCount = Math.Max(0, speculativeCheckpointSearchLineCount);
            _checkpointStates = new Dictionary<int, IStateStack?>();
            _speculativeTokenizedLines = new Dictionary<int, TextMateTokenizedLine>();
            if (lineStartStates.Length > 0)
            {
                _checkpointStates[0] = null;
            }
        }

        public override int SnapshotVersion { get; }

        public override long CompatibilityStamp { get; }

        public override bool IsComplete => Volatile.Read(ref _validLineCount) >= LineCount;

        internal TextMateTokenizationSession Session { get; }

        internal ITextSnapshot Snapshot { get; }

        internal TextMateThemeName ThemeName { get; }

        internal IStateStack?[] LineStartStates { get; }

        internal IStateStack?[] LineEndStates { get; }

        internal TextMateTokenizedLine?[] TokenizedLines { get; }

        internal int LineCount => LineStartStates.Length;

        internal int ValidLineCount => Volatile.Read(ref _validLineCount);

        int IProgressiveCodeEditorSyntaxState.CompletedLineCount => ValidLineCount;

        internal TextMateTokenizedLine? GetLineTokens(ITextSnapshot snapshot, int lineIndex)
        {
            _ = snapshot;
            if ((uint)lineIndex >= (uint)LineCount)
            {
                return null;
            }

            if (TokenizedLines[lineIndex] is { } existing)
            {
                return existing;
            }

            if (!_allowSynchronousTokenization)
            {
                return _speculativeTokenizedLines.TryGetValue(lineIndex, out var speculative) ? speculative : null;
            }

            lock (_syncRoot)
            {
                if (TokenizedLines[lineIndex] is { } cached)
                {
                    return cached;
                }

                if (_speculativeTokenizedLines.TryGetValue(lineIndex, out var speculative))
                {
                    return speculative;
                }

                EnsureTokenizedThrough(lineIndex);
                return TokenizedLines[lineIndex];
            }
        }

        internal void AdvanceBackgroundTokenization(CancellationToken cancellationToken)
        {
            if (IsComplete)
            {
                return;
            }

            for (var processedLineCount = 0; processedLineCount < _backgroundTokenizationLineBudget; processedLineCount++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                lock (_syncRoot)
                {
                    if (IsComplete)
                    {
                        return;
                    }

                    var startLine = _validLineCount;
                    var currentState = startLine == 0 ? null : LineEndStates[startLine - 1];
                    TokenizeLine(startLine, currentState);
                }
            }
        }

        internal void PrepareVisibleRange(int firstVisibleLineIndex, int lastVisibleLineIndex, CancellationToken cancellationToken)
        {
            if (!_isLargeDocument || _allowSynchronousTokenization || LineCount == 0)
            {
                return;
            }

            firstVisibleLineIndex = Math.Clamp(firstVisibleLineIndex, 0, LineCount - 1);
            lastVisibleLineIndex = Math.Clamp(lastVisibleLineIndex, firstVisibleLineIndex, LineCount - 1);

            lock (_syncRoot)
            {
                var hasMissingLine = false;
                for (var lineIndex = firstVisibleLineIndex; lineIndex <= lastVisibleLineIndex; lineIndex++)
                {
                    if (TokenizedLines[lineIndex] is null && !_speculativeTokenizedLines.ContainsKey(lineIndex))
                    {
                        hasMissingLine = true;
                        break;
                    }
                }

                if (!hasMissingLine)
                {
                    return;
                }

                BuildSpeculativeWindow(firstVisibleLineIndex, lastVisibleLineIndex, cancellationToken);
            }
        }

        internal void CopyReusableContentFrom(TextMateCodeEditorSyntaxState previousState, TextDocumentChangedEventArgs? change, int affectedStartLine)
        {
            ArgumentNullException.ThrowIfNull(previousState);

            if (LineCount == 0 || previousState.LineCount == 0)
            {
                return;
            }

            var prefixLineCount = change is null
                ? Math.Min(previousState.ValidLineCount, LineCount)
                : Math.Min(Math.Max(0, affectedStartLine), Math.Min(previousState.ValidLineCount, LineCount));
            if (prefixLineCount > 0)
            {
                Array.Copy(previousState.LineStartStates, 0, LineStartStates, 0, prefixLineCount);
                Array.Copy(previousState.LineEndStates, 0, LineEndStates, 0, prefixLineCount);
                Array.Copy(previousState.TokenizedLines, 0, TokenizedLines, 0, prefixLineCount);
                _validLineCount = prefixLineCount;

                foreach (var pair in previousState._checkpointStates)
                {
                    if (pair.Key < prefixLineCount)
                    {
                        _checkpointStates[pair.Key] = pair.Value;
                    }
                }
            }

            if (change is null)
            {
                CopyApproximateSuffix(previousState, sourceStartLine: prefixLineCount, destinationStartLine: prefixLineCount);
                return;
            }

            CopyApproximateEditedLine(previousState, change);

            var oldAffectedEndPosition = Math.Min(previousState.Snapshot.Length, change.Position + change.RemovedLength);
            var oldAffectedEndLine = previousState.Snapshot.GetLineIndexFromPosition(oldAffectedEndPosition);
            var newAffectedEndPosition = Math.Min(Snapshot.Length, change.Position + change.InsertedLength);
            var newAffectedEndLine = Snapshot.GetLineIndexFromPosition(newAffectedEndPosition);
            CopyApproximateSuffix(previousState, sourceStartLine: oldAffectedEndLine + 1, destinationStartLine: newAffectedEndLine + 1);
        }

        bool ICodeEditorSyntaxCoverageState.HasLineCoverage(int lineIndex)
        {
            if ((uint)lineIndex >= (uint)LineCount)
            {
                return false;
            }

            if (_allowSynchronousTokenization)
            {
                return true;
            }

            lock (_syncRoot)
            {
                return TokenizedLines[lineIndex] is not null || _speculativeTokenizedLines.ContainsKey(lineIndex);
            }
        }

        private void EnsureTokenizedThrough(int lineIndex)
        {
            if (lineIndex < _validLineCount)
            {
                return;
            }

            var currentState = _validLineCount == 0 ? null : LineEndStates[_validLineCount - 1];
            for (var currentLine = _validLineCount; currentLine <= lineIndex; currentLine++)
            {
                currentState = TokenizeLine(currentLine, currentState);
            }
        }

        private IStateStack? TokenizeLine(int lineIndex, IStateStack? startState)
        {
            if (lineIndex % _checkpointLineInterval == 0)
            {
                _checkpointStates[lineIndex] = startState;
            }

            var line = Snapshot.GetLine(lineIndex);
            LineStartStates[lineIndex] = startState;
            var result = Session.TokenizeLine2(GetLineText(line, includeLineBreak: true), startState);
            var nextState = result.RuleStack;
            LineEndStates[lineIndex] = nextState;
            TokenizedLines[lineIndex] = TextMateTokenizedLine.Create(line.Length, result.Tokens);
            _speculativeTokenizedLines.Remove(lineIndex);
            if (lineIndex == _validLineCount)
            {
                _validLineCount = lineIndex + 1;
            }

            return nextState;
        }

        internal bool RequiresVisibleRangePreparation(int firstVisibleLineIndex, int lastVisibleLineIndex)
        {
            if (!_isLargeDocument || _allowSynchronousTokenization || LineCount == 0)
            {
                return false;
            }

            firstVisibleLineIndex = Math.Clamp(firstVisibleLineIndex, 0, LineCount - 1);
            lastVisibleLineIndex = Math.Clamp(lastVisibleLineIndex, firstVisibleLineIndex, LineCount - 1);
            lock (_syncRoot)
            {
                for (var lineIndex = firstVisibleLineIndex; lineIndex <= lastVisibleLineIndex; lineIndex++)
                {
                    if (TokenizedLines[lineIndex] is null && !_speculativeTokenizedLines.ContainsKey(lineIndex))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void CopyApproximateEditedLine(TextMateCodeEditorSyntaxState previousState, TextDocumentChangedEventArgs change)
        {
            if (change.OldLineCount != change.NewLineCount || LineCount == 0 || previousState.LineCount == 0)
            {
                return;
            }

            var oldLineIndex = previousState.Snapshot.GetLineIndexFromPosition(Math.Clamp(change.Position, 0, previousState.Snapshot.Length));
            var newLineIndex = Snapshot.GetLineIndexFromPosition(Math.Clamp(change.Position, 0, Snapshot.Length));
            if ((uint)oldLineIndex >= (uint)previousState.LineCount || (uint)newLineIndex >= (uint)LineCount)
            {
                return;
            }

            var oldLine = previousState.Snapshot.GetLine(oldLineIndex);
            var newLine = Snapshot.GetLine(newLineIndex);
            if (oldLineIndex != newLineIndex
                || oldLine.LineBreakLength != newLine.LineBreakLength
                || previousState.TokenizedLines[oldLineIndex] is not { } oldTokenizedLine)
            {
                return;
            }

            var changeStartInLine = Math.Clamp(change.Position - oldLine.Start, 0, oldLine.Length);
            TokenizedLines[newLineIndex] = TextMateTokenizedLine.ShiftForIntraLineEdit(
                oldTokenizedLine,
                changeStartInLine,
                change.RemovedLength,
                change.InsertedLength,
                newLine.Length);
        }

        private void CopyApproximateSuffix(TextMateCodeEditorSyntaxState previousState, int sourceStartLine, int destinationStartLine)
        {
            if ((uint)sourceStartLine >= (uint)previousState.LineCount || (uint)destinationStartLine >= (uint)LineCount)
            {
                return;
            }

            var sourceLine = sourceStartLine;
            var destinationLine = destinationStartLine;
            while (sourceLine < previousState.LineCount && destinationLine < LineCount)
            {
                if (TokenizedLines[destinationLine] is null && previousState.TokenizedLines[sourceLine] is { } tokenizedLine)
                {
                    TokenizedLines[destinationLine] = tokenizedLine;
                }

                sourceLine++;
                destinationLine++;
            }
        }

        private void BuildSpeculativeWindow(int firstVisibleLineIndex, int lastVisibleLineIndex, CancellationToken cancellationToken)
        {
            var targetLineIndex = firstVisibleLineIndex;
            var startLine = Math.Max(0, firstVisibleLineIndex - _speculativeLookBehindLineCount);
            IStateStack? state = null;

            if (TryFindNearbyCheckpoint(targetLineIndex, out var checkpointLine, out var checkpointState))
            {
                startLine = checkpointLine;
                state = checkpointState;
            }

            var endLineExclusive = Math.Min(LineCount, Math.Max(lastVisibleLineIndex + 1, startLine + _speculativeWindowLineCount));
            for (var lineIndex = startLine; lineIndex < endLineExclusive; lineIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = Snapshot.GetLine(lineIndex);
                var result = Session.TokenizeLine2(GetLineText(line, includeLineBreak: true), state);
                state = result.RuleStack;
                if (TokenizedLines[lineIndex] is null)
                {
                    _speculativeTokenizedLines[lineIndex] = TextMateTokenizedLine.Create(line.Length, result.Tokens);
                }
            }

            var maxCachedLines = Math.Max(_speculativeWindowLineCount * 4, 256);
            if (_speculativeTokenizedLines.Count <= maxCachedLines)
            {
                return;
            }

            var focusLine = (firstVisibleLineIndex + lastVisibleLineIndex) / 2;
            var minLine = Math.Max(0, focusLine - maxCachedLines / 2);
            var maxLine = Math.Min(LineCount - 1, focusLine + maxCachedLines / 2);
            var linesToRemove = _speculativeTokenizedLines.Keys.Where(line => line < minLine || line > maxLine).ToArray();
            for (var i = 0; i < linesToRemove.Length; i++)
            {
                _speculativeTokenizedLines.Remove(linesToRemove[i]);
            }
        }

        private bool TryFindNearbyCheckpoint(int targetLineIndex, out int checkpointLine, out IStateStack? checkpointState)
        {
            checkpointLine = 0;
            checkpointState = null;

            if (_checkpointStates.Count == 0)
            {
                return false;
            }

            var candidateLine = targetLineIndex;
            var lowerBound = Math.Max(0, targetLineIndex - _speculativeCheckpointSearchLineCount);
            while (candidateLine >= lowerBound)
            {
                if (_checkpointStates.TryGetValue(candidateLine, out checkpointState))
                {
                    checkpointLine = candidateLine;
                    return true;
                }

                if (candidateLine == 0)
                {
                    break;
                }

                candidateLine--;
            }

            return false;
        }

        private LineText GetLineText(TextLine line, bool includeLineBreak)
        {
            var length = line.Length + (includeLineBreak ? line.LineBreakLength : 0);
            if (length <= 0)
            {
                return ReadOnlyMemory<char>.Empty;
            }

            if (_lineTextBuffer.Length < length)
            {
                _lineTextBuffer = new char[length];
            }

            var buffer = _lineTextBuffer.AsSpan(0, length);
            Snapshot.CopyTo(line.Start, buffer);
            return new LineText(_lineTextBuffer.AsMemory(0, length));
        }
    }

    internal int GetTokenizeLineCallCountForTests()
    {
        var darkSession = GetSession(Options.DarkTheme);
        var lightSession = GetSession(Options.LightTheme);
        return darkSession.TokenizeLineCallCount + (ReferenceEquals(darkSession, lightSession) ? 0 : lightSession.TokenizeLineCallCount);
    }

    internal int GetCheckpointLineIntervalForTests(ITextSnapshot? snapshot = null)
        => snapshot is null ? Math.Max(1, Options.CheckpointLineInterval) : GetCheckpointLineInterval(snapshot);

    internal int GetBackgroundTokenizationLineBudgetForTests(ITextSnapshot? snapshot = null)
        => snapshot is null ? Math.Max(1, Options.BackgroundTokenizationLineBudget) : GetBackgroundTokenizationLineBudget(snapshot);

    internal int GetCompletedLineCountForTests(CodeEditorSyntaxState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state is TextMateCodeEditorSyntaxState textMateState ? textMateState.ValidLineCount : 0;
    }
}
