// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines a contract for types that represent a style and provide a unique environment key for style resolution.
/// </summary>
/// <remarks>Implementations of this interface are expected to provide a static environment key that uniquely
/// identifies the style type. This pattern enables type-safe style retrieval and association within an environment or
/// context.</remarks>
/// <typeparam name="T">The type that implements the style interface. Must implement IStyle<T>.</typeparam>
public interface IStyle<T> where T: IStyle<T>
{
    /// <summary>
    /// Gets the unique environment key associated with the current type parameter.
    /// </summary>
    public static abstract StyleKey<T> Key { get; }
}