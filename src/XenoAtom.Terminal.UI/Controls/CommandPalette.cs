// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Command palette control: search input + filtered list of actions.
/// </summary>
public sealed class CommandPalette : Visual
{
    private readonly TextBox _searchBox;
    private readonly OptionList _results;
    private readonly Group _frame;

    private Popup? _hostPopup;
    private readonly List<CommandPaletteItem> _visibleItems;
    private int _resultsHeight = 8;

    public CommandPalette()
    {
        Focusable = false;
        Items = new BindableList<CommandPaletteItem>(this, "CommandPalette.Items");

        _visibleItems = new List<CommandPaletteItem>();

        _searchBox = new TextBox()
            .Placeholder("Type to search…")
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        _results = new OptionList()
            .ActivateOnClick(true)
            .HorizontalAlignment(HorizontalAlignment.Stretch);
        _results.MinHeight = _resultsHeight;
        _results.MaxHeight = _resultsHeight;

        _results.ItemActivated((_, e) => InvokeIndex(e.Index));

        var content = new VStack(_searchBox, _results)
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        _frame = new Group()
            .TopLeftText("Command palette")
            .Padding(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(content);

        AttachChild(_frame);

        this.MinWidth(50);
        this.MaxWidth(72);

        _results.Update(_ => RebuildResults());
    }

    public BindableList<CommandPaletteItem> Items { get; }

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

    public void Show()
    {
        VerifyAccess();

        if (Parent is not null && !ReferenceEquals(Parent, _hostPopup))
        {
            throw new InvalidOperationException("CommandPalette.Show cannot be called when the palette is part of a visual tree.");
        }

        _hostPopup ??= new Popup
        {
            Content = this,
            MatchAnchorWidth = false,
            Placement = PopupPlacement.Below,
        }.Style(PopupStyle.Default with { ShowBorder = false, Padding = Thickness.Zero });

        _hostPopup.Show();
    }

    public void Close() => _hostPopup?.Close();

    protected override int ChildrenCount => 1;

    protected override Visual GetChild(int index) => index == 0 ? _frame : throw new ArgumentOutOfRangeException(nameof(index));

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        return _frame.Measure(constraints);
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        _frame.Arrange(finalRect);
    }

    private void RebuildResults()
    {
        _visibleItems.Clear();

        var query = (_searchBox.Text ?? string.Empty).Trim();
        var hasQuery = query.Length > 0;

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

            _visibleItems.Add(item);
            _results.Items.Add(new OptionListItem(item.CreateContent(), item.CreateShortcut())
            {
                Description = item.CreateDescription(),
                IsEnabled = item.IsEnabled,
            });
        }

        if (_visibleItems.Count == 0)
        {
            _results.SelectedIndex = 0;
            return;
        }

        _results.SelectedIndex = 0;
    }

    private void InvokeIndex(int index)
    {
        if ((uint)index >= (uint)_visibleItems.Count)
        {
            return;
        }

        var item = _visibleItems[index];
        item.Action?.Invoke();
        Close();
    }
}
