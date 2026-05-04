// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Displays one tab page at a time with a clickable header strip.
/// </summary>
public sealed partial class TabControl : Visual
{
    private readonly BindableList<TabPage> _tabs;
    private readonly List<TabHeaderLayout> _headerLayouts = new();
    private readonly TabContentHost _contentHost = new();
    private readonly AttachedContentChrome _attachedContentChrome;

    private int _headerHeight = 1;
    private int _overflowButtonWidth;
    private bool _showOverflowButtons;
    private bool _canScrollPrevious;
    private bool _canScrollNext;
    private int _resolvedFirstVisibleIndex;
    private int _resolvedVisibleEndIndex;

    private ContentVisual? _contentTemplate;
    private Visual? _contentRoot;
    private Func<Visual, ContentVisual?>? _contentTemplateFactory;
    private TabControlLayoutMode _contentLayoutMode;
    private bool _suppressSelectionChangedEvents;

    private int _oldSelectedIndexForEvent = -1;
    private TabPage? _oldSelectedPageForEvent;

    [Bindable]
    internal partial int HoveredIndex { get; set; }

    [Bindable]
    internal partial TabHeaderPart HoveredPart { get; set; }

    [Bindable]
    internal partial int PressedIndex { get; set; }

    [Bindable]
    internal partial TabHeaderPart PressedPart { get; set; }

    [Bindable]
    internal partial bool IsPressedInside { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TabControl"/> class.
    /// </summary>
    public TabControl()
    {
        _tabs = new BindableList<TabPage>(
            owner: this,
            name: "TabControl.Tabs",
            onAdding: AttachPage,
            onRemoving: DetachPage);

        Focusable = true;
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;

        _contentHost.HorizontalAlignment = Align.Stretch;
        _contentHost.VerticalAlignment = Align.Stretch;
        _attachedContentChrome = new AttachedContentChrome(this)
        {
            HorizontalAlignment = Align.Stretch,
            VerticalAlignment = Align.Stretch,
        };
        HoveredIndex = -1;
        PressedIndex = -1;
        HoveredPart = TabHeaderPart.None;
        PressedPart = TabHeaderPart.None;
    }

    /// <summary>
    /// Initializes a new tab control with the provided tab pages.
    /// </summary>
    /// <param name="tabs">The tab pages.</param>
    public TabControl(params TabPage[] tabs) : this()
    {
        ArgumentNullException.ThrowIfNull(tabs);
        for (var i = 0; i < tabs.Length; i++)
        {
            AddTab(tabs[i]);
        }
    }

    /// <summary>
    /// Gets or sets the selected tab index.
    /// </summary>
    [Bindable]
    public partial int SelectedIndex { get; set; }

    /// <summary>
    /// Gets or sets the first tab index used as the preferred start of the visible tab window.
    /// </summary>
    [Bindable]
    public partial int FirstVisibleIndex { get; set; }

    partial void OnSelectedIndexChanging(ref int value)
    {
        CaptureSelectedTabForEvent(out _oldSelectedIndexForEvent, out _oldSelectedPageForEvent);
        value = Math.Max(0, value);
    }

    partial void OnSelectedIndexChanged(int value)
    {
        _ = value;
        if (_suppressSelectionChangedEvents)
        {
            return;
        }

        SyncFirstVisibleIndexToSelection();
        RaiseSelectionChangedIfNeeded(_oldSelectedIndexForEvent, _oldSelectedPageForEvent);
    }

    partial void OnFirstVisibleIndexChanging(ref int value) => value = Math.Max(0, value);

    /// <inheritdoc/>
    protected override void PrepareChildren()
    {
        var style = GetStyle<TabControlStyle>();
        EnsureContentTemplate(style);

        if (_tabs.Count == 0)
        {
            _contentHost.Content = null;
            return;
        }

        var selected = Math.Clamp(SelectedIndex, 0, _tabs.Count - 1);
        _contentHost.Content = _tabs[selected].Content;
    }

    /// <summary>
    /// Gets the tab pages owned by this control.
    /// </summary>
    public IReadOnlyList<TabPage> Tabs => _tabs;

    /// <summary>
    /// Adds a tab page from a header and a content visual.
    /// </summary>
    /// <param name="header">The tab header visual.</param>
    /// <param name="content">The tab content visual.</param>
    public void AddTab(Visual header, Visual content)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(content);
        AddTab(new TabPage(header, content));
    }

    /// <summary>
    /// Adds a tab page.
    /// </summary>
    /// <param name="page">The tab page to add.</param>
    public void AddTab(TabPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        CaptureSelectedTabForEvent(out var oldIndex, out var oldPage);
        _tabs.Add(page);
        RaiseSelectionChangedIfNeeded(oldIndex, oldPage);
    }

    /// <summary>
    /// Moves a tab page to a new index.
    /// </summary>
    /// <param name="oldIndex">The current zero-based tab index.</param>
    /// <param name="newIndex">The destination zero-based tab index.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="oldIndex"/> or <paramref name="newIndex"/> is outside the bounds of <see cref="Tabs"/>.
    /// </exception>
    public void MoveTab(int oldIndex, int newIndex)
    {
        if (!TryMoveTab(oldIndex, newIndex))
        {
            ThrowMoveTabIndexOutOfRange(oldIndex, newIndex, _tabs.Count);
        }
    }

