// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Provides helpers for registering routed events.
/// </summary>
public static class RoutedEvent
{
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

    public string Name { get; }

    public Type OwnerType { get; }

    public Action<object, TArgs> Dispatch { get; }

    public RoutingStrategy RoutingStrategy { get; }

    public override string ToString() => $"{OwnerType.Name}.{Name}";
}

