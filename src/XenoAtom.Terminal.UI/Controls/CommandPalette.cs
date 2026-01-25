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

    private Popup? _hostPopup;
    private Visual? _focusContext;
    private bool _hostPopupEventsAttached;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandPalette"/> class.
    /// </summary>
    public CommandPalette()
    {
        Focusable = false;
        _collectedCommands = new List<ResolvedCommand>(64);
        _matches = new List<(int, ResolvedCommand)>(64);

        _searchBox = new TextBox()
            .Placeholder("Type to search…")
            .HorizontalAlignment(Align.Stretch);

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
            .HorizontalAlignment(Align.Stretch);

        AttachChild(_content);

        // Ensure the palette stays navigable and doesn't close on Tab like dropdown popups.
        // We implement an explicit tab cycle between search and results.
        _searchBox.KeyDown((_, e) => OnPaletteKeyDown(e, fromSearch: true));
        _results.KeyDown((_, e) => OnPaletteKeyDown(e, fromSearch: false));

        ApplyStyle(GetStyle<CommandPaletteStyle>());
    }

    /// <summary>
    /// Shows the command palette in a popup.
    /// </summary>
    public void Show()
    {
        VerifyAccess();

        if (Parent is not null && !IsAttachedToHostPopup())
        {
            throw new InvalidOperationException("CommandPalette.Show cannot be called when the palette is part of a visual tree.");
        }

        var app = App ?? Dispatcher.AttachedApp;
        if (app is null)
        {
            throw new InvalidOperationException("Popup.Show is only supported while a TerminalApp is running.");
        }

        _hostPopup ??= new Popup
        {
            MatchAnchorWidth = false,
            Placement = PopupPlacement.Below,
            CloseOnTab = false,
        }.Style(PopupStyle.Default with { Padding = Thickness.Zero });

        if (!_hostPopupEventsAttached)
        {
            _hostPopup.Closed(RestoreFocus);
            _hostPopupEventsAttached = true;
        }

        // If already hosted, simply re-show the existing popup without re-wrapping the palette.
        // Re-wrapping would attempt to attach the palette to a new parent while it is still parented.
        if (IsAttachedToHostPopup())
        {
            _focusContext ??= app?.FocusedElement;
            InvalidateResults();
            _hostPopup.Show();
            FocusSearch();
            return;
        }

        // Capture the focus context before the popup steals focus (so commands are collected from the "app" focus).
        _focusContext ??= app?.FocusedElement;

        var style = GetStyle<CommandPaletteStyle>();
        ApplyStyle(style);
        var content = style.PopupTemplateFactory?.Invoke(this) ?? this;
        _hostPopup.Content = content;
        InvalidateResults();

        _hostPopup.Show();
        FocusSearch();
    }

    /// <summary>
    /// Closes the command palette popup if it is open.
    /// </summary>
    public void Close()
    {
        _hostPopup?.Close();
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
        MarkMeasureDirty();
    }

    private void EnsureResultsUpToDate()
    {
        var app = App ?? _hostPopup?.App;
        if (app is null)
        {
            return;
        }

        var focus = _focusContext;
        if (focus is not null && !ReferenceEquals(focus.App, app))
        {
            focus = null;
        }

        var query = (_searchBox.Text ?? string.Empty).Trim();
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
        _resultsHost.MaxHeight = resultsHeight;

        this.MinWidth(Math.Max(0, style.MinWidth));
        this.MaxWidth(Math.Max(style.MinWidth, style.MaxWidth));

        _results.ItemTemplate = style.ItemTemplate ?? CommandPaletteStyle.CreateDefaultItemTemplate();
    }

    private void FocusSearch()
    {
        var app = App ?? _hostPopup?.App;
        if (app is null)
        {
            return;
        }

        app.Focus(_searchBox);
    }

    private void RestoreFocus()
    {
        // Note: the popup is removed from the window layer before raising Closed, so Popup.App can be null here.
        var app = App ?? _hostPopup?.App ?? Dispatcher.AttachedApp;
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

    private bool IsAttachedToHostPopup()
    {
        if (_hostPopup is null)
        {
            return false;
        }

        for (var parent = Parent; parent is not null; parent = parent.Parent)
        {
            if (ReferenceEquals(parent, _hostPopup))
            {
                return true;
            }
        }

        return false;
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

        if (fromSearch && e.Key == TerminalKey.Down && _results.Items.Count > 0)
        {
            (App ?? _hostPopup?.App)?.Focus(_results);
            e.Handled = true;
            return;
        } 

        if (e.Key != TerminalKey.Tab)
        {
            return;
        }

        var app = App ?? _hostPopup?.App;
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