    /// <summary>
    /// Moves a tab page to a new index.
    /// </summary>
    /// <param name="page">The tab page to move.</param>
    /// <param name="newIndex">The destination zero-based tab index.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="page"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="page"/> does not belong to this control.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="newIndex"/> is outside the bounds of <see cref="Tabs"/>.</exception>
    public void MoveTab(TabPage page, int newIndex)
    {
        ArgumentNullException.ThrowIfNull(page);
        var oldIndex = _tabs.IndexOf(page);
        if (oldIndex < 0)
        {
            throw new ArgumentException("The tab page does not belong to this TabControl.", nameof(page));
        }

        if ((uint)newIndex >= (uint)_tabs.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(newIndex), newIndex, "The destination tab index must refer to an existing tab.");
        }

        MoveTabCore(oldIndex, newIndex);
    }

    /// <summary>
    /// Attempts to move a tab page to a new index.
    /// </summary>
    /// <param name="oldIndex">The current zero-based tab index.</param>
    /// <param name="newIndex">The destination zero-based tab index.</param>
    /// <returns><see langword="true"/> when both indexes are valid; otherwise, <see langword="false"/>.</returns>
    public bool TryMoveTab(int oldIndex, int newIndex)
    {
        if ((uint)oldIndex >= (uint)_tabs.Count || (uint)newIndex >= (uint)_tabs.Count)
        {
            return false;
        }

        MoveTabCore(oldIndex, newIndex);
        return true;
    }

    /// <summary>
    /// Attempts to move a tab page to a new index.
    /// </summary>
    /// <param name="page">The tab page to move.</param>
    /// <param name="newIndex">The destination zero-based tab index.</param>
    /// <returns><see langword="true"/> when the page belongs to this control and the destination index is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="page"/> is null.</exception>
    public bool TryMoveTab(TabPage page, int newIndex)
    {
        ArgumentNullException.ThrowIfNull(page);
        var oldIndex = _tabs.IndexOf(page);
        return oldIndex >= 0 && TryMoveTab(oldIndex, newIndex);
    }

    /// <summary>
    /// Attempts to close the tab page at the specified index.
    /// </summary>
    /// <param name="index">The tab index.</param>
    /// <returns><see langword="true"/> when the tab was closed; otherwise <see langword="false"/>.</returns>
    public bool TryCloseTab(int index) => TryCloseTab(index, TabCloseReason.Programmatic);

    /// <summary>
    /// Attempts to close the specified tab page.
    /// </summary>
    /// <param name="page">The tab page to close.</param>
    /// <returns><see langword="true"/> when the tab was closed; otherwise <see langword="false"/>.</returns>
    public bool TryCloseTab(TabPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return TryCloseTab(_tabs.IndexOf(page), TabCloseReason.Programmatic);
    }

    /// <inheritdoc/>
    protected override int ChildrenCount => _tabs.Count + (_contentRoot is null ? 0 : 1);

    /// <inheritdoc/>
    protected override Visual GetChild(int index)
    {
        if ((uint)index >= (uint)ChildrenCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (index < _tabs.Count)
        {
            return _tabs[index].Header;
        }

        if (index == _tabs.Count && _contentRoot is not null)
        {
            return _contentRoot;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = GetStyle<TabControlStyle>();
        var headerMetrics = MeasureHeaders(style, new LayoutConstraints(0, LayoutConstants.Infinite, 0, constraints.MaxHeight));

        var headerHeight = GetHeaderHeight(style, headerMetrics.Height, constraints.MaxHeight);
        var contentMaxW = constraints.MaxWidth == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxWidth);
        var contentMaxH = constraints.MaxHeight == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxHeight - headerHeight);

        var contentWidth = 0;
        var contentHeight = 0;
        if (_contentRoot is not null)
        {
            var contentHints = _contentRoot.Measure(new LayoutConstraints(0, contentMaxW, 0, contentMaxH));
            contentWidth = contentHints.Natural.Width;
            contentHeight = contentHints.Natural.Height;
        }

        var width = Math.Max(headerMetrics.TotalWidth, contentWidth);
        var height = headerHeight + contentHeight;

        var min = new Size(Math.Min(width, LayoutConstants.MaxFinite), Math.Min(height, LayoutConstants.MaxFinite));
        var natural = min;
        var max = new Size(LayoutConstants.Infinite, LayoutConstants.Infinite);
        return SizeHints.Flex(min, natural, max, growX: 1, growY: 1, shrinkX: 1, shrinkY: 1);
    }

    /// <inheritdoc/>
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var style = GetStyle<TabControlStyle>();
        var headerMetrics = MeasureHeaders(style, new LayoutConstraints(0, LayoutConstants.Infinite, 0, finalRect.Height));
        var widths = headerMetrics.Widths;
        var isAttachedLayout = style.LayoutMode == TabControlLayoutMode.Attached;
        var headerVisualTop = finalRect.Y + (isAttachedLayout ? 1 : 0);
        var headerVisualHeight = Math.Max(0, GetHeaderVisualHeight(style, GetHeaderHeight(style, headerMetrics.Height, finalRect.Height)));
        var tabBorderReserve = isAttachedLayout ? 2 : 0;

        _headerHeight = GetHeaderHeight(style, headerMetrics.Height, finalRect.Height);
        _overflowButtonWidth = GetOverflowButtonWidth(style);

        _headerLayouts.Clear();
        _resolvedFirstVisibleIndex = 0;
        _resolvedVisibleEndIndex = 0;
        _showOverflowButtons = false;
        _canScrollPrevious = false;
        _canScrollNext = false;

        for (var i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].Header.Arrange(new Rectangle(0, 0, 0, 0));
        }

        if (finalRect.Width <= 0 || finalRect.Height <= 0 || _tabs.Count == 0)
        {
            _contentRoot?.Arrange(new Rectangle(finalRect.X, finalRect.Y + _headerHeight, Math.Max(0, finalRect.Width), Math.Max(0, finalRect.Height - _headerHeight)));
            return;
        }

        _showOverflowButtons = headerMetrics.TotalWidth > finalRect.Width;
        var buttonReserve = _showOverflowButtons ? _overflowButtonWidth * 2 : 0;
        var availableTabsWidth = Math.Max(0, finalRect.Width - buttonReserve);
        _resolvedFirstVisibleIndex = ResolveVisibleStartIndex(FirstVisibleIndex, availableTabsWidth, widths, _showOverflowButtons);
        _resolvedVisibleEndIndex = ComputeVisibleEnd(_resolvedFirstVisibleIndex, availableTabsWidth, widths);

        if (_showOverflowButtons)
        {
            _canScrollPrevious = _resolvedFirstVisibleIndex > 0;
            _canScrollNext = _resolvedVisibleEndIndex < _tabs.Count;
        }

        var tabsLeft = finalRect.X + (_showOverflowButtons ? _overflowButtonWidth : 0);
        var tabsRight = finalRect.Right - (_showOverflowButtons ? _overflowButtonWidth : 0);
        var x = tabsLeft;
        var pad = style.TabPadding;

        for (var i = _resolvedFirstVisibleIndex; i < _tabs.Count && x < tabsRight; i++)
        {
            var remaining = tabsRight - x;
            var arrangedWidth = Math.Min(remaining, widths[i]);
            if (arrangedWidth <= 0)
            {
                break;
            }

            var page = _tabs[i];
            var closeReserve = page.ShowCloseButton ? GetCloseButtonReserve(style) : 0;
            var contentReserve = Math.Max(0, arrangedWidth - tabBorderReserve);
            if (closeReserve > contentReserve)
            {
                closeReserve = contentReserve;
            }

            var headerWidth = Math.Max(0, contentReserve - pad.Horizontal - closeReserve);
            var headerRect = new Rectangle(
                x + (isAttachedLayout ? 1 : 0) + pad.Left,
                headerVisualTop,
                headerWidth,
                headerVisualHeight);

            page.Header.Arrange(headerRect);

            var closeStart = -1;
            var closeEnd = -1;
            if (closeReserve > 0)
            {
                closeEnd = x + arrangedWidth - (isAttachedLayout ? 1 : 0) - finalRect.X;
                closeStart = closeEnd - closeReserve;
            }

            _headerLayouts.Add(new TabHeaderLayout(
                i,
                x - finalRect.X,
                (x - finalRect.X) + arrangedWidth,
                closeStart,
                closeEnd));

            x += arrangedWidth;
            if (i + 1 < _tabs.Count && x < tabsRight)
            {
                x++;
            }
        }

        _resolvedVisibleEndIndex = _headerLayouts.Count == 0
            ? _resolvedFirstVisibleIndex
            : _headerLayouts[^1].Index + 1;
        _canScrollNext = _showOverflowButtons && _resolvedVisibleEndIndex < _tabs.Count;

        var contentTop = finalRect.Y + _headerHeight;
        var contentHeight = Math.Max(0, finalRect.Height - _headerHeight);
        _contentRoot?.Arrange(new Rectangle(finalRect.X, contentTop, finalRect.Width, contentHeight));
    }

    /// <inheritdoc/>
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = GetStyle<TabControlStyle>();
        var focused = HasFocus;
        var isAttachedLayout = style.LayoutMode == TabControlLayoutMode.Attached;

        var headerHeight = Math.Min(Math.Max(1, _headerHeight), rect.Height);
        var stripStyle = style.ResolveStripStyle(theme);

        for (var y = rect.Y; y < rect.Y + headerHeight; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), stripStyle);
            }
        }

        if (_showOverflowButtons)
        {
            var previousStyle = style.ResolveOverflowButtonStyle(
                theme,
                enabled: _canScrollPrevious,
                hovered: HoveredPart == TabHeaderPart.ScrollPrevious,
                pressed: PressedPart == TabHeaderPart.ScrollPrevious && IsPressedInside);
            var nextStyle = style.ResolveOverflowButtonStyle(
                theme,
                enabled: _canScrollNext,
                hovered: HoveredPart == TabHeaderPart.ScrollNext,
                pressed: PressedPart == TabHeaderPart.ScrollNext && IsPressedInside);

            if (isAttachedLayout)
            {
                previousStyle = previousStyle.ClearBackground();
                nextStyle = nextStyle.ClearBackground();
            }

            var previousRect = new Rectangle(rect.X, rect.Y, _overflowButtonWidth, headerHeight);
            var nextRect = new Rectangle(rect.Right - _overflowButtonWidth, rect.Y, _overflowButtonWidth, headerHeight);
            FillRect(buffer, previousRect, previousStyle);
            FillRect(buffer, nextRect, nextStyle);
            WriteCenteredRune(buffer, previousRect, style.OverflowPreviousRune, previousStyle);
            WriteCenteredRune(buffer, nextRect, style.OverflowNextRune, nextStyle);
        }

        for (var i = 0; i < _headerLayouts.Count; i++)
        {
            var layout = _headerLayouts[i];
            if ((uint)layout.Index >= (uint)_tabs.Count)
            {
                continue;
            }

            var page = _tabs[layout.Index];
            var enabled = IsTabEnabled(page);
            var selected = layout.Index == SelectedIndex;
            var hovered = HoveredPart == TabHeaderPart.Tab && layout.Index == HoveredIndex;
            var pressed = PressedPart == TabHeaderPart.Tab && layout.Index == PressedIndex && IsPressedInside;
            var tabStyle = style.ResolveTabStyle(theme, enabled, focused, selected, hovered, pressed);
            var tabRect = new Rectangle(rect.X + layout.Start, rect.Y, layout.End - layout.Start, headerHeight);

            if (isAttachedLayout)
            {
                RenderAttachedTab(buffer, tabRect, tabStyle, style.ResolveBorderStyle(theme, focused), style.ResolveGlyphs(theme));
            }
            else
            {
                FillRect(buffer, tabRect, tabStyle);
            }

            if (layout.CloseStart >= 0 && layout.CloseEnd > layout.CloseStart)
            {
                var closeHovered = HoveredPart == TabHeaderPart.CloseButton && layout.Index == HoveredIndex;
                var closePressed = PressedPart == TabHeaderPart.CloseButton && layout.Index == PressedIndex && IsPressedInside;
                var closeHeight = isAttachedLayout ? Math.Max(0, headerHeight - 1) : headerHeight;
                if (closeHeight <= 0)
                {
                    continue;
                }

                var closeBaseStyle = isAttachedLayout ? tabStyle.ClearBackground() : tabStyle;
                var closeRect = new Rectangle(
                    rect.X + layout.CloseStart,
                    rect.Y + (isAttachedLayout ? 1 : 0),
                    layout.CloseEnd - layout.CloseStart,
                    closeHeight);
                var closeStyle = style.ResolveCloseButtonStyle(theme, closeBaseStyle, enabled, closeHovered, closePressed);
                FillRect(buffer, closeRect, closeStyle);
                WriteCenteredRune(buffer, closeRect, style.CloseButtonRune, closeStyle);
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_tabs.Count == 0)
        {
            return;
        }

        switch (e.Key)
        {
            case TerminalKey.Left:
                _ = TrySelectRelative(-1);
                e.Handled = true;
                return;
            case TerminalKey.Right:
                _ = TrySelectRelative(1);
                e.Handled = true;
                return;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var localY = e.UiY - Bounds.Y;
        if (localY < 0 || localY >= _headerHeight)
        {
            UpdateHoveredTarget(default);
            UpdatePressedInside(false);
            return;
        }

        var target = HitTestHeader(e.UiX - Bounds.X);
        UpdateHoveredTarget(target);
        UpdatePressedInside(target == new HitTarget(PressedPart, PressedIndex));
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var localY = e.UiY - Bounds.Y;
        if (localY < 0 || localY >= _headerHeight)
        {
            return;
        }

        var target = HitTestHeader(e.UiX - Bounds.X);
        if (!IsTargetEnabled(target))
        {
            return;
        }

        PressedPart = target.Part;
        PressedIndex = target.Index;
        IsPressedInside = true;
        UpdateHoveredTarget(target);
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left || PressedPart == TabHeaderPart.None)
        {
            return;
        }

        var localY = e.UiY - Bounds.Y;
        var currentTarget = localY >= 0 && localY < _headerHeight
            ? HitTestHeader(e.UiX - Bounds.X)
            : default;
        var pressedTarget = new HitTarget(PressedPart, PressedIndex);
        var activate = IsPressedInside && currentTarget == pressedTarget;

        PressedPart = TabHeaderPart.None;
        PressedIndex = -1;
        IsPressedInside = false;
        UpdateHoveredTarget(currentTarget);

        if (!activate)
        {
            return;
        }

        e.Handled = ActivateTarget(pressedTarget);
    }

    internal void ValidateReplacementVisual(Visual value, string role)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Parent is not null)
        {
            throw new InvalidOperationException($"A visual that is already in the UI tree cannot be used as a {role}.");
        }
    }

    internal void OnPageHeaderChanged(TabPage page, Visual? oldHeader, Visual newHeader)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(newHeader);

        if (oldHeader is not null && ReferenceEquals(oldHeader.Parent, this))
        {
            DetachChild(oldHeader);
        }

        AttachChild(newHeader);
    }

    internal void OnPageContentChanged(TabPage page, Visual? oldContent, Visual newContent)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(newContent);

        if (ReferenceEquals(_contentHost.Content, oldContent))
        {
            _contentHost.Content = newContent;
        }
    }

    private void AttachPage(TabPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        ValidateReplacementVisual(page.Header, "tab header");
        ValidateReplacementVisual(page.Content, "tab content");
        page.Attach(this);
        AttachChild(page.Header);
    }

    private void DetachPage(TabPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (ReferenceEquals(_contentHost.Content, page.Content))
        {
            _contentHost.Content = null;
        }

        if (ReferenceEquals(page.Header.Parent, this))
        {
            DetachChild(page.Header);
        }

        page.Detach(this);
    }

    private bool TryCloseTab(int index, TabCloseReason reason)
    {
        if ((uint)index >= (uint)_tabs.Count)
        {
            return false;
        }

        CaptureSelectedTabForEvent(out var oldIndex, out var oldPage);

        var page = _tabs[index];
        if (!page.RaiseRequestClosing(this, index, reason))
        {
            return false;
        }

        var nextSelectedIndex = ResolveSelectedIndexAfterRemoval(index);
        AdjustInteractionStateAfterRemoval(index);
        _tabs.RemoveAt(index);
        page.RaiseClosed(this, index, reason);

        if (_tabs.Count == 0)
        {
            _contentHost.Content = null;
            SelectedIndex = 0;
            FirstVisibleIndex = 0;
            RaiseSelectionChangedIfNeeded(oldIndex, oldPage);
            return true;
        }

        if (SelectedIndex != nextSelectedIndex)
        {
            SelectedIndex = nextSelectedIndex;
        }

        SyncFirstVisibleIndexToSelection();
        RaiseSelectionChangedIfNeeded(oldIndex, oldPage);
        return true;
    }

    private void MoveTabCore(int oldIndex, int newIndex)
    {
        if (oldIndex == newIndex)
        {
            return;
        }

        CaptureSelectedTabForEvent(out var oldSelectedIndex, out var oldSelectedPage);

        var nextSelectedIndex = MoveIndex(SelectedIndex, oldIndex, newIndex);
        var nextFirstVisibleIndex = MoveIndex(FirstVisibleIndex, oldIndex, newIndex);
        AdjustInteractionStateAfterMove(oldIndex, newIndex);
        _tabs.Move(oldIndex, newIndex);

        _suppressSelectionChangedEvents = true;
        try
        {
            SelectedIndex = Math.Clamp(nextSelectedIndex, 0, Math.Max(0, _tabs.Count - 1));
            FirstVisibleIndex = Math.Clamp(nextFirstVisibleIndex, 0, Math.Max(0, _tabs.Count - 1));
        }
        finally
        {
            _suppressSelectionChangedEvents = false;
        }

        SyncFirstVisibleIndexToSelection();
        RaiseSelectionChangedIfNeeded(oldSelectedIndex, oldSelectedPage);
    }

    private void CaptureSelectedTabForEvent(out int selectedIndex, out TabPage? selectedPage)
    {
        using var _ = global::XenoAtom.Terminal.UI.BindingManager.Current.SuppressReadTracking();

        if (_tabs.Count == 0)
        {
            selectedIndex = -1;
            selectedPage = null;
            return;
        }

        selectedIndex = Math.Clamp(SelectedIndex, 0, _tabs.Count - 1);
        selectedPage = _tabs[selectedIndex];
    }

    private void RaiseSelectionChangedIfNeeded(int oldIndex, TabPage? oldPage)
    {
        CaptureSelectedTabForEvent(out var newIndex, out var newPage);
        if (oldIndex == newIndex && ReferenceEquals(oldPage, newPage))
        {
            return;
        }

        RaiseEvent(SelectionChangedEvent, new TabSelectionChangedEventArgs(oldIndex, newIndex, oldPage, newPage));
    }

    private int ResolveSelectedIndexAfterRemoval(int removedIndex)
    {
        if (_tabs.Count <= 1)
        {
            return 0;
        }

        if (removedIndex < SelectedIndex)
        {
            return SelectedIndex - 1;
        }

        if (removedIndex == SelectedIndex)
        {
            return Math.Min(removedIndex, _tabs.Count - 2);
        }

        return SelectedIndex;
    }

    private void AdjustInteractionStateAfterRemoval(int removedIndex)
    {
        AdjustInteractionIndex(ref _hoveredIndex, ref _hoveredPart, removedIndex);
        AdjustInteractionIndex(ref _pressedIndex, ref _pressedPart, removedIndex);
        if (PressedPart == TabHeaderPart.None)
        {
            IsPressedInside = false;
        }
    }

    private void AdjustInteractionStateAfterMove(int oldIndex, int newIndex)
    {
        AdjustInteractionIndexAfterMove(ref _hoveredIndex, oldIndex, newIndex);
        AdjustInteractionIndexAfterMove(ref _pressedIndex, oldIndex, newIndex);
    }

    private static void AdjustInteractionIndex(ref int index, ref TabHeaderPart part, int removedIndex)
    {
        if (index < 0)
        {
            return;
        }

        if (index == removedIndex)
        {
            index = -1;
            part = TabHeaderPart.None;
            return;
        }

        if (index > removedIndex)
        {
            index--;
        }
    }

    private static void AdjustInteractionIndexAfterMove(ref int index, int oldIndex, int newIndex)
    {
        if (index < 0)
        {
            return;
        }

        index = MoveIndex(index, oldIndex, newIndex);
    }

    private static int MoveIndex(int index, int oldIndex, int newIndex)
    {
        if (index == oldIndex)
        {
            return newIndex;
        }

        if (oldIndex < newIndex)
        {
            return index > oldIndex && index <= newIndex ? index - 1 : index;
        }

        return index >= newIndex && index < oldIndex ? index + 1 : index;
    }

    private bool TrySelectRelative(int direction)
    {
        if (_tabs.Count == 0 || direction == 0)
        {
            return false;
        }

        var index = Math.Clamp(SelectedIndex, 0, _tabs.Count - 1);
        while (true)
        {
            index += direction;
            if ((uint)index >= (uint)_tabs.Count)
            {
                return false;
            }

            if (IsTabEnabled(_tabs[index]))
            {
                SelectedIndex = index;
                SyncFirstVisibleIndexToSelection();
                return true;
            }
        }
    }

    private bool ActivateTarget(HitTarget target)
    {
        switch (target.Part)
        {
            case TabHeaderPart.Tab:
                if (!IsTargetEnabled(target))
                {
                    return false;
                }

                SelectedIndex = target.Index;
                SyncFirstVisibleIndexToSelection();
                return true;
            case TabHeaderPart.CloseButton:
                return TryCloseTab(target.Index, TabCloseReason.CloseButton);
            case TabHeaderPart.ScrollPrevious:
                return ScrollTabs(-1);
            case TabHeaderPart.ScrollNext:
                return ScrollTabs(1);
            default:
                return false;
        }
    }

    private bool ScrollTabs(int delta)
    {
        if (!_showOverflowButtons || delta == 0)
        {
            return false;
        }

        var next = Math.Clamp(_resolvedFirstVisibleIndex + delta, 0, Math.Max(0, _tabs.Count - 1));
        if (delta < 0 && !_canScrollPrevious)
        {
            return false;
        }

        if (delta > 0 && !_canScrollNext)
        {
            return false;
        }

        if (FirstVisibleIndex == next)
        {
            return false;
        }

        FirstVisibleIndex = next;
        return true;
    }

    private HitTarget HitTestHeader(int localX)
    {
        if (_showOverflowButtons)
        {
            if (localX >= 0 && localX < _overflowButtonWidth)
            {
                return new HitTarget(TabHeaderPart.ScrollPrevious, -1);
            }

            if (localX >= Bounds.Width - _overflowButtonWidth && localX < Bounds.Width)
            {
                return new HitTarget(TabHeaderPart.ScrollNext, -1);
            }
        }

        for (var i = 0; i < _headerLayouts.Count; i++)
        {
            var layout = _headerLayouts[i];
            if (localX < layout.Start || localX >= layout.End)
            {
                continue;
            }

            if (layout.CloseStart >= 0 && localX >= layout.CloseStart && localX < layout.CloseEnd)
            {
                return new HitTarget(TabHeaderPart.CloseButton, layout.Index);
            }

            return new HitTarget(TabHeaderPart.Tab, layout.Index);
        }

        return default;
    }

    private bool IsTargetEnabled(HitTarget target)
    {
        return target.Part switch
        {
            TabHeaderPart.Tab => target.Index >= 0 && target.Index < _tabs.Count && IsTabEnabled(_tabs[target.Index]),
            TabHeaderPart.CloseButton => target.Index >= 0 && target.Index < _tabs.Count && IsTabEnabled(_tabs[target.Index]),
            TabHeaderPart.ScrollPrevious => _showOverflowButtons && _canScrollPrevious,
            TabHeaderPart.ScrollNext => _showOverflowButtons && _canScrollNext,
            _ => false,
        };
    }

    private bool IsTabEnabled(TabPage page) => page.IsEnabled && page.Header.IsEnabled && page.Content.IsEnabled;

    private void UpdateHoveredTarget(HitTarget target)
    {
        if (HoveredPart == target.Part && HoveredIndex == target.Index)
        {
            return;
        }

        HoveredPart = target.Part;
        HoveredIndex = target.Index;
    }

    private void UpdatePressedInside(bool value)
    {
        if (IsPressedInside == value)
        {
            return;
        }

        IsPressedInside = value;
    }

    private void SyncFirstVisibleIndexToSelection()
    {
        if (_tabs.Count == 0 || Bounds.Width <= 0)
        {
            if (FirstVisibleIndex != 0)
            {
                FirstVisibleIndex = 0;
            }

            return;
        }

        var style = GetStyle<TabControlStyle>();
        var headerMetrics = MeasureHeaders(style, new LayoutConstraints(0, LayoutConstants.Infinite, 0, Bounds.Height));
        if (headerMetrics.TotalWidth <= Bounds.Width)
        {
            if (FirstVisibleIndex != 0)
            {
                FirstVisibleIndex = 0;
            }

            return;
        }

        var availableTabsWidth = Math.Max(0, Bounds.Width - (GetOverflowButtonWidth(style) * 2));
        var next = ResolveVisibleStartIndexForSelection(FirstVisibleIndex, availableTabsWidth, headerMetrics.Widths);
        if (FirstVisibleIndex != next)
        {
            FirstVisibleIndex = next;
        }
    }

    private HeaderMetrics MeasureHeaders(TabControlStyle style, in LayoutConstraints constraints)
    {
        var widths = new int[_tabs.Count];
        var height = 1;
        var totalWidth = 0;
        var tabBorderReserve = style.LayoutMode == TabControlLayoutMode.Attached ? 2 : 0;

        for (var i = 0; i < _tabs.Count; i++)
        {
            var header = _tabs[i].Header;
            var hints = header.Measure(constraints);
            height = Math.Max(height, hints.Natural.Height);

            var width = hints.Natural.Width + style.TabPadding.Horizontal + tabBorderReserve;
            width += _tabs[i].ShowCloseButton ? GetCloseButtonReserve(style) : 0;
            widths[i] = Math.Max(0, width);
            totalWidth += widths[i];
            if (i + 1 < _tabs.Count)
            {
                totalWidth += 1;
            }
        }

        return new HeaderMetrics(Math.Max(1, height), totalWidth, widths);
    }

    private int GetCloseButtonReserve(TabControlStyle style)
    {
        var closeWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.CloseButtonRune));
        return closeWidth + Math.Max(0, style.CloseButtonSpacing);
    }

    private int GetOverflowButtonWidth(TabControlStyle style)
    {
        var previousWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.OverflowPreviousRune));
        var nextWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(style.OverflowNextRune));
        return Math.Max(previousWidth, nextWidth);
    }

    private int ResolveVisibleStartIndex(int desiredStart, int availableTabsWidth, IReadOnlyList<int> widths, bool overflowEnabled)
    {
        if (!overflowEnabled || _tabs.Count == 0)
        {
            return 0;
        }

        return Math.Clamp(desiredStart, 0, _tabs.Count - 1);
    }

    private int ResolveVisibleStartIndexForSelection(int desiredStart, int availableTabsWidth, IReadOnlyList<int> widths)
    {
        if (_tabs.Count == 0)
        {
            return 0;
        }

        var start = Math.Clamp(desiredStart, 0, _tabs.Count - 1);
        var selected = Math.Clamp(SelectedIndex, 0, _tabs.Count - 1);
        var end = ComputeVisibleEnd(start, availableTabsWidth, widths);

        if (selected < start)
        {
            start = selected;
        }
        else if (selected >= end || !IsTabFullyVisible(start, selected, availableTabsWidth, widths))
        {
            while (start < selected && !IsTabFullyVisible(start, selected, availableTabsWidth, widths))
            {
                start++;
            }
        }

        return start;
    }

    private static bool IsTabFullyVisible(int start, int index, int availableTabsWidth, IReadOnlyList<int> widths)
    {
        if (availableTabsWidth <= 0 || (uint)start >= (uint)widths.Count || (uint)index >= (uint)widths.Count || index < start)
        {
            return false;
        }

        var requiredWidth = 0;
        for (var i = start; i <= index; i++)
        {
            if (i > start)
            {
                requiredWidth++;
            }

            requiredWidth += widths[i];
            if (requiredWidth > availableTabsWidth)
            {
                return false;
            }
        }

        return true;
    }

    private static void ThrowMoveTabIndexOutOfRange(int oldIndex, int newIndex, int count)
    {
        if ((uint)oldIndex >= (uint)count)
        {
            throw new ArgumentOutOfRangeException(nameof(oldIndex), oldIndex, "The source tab index must refer to an existing tab.");
        }

        throw new ArgumentOutOfRangeException(nameof(newIndex), newIndex, "The destination tab index must refer to an existing tab.");
    }

    private int ComputeVisibleEnd(int start, int availableTabsWidth, IReadOnlyList<int> widths)
    {
        if ((uint)start >= (uint)_tabs.Count || availableTabsWidth <= 0)
        {
            return start;
        }

        var remaining = availableTabsWidth;
        var index = start;
        while (index < _tabs.Count && remaining > 0)
        {
            var tabWidth = Math.Min(remaining, widths[index]);
            if (tabWidth <= 0)
            {
                break;
            }

            remaining -= tabWidth;
            index++;

            if (index < _tabs.Count && remaining > 0)
            {
                remaining--;
            }
        }

        return index > start ? index : Math.Min(_tabs.Count, start + 1);
    }

    private static int GetHeaderHeight(TabControlStyle style, int measuredHeaderHeight, int availableHeight)
    {
        var baseHeight = Math.Max(1, measuredHeaderHeight);
        if (style.LayoutMode == TabControlLayoutMode.Attached)
        {
            baseHeight++;
        }

        return Math.Max(1, Math.Min(baseHeight, Math.Max(0, availableHeight)));
    }

    private static int GetHeaderVisualHeight(TabControlStyle style, int headerHeight)
        => style.LayoutMode == TabControlLayoutMode.Attached ? Math.Max(0, headerHeight - 1) : headerHeight;

    private static void RenderAttachedTab(CellBuffer buffer, Rectangle rect, Style tabStyle, Style borderStyle, LineGlyphs glyphs)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        FillRect(buffer, rect, tabStyle.ClearBackground());

        var left = rect.X;
        var top = rect.Y;
        var right = rect.Right - 1;
        var bottom = rect.Bottom - 1;

        buffer.SetCell(left, top, glyphs.TopLeft, borderStyle);
        if (right > left)
        {
            buffer.SetCell(right, top, glyphs.TopRight, borderStyle);
            for (var x = left + 1; x < right; x++)
            {
                buffer.SetCell(x, top, glyphs.Horizontal, borderStyle);
            }
        }

        for (var y = top + 1; y <= bottom; y++)
        {
            buffer.SetCell(left, y, glyphs.Vertical, borderStyle);
            if (right > left)
            {
                buffer.SetCell(right, y, glyphs.Vertical, borderStyle);
            }
        }
    }

    private bool TryGetSelectedHeaderGap(out int gapStart, out int gapEnd)
    {
        gapStart = -1;
        gapEnd = -1;

        for (var i = 0; i < _headerLayouts.Count; i++)
        {
            var layout = _headerLayouts[i];
            if (layout.Index != SelectedIndex)
            {
                continue;
            }

            gapStart = layout.Start;
            gapEnd = layout.End;
            return true;
        }

        return false;
    }

    private void RenderAttachedContentChrome(CellBuffer buffer, Rectangle rect)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = GetStyle<TabControlStyle>();
        var glyphs = style.ResolveGlyphs(theme);
        var inactiveJointGlyphs = glyphs.Equals(LineGlyphs.Rounded) ? LineGlyphs.Single : glyphs;
        var borderStyle = style.ResolveBorderStyle(theme, HasFocus);
        var left = rect.X;
        var top = rect.Y;
        var right = rect.Right - 1;
        var visibleLeft = _showOverflowButtons ? Math.Min(rect.Width - 1, _overflowButtonWidth) : 0;
        var visibleRight = _showOverflowButtons ? Math.Max(visibleLeft, rect.Width - _overflowButtonWidth - 1) : rect.Width - 1;

        for (var x = left; x <= right; x++)
        {
            buffer.SetCell(x, top, glyphs.Horizontal, borderStyle);
        }

        for (var i = 0; i < _headerLayouts.Count; i++)
        {
            var layout = _headerLayouts[i];
            if (layout.Index == SelectedIndex || layout.Start >= rect.Width || layout.End <= 0)
            {
                continue;
            }

            var inactiveStart = Math.Max(0, layout.Start);
            var inactiveEndExclusive = Math.Min(rect.Width, layout.End);
            var inactiveEnd = inactiveEndExclusive - 1;
            if (inactiveEnd < inactiveStart)
            {
                continue;
            }

            var startRune = inactiveJointGlyphs.TeeBottom;
            var endRune = inactiveJointGlyphs.TeeBottom;

            buffer.SetCell(left + inactiveStart, top, startRune, borderStyle);

            if (inactiveEnd > inactiveStart)
            {
                buffer.SetCell(left + inactiveEnd, top, endRune, borderStyle);
                for (var x = inactiveStart + 1; x < inactiveEnd; x++)
                {
                    buffer.SetCell(left + x, top, glyphs.Horizontal, borderStyle);
                }
            }
        }

        if (!TryGetSelectedHeaderGap(out var gapStart, out var gapEnd))
        {
            return;
        }

        var start = Math.Max(0, gapStart);
        var endExclusive = Math.Min(rect.Width, gapEnd);
        var end = endExclusive - 1;

        var startAtVisibleEdge = start <= visibleLeft;
        var endAtVisibleEdge = end >= visibleRight;

        buffer.SetCell(left + start, top, glyphs.BottomRight, borderStyle);

        buffer.SetCell(left + end, top, glyphs.BottomLeft, borderStyle);

        for (var x = start + 1; x < endExclusive - 1; x++)
        {
            buffer.SetCell(left + x, top, new Rune(' '), style.ResolveStripStyle(theme));
        }
    }

    private static void FillRect(CellBuffer buffer, Rectangle rect, Style style)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), style);
            }
        }
    }

    private static void WriteCenteredRune(CellBuffer buffer, Rectangle rect, Rune rune, Style style)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var runeWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(rune));
        if (runeWidth > rect.Width)
        {
            return;
        }

        var x = rect.X + Math.Max(0, (rect.Width - runeWidth) / 2);
        var y = rect.Y + (rect.Height / 2);
        buffer.SetCell(x, y, rune, style);
    }

    private void EnsureContentTemplate(TabControlStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);

        var factory = style.TabContentTemplateFactory;
        if (_contentRoot is not null
            && ReferenceEquals(factory, _contentTemplateFactory)
            && _contentLayoutMode == style.LayoutMode)
        {
            return;
        }

        if (_contentRoot is not null)
        {
            DetachChild(_contentRoot);
            _contentRoot = null;
        }

        _attachedContentChrome.Content = null;

        if (_contentTemplate is not null)
        {
            _contentTemplate.Content = null;
            _contentTemplate = null;
        }

        _contentTemplateFactory = factory;
        _contentLayoutMode = style.LayoutMode;

        Visual innerRoot;

        if (factory is null)
        {
            innerRoot = _contentHost;
        }
        else
        {
            var template = factory(_contentHost);
            if (template is null)
            {
                throw new InvalidOperationException($"{nameof(TabControlStyle)}.{nameof(TabControlStyle.TabContentTemplateFactory)} returned null.");
            }

            if (!ReferenceEquals(template.Content, _contentHost))
            {
                template.Content = _contentHost;
            }

            _contentTemplate = template;
            innerRoot = template;
        }

        if (style.LayoutMode == TabControlLayoutMode.Attached)
        {
            _attachedContentChrome.Content = innerRoot;
            _contentRoot = _attachedContentChrome;
        }
        else
        {
            _attachedContentChrome.Content = null;
            _contentRoot = innerRoot;
        }

        _contentRoot.HorizontalAlignment = Align.Stretch;
        _contentRoot.VerticalAlignment = Align.Stretch;
        AttachChild(_contentRoot);
    }

    private readonly record struct HeaderMetrics(int Height, int TotalWidth, int[] Widths);

    private readonly record struct TabHeaderLayout(int Index, int Start, int End, int CloseStart, int CloseEnd);

    private readonly record struct HitTarget(TabHeaderPart Part, int Index);

    /// <summary>
    /// Identifies an interactive region within the tab header strip.
    /// </summary>
    public enum TabHeaderPart
    {
        /// <summary>No interactive header element.</summary>
        None,

        /// <summary>The main tab header surface.</summary>
        Tab,

        /// <summary>The tab close button.</summary>
        CloseButton,

        /// <summary>The overflow button that reveals earlier tabs.</summary>
        ScrollPrevious,

        /// <summary>The overflow button that reveals later tabs.</summary>
        ScrollNext,
    }

    private sealed class TabContentHost : ContentVisual
    {
    }

    private sealed class AttachedContentChrome : Padder
    {
        private readonly TabControl _owner;

        public AttachedContentChrome(TabControl owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        protected override Thickness Inset => new(Left: 0, Top: 1, Right: 0, Bottom: 0);

        protected override void RenderOverride(CellBuffer buffer)
            => _owner.RenderAttachedContentChrome(buffer, Bounds);
    }

    [RoutedEvent(RoutingStrategy.Bubble)]
    private void OnSelectionChanged(TabSelectionChangedEventArgs e) { }
}

