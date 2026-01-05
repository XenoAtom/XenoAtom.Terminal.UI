// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public sealed class KeyBinding
{
    public required TerminalKeyGesture Gesture { get; init; }

    public required Action Action { get; init; }
}

