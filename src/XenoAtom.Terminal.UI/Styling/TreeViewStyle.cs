// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public static class TreeNodeIcons
{
    public static Rune FolderGlyph => new(0x1F4C1);

    public static Rune FileGlyph => new(0x1F4C4);

    public static Rune DocumentGlyph => new(0x1F4C3);

}

public sealed record TreeViewStyle : IStyle<TreeViewStyle>
{
    public static TreeViewStyle Default { get; } = new();

    public static StyleKey<TreeViewStyle> Key { get; } = new("TreeViewStyle", Default);

    public bool ShowBorder { get; init; }

    public int IndentSize { get; init; } = 2;

    public int SpaceBetweenGlyphAndText { get; init; } = 2;

    public Rune ExpandedGlyph { get; init; } = new('▾');

    public Rune CollapsedGlyph { get; init; } = new('▸');

    public Rune FocusMarkerGlyph { get; init; } = new('→');

    public Func<object?, Rune?, Rune>? IconResolver { get; init; }

    public CellStyle? Item { get; init; }
    public CellStyle? SelectedFocused { get; init; }
    public CellStyle? SelectedUnfocused { get; init; }
    public CellStyle? Disabled { get; init; }
    
    public Rune ResolveIcon(object? dataContext, Rune? nodeIcon)
    {
        return IconResolver?.Invoke(dataContext, nodeIcon) ?? (nodeIcon ?? TreeNodeIcons.DocumentGlyph);
    }

    public CellStyle ResolveItemStyle(Theme theme, bool enabled, bool selected, bool focused)
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

        return SelectedUnfocused ?? (CellStyle.None | TextStyle.Bold | theme.BorderStyle(focused: false));
    }
}
