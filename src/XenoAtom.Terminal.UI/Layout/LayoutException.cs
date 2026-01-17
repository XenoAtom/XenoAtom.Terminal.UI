// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Layout;

/// <summary>
/// Exception thrown when the layout protocol invariants are violated.
/// </summary>
public sealed class LayoutException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutException"/> class.
    /// </summary>
    public LayoutException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public LayoutException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public LayoutException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
