// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Provides contextual information for hosted update callbacks.
/// </summary>
/// <remarks>
/// This type is intentionally small and forward-compatible: more capabilities may be added over time without
/// breaking the update callback signature.
/// </remarks>
public sealed class TerminalRunningContext
{
    internal TerminalRunningContext(TerminalApp app, TerminalInstance terminal, TerminalHostKind hostKind)
    {
        App = app;
        Terminal = terminal;
        HostKind = hostKind;
    }

    /// <summary>
    /// Gets the terminal instance used by the hosting scenario.
    /// </summary>
    public TerminalInstance Terminal { get; }

    /// <summary>
    /// Gets the active host kind.
    /// </summary>
    public TerminalHostKind HostKind { get; }

    /// <summary>
    /// Gets the hosting <see cref="TerminalApp"/>.
    /// </summary>
    /// <remarks>
    /// Most applications do not need to use this property directly. It is exposed to allow advanced
    /// scenarios and future extensions.
    /// </remarks>
    public TerminalApp App { get; }

    /// <summary>
    /// Gets the current frame timestamp (Stopwatch ticks).
    /// </summary>
    public long Timestamp { get; internal set; }
}

