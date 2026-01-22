// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
/// A generic dropdown/select control that displays a popup list to pick a single item.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed partial class Select<T> : ContentVisual
{
    private Popup? _popup;
    private int _contentIndex = -1;
    private DataTemplate<T> _lastResolvedTemplate;
    private readonly State<T> _selectedState;

    /// <summary>
    /// Initializes a new instance of the <see cref="Select{T}"/> class.
    /// </summary>
    public Select()
    {
        Focusable = true;
        Items = new BindableList<T>(this, "Select.Items");
        this.SelectedIndex(0);
        _selectedState = new State<T>(default!);
    }

    /// <summary>
    /// Gets the items available for selection.
    /// </summary>
    [Bindable]
    public BindableList<T> Items { get; }

    /// <summary>
    /// Gets or sets the selected item index.
    /// </summary>
    [Bindable]
    public partial int SelectedIndex { get; set; }

    /// <summary>
    /// Gets or sets the template used to create visuals for items, both for the selected value and the popup list.
    /// </summary>
    /// <remarks>
    /// When this template is empty, the control resolves a display template from <see cref="DataTemplates"/> in the environment.
    /// If no template can be resolved, the control falls back to rendering <c>value.ToString()</c> inside a <see cref="TextBlock"/>.
    /// </remarks>
    [Bindable]
    public partial DataTemplate<T> ItemTemplate { get; set; }

    partial void OnSelectedIndexChanged(int value)
    {
        _ = value;
        UpdateSelectedContent();
    }

    partial void OnItemTemplateChanged(DataTemplate<T> value)
    {
        _ = value;
        UpdateSelectedContent(forceRebuild: true);
    }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        // Rebuild selected content before measuring when needed.
        UpdateSelectedContent();

        var style = GetStyle<SelectStyle>();
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

        var style = GetStyle<SelectStyle>();
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
        var style = GetStyle<SelectStyle>();
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

    private void UpdateSelectedContent(bool forceRebuild = false)
    {
        var items = Items;
        if (items.Count == 0)
        {
            if (Content is not null)
            {
            Content = null;
        }

        _contentIndex = -1;
        _lastResolvedTemplate = default;
        return;
    }

        var index = Math.Clamp(SelectedIndex, 0, items.Count - 1);
        if (index != SelectedIndex)
        {
            SelectedIndex = index;
            return;
        }

        var value = items[index];
        var template = ResolveItemTemplate();

        _contentIndex = index;

        if (value is Visual asVisual)
        {
            Content = asVisual;
            _lastResolvedTemplate = template;
            return;
        }

        _selectedState.Value = value;

        if (!forceRebuild && _lastResolvedTemplate.Equals(template) && Content is not null)
        {
            return;
        }

        _lastResolvedTemplate = template;
        var binding = (Binding<T>)_selectedState;
        var ctx = new DataTemplateContext(this, DataTemplateRole.Display, index, DataTemplateItemState.None);
        Content = template.IsEmpty || template.Create is null
            ? new TextBlock(() => ToStringObject(binding.GetValue()))
            : template.Create(binding, ctx);
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

        var template = ResolveItemTemplate();
        var list = new ListBox<T>()
            .Items(Items)
            .ItemTemplate(template);
        list.SelectedIndex = Math.Clamp(SelectedIndex, 0, Math.Max(0, list.Items.Count - 1));

        var style = GetStyle<SelectStyle>();
        var content = style.PopupTemplateFactory?.Invoke(list) ?? list;

        list.PointerPressed((s, e) =>
        {
            if (e.Button != TerminalMouseButton.Left)
            {
                return;
            }

            if (!TryFindSelectPopupParent(s as Visual, out var lb, out var owner))
            {
                return;
            }

            owner.SelectedIndex = lb.SelectedIndex;
            owner.ClosePopup();
        });

        list.KeyDown((s, e) =>
        {
            if (e.Key is not (TerminalKey.Enter or TerminalKey.Space))
            {
                return;
            }

            if (!TryFindSelectPopupParent(s as Visual, out var lb, out var owner))
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
            Content = content,
            MatchAnchorWidth = true,
            AdditionalWidth = 2,
            Placement = PopupPlacement.Below,
        };

        popup.Closed(static (sender, _) =>
        {
            if (sender is not Popup p || p.Anchor is not Select<T> owner)
            {
                return;
            }

            owner._popup = null;

            owner.App?.Focus(owner);
        });

        popup.Show();

        _popup = popup;
    }

    private static bool TryFindSelectPopupParent(Visual? visual, [MaybeNullWhen(false)] out ListBox<T> lb, [MaybeNullWhen(false)] out Select<T> select)
    {
        lb = null;
        select = null;
        if (visual is not ListBox<T> lbInstance)
        {
            return false;
        }

        lb = lbInstance;
        var parent = lb.Parent;
        while (parent is not null)
        {
            if (parent is Popup popup && popup.Anchor is Select<T> selectInstance)
            {
                select = selectInstance;
                return true;
            }
            parent = parent.Parent;
        }

        return false;
    }

    private void ClosePopup()
    {
        if (_popup is null)
        {
            return;
        }

        _popup.Close();
    }

    private DataTemplate<T> ResolveItemTemplate()
    {
        var template = ItemTemplate;
        if (!template.IsEmpty)
        {
            return template;
        }

        var templates = GetStyle<DataTemplates>();
        if (templates.TryResolve(DataTemplateRole.Display, out template))
        {
            return template;
        }

        return default;
    }
}
