// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// A bindable container for a value, useful for passing state between visuals.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public sealed class State<T> : Threading.DispatcherObject
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
            VerifyAccess();
            BindingManager.Current.RegisterRead(this, InternalAccessor.Instance);
            return _value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            VerifyAccess();
            if (EqualityComparer<T>.Default.Equals(_value, value))
            {
                return;
            }

            _value = value;
            BindingManager.Current.NotifyValueChanged(this, InternalAccessor.Instance);
        }
    }

    public static implicit operator T(State<T> value) => value.Value;

    public static implicit operator Binding<T>(State<T> state) => new(state, InternalAccessor.Instance);

    public static implicit operator Binding(State<T> state) => new(state, InternalAccessor.Instance);

    public override string? ToString() => Value?.ToString();

    private sealed class InternalAccessor : BindingAccessor<T>
    {
        public static BindingAccessor<T> Instance { get; } = new InternalAccessor();

        private InternalAccessor() : base(nameof(Value), StaticGetter, StaticSetter)
        {
        }

        private static T StaticGetter(object instance)
        {
            var state = ((State<T>)instance);
            return state.Value;
        }

        private static void StaticSetter(object instance, T value)
        {
            var state = ((State<T>)instance);
            state.Value = value;
        }
    }
}
