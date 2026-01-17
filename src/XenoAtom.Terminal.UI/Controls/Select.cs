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
/// Represents an item in a <see cref="Select"/> control.
/// </summary>
/// <param name="Value">The value associated with the item.</param>
/// <param name="ContentFactory">A factory creating the visual used to render the item.</param>
public sealed record SelectItem(object? Value, Func<Visual> ContentFactory)
{
    /// <summary>
    /// Initializes a new item using a text label.
    /// </summary>
    /// <param name="text">The item text.</param>
    public SelectItem(string text)
        : this(text, () => new TextBlock { Text = text })
    {
    }

    /// <summary>
    /// Creates the visual used to render this item.
    /// </summary>
    public Visual CreateVisual() => ContentFactory();
}

/// <summary>
/// A dropdown/select control that displays a popup list to pick a single item.
/// </summary>
public sealed partial class Select : ContentVisual
{
    private Popup? _popup;
    private ListBox? _popupList;
    private int _contentIndex = -1;
    private SelectItem? _contentItem;

    /// <summary>
    /// Initializes a new instance of the <see cref="Select"/> class.
    /// </summary>
    public Select()
    {
        Focusable = true;
        Items = new BindableList<SelectItem>(this, "Select.Items");
        this.SelectedIndex(0);
    }

    /// <summary>
    /// Gets the items available for selection.
    /// </summary>
    public BindableList<SelectItem> Items { get; }

    /// <summary>
    /// Gets or sets the selected item index.
    /// </summary>
    [Bindable]
    public partial int SelectedIndex { get; set; }

    partial void OnSelectedIndexChanged(int value)
    {
        _ = value;
        UpdateSelectedContent();
    }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        // Rebuild selected content before measuring when needed.
        UpdateSelectedContent();

        var style = Get<SelectStyle>();
        var padding = style.Padding;
        var arrowWidth = TerminalTextUtility.GetWidth(style.ArrowGlyph.ToString().AsSpan());

