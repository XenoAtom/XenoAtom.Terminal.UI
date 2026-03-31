// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Command palette control: search input + filtered list of actions.
/// </summary>
public sealed partial class CommandPalette : Visual
{
    private readonly TextBox _searchBox;
    private readonly OptionList<ResolvedCommand> _results;
    private readonly ScrollViewer _resultsHost;
    private readonly VStack _content;
    private readonly List<ResolvedCommand> _collectedCommands;
    private readonly List<(int Score, ResolvedCommand Command)> _matches;

    private string _lastQuery = string.Empty;
    private int _lastCommandStamp = int.MinValue;
    private Visual? _lastFocusContext;
    private int _lastQuerySnapshotVersion = -1;
    private string _lastQuerySnapshotText = string.Empty;

    private Dialog? _hostDialog;
    private Visual? _focusContext;
    private bool _hostGeometryInitialized;

    // Used to force measure/arrange updates when the search box document changes.
    // The search text is stored in a TextDocument (not a bindable property), so we expose a bindable stamp
    // that participates in dependency tracking.
    [Bindable]
    internal partial int ResultsVersion { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandPalette"/> class.
    /// </summary>
    public CommandPalette()
    {
        Focusable = false;
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;
        _collectedCommands = new List<ResolvedCommand>(64);
        _matches = new List<(int, ResolvedCommand)>(64);

        _searchBox = new TextBox()
            .Placeholder("Type to search…")
            .HorizontalAlignment(Align.Stretch);

        // The palette query is stored in the search box document. Listen to document changes instead of relying on
        // routed TextInput events, because text editors typically mark those events handled.
        _searchBox.TextDocument.Changed += OnSearchDocumentChanged;

        _results = new OptionList<ResolvedCommand>()
            .ActivateOnClick(true)
            .HorizontalAlignment(Align.Stretch);

        _results.ItemIsEnabled = (Func<ResolvedCommand, bool>)(item => item.IsEnabled);
        _results.ItemSearchText = (Func<ResolvedCommand, string?>)(item => item.Command.Name ?? item.Command.SearchText ?? item.Command.LabelMarkup);

        _results.ItemActivated((_, e) => InvokeIndex(e.Index));

        _resultsHost = new ScrollViewer(_results, focusable: false)
            .HorizontalScrollEnabled(false)
            .VerticalScrollEnabled(true)
            .HorizontalAlignment(Align.Stretch);

        _content = new VStack(_searchBox, _resultsHost)
            .Spacing(1)
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch);

        AttachChild(_content);

        // Ensure the palette stays navigable and doesn't close on Tab like dropdown popups.
        // We implement an explicit tab cycle between search and results.
        _searchBox.KeyDown((_, e) => OnPaletteKeyDown(e, fromSearch: true));
        _results.KeyDown((_, e) => OnPaletteKeyDown(e, fromSearch: false));

        ApplyStyle(GetStyle<CommandPaletteStyle>());
    }

    private void OnSearchDocumentChanged(object? sender, TextDocumentChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        InvalidateResults();

        // Filtering must update immediately as users type. Rendering can happen without a full measure pass
        // (e.g. text editor redraws), so keep the item list in sync here instead of relying solely on MeasureCore.
        EnsureResultsUpToDate();
    }

    /// <summary>
    /// Shows the command palette in a resizable overlay window.
    /// </summary>
    public void Show()
    {
        VerifyAccess();

        if (Parent is not null && !IsAttachedToHostDialog())
        {
            throw new InvalidOperationException("CommandPalette.Show cannot be called when the palette is part of a visual tree.");
        }

        var app = App ?? Dispatcher.AttachedApp
            ?? throw new InvalidOperationException("CommandPalette.Show is only supported while a TerminalApp is running.");
        EnsureHostDialog();

        // If already hosted, simply re-focus the existing window without re-wrapping the palette.
        // Re-wrapping would attempt to attach the palette to a new parent while it is still parented.
        if (IsAttachedToHostDialog())
        {
            _focusContext ??= app.FocusedElement;
            InvalidateResults();
            ApplyHostChrome(GetStyle<CommandPaletteStyle>());
            app.Post(FocusSearch);
            return;
        }

        // Capture the focus context before the host window steals focus (so commands are collected from the "app" focus).
        _focusContext ??= app.FocusedElement;

        var style = GetStyle<CommandPaletteStyle>();
        ApplyStyle(style);
        ApplyHostChrome(style);
        var content = style.PopupTemplateFactory?.Invoke(this) ?? this;
        _hostDialog!.Content = content;
        EnsureHostGeometry(app.Root.Bounds, style);
        InvalidateResults();

        app.ShowWindow(_hostDialog);
        app.Post(RealignHostDialog);
        app.Post(FocusSearch);
    }

