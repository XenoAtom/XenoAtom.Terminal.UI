// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Marks a visual that behaves as a modal element.
/// </summary>
public interface IModalVisual
{
    /// <summary>
    /// Gets a value indicating whether the visual is modal.
    /// </summary>
    bool IsModal { get; }
}
