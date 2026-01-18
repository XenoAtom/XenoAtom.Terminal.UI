// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and popup-hosting options for a <see cref="Controls.CommandPalette"/>.
/// </summary>
public sealed record CommandPaletteStyle : IStyle<CommandPaletteStyle>
{
    /// <summary>
    /// Gets the default command palette style.
    /// </summary>
    public static CommandPaletteStyle Default { get; } = new()
    {
        PopupTemplateFactory = visual => new Group
        {
            TopLeftText = "Command palette",
            Padding = new Thickness(1),
            Content = visual,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        },
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
}

