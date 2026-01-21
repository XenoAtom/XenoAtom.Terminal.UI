// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Provides helper methods for presenting values using data templates.
/// </summary>
public static class DataPresentationExtensions
{
    /// <summary>
    /// Creates a <see cref="DataPresenter{T}"/> configured to present the value provided by the specified binding.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="binding">The binding to present.</param>
    /// <param name="role">The role indicating whether the value is displayed or edited.</param>
    /// <returns>A configured <see cref="DataPresenter{T}"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="binding"/> is empty.</exception>
    public static DataPresenter<T> PresentAs<T>(this Binding<T> binding, DataTemplateRole role)
    {
        if (binding.IsEmpty)
        {
            throw new ArgumentException("The binding must not be empty.", nameof(binding));
        }

        return new DataPresenter<T>().Value(binding).Role(role);
    }

    /// <summary>
    /// Creates a <see cref="DataPresenter{T}"/> configured to present the value provided by the specified state.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="state">The state to present.</param>
    /// <param name="role">The role indicating whether the value is displayed or edited.</param>
    /// <returns>A configured <see cref="DataPresenter{T}"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="state"/> is null.</exception>
    public static DataPresenter<T> PresentAs<T>(this State<T> state, DataTemplateRole role)
    {
        ArgumentNullException.ThrowIfNull(state);
        return ((Binding<T>)state).PresentAs(role);
    }
}

