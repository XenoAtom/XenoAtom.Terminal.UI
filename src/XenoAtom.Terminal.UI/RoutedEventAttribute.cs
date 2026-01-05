// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Marks a method as the dispatch target for a routed event and generates the corresponding event definitions.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class RoutedEventAttribute : Attribute
{
    public RoutedEventAttribute(RoutingStrategy routingStrategy = RoutingStrategy.Bubble)
    {
        RoutingStrategy = routingStrategy;
    }

    public RoutingStrategy RoutingStrategy { get; }
}