/// <summary>
/// Provides data for the <see cref="TabControl.SelectionChangedEvent"/> event.
/// </summary>
public sealed class TabSelectionChangedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TabSelectionChangedEventArgs"/> class.
    /// </summary>
    /// <param name="oldIndex">The previously selected tab index, or -1 when no tab was selected.</param>
    /// <param name="newIndex">The newly selected tab index, or -1 when no tab is selected.</param>
    /// <param name="oldPage">The previously selected tab page, if any.</param>
    /// <param name="newPage">The newly selected tab page, if any.</param>
    public TabSelectionChangedEventArgs(int oldIndex, int newIndex, TabPage? oldPage, TabPage? newPage)
    {
        OldIndex = oldIndex;
        NewIndex = newIndex;
        OldPage = oldPage;
        NewPage = newPage;
    }

    /// <summary>
    /// Gets the previously selected tab index, or -1 when no tab was selected.
    /// </summary>
    public int OldIndex { get; }

    /// <summary>
    /// Gets the newly selected tab index, or -1 when no tab is selected.
    /// </summary>
    public int NewIndex { get; }

    /// <summary>
    /// Gets the previously selected tab page, if any.
    /// </summary>
    public TabPage? OldPage { get; }

    /// <summary>
    /// Gets the newly selected tab page, if any.
    /// </summary>
    public TabPage? NewPage { get; }
}
