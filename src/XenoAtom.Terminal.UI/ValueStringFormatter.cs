// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Provides culture-aware value formatting helpers used by controls and templates.
/// </summary>
/// <remarks>
/// This type centralizes formatting logic (for example, using <see cref="IFormattable"/> when available) so callers
/// don't need to duplicate the same checks throughout the codebase.
/// </remarks>
internal static class ValueStringFormatter
{
    public static string ToString(object? value, CultureInfo culture, string? format = null)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is string s)
        {
            return s;
        }

        if (value is IFormattable formattable)
        {
            return formattable.ToString(format, culture) ?? string.Empty;
        }

        return value.ToString() ?? string.Empty;
    }
}

