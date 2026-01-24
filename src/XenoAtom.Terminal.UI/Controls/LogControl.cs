// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Displays a high-performance scrolling log viewer with append, selection/copy, and built-in search.
/// </summary>
/// <remarks>
/// <para>
/// LogControl is optimized for appending (tail-like) workloads: it stores log entries as lines and renders only the
/// visible portion. It supports markup styling per entry without requiring a visual per line.
/// </para>
/// <para>
/// Built-in search is available via Ctrl+F, and can be driven programmatically via <see cref="Search(string)"/>.
/// </para>
/// </remarks>
public sealed partial class LogControl : Visual
{
    private readonly ScrollViewer _scrollViewer;
    private readonly LogContentVisual _content;
    private readonly SearchReplacePopup _searchPopup;
    private readonly BindableList<LogEntry> _entries;
    private readonly MarkupTextParser _markupParser;

    private readonly List<LogMatch> _matches;
    private List<LogMatch>?[]? _matchesByEntry;
    private int _activeMatchIndex;
    private string? _searchError;

    private LogTextPosition? _selectionAnchor;
    private LogTextPosition? _selectionActive;
    private bool _pendingFollowTailScroll;
    private bool _bulkUpdatingSearchFlags;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogControl"/> class.
    /// </summary>
    public LogControl()
    {
        Focusable = true;
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;

        _markupParser = new MarkupTextParser();
        _entries = new BindableList<LogEntry>(this, "LogControl.Entries");
        _matches = new List<LogMatch>(16);
        _activeMatchIndex = -1;

        _content = new LogContentVisual(this);

        // LogControl owns focus/key handling. The internal ScrollViewer provides scrollbars and translates content.
        _scrollViewer = new ScrollViewer(_content, focusable: false);

        // Default behavior: wrap long lines to the viewport width.
        this.WrapText(true);

        _searchPopup = new SearchReplacePopup(new LogSearchTarget(this));

        AttachChild(_scrollViewer);
        AttachChild(_searchPopup);
    }

    /// <inheritdoc/>
    protected override int ChildrenCount => 2;

    /// <inheritdoc/>
    protected override Visual GetChild(int index)
        => index switch
        {
            0 => _scrollViewer,
            1 => _searchPopup,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };

