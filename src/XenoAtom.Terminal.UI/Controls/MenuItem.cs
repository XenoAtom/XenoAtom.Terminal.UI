// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Commands;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a menu item with optional submenu items.
/// </summary>
public sealed partial class MenuItem : IVisualElement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MenuItem"/> class.
    /// </summary>
    /// <param name="header">The header content.</param>
    /// <param name="action">The action to invoke.</param>
    public MenuItem(Visual header, Action? action = null)
    {
        Header = header;
        Action = action;
        Items = new BindableList<MenuItem>(this, "MenuItemList");
        IsEnabled = true;
        IsVisible = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuItem"/> class.
    /// </summary>
    /// <param name="header">The header text.</param>
    public MenuItem(string header)
        : this((Visual)header)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuItem"/> class.
    /// </summary>
    /// <param name="header">The header content.</param>
    public MenuItem(Visual header)
        : this(header, action: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuItem"/> class.
    /// </summary>
    /// <param name="header">The header text.</param>
    /// <param name="command">An optional backing command.</param>
    public MenuItem(string header, Command? command = null)
        : this((Visual)header, command)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuItem"/> class.
    /// </summary>
    /// <param name="header">The header content.</param>
    /// <param name="command">An optional backing command.</param>
    public MenuItem(Visual header, Command? command = null)
        : this(header, action: null)
    {
        Command = command;
    }

    /// <summary>
    /// Creates a separator menu item.
    /// </summary>
    /// <returns>The separator menu item.</returns>
    public static MenuItem CreateSeparator()
        => new MenuItem(new TextBlock(string.Empty))
        {
            IsSeparator = true,
            IsEnabled = false,
        };

    /// <summary>
    /// Creates a separator menu item.
    /// </summary>
    /// <returns>The separator menu item.</returns>
    public static MenuItem Separator() => CreateSeparator();

    /// <summary>
    /// Gets the submenu items.
    /// </summary>
    [Bindable]
    public BindableList<MenuItem> Items { get; }

    /// <summary>
    /// Gets the header content.
    /// </summary>
    public Visual Header { get; }

    TerminalApp? IVisualElement.App => Header.App;

    /// <summary>
    /// Gets or sets an optional command backing this menu item.
    /// </summary>
    /// <remarks>
    /// When set, the menu will derive enabled state and invocation behavior from the command.
    /// </remarks>
    [Bindable]
    public partial Command? Command { get; set; }

    /// <summary>
    /// Gets or sets an optional target visual used when evaluating and executing <see cref="Command"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see cref="CommandTarget"/> is set, menu execution and visibility/enabled evaluation uses this target instead of the
    /// menu host's default target.
    /// </para>
    /// <para>
    /// This is primarily used for context menus built from discovered commands where each command is associated with the visual
    /// that registered it.
    /// </para>
    /// </remarks>
    public Visual? CommandTarget { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this item is a separator.
    /// </summary>
    [Bindable]
    public partial bool IsSeparator { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is visible.
    /// </summary>
    [Bindable]
    public partial bool IsVisible { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is enabled.
    /// </summary>
    [Bindable]
    public partial bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the icon visual.
    /// </summary>
    [Bindable]
    public partial Visual? Icon { get; set; }

    /// <summary>
    /// Gets or sets the shortcut visual.
    /// </summary>
    [Bindable]
    public partial Visual? Shortcut { get; set; }

    /// <summary>
    /// Gets or sets the action to invoke when activated.
    /// </summary>
    [Bindable]
    public partial Delegator<Action> Action { get; set; }

    internal bool IsVisibleFor(Visual target)
    {
        var effectiveTarget = CommandTarget ?? target;
        return IsVisible && (Command is null || Command.IsVisibleFor(effectiveTarget));
    }

    internal bool IsEnabledFor(Visual target)
    {
        var effectiveTarget = CommandTarget ?? target;
        return !IsSeparator && IsEnabled && (Command is null || Command.CanExecuteFor(effectiveTarget));
    }
}
