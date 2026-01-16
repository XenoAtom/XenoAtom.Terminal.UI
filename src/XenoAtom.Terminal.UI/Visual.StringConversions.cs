// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public abstract partial class Visual
{
    /// <summary>
    /// Converts a string into a <see cref="Controls.TextBlock"/> visual.
    /// </summary>
    /// <param name="text">The text to convert.</param>
    public static implicit operator Visual(string text)
        => new Controls.TextBlock(text ?? string.Empty);
}
