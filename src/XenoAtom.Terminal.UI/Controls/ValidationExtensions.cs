// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using XenoAtom.Terminal.UI;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Provides extension methods for adding validation messages to visuals.
/// </summary>
public static partial class ValidationExtensions
{
    /// <summary>
    /// Wraps the specified <paramref name="content"/> in a <see cref="ValidationPresenter"/> and sets a fixed message.
    /// </summary>
    /// <param name="content">The visual to wrap.</param>
    /// <param name="message">The message to display, or <see langword="null"/> to hide it.</param>
    /// <returns>The validation presenter.</returns>
    public static ValidationPresenter Validation(this Visual content, ValidationMessage? message)
        => new ValidationPresenter(content).Message(message);

    /// <summary>
    /// Wraps the specified <paramref name="content"/> in a <see cref="ValidationPresenter"/> and binds a message.
    /// </summary>
    /// <param name="content">The visual to wrap.</param>
    /// <param name="message">A binding to the message.</param>
    /// <returns>The validation presenter.</returns>
    public static ValidationPresenter Validation(this Visual content, Binding<ValidationMessage?> message)
        => new ValidationPresenter(content).Message(message);

    /// <summary>
    /// Wraps the specified <paramref name="content"/> in a <see cref="ValidationPresenter"/> and binds a message.
    /// </summary>
    /// <param name="content">The visual to wrap.</param>
    /// <param name="message">A state holding the message.</param>
    /// <returns>The validation presenter.</returns>
    public static ValidationPresenter Validation(this Visual content, State<ValidationMessage?> message)
        => new ValidationPresenter(content).Message(message);

    /// <summary>
    /// Wraps the specified <paramref name="content"/> in a <see cref="ValidationPresenter"/> and computes a message
    /// dynamically based on a bound <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The bound value type.</typeparam>
    /// <param name="content">The visual to wrap.</param>
    /// <param name="value">The binding used to compute validation.</param>
    /// <param name="validator">The validator function (returns null when valid).</param>
    /// <param name="placement">The placement of the message relative to the content.</param>
    /// <returns>The validation presenter.</returns>
    public static ValidationPresenter Validate<T>(
        this Visual content,
        Binding<T> value,
        Func<T, ValidationMessage?> validator,
        ValidationPlacement placement = ValidationPlacement.Below)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(validator);

        return new ValidationPresenter(content)
            .Placement(placement)
            .Update(presenter => presenter.Message = validator(value.GetValue()));
    }
}
