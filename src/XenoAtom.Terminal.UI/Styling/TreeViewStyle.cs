// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Provides default icons used by the <see cref="Controls.TreeView"/>.
/// </summary>
public static class TreeNodeIcons
{
    /// <summary>
    /// Gets the glyph used for folder nodes.
    /// </summary>
    public static Rune FolderGlyph => new(0x1F4C1);

    /// <summary>
    /// Gets the glyph used for file nodes.
    /// </summary>
    public static Rune FileGlyph => new(0x1F4C4);

    /// <summary>
    /// Gets the glyph used for document nodes.
    /// </summary>
    public static Rune DocumentGlyph => new(0x1F4C3);
}

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.TreeView"/>.
/// </summary>
public sealed record TreeViewStyle : IStyle<TreeViewStyle>
{
    /// <summary>
    /// Gets the default tree view style.
    /// </summary>
    public static TreeViewStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="TreeViewStyle"/>.
    /// </summary>
    public static StyleKey<TreeViewStyle> Key { get; } = new("TreeViewStyle", Default);

    /// <summary>
    /// Gets the number of spaces used per indentation level.
    /// </summary>
    public int IndentSize { get; init; } = 2;

    /// <summary>
    /// Gets the number of spaces between the node glyph and its content.
    /// </summary>
    public int SpaceBetweenGlyphAndText { get; init; } = 1;

    /// <summary>
    /// Gets the optional glyph set used to draw hierarchy guide lines.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, no hierarchy lines are drawn.
    /// </remarks>
    public LineGlyphs? HierarchyLines { get; init; } = LineGlyphs.Single;

    /// <summary>
    /// Gets the optional style used to draw hierarchy guide lines.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the tree view uses a muted border style derived from the current theme.
    /// </remarks>
    public Style? HierarchyLineStyle { get; init; }

    /// <summary>
    /// Gets a style variant with hierarchy lines disabled.
    /// </summary>
    public static TreeViewStyle NoLines { get; } = Default with { HierarchyLines = null };

    /// <summary>
    /// Gets a style variant with heavy hierarchy lines.
    /// </summary>
    public static TreeViewStyle HeavyLines { get; } = Default with { HierarchyLines = LineGlyphs.Heavy };

    /// <summary>
    /// Gets a style variant with double hierarchy lines.
    /// </summary>
    public static TreeViewStyle DoubleLines { get; } = Default with { HierarchyLines = LineGlyphs.Double };

    /// <summary>
    /// Gets the glyph used for expanded nodes.
    /// </summary>
    public Rune ExpandedGlyph { get; init; } = new('▾');

    /// <summary>
    /// Gets the glyph used for collapsed nodes.
    /// </summary>
    public Rune CollapsedGlyph { get; init; } = new('▸');

    /// <summary>
    /// Gets the glyph used to indicate the currently focused row.
    /// </summary>
    public Rune FocusMarkerGlyph { get; init; } = new('→');

    /// <summary>
    /// Gets an optional icon resolver used to compute the icon for a node.
    /// </summary>
    public Func<object?, Rune?, Rune>? IconResolver { get; init; }

    /// <summary>
    /// Gets the optional style used for a normal item.
    /// </summary>
    public Style? Item { get; init; }

    /// <summary>
    /// Gets the optional style used for a selected and focused item.
    /// </summary>
    public Style? SelectedFocused { get; init; }

    /// <summary>
    /// Gets the optional style used for a selected but unfocused item.
    /// </summary>
    public Style? SelectedUnfocused { get; init; }

    /// <summary>
    /// Gets the optional style used for disabled items.
    /// </summary>
    public Style? Disabled { get; init; }
    
    /// <summary>
    /// Resolves the icon to display for a node.
    /// </summary>
    /// <param name="dataContext">The node data context.</param>
    /// <param name="nodeIcon">An optional node-provided icon.</param>
    /// <returns>The resolved icon glyph.</returns>
    public Rune ResolveIcon(object? dataContext, Rune? nodeIcon)
    {
        return IconResolver?.Invoke(dataContext, nodeIcon) ?? (nodeIcon ?? TreeNodeIcons.DocumentGlyph);
    }

    /// <summary>
    /// Resolves the cell style for an item given its state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the item is enabled.</param>
    /// <param name="selected">Whether the item is selected.</param>
    /// <param name="focused">Whether the tree view is focused.</param>
    /// <returns>The resolved style.</returns>
    public Style ResolveItemStyle(Theme theme, bool enabled, bool selected, bool focused)
    {
        var baseStyle = theme.ForegroundTextStyle();

        if (!enabled)
        {
            if (Disabled is { } d)
            {
                return d;
            }

            var disabled = baseStyle | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                disabled = disabled.WithForeground(c);
            }
            return disabled;
        }

        if (!selected)
        {
            return Item ?? baseStyle;
        }

        if (focused)
        {
            if (SelectedFocused is { } selectedFocused)
            {
                return selectedFocused;
            }

            var selectedStyle = baseStyle | TextStyle.Bold;
            if (theme.FocusBorder is { } c)
            {
                selectedStyle = selectedStyle.WithForeground(c);
            }
            return selectedStyle;
        }

        return SelectedUnfocused ?? (Style.None | TextStyle.Bold | theme.BorderStyle(focused: false));
    }
}
