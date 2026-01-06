// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI;

public static partial class VisualExtensions
{
    public static T With<T>(this T obj, Action<T> configure) where T : Visual
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(configure);
        obj.Initialize(x => configure((T)x));
        return obj;
    }

    public static T Add<T>(this T obj, params Visual[] visuals) where T : Panel
    {
        ArgumentNullException.ThrowIfNull(obj);
        obj.AddRange(visuals);
        return obj;
    }
}
