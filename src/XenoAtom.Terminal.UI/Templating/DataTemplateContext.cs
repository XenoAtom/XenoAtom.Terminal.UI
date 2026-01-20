// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI;

namespace XenoAtom.Terminal.UI.Templating;

/// <summary>
/// Provides contextual information when materializing or updating a data template.
/// </summary>
/// <param name="Owner">The control requesting the template.</param>
/// <param name="Role">The template role.</param>
/// <param name="Index">The item index, or <c>-1</c> when not item-based.</param>
/// <param name="State">The item interaction state.</param>
public readonly record struct DataTemplateContext(Visual Owner, DataTemplateRole Role, int Index, DataTemplateItemState State);
