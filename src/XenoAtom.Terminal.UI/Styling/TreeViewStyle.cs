// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public enum TreeNodeIcon
{
    None = 0,
    Folder = 1,
    File = 2,
    Document = 3,
}

public sealed record TreeViewStyle : IStyle<TreeViewStyle>
{
    public static TreeViewStyle Default { get; } = new();

    public static StyleKey<TreeViewStyle> Key { get; } = new("TreeViewStyle", Default);

    public bool ShowBorder { get; init; }

    public int IndentSize { get; init; } = 2;

    public Rune ExpandedGlyph { get; init; } = new('▾');

    public Rune CollapsedGlyph { get; init; } = new('▸');

    public Rune FolderGlyph { get; init; } = new(0x1F4C1);

    public Rune FileGlyph { get; init; } = new(0x1F4C4);

    public Rune DocumentGlyph { get; init; } = new(0x1F4C3);

    public Rune FocusMarkerGlyph { get; init; } = new('→');

    public CellStyle? Item { get; init; }
    public CellStyle? SelectedFocused { get; init; }
    public CellStyle? SelectedUnfocused { get; init; }
    public CellStyle? Disabled { get; init; }

    public Rune ResolveIcon(TreeNodeIcon icon)
        => icon switch
        {
            TreeNodeIcon.Folder => FolderGlyph,
            TreeNodeIcon.File => FileGlyph,
            TreeNodeIcon.Document => DocumentGlyph,
            _ => new Rune(' '),
        };

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
