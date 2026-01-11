// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Layout;

/// <summary>
/// Exception thrown when the layout protocol invariants are violated.
/// </summary>
public sealed class LayoutException : Exception
{
    public LayoutException()
    {
    }

    public LayoutException(string message) : base(message)
    {
    }

    public LayoutException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

