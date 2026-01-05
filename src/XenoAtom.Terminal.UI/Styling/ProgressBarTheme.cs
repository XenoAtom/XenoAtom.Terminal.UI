// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public sealed class ProgressBarTheme
{
    public static ProgressBarTheme Default { get; } = new();

    public static EnvironmentKey<ProgressBarTheme> Key { get; } = new("ProgressBarTheme", Default);

    public CellStyle? Filled { get; init; }
    public CellStyle? Unfilled { get; init; }
    public CellStyle? Border { get; init; }

    public CellStyle ResolveBorder(Theme theme) => Border ?? (theme.BorderStyle(focused: false) | CellStyle.Dim);
    public CellStyle ResolveFilled(Theme theme) => Filled ?? theme.SelectionStyle();
    public CellStyle ResolveUnfilled(Theme theme) => Unfilled ?? (theme.BorderStyle(focused: false) | CellStyle.Dim);
}

