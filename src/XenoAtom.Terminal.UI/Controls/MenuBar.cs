// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a menu bar hosting top-level menu items.
/// </summary>
public sealed partial class MenuBar : Visual
{
    private readonly BindableList<MenuItem> _items;
    private readonly VisualList<MenuBarItem> _presenters;

    private MenuItem[]? _presenterItems;
    private readonly List<Popup> _openPopups = new();

    private int _openIndex = -1;
    private int _selectedIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuBar"/> class.
    /// </summary>
    public MenuBar()
    {
        Focusable = true;
        this.HorizontalAlignment(Align.Stretch);
        _items = new BindableList<MenuItem>(this, "MenuBar.Items");
        _presenters = new VisualList<MenuBarItem>(this, "MenuBar.Presenters");
    }

    /// <summary>
    /// Gets the menu items collection.
    /// </summary>
    [Bindable]
    public BindableList<MenuItem> Items => _items;

    internal int OpenIndex => _openIndex;

    internal int SelectedIndex => _selectedIndex;

    /// <inheritdoc />
    protected override int ChildrenCount => _presenters.Count;

    /// <inheritdoc />
    protected override Visual GetChild(int index) => _presenters[index];

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        EnsurePresenters();

        var style = GetStyle<MenuBarStyle>();
        var padding = style.Padding;
        var spacing = Math.Max(0, style.ItemSpacing);

        var width = 0;
        var height = 1;
        var maxH = constraints.MaxHeight;

        for (var i = 0; i < _presenters.Count; i++)
        {
            var item = _presenters[i];
            item.Measure(new LayoutConstraints(0, LayoutConstants.Infinite, 0, maxH));
            width += item.DesiredSize.Width;
            height = Math.Max(height, item.DesiredSize.Height);

            if (i + 1 < _presenters.Count)
            {
                width += spacing;
            }
        }

        height = Math.Max(1, height + padding.Vertical);
        width = Math.Max(1, padding.Horizontal + width);
        return SizeHints.Fixed(constraints.Clamp(new Size(width, height)));
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        EnsurePresenters();

        var style = GetStyle<MenuBarStyle>();
        var padding = style.Padding;
        var spacing = Math.Max(0, style.ItemSpacing);

        var x = finalRect.X + padding.Left;
        var y = finalRect.Y + padding.Top;
        var innerHeight = Math.Max(0, finalRect.Height - padding.Vertical);

