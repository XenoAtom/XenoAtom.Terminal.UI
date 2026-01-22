// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Command palette control: search input + filtered list of actions.
/// </summary>
public sealed partial class CommandPalette : Visual
{
    private readonly TextBox _searchBox;
    private readonly OptionList<CommandPaletteItem> _results;
    private readonly VStack _content;

    private Popup? _hostPopup;
    private int _resultsHeight = 8;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandPalette"/> class.
    /// </summary>
    public CommandPalette()
    {
        Focusable = false;
        Items = new BindableList<CommandPaletteItem>(this, "CommandPalette.Items");

        _searchBox = new TextBox()
            .Placeholder("Type to search…")
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        _results = new OptionList<CommandPaletteItem>()
            .ActivateOnClick(true)
            .HorizontalAlignment(HorizontalAlignment.Stretch);
        _results.MinHeight = _resultsHeight;
        _results.MaxHeight = _resultsHeight;

        _results.ItemIsEnabled = (Func<CommandPaletteItem, bool>)(item => item.IsEnabled);
        _results.ItemSearchText = (Func<CommandPaletteItem, string?>)(item => item.SearchText);
        _results.ItemTemplate = new DataTemplate<CommandPaletteItem>((Binding<CommandPaletteItem> binding, in DataTemplateContext _) =>
        {
            var item = binding.GetValue();
            return new OptionListItem(item.CreateContent(), item.CreateShortcut())
            {
                Description = item.CreateDescription(),
            };
        });

        _results.ItemActivated((_, e) => InvokeIndex(e.Index));

        _content = new VStack(_searchBox, _results)
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        AttachChild(_content);

        this.MinWidth(50);
        this.MaxWidth(72);

        _results.Update(_ => RebuildResults());
    }

    /// <summary>
    /// Gets the collection of command palette items.
    /// </summary>
    [Bindable]
    public BindableList<CommandPaletteItem> Items { get; }

    /// <summary>
    /// Gets or sets the number of visible result rows.
    /// </summary>
    public int ResultsHeight
    {
        get => _resultsHeight;
        set
        {
            _resultsHeight = Math.Max(1, value);
            _results.MinHeight = _resultsHeight;
            _results.MaxHeight = _resultsHeight;
        }
    }

    /// <summary>
    /// Shows the command palette in a popup.
    /// </summary>
    public void Show()
    {
        VerifyAccess();

        if (Parent is not null && !IsAttachedToHostPopup())
        {
            throw new InvalidOperationException("CommandPalette.Show cannot be called when the palette is part of a visual tree.");
        }

        _hostPopup ??= new Popup
        {
            MatchAnchorWidth = false,
            Placement = PopupPlacement.Below,
        }.Style(PopupStyle.Default with { Padding = Thickness.Zero });

        // If already hosted, simply re-show the existing popup without re-wrapping the palette.
        // Re-wrapping would attempt to attach the palette to a new parent while it is still parented.
        if (IsAttachedToHostPopup())
        {
            _hostPopup.Show();
            return;
        }

        var style = GetStyle<CommandPaletteStyle>();
        var content = style.PopupTemplateFactory?.Invoke(this) ?? this;
        _hostPopup.Content = content;

        _hostPopup.Show();
    }

    /// <summary>
    /// Closes the command palette popup if it is open.
    /// </summary>
    public void Close() => _hostPopup?.Close();

    /// <inheritdoc />
    protected override int ChildrenCount => 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index) => index == 0 ? _content : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        return _content.Measure(constraints);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        _content.Arrange(finalRect);
    }

    private bool IsAttachedToHostPopup()
    {
        if (_hostPopup is null)
        {
            return false;
        }

        for (var parent = Parent; parent is not null; parent = parent.Parent)
        {
            if (ReferenceEquals(parent, _hostPopup))
            {
                return true;
            }
        }

        return false;
    }

    private void RebuildResults()
    {
        var query = (_searchBox.Text ?? string.Empty).Trim();
        var hasQuery = query.Length > 0;

        _results.Items.Clear();
        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            if (hasQuery)
            {
                var text = item.SearchText;
                if (string.IsNullOrEmpty(text) || text.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }
            }

            _results.Items.Add(item);
        }

        if (_results.Items.Count == 0)
        {
            _results.SelectedIndex = 0;
            return;
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
        item.Action?.Invoke();
        Close();
    }
}
