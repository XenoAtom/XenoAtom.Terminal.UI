// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Rendering;

/// <summary>
/// Metrics emitted by <see cref="CellBufferDiffRenderer"/> when diagnostics are enabled.
/// </summary>
/// <param name="OutputChars">Number of characters written to the terminal.</param>
/// <param name="CellsTouched">Approximate number of cells updated (sum of changed ranges per row).</param>
/// <param name="ForceFull">Whether the renderer performed a full repaint.</param>
public readonly record struct CellBufferDiffMetrics(int OutputChars, int CellsTouched, bool ForceFull);

internal interface ICellBufferDiffMetricsSink
{
    void OnRendered(CellBufferDiffMetrics metrics);
}