    /// <summary>
    /// Gets the number of log entries currently retained.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Gets or sets the maximum number of log entries to retain.
    /// </summary>
    /// <remarks>
    /// When the capacity is exceeded, the oldest entries are removed. Set to <c>0</c> to disable trimming.
    /// </remarks>
    [Bindable]
    public partial int MaxCapacity { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether long lines wrap to the viewport width.
    /// </summary>
    /// <remarks>
    /// When enabled, horizontal scrolling is disabled and the log reflows during resize.
    /// </remarks>
    [Bindable]
    public partial bool WrapText { get; set; }

    /// <summary>
    /// Gets or sets the current search text. When set, matches are recomputed and highlighted.
    /// </summary>
    [Bindable]
    public partial string? SearchText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether search is case sensitive.
    /// </summary>
    [Bindable]
    public partial bool SearchCaseSensitive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether search matches whole words only.
    /// </summary>
    [Bindable]
    public partial bool SearchWholeWord { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether search uses regular expressions.
    /// </summary>
    [Bindable]
    public partial bool SearchRegex { get; set; }

    /// <summary>
    /// Gets the currently active search options as a flags value.
    /// </summary>
    public LogSearchOptions SearchOptions
    {
        get
        {
            var options = LogSearchOptions.None;
            if (SearchCaseSensitive) options |= LogSearchOptions.CaseSensitive;
            if (SearchWholeWord) options |= LogSearchOptions.WholeWord;
            if (SearchRegex) options |= LogSearchOptions.Regex;
            return options;
        }
        set
        {
            var bulk = _bulkUpdatingSearchFlags;
            _bulkUpdatingSearchFlags = true;
            try
            {
                SearchCaseSensitive = value.HasFlag(LogSearchOptions.CaseSensitive);
                SearchWholeWord = value.HasFlag(LogSearchOptions.WholeWord);
                SearchRegex = value.HasFlag(LogSearchOptions.Regex);
            }
            finally
            {
                _bulkUpdatingSearchFlags = bulk;
            }

            if (!bulk)
            {
                RebuildMatches();
            }
        }
    }

    /// <summary>
    /// Gets the number of matches for the current search query.
    /// </summary>
    public int MatchCount => _matches.Count;

    /// <summary>
    /// Gets the active match index within <see cref="MatchCount"/>, or <c>-1</c> when there is no active match.
    /// </summary>
    public int ActiveMatchIndex => _activeMatchIndex;

    /// <summary>
    /// Gets a value indicating whether the control is currently following the tail (auto-scrolling on append).
    /// </summary>
    public bool FollowTail { get; private set; } = true;

    /// <summary>
    /// Scrolls to the end of the log and enables follow-tail mode so subsequent appends keep the view pinned to the bottom.
    /// </summary>
    /// <remarks>
    /// This method clears any active selection, as a selection pins the viewport and disables follow-tail behavior.
    /// </remarks>
    public void ScrollToTail()
    {
        VerifyAccess();
        ClearSelection();
        FollowTail = true;
        _pendingFollowTailScroll = true;

        // Try to apply immediately when we have a known viewport; otherwise, the pending flag will be applied on the next Arrange.
        ApplyFollowTailIfNeeded();

        // Ensure the ScrollViewer re-arranges in case the offset was changed after its last Arrange call.
        _scrollViewer.MarkArrangeDirty();
    }

    /// <summary>
    /// Gets a value indicating whether a selection is active.
    /// </summary>
    public bool HasSelection => _selectionAnchor is not null && _selectionActive is not null && _selectionAnchor.Value != _selectionActive.Value;

    /// <summary>
    /// Gets a bindable version number used to invalidate rendering when selection or search state changes.
    /// </summary>
    [Bindable]
    internal partial int InteractionVersion { get; set; }

    /// <summary>
    /// Appends a plain text log entry.
    /// </summary>
    /// <param name="message">The message to append. Newlines are split into multiple entries.</param>
    public void AppendLine(string message)
    {
        VerifyAccess();
        if (string.IsNullOrEmpty(message))
        {
            AppendEntry(string.Empty, runs: null);
            return;
        }

        AppendLinesCore(message.AsSpan(), isMarkup: false);
    }

    /// <summary>
    /// Appends a markup-formatted log entry.
    /// </summary>
    /// <param name="markup">The markup text to append. Newlines are split into multiple entries.</param>
    public void AppendMarkupLine(string markup)
    {
        VerifyAccess();
        AppendLinesCore((markup ?? string.Empty).AsSpan(), isMarkup: true);
    }

    /// <summary>
    /// Appends a markup-formatted log entry from an interpolated string handler.
    /// </summary>
    /// <param name="handler">The interpolated markup handler.</param>
    public void AppendMarkupLine(ref AnsiMarkupInterpolatedStringHandler handler)
    {
        VerifyAccess();
        AppendLinesCore(handler.WrittenSpan, isMarkup: true);
    }

    /// <summary>
    /// Clears all log entries.
    /// </summary>
    public void Clear()
    {
        VerifyAccess();

        if (_entries.Count == 0)
        {
            return;
        }

        _entries.Clear();
        ClearSelection();
        RebuildMatches();
        _scrollViewer.VerticalOffset = 0;
        _pendingFollowTailScroll = false;
    }

    /// <summary>
    /// Searches for the specified text and highlights matches.
    /// </summary>
    /// <param name="searchText">The search query.</param>
    public void Search(string searchText)
    {
        VerifyAccess();
        SearchText = searchText;
        // SearchText may be bound; ensure we still compute locally.
        RebuildMatches();
    }

    /// <summary>
    /// Scrolls to the next match and selects it as active.
    /// </summary>
    public void GoToNextMatch()
    {
        VerifyAccess();
        if (_matches.Count == 0)
        {
            return;
        }

        _activeMatchIndex = _activeMatchIndex < 0 ? 0 : (_activeMatchIndex + 1) % _matches.Count;
        ScrollToMatch(_activeMatchIndex);
        InteractionVersion++;
    }

    /// <summary>
    /// Scrolls to the previous match and selects it as active.
    /// </summary>
    public void GoToPreviousMatch()
    {
        VerifyAccess();
        if (_matches.Count == 0)
        {
            return;
        }

        if (_activeMatchIndex < 0)
        {
            _activeMatchIndex = 0;
        }
        else
        {
            _activeMatchIndex--;
            if (_activeMatchIndex < 0)
            {
                _activeMatchIndex = _matches.Count - 1;
            }
        }

        ScrollToMatch(_activeMatchIndex);
        InteractionVersion++;
    }

    /// <summary>
    /// Opens the built-in search UI.
    /// </summary>
    public void OpenSearch()
    {
        VerifyAccess();
        _searchPopup.OpenFind(SearchText);
    }

    /// <summary>
    /// Closes the built-in search UI.
    /// </summary>
    public void CloseSearch()
    {
        VerifyAccess();
        _searchPopup.Close();
    }

    partial void OnMaxCapacityChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnWrapTextChanged(bool value)
    {
        _scrollViewer.HorizontalScrollEnabled = !value;
        // Reflow changes line wrapping and match positions; rebuild match table.
        RebuildMatches();
    }

    partial void OnSearchTextChanged(string? value)
    {
        _ = value;
        RebuildMatches();
    }

    partial void OnSearchCaseSensitiveChanged(bool value)
    {
        _ = value;
        if (_bulkUpdatingSearchFlags) return;
        RebuildMatches();
    }

    partial void OnSearchWholeWordChanged(bool value)
    {
        _ = value;
        if (_bulkUpdatingSearchFlags) return;
        RebuildMatches();
    }

    partial void OnSearchRegexChanged(bool value)
    {
        _ = value;
        if (_bulkUpdatingSearchFlags) return;
        RebuildMatches();
    }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = GetStyle<LogControlStyle>();
        var padding = style.Padding;

        var innerConstraints = new LayoutConstraints(
            0,
            Math.Max(0, constraints.MaxWidth - padding.Horizontal),
            0,
            Math.Max(0, constraints.MaxHeight - padding.Vertical));
        _scrollViewer.Measure(innerConstraints);

        // Add padding back for the control.
        var desiredInner = _scrollViewer.DesiredSize;
        var desired = new Size(
            Math.Max(0, desiredInner.Width + padding.Horizontal),
            Math.Max(0, desiredInner.Height + padding.Vertical));

        return SizeHints.Flex(
            min: constraints.Clamp(new Size(4, 1)),
            natural: constraints.Clamp(desired),
            max: new Size(LayoutConstants.Infinite, LayoutConstants.Infinite),
            growX: HorizontalAlignment == Align.Stretch ? 1 : 0,
            growY: VerticalAlignment == Align.Stretch ? 1 : 0,
            shrinkX: 1,
            shrinkY: 1);
    }

    /// <inheritdoc/>
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var style = GetStyle<LogControlStyle>();
        var padding = style.Padding;

        var innerRect = new Rectangle(
            finalRect.X + padding.Left,
            finalRect.Y + padding.Top,
            Math.Max(0, finalRect.Width - padding.Horizontal),
            Math.Max(0, finalRect.Height - padding.Vertical));

        _scrollViewer.Arrange(innerRect);

        // Anchor search popup to the top-right inside the padded viewport.
        _searchPopup.ArrangeWithin(innerRect);

        if (ApplyFollowTailIfNeeded())
        {
            // Follow-tail can change the ScrollViewer offset after it was arranged; ensure the content host is re-arranged
            // in the same pass so the view updates immediately (without relying on a second layout tick).
            _scrollViewer.Arrange(innerRect);
        }

        UpdateFollowTailState();
    }

    /// <inheritdoc/>
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        // Selection and match highlights live in the content visual, but the main control fills the background.
        var style = GetStyle<LogControlStyle>();
        var theme = GetTheme();
        var focused = ReferenceEquals(App?.FocusedElement, this);
        var background = style.BackgroundStyle(theme, focused);
        var padding = style.Padding;

        var innerRect = new Rectangle(
            rect.X + padding.Left,
            rect.Y + padding.Top,
            Math.Max(0, rect.Width - padding.Horizontal),
            Math.Max(0, rect.Height - padding.Vertical));

