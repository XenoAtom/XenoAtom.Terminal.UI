// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Represents a read-only bindable value whose current value is computed by a <see cref="Func{TResult}"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type is primarily used by generated fluent APIs when a bindable property is configured with a
/// <see cref="Func{TResult}"/>. The function is evaluated on demand when the value is read, and the binding
/// system tracks any bindable reads performed inside the function.
/// </para>
/// <para>
/// Because the value is computed, setting <see cref="Value"/> is not supported and will throw.
/// </para>
/// </remarks>
/// <typeparam name="T">The value type.</typeparam>
public sealed partial class FuncState<T> : IPullBindingSource
{
    private readonly Func<T> _valueFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FuncState{T}"/> class.
    /// </summary>
    /// <param name="valueFactory">A delegate that computes the current value.</param>
    public FuncState(Func<T> valueFactory)
    {
        _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
    }

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    /// <remarks>
    /// The getter evaluates the factory delegate on demand. Any bindable reads performed by the delegate are
    /// tracked by the current binding context. The setter is not supported.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when attempting to set the value.</exception>
    [Bindable]
    public T Value
    {
        get
        {
            VerifyAccess();
            BindingManager.Current.RegisterRead(this, __Value__BindingAccessor.Instance);
            return _valueFactory();
        }
        set => throw new InvalidOperationException($"{nameof(FuncState<T>)} is read-only and cannot be assigned.");
    }

    private void VerifyAccess()
    {
        if (Threading.Dispatcher.Current is { } dispatcher)
        {
            dispatcher.VerifyAccess();
        }
    }
}

