// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.ComponentModel;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Marks a binding source whose current value must be pulled on read instead of synchronized from source writes.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IPullBindingSource
{
}
