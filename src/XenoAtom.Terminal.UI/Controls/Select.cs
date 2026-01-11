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

public sealed record SelectItem(object? Value, Func<Visual> ContentFactory)
{
    public SelectItem(string text)
        : this(text, () => new TextBlock { Text = text })
    {
    }

    public Visual CreateVisual() => ContentFactory();
}

public sealed partial class Select : ContentVisual
{
    private Popup? _popup;
    private ListBox? _popupList;
    private int _contentIndex = -1;
    private SelectItem? _contentItem;

    public Select()
    {
        Focusable = true;
        Items = new BindableList<SelectItem>(this, "Select.Items");
        this.SelectedIndex(0);
    }

    public BindableList<SelectItem> Items { get; }

    [Bindable]
    public partial int SelectedIndex { get; set; }

    partial void OnSelectedIndexChanged(int value)
    {
        _ = value;
        UpdateSelectedContent();
    }

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

        int addW, addH;
        try
        {
            checked
            {
                addW = padding.Horizontal + arrowWidth;
                addH = padding.Vertical;
            }
        }
        catch (OverflowException ex)
        {
            throw new LayoutException("Overflow while computing Select padding/arrow contribution.", ex);
        }

        int minW, minH, natW, natH;
        try
        {
            checked
            {
                minW = LayoutConstants.ClampFinite(contentHints.Min.Width + addW);
                minH = LayoutConstants.ClampFinite(Math.Max(1, contentHints.Min.Height + addH));

                natW = LayoutConstants.ClampFinite(contentHints.Natural.Width + addW);
                natH = LayoutConstants.ClampFinite(Math.Max(1, contentHints.Natural.Height + addH));
            }
        }
        catch (OverflowException ex)
        {
            throw new LayoutException("Overflow while computing Select Min/Natural size.", ex);
        }

        minW = Math.Max(3, minW);
        natW = Math.Max(3, natW);

        int maxW, maxH;
        if (LayoutConstants.IsInfinite(contentHints.Max.Width))
        {
            maxW = LayoutConstants.Infinite;
        }
        else
        {
            try
            {
                maxW = LayoutConstants.ClampOrInfinite(checked(contentHints.Max.Width + addW));
            }
            catch (OverflowException ex)
            {
                throw new LayoutException("Overflow while computing Select Max.Width.", ex);
            }
        }

        if (LayoutConstants.IsInfinite(contentHints.Max.Height))
        {
            maxH = LayoutConstants.Infinite;
        }
        else
        {
            try
            {
                maxH = LayoutConstants.ClampOrInfinite(checked(contentHints.Max.Height + addH));
            }
            catch (OverflowException ex)
            {
                throw new LayoutException("Overflow while computing Select Max.Height.", ex);
            }
        }

        return SizeHints.Flex(
            new Size(minW, minH),
            new Size(natW, natH),
            new Size(maxW, maxH),
            contentHints.FlexGrowX,
            contentHints.FlexGrowY,
            contentHints.FlexShrinkX,
            contentHints.FlexShrinkY).Normalize();
    }

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

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        OpenPopup();
        e.Handled = true;
    }

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