        for (var i = 0; i < _presenters.Count; i++)
        {
            var item = _presenters[i];
            var w = Math.Min(item.DesiredSize.Width, Math.Max(0, finalRect.Right - x));
            item.Arrange(new Rectangle(x, y, w, innerHeight));
            x += w + spacing;
        }
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
        var style = GetStyle<MenuBarStyle>();
        var barStyle = style.ResolveBarStyle(theme);

        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), barStyle);
            }
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_items.Count == 0)
        {
            return;
        }

        var target = ResolveCommandTarget();

        switch (e.Key)
        {
            case TerminalKey.Left:
                _selectedIndex = FindPreviousEnabledIndex(_selectedIndex - 1, target);
                e.Handled = true;
                return;

            case TerminalKey.Right:
                _selectedIndex = FindNextEnabledIndex(_selectedIndex + 1, target);
                e.Handled = true;
                return;

            case TerminalKey.Enter:
            case TerminalKey.Space:
            case TerminalKey.Down:
                OpenMenu(_selectedIndex);
                e.Handled = true;
                return;
        }
    }

    internal void OpenMenu(int index)
    {
        if (_items.Count == 0)
        {
            return;
        }

        var target = ResolveCommandTarget();

        index = Math.Clamp(index, 0, _items.Count - 1);
        if (!_items[index].IsEnabledFor(target))
        {
            return;
        }

        _selectedIndex = index;

        if (_openIndex == index && _openPopups.Count > 0)
        {
            return;
        }

        CloseAllMenus();

        var menu = _items[index];
        if (menu.Items.Count == 0)
        {
            InvokeMenuItem(menu, target);
            _openIndex = -1;
            return;
        }

        _openIndex = index;

        var visibleItems = FilterVisibleItems(menu.Items, target);
        var list = new MenuList(this, visibleItems, parent: null, target: target);
        var menuListStyle = GetStyle<MenuListStyle>();
        var popupContent = menuListStyle.PopupTemplateFactory?.Invoke(list) ?? list;

        var popup = new Popup
        {
            Anchor = _presenters[index],
            Content = popupContent,
            MatchAnchorWidth = false,
            Placement = PopupPlacement.Below,
        }.Style(PopupStyle.Default with { Padding = Thickness.Zero });

        RegisterPopup(popup);

        popup.Closed((_, _) =>
        {
            list.ReleaseVisuals();
            UnregisterPopup(popup);
            _openIndex = -1;
            App?.Focus(this);
        });

        popup.Show();
    }

    internal void CloseAllMenus()
    {
        if (_openPopups.Count == 0)
        {
            _openIndex = -1;
            return;
        }

        var copy = _openPopups.ToArray();
        for (var i = copy.Length - 1; i >= 0; i--)
        {
            copy[i].Close();
        }

        _openIndex = -1;
    }

    internal void RegisterPopup(Popup popup)
    {
        _openPopups.Add(popup);
    }

    internal void UnregisterPopup(Popup popup)
    {
        _openPopups.Remove(popup);
    }

    private void EnsurePresenters()
    {
        var items = _items;
        var count = items.Count;

        if (_presenterItems is not null && _presenterItems.Length == count)
        {
            var same = true;
            for (var i = 0; i < count; i++)
            {
                if (!ReferenceEquals(_presenterItems[i], items[i]))
                {
                    same = false;
                    break;
                }
            }

            if (same)
            {
                return;
            }
        }

        for (var i = 0; i < _presenters.Count; i++)
        {
            _presenters[i].ReleaseVisuals();
        }

        _presenters.Clear();
        _presenterItems = count == 0 ? Array.Empty<MenuItem>() : new MenuItem[count];

        for (var i = 0; i < count; i++)
        {
            var item = items[i];
            _presenterItems[i] = item;
            _presenters.Add(new MenuBarItem(i, item));
        }

        _selectedIndex = Math.Clamp(_selectedIndex, 0, Math.Max(0, count - 1));
    }

    internal int FindNextEnabledIndex(int start, Visual target)
    {
        for (var i = Math.Max(0, start); i < _items.Count; i++)
        {
            if (_items[i].IsEnabledFor(target))
            {
                return i;
            }
        }

        return Math.Max(0, _items.Count - 1);
    }

    internal int FindPreviousEnabledIndex(int start, Visual target)
    {
        for (var i = Math.Min(start, _items.Count - 1); i >= 0; i--)
        {
            if (_items[i].IsEnabledFor(target))
            {
                return i;
            }
        }

        return 0;
    }

    private Visual ResolveCommandTarget()
    {
        var app = App;
        if (app is null)
        {
            return this;
        }

        return app.FocusedElement ?? app.Root;
    }

    private void InvokeMenuItem(MenuItem item, Visual target)
    {
        var effectiveTarget = item.CommandTarget ?? target;

        if (!item.IsEnabledFor(effectiveTarget))
        {
            return;
        }

        if (item.Command is { } cmd)
        {
            if (!cmd.IsVisibleFor(effectiveTarget) || !cmd.CanExecuteFor(effectiveTarget))
            {
                return;
            }

            cmd.Execute(effectiveTarget);
            return;
        }

        item.Action.Invoke?.Invoke();
    }

    private static IReadOnlyList<MenuItem> FilterVisibleItems(IReadOnlyList<MenuItem> items, Visual target)
    {
        var visibleCount = 0;
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].IsVisibleFor(target))
            {
                visibleCount++;
            }
        }

        if (visibleCount == items.Count)
        {
            return items;
        }

        if (visibleCount == 0)
        {
            return Array.Empty<MenuItem>();
        }

        var list = new List<MenuItem>(visibleCount);
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item.IsVisibleFor(target))
            {
                list.Add(item);
            }
        }

        return list;
    }

    private sealed class MenuBarItem : ContentVisual
    {
        private readonly MenuItem _item;
        private readonly int _index;

        public MenuBarItem(int index, MenuItem item)
        {
            _index = index;
            _item = item;
            Focusable = false;
            Content = item.Header;
        }

        public void ReleaseVisuals()
        {
            Content = null;
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            var style = GetStyle<MenuBarStyle>();
            var padding = style.ItemPadding;

            var content = Content;
            if (content is not null)
            {
                content.Measure(new LayoutConstraints(0, LayoutConstants.Infinite, 0, Math.Max(1, constraints.MaxHeight)));
            }

            var w = padding.Horizontal + (content?.DesiredSize.Width ?? 0);
            var h = Math.Max(1, padding.Vertical + (content?.DesiredSize.Height ?? 1));
            return SizeHints.Fixed(constraints.Clamp(new Size(w, h)));
        }

        protected override void ArrangeCore(in Rectangle finalRect)
        {
            var style = GetStyle<MenuBarStyle>();
            var padding = style.ItemPadding;

            var content = Content;
            if (content is not null)
            {
                content.Arrange(new Rectangle(
                    finalRect.X + padding.Left,
                    finalRect.Y + padding.Top,
                    Math.Max(0, finalRect.Width - padding.Horizontal),
                    Math.Max(0, finalRect.Height - padding.Vertical)));
            }
        }

        protected override void RenderOverride(CellBuffer buffer)
        {
            var rect = Bounds;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            var theme = GetTheme();
            var style = GetStyle<MenuBarStyle>();

            var bar = Parent as MenuBar;
            var open = bar is not null && bar.OpenIndex == _index;
            var selected = bar is not null && ReferenceEquals(bar.App?.FocusedElement, bar) && bar.SelectedIndex == _index;
            var enabled = bar is null ? _item.IsEnabled : _item.IsEnabledFor(bar.ResolveCommandTarget());
            var resolved = style.ResolveItemStyle(theme, enabled: enabled, open: open, selected: selected, hovered: IsHovered);

            for (var y = rect.Y; y < rect.Y + rect.Height; y++)
            {
                for (var x = rect.X; x < rect.X + rect.Width; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), resolved);
                }
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (Parent is not MenuBar bar || bar._openIndex < 0)
            {
                return;
            }

            if (bar._openIndex != _index)
            {
                bar.OpenMenu(_index);
            }
        }

        protected override void OnPointerPressed(PointerEventArgs e)
        {
            if (e.Button != TerminalMouseButton.Left)
            {
                return;
            }

            if (Parent is not MenuBar bar)
            {
                return;
            }

            if (bar._openIndex == _index)
            {
                bar.CloseAllMenus();
            }
            else
            {
                bar.OpenMenu(_index);
            }

            e.Handled = true;
        }
    }

    private sealed partial class MenuList : Visual
    {
        private readonly MenuBar _owner;
        private readonly IReadOnlyList<MenuItem> _items;
        private readonly VisualList<MenuListRow> _rows;
        private readonly MenuList? _parent;
        private readonly Visual _target;

        private Popup? _submenuPopup;

        [Bindable]
        private partial int SelectedIndex { get; set; }

        [Bindable]
        private partial int HoveredIndex { get; set; }

        private Rectangle _innerRect;
        private int _submenuColumnWidth;

        public MenuList(MenuBar owner, IReadOnlyList<MenuItem> items, MenuList? parent, Visual target)
        {
            _owner = owner;
            _items = items;
            _parent = parent;
            _target = target;
            Focusable = true;
            _rows = new VisualList<MenuListRow>(this, "MenuList.Rows");

            for (var i = 0; i < items.Count; i++)
            {
                _rows.Add(new MenuListRow(items[i]));
            }

            SelectedIndex = FindNextSelectableIndex(0);
            HoveredIndex = -1;
        }

        protected override int ChildrenCount => _rows.Count;

        protected override Visual GetChild(int index) => _rows[index];

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            var style = GetStyle<MenuListStyle>();
            var padding = style.Padding;

            var maxRowWidth = 0;
            var submenuWidth = 0;

            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                row.Measure(new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1));
                maxRowWidth = Math.Max(maxRowWidth, row.DesiredSize.Width);

                if (HasVisibleSubmenu(_items[i]))
                {
                    submenuWidth = Math.Max(submenuWidth, Math.Max(1, TerminalTextUtility.GetRuneWidth(style.SubmenuGlyph)) + 1);
                }
            }

            _submenuColumnWidth = submenuWidth;

            var width = padding.Horizontal + maxRowWidth + submenuWidth;
            var height = padding.Vertical + _items.Count;

            width = Math.Max(1, width);
            height = Math.Max(1, height);
            return SizeHints.Fixed(constraints.Clamp(new Size(width, height)));
        }

        protected override void ArrangeCore(in Rectangle finalRect)
        {
            var style = GetStyle<MenuListStyle>();
            var padding = style.Padding;

            _innerRect = new Rectangle(
                finalRect.X + padding.Left,
                finalRect.Y + padding.Top,
                Math.Max(0, finalRect.Width - padding.Horizontal),
                Math.Max(0, finalRect.Height - padding.Vertical));

            var rowWidth = Math.Max(0, _innerRect.Width - _submenuColumnWidth);

            for (var i = 0; i < _rows.Count; i++)
            {
                var rowRect = new Rectangle(_innerRect.X, _innerRect.Y + i, rowWidth, 1);
                _rows[i].Arrange(rowRect);
            }
        }

        protected override void RenderOverride(CellBuffer buffer)
        {
            var rect = Bounds;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            var theme = GetTheme();
            var style = GetStyle<MenuListStyle>();
            var inner = _innerRect;

            for (var i = 0; i < _items.Count; i++)
            {
                var y = inner.Y + i;
                if (y < rect.Y || y >= rect.Bottom)
                {
                    continue;
                }

                var item = _items[i];
                var enabled = item.IsEnabledFor(_target);
                var selected = i == SelectedIndex;
                var hovered = i == HoveredIndex;

                var rowStyle = item.IsSeparator
                    ? style.ResolveSeparatorStyle(theme)
                    : style.ResolveItemStyle(theme, enabled: enabled, selected: selected, hovered: hovered);

                for (var x = inner.X; x < inner.X + inner.Width; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), rowStyle);
                }

                if (item.IsSeparator)
                {
                    var glyph = theme.Lines.Horizontal;
                    for (var x = inner.X; x < inner.X + inner.Width; x++)
                    {
                        buffer.SetCell(x, y, glyph, rowStyle);
                    }
                    continue;
                }

                if (HasVisibleSubmenu(item) && _submenuColumnWidth > 0)
                {
                    var arrowX = inner.X + inner.Width - Math.Max(1, TerminalTextUtility.GetRuneWidth(style.SubmenuGlyph));
                    buffer.SetCell(arrowX, y, style.SubmenuGlyph, rowStyle | TextStyle.Dim);
                }
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            var index = TryGetIndexAtPoint(e.UiX, e.UiY);
            if (HoveredIndex != index)
            {
                HoveredIndex = index;
                if (index >= 0)
                {
                    SelectedIndex = index;
                    EnsureSubmenuForSelection();
                }
            }
        }

        protected override void OnPointerPressed(PointerEventArgs e)
        {
            if (e.Button != TerminalMouseButton.Left)
            {
                return;
            }

            var index = TryGetIndexAtPoint(e.UiX, e.UiY);
            if (index < 0)
            {
                return;
            }

            if (!IsSelectable(index))
            {
                e.Handled = true;
                return;
            }

            SelectedIndex = index;
            InvokeOrOpen(SelectedIndex);
            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_items.Count == 0)
            {
                return;
            }

            switch (e.Key)
            {
                case TerminalKey.Up:
                    SelectedIndex = FindPreviousSelectableIndex(SelectedIndex - 1);
                    CloseSubmenu();
                    e.Handled = true;
                    return;

                case TerminalKey.Down:
                    SelectedIndex = FindNextSelectableIndex(SelectedIndex + 1);
                    CloseSubmenu();
                    e.Handled = true;
                    return;

                case TerminalKey.Enter:
                case TerminalKey.Space:
                    InvokeOrOpen(SelectedIndex);
                    e.Handled = true;
                    return;

                case TerminalKey.Right:
                    if (IsSelectable(SelectedIndex) && HasVisibleSubmenu(_items[SelectedIndex]))
                    {
                        OpenSubmenuForIndex(SelectedIndex);
                    }
                    else if (_parent is null)
                    {
                        _owner.OpenMenu(_owner.FindNextEnabledIndex(_owner._openIndex + 1, _target));
                    }
                    e.Handled = true;
                    return;

                case TerminalKey.Left:
                    if (_parent is null)
                    {
                        _owner.OpenMenu(_owner.FindPreviousEnabledIndex(_owner._openIndex - 1, _target));
                    }
                    else
                    {
                        CloseSelf();
                    }
                    e.Handled = true;
                    return;

                case TerminalKey.Escape:
                    _owner.CloseAllMenus();
                    e.Handled = true;
                    return;
            }
        }

        private int TryGetIndexAtPoint(int x, int y)
        {
            if (!_innerRect.Contains(x, y))
            {
                return -1;
            }

            var row = y - _innerRect.Y;
            return (uint)row < (uint)_items.Count ? row : -1;
        }

        private bool IsSelectable(int index)
        {
            if ((uint)index >= (uint)_items.Count)
            {
                return false;
            }

            var item = _items[index];
            return item.IsEnabledFor(_target);
        }

        private int FindNextSelectableIndex(int start)
        {
            for (var i = Math.Max(0, start); i < _items.Count; i++)
            {
                if (IsSelectable(i))
                {
                    return i;
                }
            }

            return Math.Max(0, _items.Count - 1);
        }

        private int FindPreviousSelectableIndex(int start)
        {
            for (var i = Math.Min(start, _items.Count - 1); i >= 0; i--)
            {
                if (IsSelectable(i))
                {
                    return i;
                }
            }

            return 0;
        }

        private void InvokeOrOpen(int index)
        {
            if (!IsSelectable(index))
            {
                return;
            }

            var item = _items[index];
            if (HasVisibleSubmenu(item))
            {
                OpenSubmenuForIndex(index);
                return;
            }

            _owner.InvokeMenuItem(item, _target);
            _owner.CloseAllMenus();
        }

        private void EnsureSubmenuForSelection()
        {
            if (!IsSelectable(SelectedIndex))
            {
                CloseSubmenu();
                return;
            }

            if (!HasVisibleSubmenu(_items[SelectedIndex]))
            {
                CloseSubmenu();
                return;
            }

            OpenSubmenuForIndex(SelectedIndex);
        }

        private bool HasVisibleSubmenu(MenuItem item)
        {
            var children = item.Items;
            for (var i = 0; i < children.Count; i++)
            {
                if (children[i].IsVisibleFor(_target))
                {
                    return true;
                }
            }

            return false;
        }

        private void OpenSubmenuForIndex(int index)
        {
            if (!IsSelectable(index))
            {
                CloseSubmenu();
                return;
            }

            var item = _items[index];
            if (!HasVisibleSubmenu(item))
            {
                CloseSubmenu();
                return;
            }

            if (_submenuPopup is not null && ReferenceEquals(_submenuPopup.Anchor, _rows[index]))
            {
                return;
            }

            CloseSubmenu();

            var visibleItems = FilterVisibleItems(item.Items, _target);
            if (visibleItems.Count == 0)
            {
                CloseSubmenu();
                return;
            }

            var list = new MenuList(_owner, visibleItems, parent: this, target: _target);
            var menuListStyle = GetStyle<MenuListStyle>();
            var popupContent = menuListStyle.PopupTemplateFactory?.Invoke(list) ?? list;

            var popup = new Popup
            {
                Anchor = _rows[index],
                Content = popupContent,
                MatchAnchorWidth = false,
                Placement = PopupPlacement.Right,
            }.Style(PopupStyle.Default with { Padding = Thickness.Zero });

            _owner.RegisterPopup(popup);

            popup.Closed((_, _) =>
            {
                list.ReleaseVisuals();
                _submenuPopup = null;
                _owner.UnregisterPopup(popup);
            });

            _submenuPopup = popup;
            popup.Show();
        }

        private void CloseSubmenu()
        {
            if (_submenuPopup is null)
            {
                return;
            }

            _submenuPopup.Close();
            _submenuPopup = null;
        }

        private void CloseSelf()
        {
            var popup = FindPopupAncestor();
            if (popup is not null)
            {
                popup.Close();
                _owner.App?.Focus(_parent);
            }
        }

        private Popup? FindPopupAncestor()
        {
            for (var parent = Parent; parent is not null; parent = parent.Parent)
            {
                if (parent is Popup popup)
                {
                    return popup;
                }
            }

            return null;
        }

        public void ReleaseVisuals()
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                _rows[i].ReleaseVisuals();
            }
        }
    }

    private sealed class MenuListRow : Visual
    {
        private readonly MenuItem _item;
        private readonly Visual? _derivedShortcut;

        private Rectangle _iconRect;
        private Rectangle _headerRect;
        private Rectangle _shortcutRect;

        public MenuListRow(MenuItem item)
        {
            _item = item;
            if (item.IsSeparator)
            {
                return;
            }

            AttachChild(item.Header);
            if (item.Icon is not null)
            {
                AttachChild(item.Icon);
            }

            _derivedShortcut = CreateDerivedShortcut(item);
            var shortcut = item.Shortcut ?? _derivedShortcut;
            if (shortcut is not null)
            {
                AttachChild(shortcut);
            }
        }

        private static Visual? CreateDerivedShortcut(MenuItem item)
        {
            if (item.Shortcut is not null)
            {
                return null;
            }

            var cmd = item.Command;
            if (cmd is null)
            {
                return null;
            }

            if (cmd.Sequence is { } seq)
            {
                return new TextBlock(seq.ToString());
            }

            if (cmd.Gesture is { } g)
            {
                return new TextBlock(g.ToString());
            }

            return null;
        }

        private Visual? ShortcutVisual => _item.Shortcut ?? _derivedShortcut;

        protected override int ChildrenCount
            => _item.IsSeparator ? 0 : 1 + (_item.Icon is null ? 0 : 1) + (ShortcutVisual is null ? 0 : 1);

        protected override Visual GetChild(int index)
        {
            if (_item.IsSeparator)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var i = index;
            if (_item.Icon is not null)
            {
                if (i == 0) return _item.Icon;
                i--;
            }

            if (i == 0) return _item.Header;
            i--;

            if (ShortcutVisual is not null)
            {
                if (i == 0) return ShortcutVisual;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            if (_item.IsSeparator)
            {
                return SizeHints.Fixed(constraints.Clamp(new Size(1, 1)));
            }

            var style = GetStyle<MenuListStyle>();
            var iconGap = Math.Max(0, style.SpaceBetweenIconAndText);
            var shortcutGap = Math.Max(0, style.SpaceBetweenTextAndShortcut);

            var iconW = 0;
            if (_item.Icon is not null)
            {
                _item.Icon.Measure(new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1));
                iconW = _item.Icon.DesiredSize.Width;
            }

            _item.Header.Measure(new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1));
            var headerW = _item.Header.DesiredSize.Width;

            var shortcutW = 0;
            var shortcut = ShortcutVisual;
            if (shortcut is not null)
            {
                shortcut.Measure(new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1));
                shortcutW = shortcut.DesiredSize.Width;
            }

            var width = iconW;
            if (iconW > 0)
            {
                width += iconGap;
            }
            width += headerW;
            if (shortcutW > 0)
            {
                width += shortcutGap + shortcutW;
            }

            return SizeHints.Fixed(constraints.Clamp(new Size(width, 1)));
        }

        protected override void ArrangeCore(in Rectangle finalRect)
        {
            _iconRect = default;
            _headerRect = finalRect;
            _shortcutRect = default;

            if (_item.IsSeparator)
            {
                return;
            }

            var style = GetStyle<MenuListStyle>();
            var iconGap = Math.Max(0, style.SpaceBetweenIconAndText);
            var shortcutGap = Math.Max(0, style.SpaceBetweenTextAndShortcut);

            var x = finalRect.X;
            var iconW = 0;
            if (_item.Icon is not null)
            {
                iconW = Math.Min(finalRect.Width, _item.Icon.DesiredSize.Width);
                _iconRect = new Rectangle(x, finalRect.Y, iconW, 1);
                x += iconW + iconGap;
            }

            var shortcutW = 0;
            var shortcut = ShortcutVisual;
            if (shortcut is not null)
            {
                shortcutW = Math.Min(finalRect.Width, shortcut.DesiredSize.Width);
                _shortcutRect = new Rectangle(finalRect.Right - shortcutW, finalRect.Y, shortcutW, 1);
            }

            var headerWidth = Math.Max(0, finalRect.Width - (x - finalRect.X) - (shortcutW > 0 ? shortcutW + shortcutGap : 0));
            _headerRect = new Rectangle(x, finalRect.Y, headerWidth, 1);

            _item.Icon?.Arrange(_iconRect);
            _item.Header.Arrange(_headerRect);
            shortcut?.Arrange(_shortcutRect);
        }

        public void ReleaseVisuals()
        {
            if (_item.IsSeparator)
            {
                return;
            }

            if (_item.Icon is not null && ReferenceEquals(_item.Icon.Parent, this))
            {
                DetachChild(_item.Icon);
            }

            if (ReferenceEquals(_item.Header.Parent, this))
            {
                DetachChild(_item.Header);
            }

            var shortcut = ShortcutVisual;
            if (shortcut is not null && ReferenceEquals(shortcut.Parent, this))
            {
                DetachChild(shortcut);
            }
        }
    }
}