        var innerMaxW = constraints.MaxWidth == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxWidth - padding.Horizontal);

        var contentMaxW = innerMaxW == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, innerMaxW - arrowWidth);

        var content = Content;
        var contentHints = content is null
            ? SizeHints.Fixed(Size.Zero)
            : content.Measure(new LayoutConstraints(0, contentMaxW, 0, constraints.MaxHeight));

        var addW = padding.Horizontal + arrowWidth;
        var addH = padding.Vertical;

        var minW = LayoutConstants.ClampFinite(contentHints.Min.Width + addW);
        var minH = LayoutConstants.ClampFinite(Math.Max(1, contentHints.Min.Height + addH));

        var natW = LayoutConstants.ClampFinite(contentHints.Natural.Width + addW);
        var natH = LayoutConstants.ClampFinite(Math.Max(1, contentHints.Natural.Height + addH));

        minW = Math.Max(3, minW);
        natW = Math.Max(3, natW);

        var maxW = LayoutConstants.IsInfinite(contentHints.Max.Width)
            ? LayoutConstants.Infinite
            : LayoutConstants.ClampOrInfinite(contentHints.Max.Width + addW);

        var maxH = LayoutConstants.IsInfinite(contentHints.Max.Height)
            ? LayoutConstants.Infinite
            : LayoutConstants.ClampOrInfinite(contentHints.Max.Height + addH);

        return SizeHints.Flex(
            new Size(minW, minH),
            new Size(natW, natH),
            new Size(maxW, maxH),
            contentHints.FlexGrowX,
            contentHints.FlexGrowY,
            contentHints.FlexShrinkX,
            contentHints.FlexShrinkY).Normalize();
    }

    /// <inheritdoc/>
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var style = Get<SelectStyle>();
        var padding = style.Padding;
        var arrowWidth = TerminalTextUtility.GetWidth(style.ArrowGlyph.ToString().AsSpan());

        var inner = new Rectangle(
            finalRect.X + padding.Left,
            finalRect.Y + padding.Top,
            Math.Max(0, finalRect.Width - padding.Horizontal),
            Math.Max(0, finalRect.Height - padding.Vertical));

        var content = Content;
        if (content is not null)
        {
            var contentRect = new Rectangle(
                inner.X,
                inner.Y,
                Math.Max(0, inner.Width - arrowWidth),
                inner.Height);

            content.Arrange(contentRect);
        }
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
        var style = Get<SelectStyle>();
        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var resolved = style.ResolveStyle(theme, IsEnabled, isFocused, IsHovered);

        // Clear background.
        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), resolved);
            }
        }

        // Render arrow on the right.
        var arrowText = style.ArrowGlyph.ToString();
        var arrowCells = TerminalTextUtility.GetWidth(arrowText.AsSpan());
        var arrowX = rect.X + Math.Max(0, rect.Width - arrowCells - 1);
        buffer.WriteText(arrowX, rect.Y, arrowText.AsSpan(), resolved | TextStyle.Dim);
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        OpenPopup();
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is TerminalKey.Enter or TerminalKey.Space)
        {
            OpenPopup();
            e.Handled = true;
            return;
        }

        // Optional quick selection when closed.
        if (Items.Count > 0 && e.Key is TerminalKey.Up or TerminalKey.Down)
        {
            var selected = Math.Clamp(SelectedIndex, 0, Items.Count - 1);
            SelectedIndex = e.Key == TerminalKey.Up ? Math.Max(0, selected - 1) : Math.Min(Items.Count - 1, selected + 1);
            e.Handled = true;
        }
    }

    private void UpdateSelectedContent()
    {
        var items = Items;
        if (items.Count == 0)
        {
            if (Content is not null)
            {
                Content = null;
            }
            _contentIndex = -1;
            _contentItem = null;
            return;
        }

        var index = Math.Clamp(SelectedIndex, 0, items.Count - 1);
        if (index != SelectedIndex)
        {
            SelectedIndex = index;
            return;
        }

        var item = items[index];
        if (_contentIndex == index && ReferenceEquals(_contentItem, item) && Content is not null)
        {
            return;
        }

        _contentIndex = index;
        _contentItem = item;
        Content = item.CreateVisual();
    }

    private void OpenPopup()
    {
        VerifyAccess();

        if (_popup is not null)
        {
            return;
        }

        var app = App ?? Dispatcher.AttachedApp;
        if (app is null)
        {
            return;
        }

        var list = new ListBox();
        for (var i = 0; i < Items.Count; i++)
        {
            list.Items.Add(Items[i].CreateVisual());
        }
        list.SelectedIndex = Math.Clamp(SelectedIndex, 0, Math.Max(0, list.Items.Count - 1));

        list.PointerPressed(static (s, e) =>
        {
            if (e.Button != TerminalMouseButton.Left)
            {
                return;
            }

            if (s is not ListBox lb || lb.Parent is not Popup popup || popup.Anchor is not Select owner)
            {
                return;
            }

            owner.SelectedIndex = lb.SelectedIndex;
            owner.ClosePopup();
        });

        list.KeyDown(static (s, e) =>
        {
            if (e.Key is not (TerminalKey.Enter or TerminalKey.Space))
            {
                return;
            }

            if (s is not ListBox lb || lb.Parent is not Popup popup || popup.Anchor is not Select owner)
            {
                return;
            }

            owner.SelectedIndex = lb.SelectedIndex;
            owner.ClosePopup();
            e.Handled = true;
        });

        var popup = new Popup
        {
            Anchor = this,
            Content = list,
            MatchAnchorWidth = true,
            AdditionalWidth = 2,
            Placement = PopupPlacement.Below,
        };

        popup.Closed(static (sender, _) =>
        {
            if (sender is not Popup p || p.Anchor is not Select owner)
            {
                return;
            }

            owner._popup = null;
            owner._popupList = null;

            owner.App?.Focus(owner);
        });

        popup.Show();

        _popup = popup;
        _popupList = list;
    }

    private void ClosePopup()
    {
        if (_popup is null)
        {
            return;
        }

        _popup.Close();
    }
}
