// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Styling;

public sealed class ProgressBarStyle
{
    public static ProgressBarStyle Default { get; } = new();

    public static EnvironmentKey<ProgressBarStyle> Key { get; } = new("ProgressBarStyle", Default);

    public char FillGlyph { get; init; } = '█';
    public char TrackGlyph { get; init; } = '░';

    public Cell? Filled { get; init; }
    public Cell? Unfilled { get; init; }
    public Cell? Border { get; init; }

    public Cell ResolveBorder(Theme theme) => Border ?? (theme.BorderStyle(focused: false) | TextStyle.Dim);
    public Cell ResolveFilled(Theme theme) => Filled ?? theme.SelectionStyle();
    public Cell ResolveUnfilled(Theme theme) => Unfilled ?? (theme.BorderStyle(focused: false) | TextStyle.Dim);
}
