// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections;
using System.Runtime.InteropServices;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Stores the graphics display list collected for a UI frame.
/// </summary>
public sealed class GraphicsCommandBuffer : IReadOnlyList<GraphicsCommand>
{
    private readonly List<GraphicsCommand> _commands = new();

    /// <summary>
    /// Gets the number of commands in the buffer.
    /// </summary>
    public int Count => _commands.Count;

    /// <summary>
    /// Gets the command at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The command index.</param>
    public GraphicsCommand this[int index] => _commands[index];

    /// <summary>
    /// Removes all commands from the buffer.
    /// </summary>
    public void Clear() => _commands.Clear();

    /// <summary>
    /// Adds a command to the end of the buffer.
    /// </summary>
    /// <param name="command">The command to add.</param>
    public void Add(in GraphicsCommand command) => _commands.Add(command);

    /// <summary>
    /// Returns a read-only span over the current commands.
    /// </summary>
    /// <returns>A read-only command span valid until the buffer is modified.</returns>
    public ReadOnlySpan<GraphicsCommand> AsSpan() => CollectionsMarshal.AsSpan(_commands);

    /// <inheritdoc />
    public IEnumerator<GraphicsCommand> GetEnumerator() => _commands.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
