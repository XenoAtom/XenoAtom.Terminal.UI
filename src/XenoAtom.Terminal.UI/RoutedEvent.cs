// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Provides helpers for registering routed events.
/// </summary>
public static class RoutedEvent
{
    /// <summary>
    /// Registers a routed event for the specified owner type.
    /// </summary>
    /// <typeparam name="TOwner">The owner type.</typeparam>
    /// <typeparam name="TArgs">The event args type.</typeparam>
    /// <param name="name">The event name.</param>
    /// <param name="dispatch">The dispatch callback.</param>
    /// <param name="routingStrategy">The routing strategy.</param>
    /// <returns>The registered routed event metadata.</returns>
    public static RoutedEvent<TArgs> Register<TOwner, TArgs>(
        string name,
        Action<object, TArgs> dispatch,
        RoutingStrategy routingStrategy)
        where TArgs : EventArgs
    {
        return new RoutedEvent<TArgs>(name, typeof(TOwner), dispatch, routingStrategy);
    }
}

/// <summary>
/// Represents a routed event metadata and dispatch logic.
/// </summary>
/// <typeparam name="TArgs">The event args type.</typeparam>
public sealed class RoutedEvent<TArgs>
    where TArgs : EventArgs
{
    internal RoutedEvent(string name, Type ownerType, Action<object, TArgs> dispatch, RoutingStrategy routingStrategy)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
        Dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
        RoutingStrategy = routingStrategy;
    }

    /// <summary>
    /// Gets the event name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the owner type of the event.
    /// </summary>
    public Type OwnerType { get; }

    /// <summary>
    /// Gets the dispatch callback.
    /// </summary>
    public Action<object, TArgs> Dispatch { get; }

    /// <summary>
    /// Gets the routing strategy used by the event.
    /// </summary>
    public RoutingStrategy RoutingStrategy { get; }

    /// <inheritdoc />
    public override string ToString() => $"{OwnerType.Name}.{Name}";
}
