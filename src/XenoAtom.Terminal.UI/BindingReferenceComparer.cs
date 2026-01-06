// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace XenoAtom.Terminal.UI;

internal sealed class BindingReferenceComparer : IEqualityComparer<Binding>
{
    public static BindingReferenceComparer Instance { get; } = new();

    public bool Equals(Binding x, Binding y)
        => ReferenceEquals(x.Owner, y.Owner) && ReferenceEquals(x.Accessor, y.Accessor);

    public int GetHashCode(Binding obj)
        => HashCode.Combine(RuntimeHelpers.GetHashCode(obj.Owner), RuntimeHelpers.GetHashCode(obj.Accessor));
}

