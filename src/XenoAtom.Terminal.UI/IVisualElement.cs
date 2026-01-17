// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Tags an element that is part of the visual tree, but not necessarily a <see cref="Visual"/>.
/// </summary>
public interface IVisualElement
{
    /// <summary>
    /// Gets the owning <see cref="TerminalApp"/> if attached.
    /// </summary>
    TerminalApp? App { get; }
}
