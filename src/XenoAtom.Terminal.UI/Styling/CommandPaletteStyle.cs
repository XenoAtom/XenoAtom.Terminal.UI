// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and popup-hosting options for a <see cref="Controls.CommandPalette"/>.
/// </summary>
public sealed record CommandPaletteStyle : IStyle<CommandPaletteStyle>
{
    private static readonly DataTemplate<ResolvedCommand> DefaultItemTemplate = CreateDefaultItemTemplateCore(showDescription: true);

    /// <summary>
    /// Gets the default command palette style.
    /// </summary>
    public static CommandPaletteStyle Default { get; } = new()
    {
        MinWidth = 50,
        MaxWidth = 72,
        ResultsHeight = 8,
        PopupHorizontalAlignment = Align.Center,
        PopupVerticalAlignment = Align.Start,
        PopupIsDraggable = true,
        PopupDragHandleHeight = 1,
        PopupTemplateFactory = visual => new Group
        {
            TopLeftText = "Command palette",
            Padding = new Thickness(1),
            Content = visual,
            HorizontalAlignment = Align.Stretch,
        },
        ItemTemplate = DefaultItemTemplate,
    };

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="CommandPaletteStyle"/>.
    /// </summary>
    public static StyleKey<CommandPaletteStyle> Key { get; } = new("CommandPaletteStyle", Default);

    /// <summary>
    /// Gets the factory used to wrap the palette when it is shown via <see cref="CommandPalette.Show"/>.
    /// </summary>
    /// <remarks>
    /// This allows customizing the chrome (e.g. border and padding) around the command palette without modifying the palette content.
    /// </remarks>
    public Func<Visual, Visual?>? PopupTemplateFactory { get; init; }

    /// <summary>
    /// Gets the horizontal alignment used when the palette popup is not anchored.
    /// </summary>
    public Align PopupHorizontalAlignment { get; init; } = Align.Center;

    /// <summary>
    /// Gets the vertical alignment used when the palette popup is not anchored.
    /// </summary>
    public Align PopupVerticalAlignment { get; init; } = Align.Start;

    /// <summary>
    /// Gets the horizontal offset applied to the palette popup position.
    /// </summary>
    public int PopupOffsetX { get; init; }

    /// <summary>
    /// Gets the vertical offset applied to the palette popup position.
    /// </summary>
    public int PopupOffsetY { get; init; }

    /// <summary>
    /// Gets a value indicating whether the palette popup can be repositioned by dragging.
    /// </summary>
    public bool PopupIsDraggable { get; init; } = true;

    /// <summary>
    /// Gets the height (in rows) of the draggable area at the top of the palette popup.
    /// </summary>
    public int PopupDragHandleHeight { get; init; } = 1;

    /// <summary>
    /// Gets the number of visible result rows in the palette.
    /// </summary>
    public int ResultsHeight { get; init; } = 8;

    /// <summary>
    /// Gets the minimum width, in cells, of the palette.
    /// </summary>
    public int MinWidth { get; init; } = 50;

    /// <summary>
    /// Gets the maximum width, in cells, of the palette.
    /// </summary>
    public int MaxWidth { get; init; } = 72;

    /// <summary>
    /// Gets the item template used by the palette results list.
    /// </summary>
    /// <remarks>
    /// The default template shows the label on the left and the shortcut on the right, with an optional second line for
    /// the command description.
    /// </remarks>
    public DataTemplate<ResolvedCommand>? ItemTemplate { get; init; }

    internal static DataTemplate<ResolvedCommand> CreateDefaultItemTemplate() => DefaultItemTemplate;

    private static DataTemplate<ResolvedCommand> CreateDefaultItemTemplateCore(bool showDescription)
        => new((Binding<ResolvedCommand> binding, in DataTemplateContext _) =>
        {
            var entry = binding.GetValue();
            var cmd = entry.Command;

            Visual label = new Markup(cmd.LabelMarkup);

            Visual? shortcut = null;
            if (cmd.Sequence is { } seq)
            {
                shortcut = new TextBlock(seq.ToString());
            }
            else if (cmd.Gesture is { } g)
            {
                shortcut = new TextBlock(g.ToString());
            }

            var item = new OptionListItem(label, shortcut)
            {
                SearchText = cmd.Name ?? cmd.SearchText,
            };

            if (showDescription && !string.IsNullOrEmpty(cmd.DescriptionMarkup))
            {
                item.Description = new Markup(cmd.DescriptionMarkup);
            }

            return item;
        });
}
