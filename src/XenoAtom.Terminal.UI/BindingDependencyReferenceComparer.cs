// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace XenoAtom.Terminal.UI;

internal sealed class BindingDependencyReferenceComparer : IEqualityComparer<BindingDependency>
{
    public static BindingDependencyReferenceComparer Instance { get; } = new();

    public bool Equals(BindingDependency x, BindingDependency y)
        => ReferenceEquals(x.Owner, y.Owner) && ReferenceEquals(x.Name, y.Name);

    public int GetHashCode(BindingDependency obj)
        => HashCode.Combine(RuntimeHelpers.GetHashCode(obj.Owner), RuntimeHelpers.GetHashCode(obj.Name));
}

