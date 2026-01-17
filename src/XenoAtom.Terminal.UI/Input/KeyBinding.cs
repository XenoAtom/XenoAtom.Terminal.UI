// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Represents a key binding that triggers an action.
/// </summary>
public sealed class KeyBinding
{
    /// <summary>
    /// Gets the key gesture that activates the binding.
    /// </summary>
    public required TerminalKeyGesture Gesture { get; init; }

    /// <summary>
    /// Gets the action to execute.
    /// </summary>
    public required Action Action { get; init; }
}
