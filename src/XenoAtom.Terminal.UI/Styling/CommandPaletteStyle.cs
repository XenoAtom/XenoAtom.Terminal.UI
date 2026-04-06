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
        PopupIsResizable = true,
        PopupDragHandleHeight = 1,
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
    /// Gets an optional popup width as a percentage of the available viewport width.
    /// </summary>
    /// <remarks>
    /// Values are expressed in the [0, 100] range. Non-positive, <see cref="double.NaN"/>, and infinite values are ignored.
    /// When set, the percentage-derived width is used as the initial popup width before alignment and offsets are applied.
    /// </remarks>
    public double? PopupWidthPercent { get; init; }

    /// <summary>
    /// Gets an optional popup height as a percentage of the available viewport height.
    /// </summary>
    /// <remarks>
    /// Values are expressed in the [0, 100] range. Non-positive, <see cref="double.NaN"/>, and infinite values are ignored.
    /// When set, the percentage-derived height is used as the initial popup height before alignment and offsets are applied.
    /// </remarks>
    public double? PopupHeightPercent { get; init; }

    /// <summary>
    /// Gets a value indicating whether the palette popup can be repositioned by dragging.
    /// </summary>
    public bool PopupIsDraggable { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the palette host window can be resized with the mouse.
    /// </summary>
    public bool PopupIsResizable { get; init; } = true;

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

    /// <summary>
    /// Gets a value indicating whether the default item template shows <see cref="Command.Name"/> before the label markup.
    /// </summary>
    /// <remarks>
    /// This setting is only used when <see cref="ItemTemplate"/> is not overridden.
    /// </remarks>
    public bool ShowCommandName { get; init; } = true;

    /// <summary>
    /// Gets the prefix inserted before the command name by the default item template.
    /// </summary>
    /// <remarks>
    /// This setting is only used when <see cref="ItemTemplate"/> is not overridden and the command defines a non-empty <see cref="Command.Name"/>.
    /// </remarks>
    public string CommandNamePrefix { get; init; } = "/";

    /// <summary>
    /// Gets the separator inserted between the command name and label markup by the default item template.
    /// </summary>
    /// <remarks>
    /// This setting is only used when <see cref="ItemTemplate"/> is not overridden and the command defines a non-empty <see cref="Command.Name"/>.
    /// </remarks>
    public string CommandNameSeparator { get; init; } = " - ";

    internal static DataTemplate<ResolvedCommand> CreateDefaultItemTemplate() => DefaultItemTemplate;

    internal static DataTemplate<ResolvedCommand> CreateDefaultItemTemplate(CommandPaletteStyle style)
        => CreateDefaultItemTemplateCore(
            showDescription: true,
            showCommandName: style.ShowCommandName,
            commandNamePrefix: style.CommandNamePrefix,
            commandNameSeparator: style.CommandNameSeparator);

    internal static bool UsesDefaultItemTemplate(DataTemplate<ResolvedCommand>? itemTemplate)
        => itemTemplate is null || itemTemplate.Value.Equals(DefaultItemTemplate);

    private static DataTemplate<ResolvedCommand> CreateDefaultItemTemplateCore(
        bool showDescription,
        bool showCommandName = true,
        string? commandNamePrefix = "/",
        string? commandNameSeparator = " - ")
        => new(Display: (DataTemplateValue<ResolvedCommand> entryValue, in DataTemplateContext _) =>
        {
            var entry = entryValue.GetValue();
            var cmd = entry.Command;

            Visual label = CreateLabelContent(cmd, showCommandName, commandNamePrefix, commandNameSeparator);

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
        }, Editor: null);

    private static Visual CreateLabelContent(Command command, bool showCommandName, string? commandNamePrefix, string? commandNameSeparator)
    {
        if (!showCommandName || string.IsNullOrEmpty(command.Name))
        {
            return new Markup(command.LabelMarkup);
        }

        return new HStack(
            new TextBlock($"{commandNamePrefix}{command.Name}{commandNameSeparator}"),
            new Markup(command.LabelMarkup))
        {
            Spacing = 0,
        };
    }
}