        for (var y = innerRect.Y; y < innerRect.Bottom; y++)
        {
            for (var x = innerRect.X; x < innerRect.Right; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), background);
            }
        }

        // Ensure selection/matches participate in dependency tracking for rendering.
        _ = InteractionVersion;
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Ctrl+F: open search.
        if ((e.Modifiers & TerminalModifiers.Ctrl) != 0 && e.Char is TerminalChar.CtrlF)
        {
            OpenSearch();
            e.Handled = true;
            return;
        }

        if ((e.Modifiers & TerminalModifiers.Ctrl) != 0 && e.Char is TerminalChar.CtrlA)
        {
            SelectAll();
            e.Handled = true;
            return;
        }

        if ((e.Modifiers & TerminalModifiers.Ctrl) != 0 && e.Char is TerminalChar.CtrlC)
        {
            CopySelectionToClipboard();
            e.Handled = true;
            return;
        }

        // Without Shift, arrows scroll and clear selection.
        if ((e.Modifiers & TerminalModifiers.Shift) == 0 && HasSelection
            && e.Key is TerminalKey.Up or TerminalKey.Down or TerminalKey.Left or TerminalKey.Right
                or TerminalKey.PageUp or TerminalKey.PageDown or TerminalKey.Home or TerminalKey.End)
        {
            ClearSelection();
        }

        var viewportHeight = Math.Max(1, _scrollViewer.ViewportHeight);
        var maxOffset = Math.Max(0, _content.ExtentHeight - viewportHeight);
        var page = Math.Max(1, viewportHeight - 1);

        switch (e.Key)
        {
            case TerminalKey.Up:
                _scrollViewer.VerticalOffset = Math.Max(0, _scrollViewer.VerticalOffset - 1);
                FollowTail = false;
                e.Handled = true;
                return;
            case TerminalKey.Down:
                _scrollViewer.VerticalOffset = Math.Min(maxOffset, _scrollViewer.VerticalOffset + 1);
                FollowTail = _scrollViewer.VerticalOffset >= maxOffset;
                e.Handled = true;
                return;
            case TerminalKey.PageUp:
                _scrollViewer.VerticalOffset = Math.Max(0, _scrollViewer.VerticalOffset - page);
                FollowTail = false;
                e.Handled = true;
                return;
            case TerminalKey.PageDown:
                _scrollViewer.VerticalOffset = Math.Min(maxOffset, _scrollViewer.VerticalOffset + page);
                FollowTail = _scrollViewer.VerticalOffset >= maxOffset;
                e.Handled = true;
                return;
            case TerminalKey.Home:
                _scrollViewer.VerticalOffset = 0;
                FollowTail = false;
                e.Handled = true;
                return;
            case TerminalKey.End:
                _scrollViewer.VerticalOffset = maxOffset;
                FollowTail = true;
                e.Handled = true;
                return;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerWheel(PointerEventArgs e)
    {
        // Preview: disable follow-tail when the user scrolls.
        if (e.Kind == TerminalMouseKind.Wheel && e.WheelDelta != 0)
        {
            FollowTail = false;
        }
    }

    private string GetSearchStatusText()
    {
        // Ensure this participates in dependency tracking: search text/options and entries version affect matches.
        _ = _entries.Version;
        _ = InteractionVersion;

        if (string.IsNullOrEmpty(SearchText))
        {
            return "No search";
        }

        if (_matches.Count == 0)
        {
            return "0 matches";
        }

        var active = _activeMatchIndex < 0 ? 0 : _activeMatchIndex + 1;
        return $"{active}/{_matches.Count}";
    }

    private string? GetSearchErrorText()
    {
        _ = _entries.Version;
        _ = InteractionVersion;
        return _searchError;
    }

    private void AppendLinesCore(ReadOnlySpan<char> text, bool isMarkup)
    {
        // Split the input into hard lines (Append*Line implies line semantics).
        if (isMarkup)
        {
            var markup = text.ToString();
            var parsed = _markupParser.Parse(markup, out var runs, GetTheme().GetMarkupStyles());
            AppendParsedTextAsLines(parsed, runs);
        }
        else
        {
            AppendParsedTextAsLines(text.ToString(), runs: null);
        }

        TrimToCapacity();
        if (FollowTail && !HasSelection)
        {
            _pendingFollowTailScroll = true;
            _scrollViewer.VerticalOffset = int.MaxValue;
        }

        if (!string.IsNullOrEmpty(SearchText))
        {
            RebuildMatches();
        }
    }

    private void AppendParsedTextAsLines(string plainText, StyledRun[]? runs)
    {
        if (plainText.Length == 0)
        {
            AppendEntry(string.Empty, runs: runs);
            return;
        }

        var start = 0;
        for (var i = 0; i < plainText.Length; i++)
        {
            var ch = plainText[i];
            if (ch is not '\n' and not '\r')
            {
                continue;
            }

            var end = i;
            if (ch == '\r' && i + 1 < plainText.Length && plainText[i + 1] == '\n')
            {
                i++;
            }

            AppendEntry(plainText.Substring(start, end - start), SliceRunsForLine(runs, start, end));
            start = i + 1;
        }

        if (start <= plainText.Length)
        {
            AppendEntry(plainText.Substring(start), SliceRunsForLine(runs, start, plainText.Length));
        }
    }

    private StyledRun[]? SliceRunsForLine(StyledRun[]? runs, int lineStart, int lineEnd)
    {
        if (runs is null || runs.Length == 0)
        {
            return runs;
        }

        List<StyledRun>? list = null;
        for (var i = 0; i < runs.Length; i++)
        {
            var run = runs[i];
            var runStart = run.Start;
            var runEnd = runStart + run.Length;

            if (runEnd <= lineStart)
            {
                continue;
            }

            if (runStart >= lineEnd)
            {
                break;
            }

            var s = Math.Max(runStart, lineStart);
            var e = Math.Min(runEnd, lineEnd);
            if (e <= s)
            {
                continue;
            }

            list ??= new List<StyledRun>(4);
            list.Add(new StyledRun(s - lineStart, e - s, run.Style));
        }

        if (list is null || list.Count == 0)
        {
            return Array.Empty<StyledRun>();
        }

        return list.ToArray();
    }

    private void AppendEntry(string text, StyledRun[]? runs)
    {
        _entries.Add(new LogEntry(text, runs));
    }

    private void TrimToCapacity()
    {
        var cap = MaxCapacity;
        if (cap <= 0)
        {
            return;
        }

        if (_entries.Count <= cap)
        {
            return;
        }

        var excess = _entries.Count - cap;
        if (excess <= 0)
        {
            return;
        }

        // When not following the tail, keep the viewport stable by shifting offsets by the removed height.
        if (!FollowTail)
        {
            var removedRows = 0;
            var wrap = WrapText;
            var width = _content.LastWrapWidth;
            for (var i = 0; i < excess; i++)
            {
                removedRows += _entries[i].GetRowCount(wrap, width);
            }

            if (removedRows > 0)
            {
                _scrollViewer.VerticalOffset = Math.Max(0, _scrollViewer.VerticalOffset - removedRows);
            }
        }

        for (var i = 0; i < excess; i++)
        {
            _entries.RemoveAt(0);
        }

        ClearSelection();
        RebuildMatches();
    }

    private bool ApplyFollowTailIfNeeded()
    {
        if (!_pendingFollowTailScroll || !FollowTail || HasSelection)
        {
            return false;
        }

        var viewportHeight = Math.Max(1, _scrollViewer.ViewportHeight);
        var maxOffset = Math.Max(0, _content.ExtentHeight - viewportHeight);
        _pendingFollowTailScroll = false;

        if (_scrollViewer.VerticalOffset == maxOffset)
        {
            return false;
        }

        _scrollViewer.VerticalOffset = maxOffset;
        return true;
    }

    private void UpdateFollowTailState()
    {
        if (HasSelection)
        {
            FollowTail = false;
            return;
        }

        var viewportHeight = Math.Max(1, _scrollViewer.ViewportHeight);
        var maxOffset = Math.Max(0, _content.ExtentHeight - viewportHeight);
        FollowTail = _scrollViewer.VerticalOffset >= maxOffset;
    }

    private void RebuildMatches()
    {
        _searchError = null;
        _matches.Clear();
        _matchesByEntry = null;
        _activeMatchIndex = -1;

        var query = SearchText ?? string.Empty;
        if (string.IsNullOrEmpty(query) || _entries.Count == 0)
        {
            InteractionVersion++;
            return;
        }

        try
        {
            BuildMatches(query);
        }
        catch (ArgumentException ex) when (SearchRegex)
        {
            // Invalid regex; expose an error without throwing.
            _searchError = ex.Message;
            _matches.Clear();
            _matchesByEntry = null;
            _activeMatchIndex = -1;
        }

        if (_matches.Count > 0)
        {
            _activeMatchIndex = 0;
        }

        InteractionVersion++;
    }

    private void BuildMatches(string query)
    {
        var byEntry = new List<LogMatch>?[_entries.Count];

        if (SearchRegex)
        {
            var pattern = SearchWholeWord ? $"\\b(?:{query})\\b" : query;
            var options = RegexOptions.CultureInvariant;
            if (!SearchCaseSensitive)
            {
                options |= RegexOptions.IgnoreCase;
            }

            var regex = new Regex(pattern, options);
            for (var entryIndex = 0; entryIndex < _entries.Count; entryIndex++)
            {
                var text = _entries[entryIndex].Text;
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                foreach (Match match in regex.Matches(text))
                {
                    if (!match.Success || match.Length <= 0)
                    {
                        continue;
                    }

                    var m = new LogMatch(entryIndex, match.Index, match.Length);
                    _matches.Add(m);
                    (byEntry[entryIndex] ??= new List<LogMatch>(4)).Add(m);
                }
            }
        }
        else
        {
            var comparison = SearchCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            for (var entryIndex = 0; entryIndex < _entries.Count; entryIndex++)
            {
                var text = _entries[entryIndex].Text;
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                var start = 0;
                while (start < text.Length)
                {
                    var found = text.IndexOf(query, start, comparison);
                    if (found < 0)
                    {
                        break;
                    }

                    var ok = true;
                    if (SearchWholeWord)
                    {
                        ok = IsWordBoundary(text, found, query.Length);
                    }

                    if (ok)
                    {
                        var m = new LogMatch(entryIndex, found, query.Length);
                        _matches.Add(m);
                        (byEntry[entryIndex] ??= new List<LogMatch>(4)).Add(m);
                    }

                    start = found + Math.Max(1, query.Length);
                }
            }
        }

        _matchesByEntry = byEntry;
    }

    private static bool IsWordBoundary(string text, int start, int length)
    {
        static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

        var before = start > 0 ? text[start - 1] : '\0';
        var afterIndex = start + length;
        var after = afterIndex < text.Length ? text[afterIndex] : '\0';

        var beforeOk = start == 0 || !IsWordChar(before);
        var afterOk = afterIndex >= text.Length || !IsWordChar(after);
        return beforeOk && afterOk;
    }

    private void ScrollToMatch(int matchIndex)
    {
        if ((uint)matchIndex >= (uint)_matches.Count)
        {
            return;
        }

        FollowTail = false;
        var match = _matches[matchIndex];
        if (!_content.TryGetContentLocation(match, out var row))
        {
            return;
        }

        var viewportHeight = Math.Max(1, _scrollViewer.ViewportHeight);
        var maxOffset = Math.Max(0, _content.ExtentHeight - viewportHeight);

        var offset = _scrollViewer.VerticalOffset;
        if (row < offset)
        {
            offset = row;
        }
        else if (row >= offset + viewportHeight)
        {
            offset = Math.Max(0, row - viewportHeight + 1);
        }

        _scrollViewer.VerticalOffset = Math.Clamp(offset, 0, maxOffset);
    }

    private void SelectAll()
    {
        if (_entries.Count == 0)
        {
            return;
        }

        _selectionAnchor = new LogTextPosition(0, 0);
        var lastIndex = _entries.Count - 1;
        _selectionActive = new LogTextPosition(lastIndex, _entries[lastIndex].Text.Length);
        FollowTail = false;
        InteractionVersion++;
    }

    private void ClearSelection()
    {
        if (_selectionAnchor is null && _selectionActive is null)
        {
            return;
        }

        _selectionAnchor = null;
        _selectionActive = null;
        InteractionVersion++;
    }

    private void CopySelectionToClipboard()
    {
        if (!HasSelection)
        {
            return;
        }

        var (start, end) = GetOrderedSelection();
        var sb = new StringBuilder();
        for (var entryIndex = start.EntryIndex; entryIndex <= end.EntryIndex; entryIndex++)
        {
            var text = _entries[entryIndex].Text;
            var from = entryIndex == start.EntryIndex ? Math.Clamp(start.TextIndex, 0, text.Length) : 0;
            var to = entryIndex == end.EntryIndex ? Math.Clamp(end.TextIndex, 0, text.Length) : text.Length;
            if (to > from)
            {
                sb.Append(text.AsSpan(from, to - from));
            }

            if (entryIndex != end.EntryIndex)
            {
                sb.Append('\n');
            }
        }

        if (sb.Length > 0)
        {
            App?.Terminal.Clipboard.TrySetText(sb.ToString());
        }
    }

    private (LogTextPosition Start, LogTextPosition End) GetOrderedSelection()
    {
        var a = _selectionAnchor ?? default;
        var b = _selectionActive ?? default;
        return a.CompareTo(b) <= 0 ? (a, b) : (b, a);
    }

    private bool TryGetSelectionForEntry(int entryIndex, out int start, out int end)
    {
        start = 0;
        end = 0;

        if (!HasSelection)
        {
            return false;
        }

        var (s, e) = GetOrderedSelection();
        if (entryIndex < s.EntryIndex || entryIndex > e.EntryIndex)
        {
            return false;
        }

        var text = _entries[entryIndex].Text;
        start = entryIndex == s.EntryIndex ? Math.Clamp(s.TextIndex, 0, text.Length) : 0;
        end = entryIndex == e.EntryIndex ? Math.Clamp(e.TextIndex, 0, text.Length) : text.Length;
        return end > start;
    }

    private bool TryGetMatchesForEntry(int entryIndex, out List<LogMatch>? list)
    {
        list = null;
        var byEntry = _matchesByEntry;
        if (byEntry is null || (uint)entryIndex >= (uint)byEntry.Length)
        {
            return false;
        }

        list = byEntry[entryIndex];
        return list is { Count: > 0 };
    }

    private sealed record LogEntry(string Text, StyledRun[]? Runs)
    {
        private int _cachedWrapWidth;
        private bool _cachedWrap;
        private int _cachedRowCount = 1;

        public int GetRowCount(bool wrap, int wrapWidth)
        {
            if (!wrap)
            {
                _cachedWrap = false;
                _cachedWrapWidth = 0;
                _cachedRowCount = 1;
                return 1;
            }

            wrapWidth = Math.Max(1, wrapWidth);
            if (_cachedWrap && _cachedWrapWidth == wrapWidth)
            {
                return _cachedRowCount;
            }

            _cachedWrap = true;
            _cachedWrapWidth = wrapWidth;
            _cachedRowCount = Math.Max(1, LogContentVisual.CountWrappedLines(Text.AsSpan(), wrapWidth));
            return _cachedRowCount;
        }
    }

    private readonly record struct LogMatch(int EntryIndex, int Start, int Length);

    private readonly record struct LogTextPosition(int EntryIndex, int TextIndex) : IComparable<LogTextPosition>
    {
        public int CompareTo(LogTextPosition other)
        {
            var c = EntryIndex.CompareTo(other.EntryIndex);
            return c != 0 ? c : TextIndex.CompareTo(other.TextIndex);
        }
    }

    private sealed class LogSearchTarget : ISearchReplaceTarget
    {
        private readonly LogControl _owner;

        public LogSearchTarget(LogControl owner)
        {
            _owner = owner;
        }

        public string Title => "Search";

        public bool SupportsReplace => false;

        public void SetQuery(in SearchQuery query)
        {
            var bulk = _owner._bulkUpdatingSearchFlags;
            _owner._bulkUpdatingSearchFlags = true;
            try
            {
                _owner.SearchText = query.Text;
                _owner.SearchCaseSensitive = query.CaseSensitive;
                _owner.SearchWholeWord = query.WholeWord;
                _owner.SearchRegex = query.UseRegex;
            }
            finally
            {
                _owner._bulkUpdatingSearchFlags = bulk;
            }

            if (!bulk)
            {
                _owner.RebuildMatches();
            }
        }

        public void NextMatch() => _owner.GoToNextMatch();

        public void PreviousMatch() => _owner.GoToPreviousMatch();

        public int ReplaceCurrent(string replacement)
        {
            _ = replacement;
            return 0;
        }

        public int ReplaceAll(string replacement)
        {
            _ = replacement;
            return 0;
        }

        public string GetStatusText() => _owner.GetSearchStatusText();

        public string? GetErrorText() => _owner.GetSearchErrorText();
    }

    private sealed class LogContentVisual : Visual
    {
        private readonly LogControl _owner;
        private int[]? _rowPrefix;
        private int _lastWrapWidth;
        private int _extentHeight;
        private int _extentWidth;

        public LogContentVisual(LogControl owner)
        {
            _owner = owner;
            HorizontalAlignment = Align.Stretch;
            VerticalAlignment = Align.Stretch;
            Focusable = false;
        }

        public int ExtentHeight => _extentHeight;

        public int ExtentWidth => _extentWidth;

        public int LastWrapWidth => _lastWrapWidth;

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            var wrap = _owner.WrapText;
            var maxWidth = Math.Max(0, constraints.MaxWidth);
            var maxHeight = Math.Max(0, constraints.MaxHeight);

            _lastWrapWidth = constraints.IsWidthBounded ? Math.Max(1, maxWidth) : Math.Max(1, maxWidth);

            _ = _owner._entries.Version;
            _ = _owner.InteractionVersion;

            var entries = _owner._entries;
            var count = entries.Count;

            var maxLineCells = 0;
            for (var i = 0; i < count; i++)
            {
                var w = MeasureCellWidth(entries[i].Text.AsSpan());
                maxLineCells = Math.Max(maxLineCells, w);
            }
            _extentWidth = maxLineCells;

            var rowPrefix = _rowPrefix;
            if (rowPrefix is null || rowPrefix.Length != count + 1)
            {
                rowPrefix = new int[count + 1];
                _rowPrefix = rowPrefix;
            }

            var totalRows = 0;
            rowPrefix[0] = 0;
            for (var i = 0; i < count; i++)
            {
                var rows = wrap ? entries[i].GetRowCount(wrap: true, _lastWrapWidth) : 1;
                totalRows += rows;
                rowPrefix[i + 1] = totalRows;
            }

            _extentHeight = totalRows;

            var desiredWidth = constraints.IsWidthBounded ? Math.Min(maxWidth, maxLineCells) : maxLineCells;
            var desiredHeight = constraints.IsHeightBounded ? Math.Min(maxHeight, totalRows) : totalRows;

            var shrinkX = wrap ? 1 : 0;

            return SizeHints.Flex(
                min: Size.Zero,
                natural: new Size(Math.Max(0, desiredWidth), Math.Max(0, desiredHeight)),
                max: new Size(LayoutConstants.Infinite, LayoutConstants.Infinite),
                growX: 1,
                growY: 1,
                shrinkX: shrinkX,
                shrinkY: 1);
        }

        protected override void ArrangeCore(in Rectangle finalRect)
        {
            Bounds = finalRect;
        }

        protected override void RenderOverride(CellBuffer buffer)
        {
            var rect = Bounds;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            var entries = _owner._entries;
            var count = entries.Count;
            if (count == 0)
            {
                return;
            }

            var wrap = _owner.WrapText;
            var wrapWidth = Math.Max(1, rect.Width);

            var theme = GetTheme();
            var logStyle = GetStyle<LogControlStyle>();
            var searchStyle = GetStyle<LogControlSearchStyle>();
            var focused = ReferenceEquals(App?.FocusedElement, _owner);
            var baseStyle = logStyle.BackgroundStyle(theme, focused);
            var selectionStyle = logStyle.SelectionStyle(theme);
            var matchStyle = searchStyle.ResolveMatchStyle(theme);
            var activeMatchStyle = searchStyle.ResolveActiveMatchStyle(theme);

            var firstScreenY = Math.Max(0, rect.Y);
            var lastScreenY = Math.Min(buffer.Height, rect.Bottom);
            var firstContentRow = Math.Max(0, firstScreenY - rect.Y);
            var lastContentRow = Math.Min(_extentHeight, lastScreenY - rect.Y);

            if (_rowPrefix is null || _rowPrefix.Length != count + 1)
            {
                return;
            }

            var rowPrefix = _rowPrefix;
            var row = firstContentRow;
            var screenY = rect.Y + row;

            if (!TryMapRowToEntry(rowPrefix, row, out var entryIndex, out var rowInEntry))
            {
                return;
            }

            List<LogMatch>? matchList = null;
            var matchListIndex = 0;
            _ = _owner.TryGetMatchesForEntry(entryIndex, out matchList);

            while (row < lastContentRow && screenY < lastScreenY)
            {
                if (entryIndex >= count)
                {
                    break;
                }

                var entry = entries[entryIndex];
                var lineText = entry.Text.AsSpan();

                var sliceStart = 0;
                var sliceEnd = lineText.Length;
                if (wrap)
                {
                    GetWrapSliceForRow(lineText, wrapWidth, rowInEntry, out sliceStart, out sliceEnd);
                }

                var fillStartX = Math.Max(0, rect.X);
                var fillEndX = Math.Min(buffer.Width, rect.Right);
                for (var x = fillStartX; x < fillEndX; x++)
                {
                    buffer.SetCell(x, screenY, new Rune(' '), baseStyle);
                }

                RenderLine(buffer, rect.X, screenY, baseStyle, selectionStyle, matchStyle, activeMatchStyle,
                    entryIndex, entry, sliceStart, sliceEnd, matchList, ref matchListIndex, _owner);

                row++;
                screenY++;
                rowInEntry++;

                if (wrap)
                {
                    var rowCount = entry.GetRowCount(wrap, wrapWidth);
                    if (rowInEntry >= rowCount)
                    {
                        entryIndex++;
                        rowInEntry = 0;
                        matchListIndex = 0;
                        _ = _owner.TryGetMatchesForEntry(entryIndex, out matchList);
                    }
                }
                else
                {
                    entryIndex++;
                    rowInEntry = 0;
                    matchListIndex = 0;
                    _ = _owner.TryGetMatchesForEntry(entryIndex, out matchList);
                }
            }
        }

        protected override void OnPointerPressed(PointerEventArgs e)
        {
            if (e.Button != TerminalMouseButton.Left)
            {
                return;
            }

            if (!TryGetTextPositionAtPointer(e.LocalX, e.LocalY, out var pos))
            {
                return;
            }

            _owner.FollowTail = false;

            if ((e.Modifiers & TerminalModifiers.Shift) != 0 && _owner._selectionAnchor is { } existingAnchor)
            {
                _owner._selectionAnchor = existingAnchor;
                _owner._selectionActive = pos;
            }
            else
            {
                _owner._selectionAnchor = pos;
                _owner._selectionActive = pos;
            }

            _owner.InteractionVersion++;
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (e.Kind != TerminalMouseKind.Drag || _owner._selectionAnchor is null)
            {
                return;
            }

            if (!TryGetTextPositionAtPointer(e.LocalX, e.LocalY, out var pos))
            {
                return;
            }

            _owner.FollowTail = false;
            _owner._selectionActive = pos;
            _owner.InteractionVersion++;
            e.Handled = true;
        }

        private bool TryGetTextPositionAtPointer(int localX, int localY, out LogTextPosition pos)
        {
            pos = default;

            var entries = _owner._entries;
            if (entries.Count == 0 || localY < 0 || localX < 0)
            {
                return false;
            }

            if (_rowPrefix is null || _rowPrefix.Length != entries.Count + 1)
            {
                return false;
            }

            var contentRow = localY;
            if (contentRow < 0 || contentRow >= _extentHeight)
            {
                // Clamp to last available row.
                if (_extentHeight <= 0)
                {
                    return false;
                }

                contentRow = Math.Clamp(contentRow, 0, _extentHeight - 1);
            }

            if (!TryMapRowToEntry(_rowPrefix, contentRow, out var entryIndex, out var rowInEntry))
            {
                return false;
            }

            var entry = entries[entryIndex];
            var text = entry.Text.AsSpan();
            var wrap = _owner.WrapText;
            var wrapWidth = Math.Max(1, Bounds.Width);

            var sliceStart = 0;
            var sliceEnd = text.Length;
            if (wrap)
            {
                GetWrapSliceForRow(text, wrapWidth, rowInEntry, out sliceStart, out sliceEnd);
            }

            var slice = text.Slice(sliceStart, Math.Max(0, sliceEnd - sliceStart));
            var rel = GetStartIndexAtCell(slice, localX);
            pos = new LogTextPosition(entryIndex, Math.Clamp(sliceStart + rel, 0, text.Length));
            return true;
        }

        public bool TryGetContentLocation(LogMatch match, out int row)
        {
            row = 0;
            var entries = _owner._entries;
            if ((uint)match.EntryIndex >= (uint)entries.Count)
            {
                return false;
            }

            if (_rowPrefix is null || _rowPrefix.Length != entries.Count + 1)
            {
                return false;
            }

            var wrap = _owner.WrapText;
            if (!wrap)
            {
                row = match.EntryIndex;
                return true;
            }

            var rect = Bounds;
            var wrapWidth = Math.Max(1, rect.Width);
            var entry = entries[match.EntryIndex];
            var localRow = GetRowIndexForTextIndex(entry.Text.AsSpan(), wrapWidth, match.Start);
            row = _rowPrefix[match.EntryIndex] + localRow;
            return true;
        }

        private static void RenderLine(
            CellBuffer buffer,
            int x,
            int y,
            Style baseStyle,
            Style selectionStyle,
            Style matchStyle,
            Style activeMatchStyle,
            int entryIndex,
            LogEntry entry,
            int sliceStart,
            int sliceEnd,
            List<LogMatch>? matches,
            ref int matchListIndex,
            LogControl owner)
        {
            if (sliceEnd <= sliceStart)
            {
                return;
            }

            var text = entry.Text.AsSpan();
            var lineSpan = text.Slice(sliceStart, sliceEnd - sliceStart);

            // Skip leading cells when horizontally scrolled (Bounds.X can be negative).
            var posX = x;
            var skipCells = 0;
            if (posX < 0)
            {
                skipCells = -posX;
                posX = 0;
            }

            var startIndexInSlice = skipCells > 0 ? GetStartIndexAtCell(lineSpan, skipCells) : 0;
            var startIndex = sliceStart + startIndexInSlice;
            if (startIndex >= sliceEnd)
            {
                return;
            }

            // Advance match cursor to the first match that can intersect this slice.
            if (matches is not null)
            {
                while (matchListIndex < matches.Count)
                {
                    var m = matches[matchListIndex];
                    if (m.Start + m.Length <= startIndex)
                    {
                        matchListIndex++;
                        continue;
                    }
                    break;
                }
            }

            var runs = entry.Runs;
            if (runs is null || runs.Length == 0)
            {
                WriteWithHighlights(buffer, posX, y, baseStyle, selectionStyle, matchStyle, activeMatchStyle,
                    entryIndex, startIndex, sliceEnd, text, matches, matchListIndex, owner);
                return;
            }

            // Runs are relative to the entry start.
            var renderEnd = sliceEnd;
            var cursor = startIndex;
            for (var i = 0; i < runs.Length && cursor < renderEnd; i++)
            {
                var run = runs[i];
                var runStart = run.Start;
                var runEnd = runStart + run.Length;

                if (runEnd <= cursor)
                {
                    continue;
                }

                if (runStart >= renderEnd)
                {
                    break;
                }

                var segStart = Math.Max(cursor, runStart);
                var segEnd = Math.Min(renderEnd, runEnd);
                if (segEnd <= segStart)
                {
                    continue;
                }

                var effective = run.Style.MergeUnspecified(baseStyle);
                WriteWithHighlights(buffer, posX, y, effective, selectionStyle, matchStyle, activeMatchStyle,
                    entryIndex, segStart, segEnd, text, matches, matchListIndex, owner);
                posX += MeasureCellWidth(text.Slice(segStart, segEnd - segStart));
                cursor = segEnd;
            }

            if (cursor < renderEnd)
            {
                WriteWithHighlights(buffer, posX, y, baseStyle, selectionStyle, matchStyle, activeMatchStyle,
                    entryIndex, cursor, renderEnd, text, matches, matchListIndex, owner);
            }
        }

        private static void WriteWithHighlights(
            CellBuffer buffer,
            int x,
            int y,
            Style baseStyle,
            Style selectionStyle,
            Style matchStyle,
            Style activeMatchStyle,
            int entryIndex,
            int startIndex,
            int endIndex,
            ReadOnlySpan<char> fullText,
            List<LogMatch>? matches,
            int matchCursor,
            LogControl owner)
        {
            if (startIndex >= endIndex || x >= buffer.Width)
            {
                return;
            }

            var selectionStart = int.MaxValue;
            var selectionEnd = int.MinValue;
            if (owner.TryGetSelectionForEntry(entryIndex, out var s, out var e))
            {
                selectionStart = s;
                selectionEnd = e;
            }

            LogMatch? activeMatch = owner._activeMatchIndex >= 0 && owner._activeMatchIndex < owner._matches.Count
                ? owner._matches[owner._activeMatchIndex]
                : null;

            var pos = startIndex;
            var posX = x;

            while (pos < endIndex && posX < buffer.Width)
            {
                var nextBoundary = endIndex;

                // Selection boundary.
                if (selectionStart < selectionEnd)
                {
                    if (pos < selectionStart)
                    {
                        nextBoundary = Math.Min(nextBoundary, selectionStart);
                    }
                    else if (pos < selectionEnd)
                    {
                        nextBoundary = Math.Min(nextBoundary, selectionEnd);
                    }
                }

                // Match boundary.
                LogMatch? currentMatch = null;
                if (matches is not null && matchCursor < matches.Count)
                {
                    var m = matches[matchCursor];
                    if (m.Start < endIndex && m.Start + m.Length > pos)
                    {
                        currentMatch = m;
                        if (pos < m.Start)
                        {
                            nextBoundary = Math.Min(nextBoundary, m.Start);
                        }
                        else
                        {
                            nextBoundary = Math.Min(nextBoundary, m.Start + m.Length);
                        }
                    }
                    else if (m.Start + m.Length <= pos)
                    {
                        matchCursor++;
                        continue;
                    }
                }

                if (nextBoundary <= pos)
                {
                    nextBoundary = Math.Min(endIndex, pos + 1);
                }

                var style = baseStyle;
                if (selectionStart < selectionEnd && pos >= selectionStart && pos < selectionEnd)
                {
                    style |= selectionStyle;
                }
                else if (currentMatch is { } cm && pos >= cm.Start && pos < cm.Start + cm.Length)
                {
                    style |= activeMatch is { } am && am.Equals(cm) ? activeMatchStyle : matchStyle;
                }

                var span = fullText.Slice(pos, nextBoundary - pos);
                buffer.WriteText(posX, y, span, style);
                posX += MeasureCellWidth(span);
                pos = nextBoundary;
            }
        }

        private static bool TryMapRowToEntry(int[] rowPrefix, int row, out int entryIndex, out int rowInEntry)
        {
            entryIndex = 0;
            rowInEntry = 0;
            if (rowPrefix.Length <= 1 || row < 0)
            {
                return false;
            }

            // Binary search for the first prefix > row.
            var lo = 0;
            var hi = rowPrefix.Length - 1;
            while (lo < hi)
            {
                var mid = (lo + hi) / 2;
                if (rowPrefix[mid] <= row)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            entryIndex = Math.Max(0, lo - 1);
            rowInEntry = row - rowPrefix[entryIndex];
            return entryIndex >= 0 && entryIndex < rowPrefix.Length - 1;
        }

        private static int GetRowIndexForTextIndex(ReadOnlySpan<char> text, int wrapWidth, int textIndex)
        {
            if (textIndex <= 0)
            {
                return 0;
            }

            wrapWidth = Math.Max(1, wrapWidth);
            var row = 0;
            var start = 0;
            while (start < text.Length)
            {
                if (!TryGetNextWrapSlice(text, start, wrapWidth, out var end, out var next))
                {
                    break;
                }

                if (textIndex >= start && textIndex < end)
                {
                    return row;
                }

                row++;
                start = next;
            }

            return row;
        }

        private static void GetWrapSliceForRow(ReadOnlySpan<char> text, int width, int rowInEntry, out int start, out int end)
        {
            start = 0;
            end = text.Length;

            if (rowInEntry <= 0)
            {
                if (TryGetNextWrapSlice(text, 0, width, out end, out _))
                {
                    start = 0;
                }
                return;
            }

            var current = 0;
            var index = 0;
            while (index < text.Length)
            {
                if (!TryGetNextWrapSlice(text, index, width, out var sliceEnd, out var next))
                {
                    break;
                }

                if (current == rowInEntry)
                {
                    start = index;
                    end = sliceEnd;
                    return;
                }

                current++;
                index = next;
            }

            start = text.Length;
            end = text.Length;
        }

        internal static int CountWrappedLines(ReadOnlySpan<char> text, int width)
        {
            if (text.IsEmpty)
            {
                return 1;
            }

            width = Math.Max(1, width);
            var count = 0;
            var start = 0;
            while (start < text.Length)
            {
                if (!TryGetNextWrapSlice(text, start, width, out _, out var next))
                {
                    break;
                }

                count++;
                if (next <= start)
                {
                    break;
                }

                start = next;
            }

            return Math.Max(1, count);
        }

        private static bool TryGetNextWrapSlice(ReadOnlySpan<char> text, int start, int width, out int endExclusive, out int nextStart)
        {
            endExclusive = start;
            nextStart = start;

            if (start >= text.Length)
            {
                return false;
            }

            width = Math.Max(1, width);

            var tentativeEnd = start + GetEndIndexAtCell(text[start..], width);
            tentativeEnd = Math.Clamp(tentativeEnd, start, text.Length);

            var wrapEnd = tentativeEnd;
            if (tentativeEnd < text.Length)
            {
                var lastSpace = -1;
                for (var i = tentativeEnd - 1; i > start; i--)
                {
                    if (char.IsWhiteSpace(text[i]))
                    {
                        lastSpace = i;
                        break;
                    }
                }

                if (lastSpace > start)
                {
                    wrapEnd = lastSpace;
                }
            }

            endExclusive = wrapEnd;

            nextStart = wrapEnd;
            while (nextStart < text.Length && char.IsWhiteSpace(text[nextStart]))
            {
                nextStart++;
            }

            return endExclusive > start;
        }

        private static int GetStartIndexAtCell(ReadOnlySpan<char> text, int cellOffset)
        {
            if (cellOffset <= 0 || text.IsEmpty)
            {
                return 0;
            }

            var x = 0;
            var index = 0;
            while (index < text.Length)
            {
                var elementLength = System.Globalization.StringInfo.GetNextTextElementLength(text[index..]);
                if (elementLength <= 0)
                {
                    elementLength = 1;
                }

                var element = text.Slice(index, Math.Min(elementLength, text.Length - index));
                var w = GetTextElementWidth(element);
                if (w <= 0)
                {
                    index += elementLength;
                    continue;
                }

                if (x + w > cellOffset)
                {
                    return index;
                }

                x += w;
                index += elementLength;
            }

            return text.Length;
        }

        private static int GetEndIndexAtCell(ReadOnlySpan<char> text, int maxCells)
        {
            if (maxCells <= 0 || text.IsEmpty)
            {
                return 0;
            }

            var x = 0;
            var index = 0;
            while (index < text.Length)
            {
                var elementLength = System.Globalization.StringInfo.GetNextTextElementLength(text[index..]);
                if (elementLength <= 0)
                {
                    elementLength = 1;
                }

                var element = text.Slice(index, Math.Min(elementLength, text.Length - index));
                var w = GetTextElementWidth(element);
                if (w <= 0)
                {
                    index += elementLength;
                    continue;
                }

                if (x + w > maxCells)
                {
                    break;
                }

                x += w;
                index += elementLength;
            }

            return index;
        }

        private static int MeasureCellWidth(ReadOnlySpan<char> text)
        {
            if (text.IsEmpty)
            {
                return 0;
            }

            var width = 0;
            var index = 0;
            while (index < text.Length)
            {
                var elementLength = System.Globalization.StringInfo.GetNextTextElementLength(text[index..]);
                if (elementLength <= 0)
                {
                    elementLength = 1;
                }

                var element = text.Slice(index, Math.Min(elementLength, text.Length - index));
                if (element.Length == 1 && element[0] == '\t')
                {
                    var tabWidth = TerminalTextUtility.DefaultTabWidth;
                    var spaces = tabWidth - (width % tabWidth);
                    spaces = Math.Max(1, spaces);
                    width += spaces;
                    index += elementLength;
                    continue;
                }

                width += Math.Max(1, GetTextElementWidth(element));
                index += elementLength;
            }

            return Math.Max(0, width);
        }

        private static int GetTextElementWidth(ReadOnlySpan<char> element)
        {
            if (element.IsEmpty)
            {
                return 0;
            }

            // Match CellBuffer's grapheme width behavior: emoji presentation sequences often need 2 cells.
            var forceDoubleWidth = false;
            for (var j = 0; j < element.Length; j++)
            {
                var ch = element[j];
                if (ch is '\uFE0F' or '\u200D' or '\u20E3')
                {
                    forceDoubleWidth = true;
                    break;
                }
            }

            if (Rune.DecodeFromUtf16(element, out var rune, out var consumed) != System.Buffers.OperationStatus.Done || consumed <= 0)
            {
                return Math.Max(1, TerminalTextUtility.GetRuneWidth(Rune.ReplacementChar));
            }

            if (element.Length == consumed)
            {
                var w = Math.Max(1, TerminalTextUtility.GetRuneWidth(rune));
                return forceDoubleWidth ? Math.Max(2, w) : w;
            }

            var max = Math.Max(0, TerminalTextUtility.GetRuneWidth(rune));
            var i = consumed;
            while (i < element.Length)
            {
                if (Rune.DecodeFromUtf16(element[i..], out var next, out var c) != System.Buffers.OperationStatus.Done || c <= 0)
                {
                    max = Math.Max(max, TerminalTextUtility.GetRuneWidth(Rune.ReplacementChar));
                    i++;
                    continue;
                }

                max = Math.Max(max, TerminalTextUtility.GetRuneWidth(next));
                i += c;
            }

            max = Math.Max(0, max);
            return forceDoubleWidth ? Math.Max(2, max) : max;
        }
    }
}

/// <summary>
/// Specifies which search behaviors are enabled for <see cref="LogControl"/>.
/// </summary>
[Flags]
public enum LogSearchOptions
{
    /// <summary>No options.</summary>
    None = 0,

    /// <summary>Match case-sensitively.</summary>
    CaseSensitive = 1 << 0,

    /// <summary>Match whole words only.</summary>
    WholeWord = 1 << 1,

    /// <summary>Interpret the query as a regular expression.</summary>
    Regex = 1 << 2,
}
