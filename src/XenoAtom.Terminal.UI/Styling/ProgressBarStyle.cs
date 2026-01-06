// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record ProgressBarStyle
{
    public static ProgressBarStyle Default { get; } = new();

    public static EnvironmentKey<ProgressBarStyle> Key { get; } = new("ProgressBarStyle", Default);

    public Rune FillGlyph { get; init; } = new(0x2588);
    public Rune TrackGlyph { get; init; } = new(0x2591);

    public CellStyle? Filled { get; init; }
    public CellStyle? Unfilled { get; init; }
    public CellStyle? Border { get; init; }

    public CellStyle ResolveBorder(Theme theme) => Border ?? (theme.BorderStyle(focused: false) | TextStyle.Dim);
    public CellStyle ResolveFilled(Theme theme) => Filled ?? theme.SelectionStyle();
    public CellStyle ResolveUnfilled(Theme theme) => Unfilled ?? (theme.BorderStyle(focused: false) | TextStyle.Dim);
}

