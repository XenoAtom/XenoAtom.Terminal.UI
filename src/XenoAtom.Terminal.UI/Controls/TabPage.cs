// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a tab page with a header and a content visual.
/// </summary>
public sealed record class TabPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TabPage"/> class.
    /// </summary>
    /// <param name="header">The tab header visual.</param>
    /// <param name="content">The tab content visual.</param>
    public TabPage(Visual header, Visual content)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    /// <summary>
    /// Gets the tab header visual.
    /// </summary>
    public Visual Header { get; }

    /// <summary>
    /// Gets the tab content visual.
    /// </summary>
    public Visual Content { get; }
}
