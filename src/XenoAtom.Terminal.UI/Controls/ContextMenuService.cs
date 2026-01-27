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
using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Provides helpers for showing context menus in fullscreen apps.
/// </summary>
/// <remarks>
/// Context menus are hosted as <see cref="Popup"/> windows (fullscreen only) and are typically opened via a right-click.
/// </remarks>
public static partial class ContextMenuService
{
    /// <summary>
    /// Shows a context menu for the specified <paramref name="target"/> at the given UI coordinate.
    /// </summary>
    /// <param name="target">The visual associated with the context menu request.</param>
    /// <param name="items">The menu items to display.</param>
    /// <param name="uiX">The UI X coordinate where the menu should open.</param>
    /// <param name="uiY">The UI Y coordinate where the menu should open.</param>
    /// <returns>The popup hosting the context menu.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when called while no fullscreen <see cref="TerminalApp"/> is running.</exception>
    public static Popup Show(Visual target, IEnumerable<MenuItem> items, int uiX, int uiY)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(items);

        var app = target.App ?? Dispatcher.Current.AttachedApp;
        if (app is null)
        {
            throw new InvalidOperationException("Context menus are only supported while a TerminalApp is running.");
        }

        return app.ShowContextMenu(target, items, uiX, uiY);
    }

    internal static Popup CreatePopup(Visual target, IReadOnlyList<MenuItem> items, int uiX, int uiY)
    {
        var listStyle = target.GetStyle<MenuListStyle>();

        var popup = new Popup
        {
            AnchorRect = new Rectangle(uiX, uiY, 1, 1),
            MatchAnchorWidth = false,
            Placement = PopupPlacement.Below,
            CloseOnTab = true,
        }.Style(PopupStyle.Default with { Padding = Thickness.Zero });

        var list = new ContextMenuList(popup, items, target);
        var content = listStyle.PopupTemplateFactory?.Invoke(list) ?? list;
        popup.Content = content;

        popup.Closed((_, _) => list.OnHostClosed());
        return popup;
    }

    private sealed partial class ContextMenuList : Visual
    {
        private readonly Popup _rootPopup;
        private readonly ContextMenuList _root;
        private readonly ContextMenuList? _parent;
        private readonly IReadOnlyList<MenuItem> _items;
        private readonly Visual _target;

        private readonly VisualList<MenuListRow> _rows;
        private readonly List<Popup> _openPopups;

        private Popup? _submenuPopup;

        [Bindable]
        private partial int SelectedIndex { get; set; }

        [Bindable]
        private partial int HoveredIndex { get; set; }

        private Rectangle _innerRect;
        private int _submenuColumnWidth;

        public ContextMenuList(Popup rootPopup, IReadOnlyList<MenuItem> items, Visual target)
            : this(rootPopup, items, target, parent: null, root: null)
        {
        }

        private ContextMenuList(Popup rootPopup, IReadOnlyList<MenuItem> items, Visual target, ContextMenuList? parent, ContextMenuList? root)
        {
            _rootPopup = rootPopup;
            _items = items;
            _target = target;
            _parent = parent;
            _root = root ?? this;
            _openPopups = ReferenceEquals(_root, this) ? new List<Popup>() : _root._openPopups;

            Focusable = true;
            _rows = new VisualList<MenuListRow>(this, "ContextMenuList.Rows");

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
            InvokeOrOpen(index);
            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
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
                        e.Handled = true;
                    }
                    return;

                case TerminalKey.Left:
                    if (_parent is null)
                    {
                        _rootPopup.Close();
                    }
                    else
                    {
                        CloseSelf();
                    }
                    e.Handled = true;
                    return;

                case TerminalKey.Escape:
                    _rootPopup.Close();
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

            InvokeItem(item);
            _rootPopup.Close();
        }

        private void InvokeItem(MenuItem item)
        {
            if (!item.IsEnabledFor(_target))
            {
                return;
            }

            if (item.Command is { } cmd)
            {
                var effectiveTarget = item.CommandTarget ?? _target;
                if (!cmd.IsVisibleFor(effectiveTarget) || !cmd.CanExecuteFor(effectiveTarget))
                {
                    return;
                }

                cmd.Execute(effectiveTarget);
                return;
            }

            item.Action.Invoke?.Invoke();
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

            var listStyle = GetStyle<MenuListStyle>();
            var list = new ContextMenuList(_rootPopup, items: visibleItems, target: _target, parent: this, root: _root);
            var popupContent = listStyle.PopupTemplateFactory?.Invoke(list) ?? list;

            var popup = new Popup
            {
                Anchor = _rows[index],
                Content = popupContent,
                MatchAnchorWidth = false,
                Placement = PopupPlacement.Right,
            }.Style(PopupStyle.Default with { Padding = Thickness.Zero });

            _root.RegisterPopup(popup);

            popup.Closed((_, _) =>
            {
                list.ReleaseVisuals();
                _submenuPopup = null;
                _root.UnregisterPopup(popup);
            });

            _submenuPopup = popup;
            popup.Show();
        }

        private void RegisterPopup(Popup popup)
        {
            if (!ReferenceEquals(_root, this))
            {
                _root.RegisterPopup(popup);
                return;
            }

            _openPopups.Add(popup);
        }

        private void UnregisterPopup(Popup popup)
        {
            if (!ReferenceEquals(_root, this))
            {
                _root.UnregisterPopup(popup);
                return;
            }

            _openPopups.Remove(popup);
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
                _rootPopup.App?.Focus(_parent);
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

        public void OnHostClosed()
        {
            if (!ReferenceEquals(_root, this))
            {
                return;
            }

            // Ensure submenus do not remain visible after closing the root popup.
            var copy = _openPopups.ToArray();
            for (var i = copy.Length - 1; i >= 0; i--)
            {
                copy[i].Close();
            }

            _openPopups.Clear();
            ReleaseVisuals();
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
