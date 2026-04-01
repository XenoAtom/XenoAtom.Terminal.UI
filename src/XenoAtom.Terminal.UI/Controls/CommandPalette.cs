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

    private bool _syncingQueryTextFromSearchBox;
    private string _lastQuery = string.Empty;
    private int _lastCommandStamp = int.MinValue;
    private Visual? _lastFocusContext;

    private Dialog? _hostDialog;
    private Visual? _focusContext;
    private bool _hostGeometryInitialized;

    // Used to force measure/arrange updates when the search box document changes.
    // QueryText is bindable, but the editor still mutates through a TextDocument, so we expose a bindable stamp
    // that participates in dependency tracking for live filtering/layout updates.
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
        this.QueryText(string.Empty);
        this.ClearQueryOnShow(true);

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
        SyncQueryTextFromSearchBox();
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
        ResetQueryOnShowIfRequested();

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

        _hostDialog.Show();
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

        if (_hostDialog.Parent is null)
        {
            _hostDialog.Content = null;
            _hostGeometryInitialized = false;
            _focusContext = null;
            return;
        }

        var app = App ?? _hostDialog.App ?? Dispatcher.AttachedApp;
        if (app is null)
        {
            return;
        }

        _hostDialog.Close();
        _hostDialog?.Content = null;
        _hostGeometryInitialized = false;
        _focusContext = null;
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

    /// <summary>
    /// Gets or sets the current query text shown in the palette search box.
    /// </summary>
    /// <remarks>
    /// This property stays synchronized with the search editor so applications can bind it to other controls or set it
    /// programmatically before showing the palette.
    /// </remarks>
    [Bindable]
    public partial string? QueryText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Show"/> clears the current query before focusing the palette.
    /// </summary>
    /// <remarks>
    /// The default is <see langword="true"/> so repeated openings start from a clean search.
    /// </remarks>
    [Bindable]
    public partial bool ClearQueryOnShow { get; set; }

    partial void OnQueryTextChanging(ref string? value)
        => value ??= string.Empty;

    partial void OnQueryTextChanged(string? value)
    {
        if (_syncingQueryTextFromSearchBox)
        {
            return;
        }

        SyncSearchBoxFromQueryText(value ?? string.Empty);
        InvalidateResults();
        EnsureResultsUpToDate();
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

        var query = (QueryText ?? string.Empty).Trim();
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
        _resultsHost.MaxHeight = LayoutConstants.Infinite;

        var minWidth = Math.Max(0, style.MinWidth);
        var maxWidth = style.MaxWidth == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(minWidth, style.MaxWidth);

        this.MinWidth(minWidth);
        this.MaxWidth(maxWidth);

        _results.ItemTemplate = style.ItemTemplate ?? CommandPaletteStyle.CreateDefaultItemTemplate();

        var theme = GetTheme();
        _searchBox.Style(ResolveSearchBoxStyle(theme));
        _results.Style(ResolveResultsStyle(theme));
    }

    private void ApplyHostChrome(CommandPaletteStyle style)
    {
        if (_hostDialog is null)
        {
            return;
        }

        var dialogPadding = new Thickness(1);
        var minContentWidth = Math.Max(0, style.MinWidth);
        var maxContentWidth = style.MaxWidth == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(minContentWidth, style.MaxWidth);

        _hostDialog.Title = "Command palette";
        _hostDialog.Padding = dialogPadding;
        _hostDialog.MinWidth = 2 + dialogPadding.Horizontal + minContentWidth;
        _hostDialog.MaxWidth = maxContentWidth == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : 2 + dialogPadding.Horizontal + maxContentWidth;
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

        var width = ResolvePopupWidth(viewport.Width, style);
        var height = ResolvePopupHeight(viewport.Height, style);

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

    private int ResolvePopupWidth(int viewportWidth, CommandPaletteStyle style)
    {
        var maxWidth = Math.Min(_hostDialog!.MaxWidth, viewportWidth);
        if (TryResolveDimensionFromPercent(style.PopupWidthPercent, viewportWidth, out var percentWidth))
        {
            return Math.Clamp(percentWidth, _hostDialog.MinWidth, maxWidth);
        }

        return Math.Clamp(_hostDialog.DesiredSize.Width, _hostDialog.MinWidth, maxWidth);
    }

    private int ResolvePopupHeight(int viewportHeight, CommandPaletteStyle style)
    {
        if (TryResolveDimensionFromPercent(style.PopupHeightPercent, viewportHeight, out var percentHeight))
        {
            return Math.Clamp(percentHeight, _hostDialog!.MinHeight, viewportHeight);
        }

        return Math.Clamp(_hostDialog!.DesiredSize.Height, _hostDialog.MinHeight, viewportHeight);
    }

    private static bool TryResolveDimensionFromPercent(double? percentValue, int viewportSize, out int size)
    {
        size = viewportSize;
        if (!percentValue.HasValue)
        {
            return false;
        }

        var percent = percentValue.Value;
        if (double.IsNaN(percent) || double.IsInfinity(percent) || percent <= 0d)
        {
            return false;
        }

        var clampedPercent = Math.Min(100d, percent);
        size = (int)Math.Floor(viewportSize * (clampedPercent / 100d));
        size = Math.Clamp(size, 1, viewportSize);
        return true;
    }

    private void ResetQueryOnShowIfRequested()
    {
        if (!ClearQueryOnShow || string.IsNullOrEmpty(QueryText))
        {
            return;
        }

        QueryText = string.Empty;
    }

    private void SyncQueryTextFromSearchBox()
    {
        var query = TextDocumentUtility.GetText(_searchBox.TextDocument);
        if (string.Equals(QueryText, query, StringComparison.Ordinal))
        {
            return;
        }

        _syncingQueryTextFromSearchBox = true;
        try
        {
            QueryText = query;
        }
        finally
        {
            _syncingQueryTextFromSearchBox = false;
        }
    }

    private void SyncSearchBoxFromQueryText(string query)
    {
        if (string.Equals(TextDocumentUtility.GetText(_searchBox.TextDocument), query, StringComparison.Ordinal))
        {
            return;
        }

        _searchBox.Text = query;
        _searchBox.CaretIndex = _searchBox.TextDocument.CurrentSnapshot.Length;
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

    private static TextBoxStyle ResolveSearchBoxStyle(Theme theme)
    {
        var popupBackground = ResolvePaletteBackground(theme);
        Color? background = theme.InputFillFocused ?? theme.InputFill ?? popupBackground;
        if (popupBackground.IsRgbLike && (theme.Background ?? popupBackground).IsRgbLike)
        {
            background = popupBackground.ToRgb().Mix((theme.Background ?? popupBackground).ToRgb(), 0.34f, ColorMixSpace.LinearRgb);
        }

        return TextBoxStyle.Default with
        {
            Background = background,
            Placeholder = theme.Muted ?? theme.Foreground,
        };
    }

    private static OptionListStyle ResolveResultsStyle(Theme theme)
    {
        var item = theme.ForegroundTextStyle();
        var popupBackground = ResolvePaletteBackground(theme);

        var hovered = item;
        if (popupBackground.IsRgbLike && (theme.Foreground ?? popupBackground).IsRgbLike)
        {
            var hoverBackground = popupBackground.ToRgb().Mix((theme.Foreground ?? popupBackground).ToRgb(), 0.05f, ColorMixSpace.LinearRgb);
            hovered = hovered.WithBackground(hoverBackground);
        }
        else if ((theme.ControlFill ?? theme.SurfaceAlt ?? theme.PopupSurface ?? theme.Surface) is { } hoverBackground)
        {
            hovered = hovered.WithBackground(hoverBackground);
        }

        var selectedFocused = item | TextStyle.Bold;
        if (popupBackground.IsRgbLike && (theme.FocusBorder ?? theme.Accent ?? theme.Primary ?? popupBackground).IsRgbLike)
        {
            var selectedFocusedBackground = popupBackground.ToRgb().Mix((theme.FocusBorder ?? theme.Accent ?? theme.Primary ?? popupBackground).ToRgb(), 0.24f, ColorMixSpace.LinearRgb);
            selectedFocused = selectedFocused.WithBackground(selectedFocusedBackground);
        }
        else if ((theme.Selection ?? theme.ControlFillHover ?? theme.ControlFill ?? theme.SurfaceAlt) is { } selectedFocusedBackground)
        {
            selectedFocused = selectedFocused.WithBackground(selectedFocusedBackground);
        }

        var selectedUnfocused = item | TextStyle.Bold;
        if (popupBackground.IsRgbLike && (theme.FocusBorder ?? theme.Accent ?? theme.Primary ?? popupBackground).IsRgbLike)
        {
            var selectedBackground = popupBackground.ToRgb().Mix((theme.FocusBorder ?? theme.Accent ?? theme.Primary ?? popupBackground).ToRgb(), 0.12f, ColorMixSpace.LinearRgb);
            selectedUnfocused = selectedUnfocused.WithBackground(selectedBackground);
        }
        else if ((theme.ControlFill ?? theme.SurfaceAlt ?? theme.PopupSurface ?? theme.Surface) is { } selectedBackground)
        {
            selectedUnfocused = selectedUnfocused.WithBackground(selectedBackground);
        }

        return OptionListStyle.Default with
        {
            Item = item,
            Hovered = hovered,
            SelectedFocused = selectedFocused,
            SelectedUnfocused = selectedUnfocused,
        };
    }

    private static Color ResolvePaletteBackground(Theme theme)
        => (theme.PopupSurface ?? theme.SurfaceAlt ?? theme.Surface ?? theme.Background ?? Colors.Black).ToRgb();
}
