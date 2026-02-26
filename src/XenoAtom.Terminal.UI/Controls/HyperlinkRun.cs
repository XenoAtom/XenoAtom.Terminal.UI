// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a hyperlink span within a text string.
/// </summary>
public readonly record struct HyperlinkRun
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HyperlinkRun"/> struct.
    /// </summary>
    /// <param name="start">The UTF-16 start index.</param>
    /// <param name="length">The UTF-16 length.</param>
    /// <param name="uri">The target URI.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="start"/> or <paramref name="length"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="uri"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="uri"/> is <see langword="null"/>.</exception>
    public HyperlinkRun(int start, int length, string uri)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentException.ThrowIfNullOrEmpty(uri);

        Start = start;
        Length = length;
        Uri = uri;
    }

    /// <summary>
    /// Gets the UTF-16 start index.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Gets the UTF-16 length.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets the target URI.
    /// </summary>
    public string Uri { get; }
}
