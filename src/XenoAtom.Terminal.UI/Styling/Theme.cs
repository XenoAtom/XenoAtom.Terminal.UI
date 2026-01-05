// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public sealed class Theme
{
    public static Theme Default { get; } = new Theme
    {
        Foreground = 0, // default
        Background = 0, // default
        Border = 8,     // gray
        FocusBorder = 12, // bright blue
        Accent = 12,      // bright blue
        Selection = 12,   // bright blue
        Disabled = 8,     // gray
    };

    public static EnvironmentKey<Theme> Key { get; } = new("Theme", Default);

    /// <summary>0 = default; otherwise basic16 index+1.</summary>
    public int Foreground { get; init; }

    /// <summary>0 = default; otherwise basic16 index+1.</summary>
    public int Background { get; init; }

    /// <summary>0 = default; otherwise basic16 index+1.</summary>
    public int Border { get; init; }

    /// <summary>0 = default; otherwise basic16 index+1.</summary>
    public int FocusBorder { get; init; }

    /// <summary>0 = default; otherwise basic16 index+1.</summary>
    public int Accent { get; init; }

    /// <summary>0 = default; otherwise basic16 index+1.</summary>
    public int Selection { get; init; }

    /// <summary>0 = default; otherwise basic16 index+1.</summary>
    public int Disabled { get; init; }

    public CellStyle BorderStyle(bool focused)
    {
        var idx = focused ? FocusBorder : Border;
        var style = CellStyle.None;
        if (idx > 0)
        {
            style = style.WithForegroundBasic16(idx - 1);
        }
        return style;
    }

    public CellStyle SelectionStyle()
    {
        var style = CellStyle.None;
        if (Selection > 0)
        {
            style = style.WithBackgroundBasic16(Selection - 1);
        }
        style |= CellStyle.Bold;
        return style;
    }
}
