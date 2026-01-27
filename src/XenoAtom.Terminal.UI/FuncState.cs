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
public sealed partial class FuncState<T>
{
    private readonly Func<T> _valueFactory;
    private readonly Action<Binding> _onDependencyChanged;
    private HashSet<Binding>? _dependencies;
    private bool _dirty = true;
    private bool _hasCachedValue;
    private T _cachedValue = default!;

    /// <summary>
    /// Initializes a new instance of the <see cref="FuncState{T}"/> class.
    /// </summary>
    /// <param name="valueFactory">A delegate that computes the current value.</param>
    public FuncState(Func<T> valueFactory)
    {
        _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
        _onDependencyChanged = OnDependencyChanged;
        BindingManager.Current.ValueChanged += _onDependencyChanged;
    }

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    /// <remarks>
    /// The getter evaluates the factory delegate on demand and caches the result until one of the bindable
    /// dependencies read during evaluation changes. The setter is not supported.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when attempting to set the value.</exception>
    [Bindable]
    public T Value
    {
        get
        {
            BindingManager.Current.RegisterRead(this, __Value__BindingAccessor.Instance);
            if (_dirty)
            {
                Recompute();
            }

            return _cachedValue;
        }
        set => throw new InvalidOperationException($"{nameof(FuncState<T>)} is read-only and cannot be assigned.");
    }

    private void Recompute()
    {
        VerifyAccess();

        using var session = BindingManager.Current.StartTracking();
        _cachedValue = _valueFactory();
        _hasCachedValue = true;

        if (_dependencies is null)
        {
            _dependencies = new HashSet<Binding>(BindingReferenceComparer.Instance);
        }
        else
        {
            _dependencies.Clear();
        }

        foreach (var dep in session.Dependencies)
        {
            _dependencies.Add(dep);
        }

        _dirty = false;
    }

    private void OnDependencyChanged(Binding binding)
    {
        if (ReferenceEquals(binding.Owner, this))
        {
            return;
        }

        if (!_dirty && _dependencies is not null && _dependencies.Contains(binding))
        {
            _dirty = true;
            // Preserve the previous cached value until the next read triggers a recomputation.
            // This keeps reads deterministic and avoids expensive recomputation when the value is not observed.
            if (_hasCachedValue)
            {
                BindingManager.Current.NotifyValueChanged(this, __Value__BindingAccessor.Instance);
            }
        }
    }

    private void VerifyAccess()
    {
        if (Threading.Dispatcher.Current is { } dispatcher)
        {
            dispatcher.VerifyAccess();
        }
    }
}

