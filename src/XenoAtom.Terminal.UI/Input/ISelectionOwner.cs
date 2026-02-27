// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Represents a control that can expose a clipboard selection.
/// </summary>
/// <remarks>
/// The application keeps a single active selection owner at a time. When the user starts a selection in another
/// <see cref="ISelectionOwner"/>, the previous owner is asked to clear its selection.
/// </remarks>
public interface ISelectionOwner
{
    /// <summary>
    /// Gets a value indicating whether this control participates in selection ownership.
    /// </summary>
    bool IsSelectable { get; }

    /// <summary>
    /// Gets a value indicating whether this control currently has a non-empty selection.
    /// </summary>
    bool HasSelection { get; }

    /// <summary>
    /// Clears the current selection.
    /// </summary>
    void ClearSelection();

    /// <summary>
    /// Tries to return the current selection as text.
    /// </summary>
    /// <param name="text">The selected text.</param>
    /// <returns><see langword="true"/> when selection text is available; otherwise <see langword="false"/>.</returns>
    bool TryCopySelection(out string text);
}

