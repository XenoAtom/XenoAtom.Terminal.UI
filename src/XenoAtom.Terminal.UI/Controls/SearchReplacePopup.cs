// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Describes a search query for <see cref="SearchReplacePopup"/>.
/// </summary>
/// <param name="Text">The search text.</param>
/// <param name="CaseSensitive">Whether the search is case-sensitive.</param>
/// <param name="WholeWord">Whether the search matches whole words only.</param>
/// <param name="UseRegex">Whether the search text is interpreted as a regular expression.</param>
public readonly record struct SearchQuery(string? Text, bool CaseSensitive, bool WholeWord, bool UseRegex);

/// <summary>
/// Defines whether a <see cref="SearchReplacePopup"/> is in find-only mode or find-and-replace mode.
/// </summary>
public enum SearchReplaceMode
{
    /// <summary>
    /// Find-only mode.
    /// </summary>
    Find,

    /// <summary>
    /// Find-and-replace mode.
    /// </summary>
    Replace,
}

/// <summary>
/// Provides the integration surface for <see cref="SearchReplacePopup"/>.
/// </summary>
/// <remarks>
/// Implementations translate query updates and navigation/replace commands into their own model.
/// </remarks>
public interface ISearchReplaceTarget
{
    /// <summary>
    /// Gets the title displayed in the popup header.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets a value indicating whether the target supports replace operations.
    /// </summary>
    bool SupportsReplace { get; }

    /// <summary>
    /// Applies the query settings to the target and rebuilds matches as needed.
    /// </summary>
    /// <param name="query">The query to apply.</param>
    void SetQuery(in SearchQuery query);

    /// <summary>
    /// Navigates to the next match (wrapping as needed).
    /// </summary>
    void NextMatch();

    /// <summary>
    /// Navigates to the previous match (wrapping as needed).
    /// </summary>
    void PreviousMatch();

    /// <summary>
    /// Replaces the active match with the specified replacement text.
    /// </summary>
    /// <param name="replacement">The replacement text.</param>
    /// <returns>The number of replacements performed (typically 0 or 1).</returns>
    int ReplaceCurrent(string replacement);

    /// <summary>
    /// Replaces all matches with the specified replacement text.
    /// </summary>
    /// <param name="replacement">The replacement text.</param>
    /// <returns>The number of replacements performed.</returns>
    int ReplaceAll(string replacement);

    /// <summary>
    /// Gets a status text displayed in the popup (e.g. <c>3/10</c>).
    /// </summary>
    string GetStatusText();

    /// <summary>
    /// Gets an error message to display (e.g. invalid regex), or <see langword="null"/> when there is no error.
    /// </summary>
    string? GetErrorText();
}

/// <summary>
/// Displays a reusable find / find-and-replace popup.
/// </summary>
/// <remarks>
/// This visual is used as an anchor within a host control. The popup itself is rendered in the app window layer.
/// The host should arrange this visual to a 0-sized rectangle inside its bounds (typically top-right) and call
/// <see cref="ArrangeWithin"/> from its arrange pass.
/// </remarks>
public sealed partial class SearchReplacePopup : Visual
{
    private readonly ISearchReplaceTarget _target;
    private Popup? _popup;
    private Rectangle _hostRect;
    private int _offsetX;
    private int _offsetY;
    private Visual? _restoreFocus;

    private bool _bulkUpdating;
    private bool _rebuildingPopup;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchReplacePopup"/> class.
    /// </summary>
    /// <param name="target">The target implementation.</param>
    public SearchReplacePopup(ISearchReplaceTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _target = target;
        Focusable = false;
    }

    /// <summary>
    /// Gets a value indicating whether the popup is currently open.
    /// </summary>
    public bool IsOpen => _popup is not null;

    /// <summary>
    /// Gets the current query state.
    /// </summary>
    public SearchQuery Query => new(SearchText, CaseSensitive, WholeWord, UseRegex);

    /// <summary>
    /// Gets or sets the search text.
    /// </summary>
    [Bindable]
    public partial string? SearchText { get; set; }

