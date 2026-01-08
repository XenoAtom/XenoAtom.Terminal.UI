// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

public sealed class StyleKey<T>
{
    public StyleKey(string name, T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = string.Intern(name);
        DependencyAccessor = new EnvironmentBindingAccessor(string.Intern("$env$" + Name));
        DefaultValue = defaultValue;
    }

    public string Name { get; }

    internal BindingAccessor DependencyAccessor { get; }

    public T DefaultValue { get; }

    public override string ToString() => Name;

    private sealed class EnvironmentBindingAccessor : BindingAccessor
    {
        public EnvironmentBindingAccessor(string name) : base(name)
        {
        }

        public override object? GetValue(object instance) => throw new NotSupportedException();

        public override void SetValue(object instance, object? value) => throw new NotSupportedException();
    }
}
