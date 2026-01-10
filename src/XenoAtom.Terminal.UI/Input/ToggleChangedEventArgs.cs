// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

public sealed class ToggleChangedEventArgs : RoutedEventArgs
{
    public bool OldValue { get; init; }

    public bool NewValue { get; init; }
}

