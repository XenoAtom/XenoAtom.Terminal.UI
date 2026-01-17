// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Provides a cursor location within a visual.
/// </summary>
public interface ICursorProvider
{
    /// <summary>
    /// Attempts to get the cursor cell location.
    /// </summary>
    /// <param name="x">The column index.</param>
    /// <param name="y">The row index.</param>
    /// <returns><see langword="true"/> if a cursor location is available.</returns>
    bool TryGetCursorCell(out int x, out int y);
}
