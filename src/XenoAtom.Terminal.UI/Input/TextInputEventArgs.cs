// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Provides data for text input events.
/// </summary>
public sealed class TextInputEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Gets the input text.
    /// </summary>
    public required string Text { get; init; }
}
