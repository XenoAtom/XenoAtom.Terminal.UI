// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Provides data for paste events.
/// </summary>
public sealed class PasteEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Gets the pasted text.
    /// </summary>
    public required string Text { get; init; }
}
