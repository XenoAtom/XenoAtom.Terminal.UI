// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Templating;

/// <summary>
/// Describes why a data template is being used.
/// </summary>
public enum DataTemplateRole
{
    /// <summary>
    /// Renders a value for viewing.
    /// </summary>
    Display = 0,

    /// <summary>
    /// Renders a value for editing (typically requires a bindable source such as <c>State&lt;T&gt;</c> or <c>Binding&lt;T&gt;</c>).
    /// </summary>
    Editor = 1,
}

