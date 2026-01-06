// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Geometry;

public readonly record struct Thickness(int Left, int Top, int Right, int Bottom)
{
    public Thickness(int uniform) : this(uniform, uniform, uniform, uniform)
    {
    }

    public static readonly Thickness Zero = new(0);

    public int Horizontal => Left + Right;
    public int Vertical => Top + Bottom;
}

