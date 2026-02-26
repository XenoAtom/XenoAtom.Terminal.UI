// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a document item rendered by <see cref="DocumentFlow"/>.
/// </summary>
public readonly record struct DocumentFlowItem
{
    /// <summary>
    /// Gets the document content.
    /// </summary>
    public required IDocumentFlowContent Content { get; init; }

    /// <summary>
    /// Gets the item alignment.
    /// </summary>
    public DocumentFlowAlignment Alignment { get; init; }

    /// <summary>
    /// Gets an optional maximum width for the item bubble, in cells.
    /// </summary>
    public int? MaxWidth { get; init; }

    /// <summary>
    /// Gets optional per-item padding. When null, <see cref="DocumentFlow.ItemPadding"/> is used.
    /// </summary>
    public Thickness? Padding { get; init; }

    /// <summary>
    /// Gets an optional background style for the item bubble.
    /// </summary>
    public Style? BackgroundStyle { get; init; }

    /// <summary>
    /// Gets an optional border style for the item bubble.
    /// </summary>
    public Style? BorderStyle { get; init; }
}
