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
    /// <summary>
    /// Initializes a new instance of the <see cref="VisualList{T}"/> class.
    /// </summary>
    /// <param name="owner">The owning visual that will attach/detach children.</param>
    /// <param name="name">The name of the list, used for debugging and diagnostics.</param>
    public VisualList(Visual owner, string name)
        : base(owner, name, onAdding: owner.AttachCollectionChild, onRemoving: owner.DetachCollectionChild)
    {
        ArgumentNullException.ThrowIfNull(owner);
    }

    /// <summary>
    /// Gets the owning visual.
    /// </summary>
    public Visual VisualOwner => (Visual)Owner;
}
