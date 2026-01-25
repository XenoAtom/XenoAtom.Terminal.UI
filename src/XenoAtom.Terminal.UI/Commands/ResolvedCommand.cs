// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Commands;

/// <summary>
/// Represents a command resolved for a specific visual target.
/// </summary>
/// <remarks>
/// UI surfaces such as command palettes and context menus collect commands based on the current focus/hover context.
/// The same command may be visible or enabled for some targets but not others; this type captures the resolved target
/// together with the computed enabled state.
/// </remarks>
public readonly record struct ResolvedCommand(Command Command, Visual Target, bool IsEnabled, bool IsGlobal)
{
    // Used internally to produce deterministic ordering (importance + insertion order) without exposing this implementation
    // detail on the public API surface.
    internal int Order { get; init; }
}

