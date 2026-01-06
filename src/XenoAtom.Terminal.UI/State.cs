// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// A bindable container for a value, useful for passing state between visuals.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public sealed class State<T>
{
    private T _value;

    public State(T value, [CallerMemberName] string? name = null)
    {
        _value = value;
        Name = name;
    }

    public string? Name { get; }

    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            BindingManager.Current.RegisterRead(this, InternalAccessor.Instance);
            return _value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value))
            {
                return;
            }

            _value = value;
            BindingManager.Current.NotifyValueChanged(this, InternalAccessor.Instance);
        }
    }

    public static implicit operator T(State<T> state) => state.Value;

    public static implicit operator State<T>(T value) => new(value);

    public static implicit operator Binding<T>(State<T> state) => new(state, InternalAccessor.Instance);

    public static implicit operator Binding(State<T> state) => new(state, InternalAccessor.Instance);

    public override string? ToString() => Value?.ToString();

    private sealed class InternalAccessor : BindingAccessor<T>
    {
        public static BindingAccessor<T> Instance { get; } = new InternalAccessor();

        private InternalAccessor() : base(nameof(Value), StaticGetter, StaticSetter)
        {
        }

        private static T StaticGetter(object instance) => ((State<T>)instance)._value;

        private static void StaticSetter(object instance, T value) => ((State<T>)instance).Value = value;
    }
}

