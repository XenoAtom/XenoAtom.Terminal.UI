// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

public sealed class SelectionChangedEventArgs : RoutedEventArgs
{
    public int OldIndex { get; init; }

    public int NewIndex { get; init; }
}

