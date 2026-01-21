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
using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a list box that displays a vertical list of items with a single selection.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed partial class ListBox<T> : Visual
{
    private int _scrollOffset;
    private readonly BindableList<Visual> _itemVisuals;
    private readonly List<Visual> _recyclePool = new();
    private readonly List<State<T>> _itemStates = new();
    private readonly List<State<T>> _recycleStatePool = new();
    private int _lastItemsVersion = -1;
    private DataTemplate<T> _lastResolvedTemplate;

    /// <summary>
    /// Gets the collection of items displayed by the list box.
    /// </summary>
    [Bindable]
    public BindableList<T> Items { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListBox{T}"/> class.
    /// </summary>
    public ListBox()
    {
        Items = new BindableList<T>(this, "ListBox.Items");
        _itemVisuals = new BindableList<Visual>(
            this,
            "ListBox.ItemVisuals",
            onAdding: AttachCollectionChild,
            onRemoving: v =>
            {
                DetachCollectionChild(v);
                _recyclePool.Add(v);
            });
        Focusable = true;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    /// <summary>
    /// Gets or sets the selected item index.
    /// </summary>
    [Bindable]
    public partial int SelectedIndex { get; set; }

    /// <inheritdoc />
    protected override int ChildrenCount => _itemVisuals.Count;

    /// <inheritdoc />
    protected override Visual GetChild(int index) => _itemVisuals[index];

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        EnsureItemVisuals();
        var items = _itemVisuals;
        var itemWidth = 0;
        if (items.Count > 0)
        {
            var itemConstraints = new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                item.Measure(itemConstraints);
                itemWidth = Math.Max(itemWidth, item.DesiredSize.Width);
            }
        }

        // Marker + space.
        var width = itemWidth + 2;
        var desiredHeight = Math.Max(1, items.Count);

        var min = new Size(2, 1);
        var natural = new Size(Math.Max(min.Width, width), Math.Max(min.Height, desiredHeight));
        var max = new Size(LayoutConstants.Infinite, LayoutConstants.Infinite);
        return SizeHints.Flex(min, natural, max, growX: 1, growY: 1, shrinkX: 1, shrinkY: 1);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var rect = finalRect;
        EnsureItemVisuals();
        var items = _itemVisuals;
        if (rect.Width <= 0 || rect.Height <= 0 || items.Count == 0)
        {
            return;
        }

        var listBoxStyle = Get<ListBoxStyle>();
        var innerLeft = rect.X;
        var innerTop = rect.Y;
        var innerWidth = Math.Max(0, rect.Width);
        var innerHeight = Math.Max(0, rect.Height);

        var count = items.Count;
        var selected = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));

        if (selected < _scrollOffset)
        {
            _scrollOffset = selected;
        }
        else if (selected >= _scrollOffset + Math.Max(1, innerHeight))
        {
            _scrollOffset = Math.Max(0, selected - Math.Max(1, innerHeight) + 1);
        }

        var itemLeft = innerLeft + 2;
        var itemWidth = Math.Max(0, innerWidth - 2);
        for (var i = 0; i < count; i++)
        {
            var y = innerTop + (i - _scrollOffset);
            items[i].Arrange(new Rectangle(itemLeft, y, itemWidth, 1));
        }
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        var items = _itemVisuals;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var listBoxStyle = Get<ListBoxStyle>();
        var innerLeft = rect.X;
        var innerTop = rect.Y;
        var innerWidth = Math.Max(0, rect.Width);
        var innerHeight = Math.Max(0, rect.Height);

        var count = items.Count;
        var selected = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));

        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();

        // Fill background.
        var background = Style.None;
        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), background);
            }
        }

        for (var row = 0; row < innerHeight; row++)
        {
            var itemIndex = _scrollOffset + row;
            var y = innerTop + row;

            if ((uint)itemIndex >= (uint)count)
            {
                continue;
            }

            var isSelected = itemIndex == selected;
            var style = listBoxStyle.ResolveItemStyle(theme, IsEnabled, isSelected, isFocused);

            if (innerWidth <= 0)
            {
                continue;
            }

            // Fill row background/style so that child visuals using CellStyle.None inherit foreground/background.
            for (var x = 0; x < innerWidth; x++)
            {
                buffer.SetCell(innerLeft + x, y, new Rune(' '), style);
            }

            if (innerWidth >= 2)
            {
                buffer.SetCell(innerLeft, y, isSelected ? listBoxStyle.MarkerGlyph : new Rune(' '), style);
                buffer.SetCell(innerLeft + 1, y, new Rune(' '), style);
            }
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        var count = Items.Count;
        if (count == 0)
        {
            return;
        }

        var viewportHeight = Math.Max(1, Bounds.Height);
        var selected = Math.Clamp(SelectedIndex, 0, count - 1);
        switch (e.Key)
        {
            case TerminalKey.Up:
                SelectedIndex = Math.Max(0, selected - 1);
                e.Handled = true;
                return;
            case TerminalKey.Down:
                SelectedIndex = Math.Min(count - 1, selected + 1);
                e.Handled = true;
                return;
            case TerminalKey.Home:
                SelectedIndex = 0;
                e.Handled = true;
                return;
            case TerminalKey.End:
                SelectedIndex = count - 1;
                e.Handled = true;
                return;
            case TerminalKey.PageUp:
                SelectedIndex = Math.Max(0, selected - viewportHeight);
                e.Handled = true;
                return;
            case TerminalKey.PageDown:
                SelectedIndex = Math.Min(count - 1, selected + viewportHeight);
                e.Handled = true;
                return;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var count = Items.Count;
        if (count == 0)
        {
            return;
        }

        var innerY = e.UiY - Bounds.Y;
        var innerHeight = Math.Max(0, Bounds.Height);
        if ((uint)innerY >= (uint)innerHeight)
        {
            return;
        }

        var index = _scrollOffset + innerY;
        if ((uint)index < (uint)count)
        {
            SelectedIndex = index;
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerWheel(PointerEventArgs e)
    {
        var count = Items.Count;
        if (count == 0 || e.WheelDelta == 0)
        {
            return;
        }

        var selected = Math.Clamp(SelectedIndex, 0, count - 1);
        SelectedIndex = e.WheelDelta > 0 ? Math.Max(0, selected - 1) : Math.Min(count - 1, selected + 1);
        e.Handled = true;
    }

    private void EnsureItemVisuals()
    {
        var items = Items;
        var template = ResolveItemTemplate();

        if (items.Version == _lastItemsVersion && template.Equals(_lastResolvedTemplate))
        {
            return;
        }

        _lastItemsVersion = items.Version;
        _lastResolvedTemplate = template;

        if (_itemStates.Count != 0)
        {
            _recycleStatePool.AddRange(_itemStates);
            _itemStates.Clear();
        }

        _itemVisuals.Clear();

        if (items.Count == 0)
        {
            _recycleStatePool.Clear();
            _recyclePool.Clear();
            return;
        }

        var ctxBase = new DataTemplateContext(this, DataTemplateRole.Display, -1, DataTemplateItemState.None);

        for (var i = 0; i < items.Count; i++)
        {
            var value = items[i];
            var state = _recycleStatePool.Count != 0
                ? PopLastState()
                : new State<T>(default!);
            state.Value = value;
            _itemStates.Add(state);

            if (value is Visual asVisual)
            {
                _itemVisuals.Add(asVisual);
                continue;
            }

            var binding = (Binding<T>)state;

            if (template.IsEmpty || template.Create is null)
            {
                _itemVisuals.Add(new TextBlock(() => (binding.GetValue() as object)?.ToString() ?? string.Empty));
                continue;
            }

            Visual? reused = null;
            if (_recyclePool.Count != 0)
            {
                var last = _recyclePool.Count - 1;
                reused = _recyclePool[last];
                _recyclePool.RemoveAt(last);
            }

            var ctx = ctxBase with { Index = i };
            if (reused is not null && template.TryUpdate is { } updater && updater(reused, binding, ctx))
            {
                _itemVisuals.Add(reused);
                continue;
            }

            if (reused is not null && template.Release is { } release)
            {
                release(reused);
            }

            _itemVisuals.Add(template.Create(binding, ctx));
        }

        _recyclePool.Clear();
        _recycleStatePool.Clear();
    }

    private State<T> PopLastState()
    {
        var last = _recycleStatePool.Count - 1;
        var state = _recycleStatePool[last];
        _recycleStatePool.RemoveAt(last);
        return state;
    }

    private DataTemplate<T> ResolveItemTemplate()
    {
        var template = ItemTemplate;
        if (!template.IsEmpty)
        {
            return template;
        }

        var templates = Get<DataTemplates>();
        if (templates.TryResolve(DataTemplateRole.Display, out template))
        {
            return template;
        }

        return default;
    }

    /// <summary>
    /// Gets or sets the template used to create visuals for items.
    /// </summary>
    [Bindable]
    public partial DataTemplate<T> ItemTemplate { get; set; }
}
