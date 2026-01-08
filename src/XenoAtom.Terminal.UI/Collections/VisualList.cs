// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Collections;

/// <summary>
/// A bindable list of visuals that automatically attaches/detaches children to the owning visual.
/// </summary>
/// <typeparam name="T">The visual type.</typeparam>
public sealed class VisualList<T> : BindableList<T>
    where T : Visual
{
    public VisualList(Visual owner, string name)
        : base(owner, name, onAdding: owner.AttachCollectionChild, onRemoving: owner.DetachCollectionChild)
    {
        ArgumentNullException.ThrowIfNull(owner);
    }

    public Visual VisualOwner => (Visual)Owner;
}

