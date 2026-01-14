// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

public static partial class SparklineExtensions
{
    public static Sparkline Values(this Sparkline visual, IEnumerable<double> values)
    {
        ArgumentNullException.ThrowIfNull(visual);
        ArgumentNullException.ThrowIfNull(values);
        visual.Values!.Clear();
        visual.Values.AddRange(values);
        return visual;
    }
}