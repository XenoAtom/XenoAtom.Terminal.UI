// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Provides options for one-shot visual output via <see cref="TerminalExtensions"/>.
/// </summary>
public sealed class TerminalWriteOptions
{
    /// <summary>
    /// Gets or sets the optional presenter used to emit terminal graphics commands as part of the one-shot visual output.
    /// </summary>
    /// <remarks>
    /// When this value is <see langword="null"/>, graphics-capable visuals render their fallback content, if any. The caller
    /// retains ownership of the presenter and is responsible for disposing it when appropriate.
    /// </remarks>
    public ITerminalGraphicsPresenter? GraphicsPresenter { get; set; }
}
