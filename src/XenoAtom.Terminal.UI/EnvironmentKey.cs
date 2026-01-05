// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public sealed class EnvironmentKey<T>
{
    public EnvironmentKey(string name, T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = string.Intern(name);
        DependencyName = string.Intern("$env$" + Name);
        DefaultValue = defaultValue;
    }

    public string Name { get; }

    internal string DependencyName { get; }

    public T DefaultValue { get; }

    public override string ToString() => Name;
}

