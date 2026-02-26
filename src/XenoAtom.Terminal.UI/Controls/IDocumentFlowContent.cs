// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents the content model for a single <see cref="DocumentFlowItem"/>.
/// </summary>
public interface IDocumentFlowContent
{
    /// <summary>
    /// Gets a monotonically increasing version. Increment this value when block structure or block data changes.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Gets the number of blocks in this document.
    /// </summary>
    int BlockCount { get; }

    /// <summary>
    /// Gets a block by index.
    /// </summary>
    /// <param name="index">The block index.</param>
    /// <returns>The block at the specified index.</returns>
    DocumentFlowBlock GetBlock(int index);
}
