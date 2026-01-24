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

        switch (e.Key)
        {
            case TerminalKey.Left:
                _selectedIndex = FindPreviousEnabledIndex(_selectedIndex - 1);
                e.Handled = true;
                return;

            case TerminalKey.Right:
                _selectedIndex = FindNextEnabledIndex(_selectedIndex + 1);
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

        index = Math.Clamp(index, 0, _items.Count - 1);
        if (!_items[index].IsEnabled)
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
            menu.Action.Invoke?.Invoke();
            _openIndex = -1;
            return;
        }

        _openIndex = index;

        var list = new MenuList(this, menu.Items, parent: null);
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

    private int FindNextEnabledIndex(int start)
    {
        for (var i = Math.Max(0, start); i < _items.Count; i++)
        {
            if (_items[i].IsEnabled)
            {
                return i;
            }
        }

        return Math.Max(0, _items.Count - 1);
    }

    private int FindPreviousEnabledIndex(int start)
    {
        for (var i = Math.Min(start, _items.Count - 1); i >= 0; i--)
        {
            if (_items[i].IsEnabled)
            {
                return i;
            }
        }

        return 0;
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
            var resolved = style.ResolveItemStyle(theme, enabled: _item.IsEnabled, open: open, selected: selected, hovered: IsHovered);

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

    private sealed class MenuList : Visual
    {
        private readonly MenuBar _owner;
        private readonly IReadOnlyList<MenuItem> _items;
        private readonly VisualList<MenuListRow> _rows;
        private readonly MenuList? _parent;

        private Popup? _submenuPopup;
        private int _selected;
        private int _hovered = -1;

        private Rectangle _innerRect;
        private int _submenuColumnWidth;

        public MenuList(MenuBar owner, IReadOnlyList<MenuItem> items, MenuList? parent)
        {
            _owner = owner;
            _items = items;
            _parent = parent;
            Focusable = true;
            _rows = new VisualList<MenuListRow>(this, "MenuList.Rows");

            for (var i = 0; i < items.Count; i++)
            {
                _rows.Add(new MenuListRow(items[i]));
            }

            _selected = FindNextSelectableIndex(0);
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

                if (_items[i].Items.Count > 0)
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
                var enabled = item.IsEnabled && !item.IsSeparator;
                var selected = i == _selected;
                var hovered = i == _hovered;

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

                if (item.Items.Count > 0 && _submenuColumnWidth > 0)
                {
                    var arrowX = inner.X + inner.Width - Math.Max(1, TerminalTextUtility.GetRuneWidth(style.SubmenuGlyph));
                    buffer.SetCell(arrowX, y, style.SubmenuGlyph, rowStyle | TextStyle.Dim);
                }
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            var index = TryGetIndexAtPoint(e.UiX, e.UiY);
            if (_hovered != index)
            {
                _hovered = index;
                if (index >= 0)
                {
                    _selected = index;
                    EnsureSubmenuForSelection();
                }
                Invalidate();
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

            _selected = index;
            InvokeOrOpen(_selected);
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
                    _selected = FindPreviousSelectableIndex(_selected - 1);
                    CloseSubmenu();
                    e.Handled = true;
                    return;

                case TerminalKey.Down:
                    _selected = FindNextSelectableIndex(_selected + 1);
                    CloseSubmenu();
                    e.Handled = true;
                    return;

                case TerminalKey.Enter:
                case TerminalKey.Space:
                    InvokeOrOpen(_selected);
                    e.Handled = true;
                    return;

                case TerminalKey.Right:
                    if (IsSelectable(_selected) && _items[_selected].Items.Count > 0)
                    {
                        OpenSubmenuForIndex(_selected);
                    }
                    else if (_parent is null)
                    {
                        _owner.OpenMenu(_owner.FindNextEnabledIndex(_owner._openIndex + 1));
                    }
                    e.Handled = true;
                    return;

                case TerminalKey.Left:
                    if (_parent is null)
                    {
                        _owner.OpenMenu(_owner.FindPreviousEnabledIndex(_owner._openIndex - 1));
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
            return !item.IsSeparator && item.IsEnabled;
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
            if (item.Items.Count > 0)
            {
                OpenSubmenuForIndex(index);
                return;
            }

            item.Action.Invoke?.Invoke();
            _owner.CloseAllMenus();
        }

        private void EnsureSubmenuForSelection()
        {
            if (!IsSelectable(_selected))
            {
                CloseSubmenu();
                return;
            }

            if (_items[_selected].Items.Count == 0)
            {
                CloseSubmenu();
                return;
            }

            OpenSubmenuForIndex(_selected);
        }

        private void OpenSubmenuForIndex(int index)
        {
            if (!IsSelectable(index))
            {
                CloseSubmenu();
                return;
            }

            var item = _items[index];
            if (item.Items.Count == 0)
            {
                CloseSubmenu();
                return;
            }

            if (_submenuPopup is not null && ReferenceEquals(_submenuPopup.Anchor, _rows[index]))
            {
                return;
            }

            CloseSubmenu();

            var list = new MenuList(_owner, item.Items, parent: this);
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
            if (item.Shortcut is not null)
            {
                AttachChild(item.Shortcut);
            }
        }

        protected override int ChildrenCount
            => _item.IsSeparator ? 0 : 1 + (_item.Icon is null ? 0 : 1) + (_item.Shortcut is null ? 0 : 1);

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

            if (_item.Shortcut is not null)
            {
                if (i == 0) return _item.Shortcut;
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
            if (_item.Shortcut is not null)
            {
                _item.Shortcut.Measure(new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1));
                shortcutW = _item.Shortcut.DesiredSize.Width;
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
            if (_item.Shortcut is not null)
            {
                shortcutW = Math.Min(finalRect.Width, _item.Shortcut.DesiredSize.Width);
                _shortcutRect = new Rectangle(finalRect.Right - shortcutW, finalRect.Y, shortcutW, 1);
            }

            var headerWidth = Math.Max(0, finalRect.Width - (x - finalRect.X) - (shortcutW > 0 ? shortcutW + shortcutGap : 0));
            _headerRect = new Rectangle(x, finalRect.Y, headerWidth, 1);

            _item.Icon?.Arrange(_iconRect);
            _item.Header.Arrange(_headerRect);
            _item.Shortcut?.Arrange(_shortcutRect);
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

            if (_item.Shortcut is not null && ReferenceEquals(_item.Shortcut.Parent, this))
            {
                DetachChild(_item.Shortcut);
            }
        }
    }
}
