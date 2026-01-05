// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Base type for routed event args.
/// </summary>
public abstract class RoutedEventArgs : EventArgs
{
    public bool Handled { get; set; }

    public Visual? OriginalSource { get; internal set; }

    public Visual? Source { get; internal set; }
}
