// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Provides data for item activation events.
/// </summary>
public sealed class ItemActivatedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Gets the activated item index.
    /// </summary>
    public int Index { get; init; }
}