    /// <summary>
    /// Closes the command palette window if it is open.
    /// </summary>
    public void Close()
    {
        if (_hostDialog is null)
        {
            return;
        }

        var app = App ?? _hostDialog.App ?? Dispatcher.AttachedApp;
        if (app is null || _hostDialog.Parent is null)
        {
            return;
        }

        app.CloseWindow(_hostDialog);
        _hostDialog?.Content = null;
        _hostGeometryInitialized = false;
        RestoreFocus();
    }

    /// <inheritdoc />
    protected override int ChildrenCount => 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index) => index == 0 ? _content : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        EnsureResultsUpToDate();
        return _content.Measure(constraints);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        _content.Arrange(finalRect);
    }

    private void InvalidateResults()
    {
        _lastCommandStamp = int.MinValue;
        _lastQuery = string.Empty;
        _lastFocusContext = null;
        ResultsVersion++;
    }

    private void EnsureResultsUpToDate()
    {
        _ = ResultsVersion;

        var app = App ?? _hostDialog?.App;
        if (app is null)
        {
            return;
        }

        var focus = _focusContext;
        if (focus is not null && !ReferenceEquals(focus.App, app))
        {
            focus = null;
        }

        var query = GetQueryText().Trim();
        var stamp = ComputeCommandStamp(app, focus);

        if (stamp == _lastCommandStamp
            && string.Equals(query, _lastQuery, StringComparison.Ordinal)
            && ReferenceEquals(focus, _lastFocusContext))
        {
            return;
        }

        _lastCommandStamp = stamp;
        _lastQuery = query;
        _lastFocusContext = focus;
        RebuildResults(app, focus, query);
    }

    private string GetQueryText()
    {
        // Command palette filtering must reflect what is currently displayed in the editor.
        // Using the document snapshot avoids relying on TextBox.Text being synchronized.
        var snapshot = _searchBox.TextDocument.CurrentSnapshot;
        if (snapshot.Version == _lastQuerySnapshotVersion)
        {
            return _lastQuerySnapshotText;
        }

        _lastQuerySnapshotVersion = snapshot.Version;
        if (snapshot.Length == 0)
        {
            _lastQuerySnapshotText = string.Empty;
            return _lastQuerySnapshotText;
        }

        var length = snapshot.Length;
        Span<char> buffer = length <= 256 ? stackalloc char[length] : new char[length];
        snapshot.CopyTo(0, buffer);
        _lastQuerySnapshotText = new string(buffer);
        return _lastQuerySnapshotText;
    }

    private static int ComputeCommandStamp(TerminalApp app, Visual? focus)
    {
        focus ??= app.FocusedElement;

        var stamp = 17;
        for (var v = focus; v is not null; v = v.Parent)
        {
            stamp = unchecked((stamp * 31) + v.Commands.Version);
        }

        stamp = unchecked((stamp * 31) + app.GlobalCommands.Version);
        return stamp;
    }

    private void ApplyStyle(CommandPaletteStyle style)
    {
        var resultsHeight = Math.Max(1, style.ResultsHeight);
        _resultsHost.MinHeight = resultsHeight;
        _resultsHost.MaxHeight = LayoutConstants.Infinite;

        this.MinWidth(Math.Max(0, style.MinWidth));
        this.MaxWidth(Math.Max(style.MinWidth, style.MaxWidth));

        _results.ItemTemplate = style.ItemTemplate ?? CommandPaletteStyle.CreateDefaultItemTemplate();
    }

    private void ApplyHostChrome(CommandPaletteStyle style)
    {
        if (_hostDialog is null)
        {
            return;
        }

        var dialogPadding = new Thickness(1);
        var minContentWidth = Math.Max(0, style.MinWidth);
        var maxContentWidth = Math.Max(minContentWidth, style.MaxWidth);

        _hostDialog.Title = "Command palette";
        _hostDialog.Padding = dialogPadding;
        _hostDialog.MinWidth = 2 + dialogPadding.Horizontal + minContentWidth;
        _hostDialog.MaxWidth = 2 + dialogPadding.Horizontal + maxContentWidth;
        _hostDialog.MinHeight = 2 + dialogPadding.Vertical + 3;
        _hostDialog.IsResizable = style.PopupIsResizable;
        _hostDialog.IsDraggable = style.PopupIsDraggable;
        _hostDialog.DragHandleHeight = Math.Max(1, style.PopupDragHandleHeight);
    }

    private void FocusSearch()
    {
        var app = App ?? _hostDialog?.App;
        if (app is null)
        {
            return;
        }

        app.Focus(_searchBox);
    }

    private void RealignHostDialog()
    {
        var app = App ?? _hostDialog?.App ?? Dispatcher.AttachedApp;
        if (app is null || _hostDialog is null || _hostDialog.Parent is null)
        {
            return;
        }

        _hostGeometryInitialized = false;
        EnsureHostGeometry(app.Root.Bounds, GetStyle<CommandPaletteStyle>());
        _hostDialog.Arrange(app.Root.Bounds);
    }

    private void RestoreFocus()
    {
        var app = App ?? _hostDialog?.App ?? Dispatcher.AttachedApp;
        if (app is null)
        {
            return;
        }

        if (_focusContext is not null && ReferenceEquals(_focusContext.App, app))
        {
            app.Focus(_focusContext);
        }

        _focusContext = null;
    }

    private bool IsAttachedToHostDialog()
    {
        if (_hostDialog is null || _hostDialog.Parent is null)
        {
            return false;
        }

        var contentRoot = _hostDialog?.Content;
        if (ReferenceEquals(contentRoot, this))
        {
            return true;
        }

        for (var parent = Parent; parent is not null; parent = parent.Parent)
        {
            if (ReferenceEquals(parent, _hostDialog))
            {
                return true;
            }

            if (ReferenceEquals(parent, contentRoot))
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureHostDialog()
    {
        if (_hostDialog is not null)
        {
            return;
        }

        _hostDialog = new Dialog
        {
            IsModal = false,
        };
        _hostGeometryInitialized = false;
    }

    private void EnsureHostGeometry(in Rectangle viewport, CommandPaletteStyle style)
    {
        if (_hostDialog is null || _hostGeometryInitialized)
        {
            return;
        }

        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return;
        }

        _hostDialog.Measure(new Size(viewport.Width, viewport.Height));

        var width = Math.Clamp(_hostDialog.DesiredSize.Width, _hostDialog.MinWidth, Math.Min(_hostDialog.MaxWidth, viewport.Width));
        var height = Math.Clamp(_hostDialog.DesiredSize.Height, _hostDialog.MinHeight, viewport.Height);

        if (style.PopupHorizontalAlignment == Align.Stretch)
        {
            width = viewport.Width;
        }

        if (style.PopupVerticalAlignment == Align.Stretch)
        {
            height = viewport.Height;
        }

        var maxLeft = Math.Max(0, viewport.Width - width);
        var maxTop = Math.Max(0, viewport.Height - height);

        var left = style.PopupHorizontalAlignment switch
        {
            Align.End => maxLeft + style.PopupOffsetX,
            Align.Center => (maxLeft / 2) + style.PopupOffsetX,
            _ => style.PopupOffsetX,
        };
        var top = style.PopupVerticalAlignment switch
        {
            Align.End => maxTop + style.PopupOffsetY,
            Align.Center => (maxTop / 2) + style.PopupOffsetY,
            _ => style.PopupOffsetY,
        };

        if (_hostDialog.Left != left)
        {
            _hostDialog.Left = left;
        }

        if (_hostDialog.Top != top)
        {
            _hostDialog.Top = top;
        }

        if (_hostDialog.Width != width)
        {
            _hostDialog.Width = width;
        }

        if (_hostDialog.Height != height)
        {
            _hostDialog.Height = height;
        }

        _hostGeometryInitialized = true;
    }

    private void RebuildResults(TerminalApp app, Visual? focusContext, string query)
    {
        var hasQuery = query.Length > 0;

        _collectedCommands.Clear();
        CommandQuery.Collect(app, focusContext, CommandPresentation.CommandPalette, _collectedCommands);

        _results.Items.Clear();
        _matches.Clear();

        for (var i = 0; i < _collectedCommands.Count; i++)
        {
            var item = _collectedCommands[i];
            var score = hasQuery ? GetMatchScore(item.Command, query) : 0;
            if (hasQuery && score == int.MaxValue)
            {
                continue;
            }

            _matches.Add((score, item));
        }

        if (_matches.Count == 0)
        {
            _results.SelectedIndex = 0;
            return;
        }

        if (hasQuery)
        {
            _matches.Sort(static (a, b) =>
            {
                var score = a.Score.CompareTo(b.Score);
                if (score != 0)
                {
                    return score;
                }

                if (a.Command.Command.Importance != b.Command.Command.Importance)
                {
                    return a.Command.Command.Importance.CompareTo(b.Command.Command.Importance);
                }

                if (a.Command.IsGlobal != b.Command.IsGlobal)
                {
                    return a.Command.IsGlobal ? 1 : -1;
                }

                return a.Command.Order.CompareTo(b.Command.Order);
            });
        }

        for (var i = 0; i < _matches.Count; i++)
        {
            _results.Items.Add(_matches[i].Command);
        }

        _results.SelectedIndex = 0;
    }

    private void InvokeIndex(int index)
    {
        if ((uint)index >= (uint)_results.Items.Count)
        {
            return;
        }

        var item = _results.Items[index];
        if (!item.IsEnabled)
        {
            return;
        }

        item.Command.Execute(item.Target);
        Close();
    }

    private void OnPaletteKeyDown(KeyEventArgs e, bool fromSearch)
    {
        if (e.Key == TerminalKey.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        if (fromSearch && e.Key == TerminalKey.Enter)
        {
            InvokeIndex(_results.SelectedIndex);
            e.Handled = true;
            return;
        }

        if (fromSearch && e.Key == TerminalKey.Down && _results.Items.Count > 0)
        {
            _results.SelectedIndex = Math.Clamp(_results.SelectedIndex, 0, _results.Items.Count - 1) + 1;
            (App ?? _hostDialog?.App)?.Focus(_results);
            e.Handled = true;
            return;
        }

        if (e.Key != TerminalKey.Tab)
        {
            return;
        }

        var app = App ?? _hostDialog?.App;
        if (app is null)
        {
            return;
        }

        app.Focus(fromSearch ? _results : _searchBox);

        e.Handled = true;
    }

    private static int GetMatchScore(Command command, string query)
    {
        var best = int.MaxValue;

        if (!string.IsNullOrEmpty(command.Name))
        {
            Consider(ref best, GetMatchScoreForText(command.Name, query), bias: 0);
        }

        if (!string.IsNullOrEmpty(command.LabelMarkup))
        {
            Consider(ref best, GetMatchScoreForText(StripMarkup(command.LabelMarkup), query), bias: 10);
        }

        if (!string.IsNullOrEmpty(command.SearchText))
        {
            Consider(ref best, GetMatchScoreForText(command.SearchText, query), bias: 20);
        }

        if (!string.IsNullOrEmpty(command.DescriptionMarkup))
        {
            Consider(ref best, GetMatchScoreForText(StripMarkup(command.DescriptionMarkup), query), bias: 30);
        }

        return best;
    }

    private static void Consider(ref int best, int score, int bias)
    {
        if (score == int.MaxValue)
        {
            return;
        }

        best = Math.Min(best, score + bias);
    }

    private static int GetMatchScoreForText(string text, string query)
    {
        if (text.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (text.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1000;
        }

        var boundaryIndex = IndexOfAtWordBoundary(text, query);
        if (boundaryIndex >= 0)
        {
            return 2000 + boundaryIndex;
        }

        var index = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? (3000 + index) : int.MaxValue;
    }

    private static int IndexOfAtWordBoundary(string text, string query)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (i != 0 && !IsWordBoundary(text[i - 1]))
            {
                continue;
            }

            if (text.AsSpan(i).StartsWith(query, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsWordBoundary(char c)
        => !char.IsLetterOrDigit(c);

    private static string StripMarkup(string markup)
    {
        var start = markup.IndexOf('[');
        if (start < 0)
        {
            return markup;
        }

        var sb = new StringBuilder(markup.Length);
        for (var i = 0; i < markup.Length; i++)
        {
            var c = markup[i];
            if (c == '[')
            {
                var end = markup.IndexOf(']', i + 1);
                if (end >= 0)
                {
                    i = end;
                    continue;
                }
            }

            sb.Append(c);
        }

        return sb.ToString();
    }
}
