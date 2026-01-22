// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI;

public abstract partial class Visual
{
    /// <summary>
    /// Gets the culture used for formatting values within this visual subtree.
    /// </summary>
    /// <remarks>
    /// The culture is resolved from the environment via <see cref="CultureStyle"/>. The host typically sets the
    /// application culture when creating the <see cref="TerminalApp"/>. You can override the culture for a subtree by
    /// setting <see cref="CultureStyle.Key"/> on any ancestor visual.
    /// </remarks>
    public CultureInfo GetCulture()
    {
        VerifyAccess();
        return Get<CultureStyle>().Culture;
    }

    /// <summary>
    /// Converts a value to a string using the culture resolved for this visual subtree.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">An optional format string passed to <see cref="IFormattable"/> implementations.</param>
    /// <returns>The formatted string, or an empty string for <see langword="null"/> values.</returns>
    public string ToStringValue<T>(T value, string? format = null)
        => ToStringObject(value, format);

    /// <summary>
    /// Converts an object to a string using the culture resolved for this visual subtree.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">An optional format string passed to <see cref="IFormattable"/> implementations.</param>
    /// <returns>The formatted string, or an empty string for <see langword="null"/> values.</returns>
    public string ToStringObject(object? value, string? format = null)
    {
        VerifyAccess();
        return ValueStringFormatter.ToString(value, GetCulture(), format);
    }
}