    /// <summary>
    /// Gets or sets the replacement text (only used in replace mode).
    /// </summary>
    [Bindable]
    public partial string? ReplaceText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the search is case-sensitive.
    /// </summary>
    [Bindable]
    public partial bool CaseSensitive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the search matches whole words only.
    /// </summary>
    [Bindable]
    public partial bool WholeWord { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the search uses regular expressions.
    /// </summary>
    [Bindable]
    public partial bool UseRegex { get; set; }

    /// <summary>
    /// Gets or sets the current mode of the popup.
    /// </summary>
    [Bindable]
    public partial SearchReplaceMode Mode { get; set; }

    /// <summary>
    /// Resets the popup position back to its default anchor placement.
    /// </summary>
    public void ResetPosition()
    {
        VerifyAccess();
        _offsetX = 0;
        _offsetY = 0;
        MarkArrangeDirty();
        App?.RequestRender();
    }

    /// <summary>
    /// Opens the popup in find-only mode.
    /// </summary>
    /// <param name="initialSearchText">Optional initial search text.</param>
    /// <returns><see langword="true"/> if the popup was opened; otherwise <see langword="false"/>.</returns>
    public bool OpenFind(string? initialSearchText = null)
        => OpenCore(SearchReplaceMode.Find, initialSearchText);

    /// <summary>
    /// Opens the popup in find-and-replace mode.
    /// </summary>
    /// <param name="initialSearchText">Optional initial search text.</param>
    /// <returns><see langword="true"/> if the popup was opened; otherwise <see langword="false"/>.</returns>
    public bool OpenReplace(string? initialSearchText = null)
        => OpenCore(SearchReplaceMode.Replace, initialSearchText);

    /// <summary>
    /// Closes the popup if it is open.
    /// </summary>
    public void Close()
    {
        VerifyAccess();
        _popup?.Close();
    }

    /// <summary>
    /// Arranges the anchor position within the specified host rectangle.
    /// </summary>
    /// <param name="hostRect">The rectangle within which the popup should stay anchored.</param>
    public void ArrangeWithin(in Rectangle hostRect)
    {
        _hostRect = hostRect;

        // Anchor is a 0-sized rectangle; PopupPlacement.Left uses X as the popup right edge.
        var x = hostRect.Right + _offsetX;
        var y = hostRect.Y + _offsetY;

        // Keep the anchor point within the host rect.
        x = Math.Clamp(x, hostRect.X, hostRect.Right);
        y = Math.Clamp(y, hostRect.Y, hostRect.Bottom);

        Arrange(new Rectangle(x, y, 0, 0));
    }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        => SizeHints.Fixed(constraints.Clamp(Size.Zero));

    /// <inheritdoc/>
    protected override void ArrangeCore(in Rectangle finalRect)
        => Bounds = finalRect;

    partial void OnSearchTextChanged(string? value) => ApplyQuery();
    partial void OnCaseSensitiveChanged(bool value) => ApplyQuery();
    partial void OnWholeWordChanged(bool value) => ApplyQuery();
    partial void OnUseRegexChanged(bool value) => ApplyQuery();

    partial void OnModeChanged(SearchReplaceMode value)
    {
        if (_bulkUpdating)
        {
            return;
        }

        if (value == SearchReplaceMode.Replace && !_target.SupportsReplace)
        {
            _bulkUpdating = true;
            try
            {
                Mode = SearchReplaceMode.Find;
            }
            finally
            {
                _bulkUpdating = false;
            }
            return;
        }

        RebuildPopupIfOpen();
    }

    private bool OpenCore(SearchReplaceMode mode, string? initialSearchText)
    {
        VerifyAccess();

        if (mode == SearchReplaceMode.Replace && !_target.SupportsReplace)
        {
            mode = SearchReplaceMode.Find;
        }

        _bulkUpdating = true;
        try
        {
            Mode = mode;
            if (initialSearchText is not null)
            {
                SearchText = initialSearchText;
            }
        }
        finally
        {
            _bulkUpdating = false;
        }

        ApplyQuery();

        EnsurePopup();
        if (_popup is null)
        {
            return false;
        }

        return true;
    }

    private void ApplyQuery()
    {
        if (_bulkUpdating)
        {
            return;
        }

        _target.SetQuery(Query);
    }

    private void EnsurePopup()
    {
        if (_popup is not null)
        {
            return;
        }

        _restoreFocus = App?.FocusedElement ?? Dispatcher.AttachedApp?.FocusedElement;

        var searchBox = new TextBox()
            .Placeholder("Find…")
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Text(((IBindings)this).SearchText);

        var replaceBox = new TextBox()
            .Placeholder("Replace…")
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Text(((IBindings)this).ReplaceText);

        var caseToggle = new Switch("Case").IsOn(((IBindings)this).CaseSensitive);
        var wordToggle = new Switch("Word").IsOn(((IBindings)this).WholeWord);
        var regexToggle = new Switch("Regex").IsOn(((IBindings)this).UseRegex);

        var status = new TextBlock(() => _target.GetStatusText())
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var error = new ComputedVisual(() =>
        {
            var text = _target.GetErrorText();
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            var theme = GetTheme();
            var fg = theme.Error;
            return new TextBlock(text).Style(new TextBlockStyle { Foreground = fg });
        });

        var prev = new Button("Prev").Click(_target.PreviousMatch);
        var next = new Button("Next").Click(_target.NextMatch);

        var row2 = new HStack(caseToggle, wordToggle, regexToggle)
            .Spacing(2);

        var body = new VStack(searchBox)
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        Visual row1;
        if (_target.SupportsReplace && Mode == SearchReplaceMode.Replace)
        {
            var replace = new Button("Replace").Click(() =>
            {
                _target.ReplaceCurrent(ReplaceText ?? string.Empty);
                _target.SetQuery(Query);
            });

            var replaceAll = new Button("All").Click(() =>
            {
                _target.ReplaceAll(ReplaceText ?? string.Empty);
                _target.SetQuery(Query);
            });

            row1 = new HStack(prev, next, replace, replaceAll, status)
                .Spacing(1)
                .HorizontalAlignment(HorizontalAlignment.Stretch);

            body.Add(replaceBox);
        }
        else
        {
            row1 = new HStack(prev, next, status)
                .Spacing(1)
                .HorizontalAlignment(HorizontalAlignment.Stretch);
        }

        body.Add(row1);

        // When replace is supported, expose a quick toggle from Find <-> Replace.
        if (_target.SupportsReplace)
        {
            var toggleMode = Mode == SearchReplaceMode.Replace ? "Find" : "Replace";
            row2.Add(new Button(toggleMode).Click(() =>
            {
                Mode = Mode == SearchReplaceMode.Replace ? SearchReplaceMode.Find : SearchReplaceMode.Replace;
            }));
        }

        body.Add(row2);
        body.Add(error);

        var title = _target.SupportsReplace ? "Find / Replace" : _target.Title;
        if (!_target.SupportsReplace)
        {
            title = _target.Title;
        }
        else if (Mode == SearchReplaceMode.Find)
        {
            title = "Find";
        }

        var content = new Group(title)
            .Padding(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(body);

        var popup = new Popup
        {
            Anchor = this,
            Content = content,
            MatchAnchorWidth = false,
            CloseOnTab = false,
            Placement = PopupPlacement.Left,
        };

        popup.KeyDown((_, args) =>
        {
            if (args.Key == TerminalKey.Enter || args.Key == TerminalKey.F3)
            {
                if ((args.Modifiers & TerminalModifiers.Shift) != 0)
                {
                    _target.PreviousMatch();
                }
                else
                {
                    _target.NextMatch();
                }

                args.Handled = true;
                return;
            }

            if ((args.Modifiers & TerminalModifiers.Ctrl) != 0 && args.Char is TerminalChar.CtrlH && _target.SupportsReplace)
            {
                Mode = Mode == SearchReplaceMode.Replace ? SearchReplaceMode.Find : SearchReplaceMode.Replace;
                args.Handled = true;
                return;
            }

            if ((args.Modifiers & TerminalModifiers.Alt) != 0)
            {
                var moved = args.Key switch
                {
                    TerminalKey.Left => MoveAnchor(-1, 0),
                    TerminalKey.Right => MoveAnchor(1, 0),
                    TerminalKey.Up => MoveAnchor(0, -1),
                    TerminalKey.Down => MoveAnchor(0, 1),
                    _ => false,
                };

                if (moved)
                {
                    args.Handled = true;
                }
            }
        });

        popup.Closed((_, _) =>
        {
            _popup = null;
            if (!_rebuildingPopup && _restoreFocus is not null)
            {
                var app = App ?? Dispatcher.AttachedApp;
                app?.Focus(_restoreFocus);
            }
            _restoreFocus = null;
            _rebuildingPopup = false;
        });

        _popup = popup;
        try
        {
            popup.Show();
        }
        catch (InvalidOperationException)
        {
            // Popups are only supported in fullscreen apps; ignore when not available.
            _popup = null;
        }
    }

    private bool MoveAnchor(int dx, int dy)
    {
        if (_hostRect.Width <= 0 || _hostRect.Height <= 0)
        {
            _offsetX += dx;
            _offsetY += dy;
        }
        else
        {
            _offsetX += dx;
            _offsetY += dy;
        }

        MarkArrangeDirty();
        App?.RequestRender();
        return true;
    }

    private void RebuildPopupIfOpen()
    {
        if (_popup is null)
        {
            return;
        }

        var popup = _popup;
        _popup = null;
        _rebuildingPopup = true;
        popup.Close();

        // Reopen using the current mode/query state.
        EnsurePopup();
    }
}
