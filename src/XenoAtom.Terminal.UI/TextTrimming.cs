// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Specifies how text is trimmed when it does not fit in the available width.
/// </summary>
public enum TextTrimming
{
    /// <summary>
    /// Clip without any indicator.
    /// </summary>
    Clip = 0,
    /// <summary>
    /// Trim at the end and show an ellipsis/indicator.
    /// </summary>
    EndEllipsis = 1,
    /// <summary>
    /// Trim at the start and show an ellipsis/indicator.
    /// </summary>
    StartEllipsis = 2,
}
