// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

public sealed class ValueChangedEventArgs : RoutedEventArgs
{
    public double OldValue { get; init; }

    public double NewValue { get; init; }
}

