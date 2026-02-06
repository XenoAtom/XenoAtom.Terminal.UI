// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Extension methods for building and configuring <see cref="Visual"/> trees fluently.
/// </summary>
public static partial class VisualExtensions
{
    /// <summary>
    /// Wraps the specified visual element in a new ScrollViewer to provide scrollable content.
    /// </summary>
    /// <param name="visual">The visual element to be wrapped in a ScrollViewer. Cannot be null.</param>
    /// <returns>A new ScrollViewer instance that contains the specified visual element as its content.</returns>
    public static ScrollViewer Scrollable(this Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        return new ScrollViewer(visual);
    }

    /// <summary>
    /// Sets both the horizontal and vertical alignment of the specified visual to stretch, and returns the modified
    /// visual.
    /// </summary>
    /// <remarks>This method is intended for use with fluent configuration of visuals. The method modifies the
    /// input visual in place and returns it for chaining.</remarks>
    /// <typeparam name="T">The type of the visual to modify. Must inherit from Visual.</typeparam>
    /// <param name="obj">The visual whose alignment will be set to stretch. Cannot be null.</param>
    /// <returns>The same visual instance with its horizontal and vertical alignment set to stretch.</returns>
    public static T Stretch<T>(this T obj) where T : Visual
    {
        ArgumentNullException.ThrowIfNull(obj);
        obj.HorizontalAlignment = Align.Stretch;
        obj.VerticalAlignment = Align.Stretch;
        return obj;
    }
    
    /// <summary>
    /// Registers a dynamic update callback for the visual and returns the same instance.
    /// </summary>
    /// <remarks>
    /// Dynamic updates are re-evaluated by the framework when bindings they access change.
    /// Prefer using the generated fluent APIs for simple property assignments; use this method for custom dynamic logic.
    /// </remarks>
    /// <typeparam name="T">The visual type.</typeparam>
    /// <param name="obj">The instance to configure.</param>
    /// <param name="configure">A callback invoked during the dynamic update pass.</param>
    /// <returns>The same instance for chaining.</returns>
    public static T Update<T>(this T obj, Action<T> configure) where T : Visual
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(configure);
        obj.RegisterDynamicUpdate(x => configure((T)x));
        return obj;
    }

    /// <summary>
    /// Adds child visuals to a panel and returns the same instance.
    /// </summary>
    /// <typeparam name="T">The panel type.</typeparam>
    /// <param name="obj">The panel to add children to.</param>
    /// <param name="visuals">The child visuals to add.</param>
    /// <returns>The same instance for chaining.</returns>
    public static T Add<T>(this T obj, params Visual[] visuals) where T : Panel
    {
        ArgumentNullException.ThrowIfNull(obj);
        obj.VerifyAccess();
        obj.AddRange(visuals);
        return obj;
    }

    /// <summary>
    /// Wraps the visual in a <see cref="Padder"/> and applies padding around it.
    /// </summary>
    /// <param name="visual">The visual to pad.</param>
    /// <param name="padding">The padding around the visual.</param>
    /// <returns>A <see cref="Padder"/> containing <paramref name="visual"/>.</returns>
    public static Padder Pad(this Visual visual, Thickness padding)
    {
        ArgumentNullException.ThrowIfNull(visual);
        return new Padder(visual).Padding(padding);
    }

    /// <summary>
    /// Wraps the visual in a <see cref="TooltipHost"/> and configures a tooltip for it.
    /// </summary>
    /// <param name="visual">The visual to wrap.</param>
    /// <param name="tooltip">The tooltip content.</param>
    /// <returns>A <see cref="TooltipHost"/> containing <paramref name="visual"/>.</returns>
    public static TooltipHost Tooltip(this Visual visual, Visual tooltip)
    {
        ArgumentNullException.ThrowIfNull(visual);
        ArgumentNullException.ThrowIfNull(tooltip);
        return new TooltipHost(visual).TooltipContent(tooltip);
    }

    /// <summary>
    /// Adds a header cell to the table and returns the same instance.
    /// </summary>
    /// <param name="obj">The table to configure.</param>
    /// <param name="header">The header cell to add.</param>
    /// <returns>The same instance for chaining.</returns>
    public static Table AddHeader(this Table obj, Visual header)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(header);
        obj.VerifyAccess();
        obj.HeaderCells.Add(header);
        return obj;
    }

    /// <summary>
    /// Replaces the header cells of the table and returns the same instance.
    /// </summary>
    /// <param name="obj">The table to configure.</param>
    /// <param name="headers">The header cells.</param>
    /// <returns>The same instance for chaining.</returns>
    public static Table Headers(this Table obj, params Visual[] headers)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(headers);
        obj.VerifyAccess();
        obj.HeaderCells.Clear();
        obj.HeaderCells.AddRange(headers);
        return obj;
    }

    /// <summary>
    /// Adds a row to the table and returns the same instance.
    /// </summary>
    /// <param name="obj">The table to configure.</param>
    /// <param name="cells">The row cells.</param>
    /// <returns>The same instance for chaining.</returns>
    public static Table AddRow(this Table obj, params Visual[] cells)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(cells);
        obj.VerifyAccess();

        var row = new VisualList<Visual>(obj, "Table.Row");
        row.AddRange(cells);
        obj.RowCells.Add(row);
        return obj;
    }

    /// <summary>
    /// Replaces the table rows with a single row (if any) and returns the same instance.
    /// </summary>
    /// <param name="obj">The table to configure.</param>
    /// <param name="cells">The row cells.</param>
    /// <returns>The same instance for chaining.</returns>
    public static Table Rows(this Table obj, params Visual[] cells)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(cells);
        obj.VerifyAccess();

        obj.RowCells.Clear();
        if (cells.Length > 0)
        {
            obj.AddRow(cells);
        }

        return obj;
    }

    /// <summary>
    /// Replaces the table rows and returns the same instance.
    /// </summary>
    /// <param name="obj">The table to configure.</param>
    /// <param name="rows">The rows to set.</param>
    /// <returns>The same instance for chaining.</returns>
    public static Table Rows(this Table obj, params Visual[][] rows)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(rows);
        obj.VerifyAccess();

        obj.RowCells.Clear();
        for (var i = 0; i < rows.Length; i++)
        {
            var row = rows[i] ?? throw new ArgumentNullException(nameof(rows));
            obj.AddRow(row);
        }

        return obj;
    }

    /// <summary>
    /// Applies a style instance to a visual by storing it in the visual environment and returns the same instance.
    /// </summary>
    /// <typeparam name="T">The visual type.</typeparam>
    /// <typeparam name="TStyle">The style type.</typeparam>
    /// <param name="obj">The visual to style.</param>
    /// <param name="style">The style instance.</param>
    /// <returns>The same instance for chaining.</returns>
    public static T Style<T, TStyle>(this T obj, TStyle style) where T : Visual where TStyle : IStyle<TStyle>
    {
        ArgumentNullException.ThrowIfNull(obj);
        obj.VerifyAccess();
        obj.SetStyle(style);
        return obj;
    }

    /// <summary>
    /// Applies a style factory to a visual by storing it in the visual environment and returns the same instance.
    /// </summary>
    /// <typeparam name="T">The visual type.</typeparam>
    /// <typeparam name="TStyle">The style type.</typeparam>
    /// <param name="obj">The visual to style.</param>
    /// <param name="style">The style factory.</param>
    /// <returns>The same instance for chaining.</returns>
    public static T Style<T, TStyle>(this T obj, Func<TStyle> style) where T : Visual where TStyle : IStyle<TStyle>
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(style);
        obj.VerifyAccess();
        obj.SetStyle(style);
        return obj;
    }

    /// <summary>
    /// Applies a style binding to a visual by storing it in the visual environment and returns the same instance.
    /// </summary>
    /// <typeparam name="T">The visual type.</typeparam>
    /// <typeparam name="TStyle">The style type.</typeparam>
    /// <param name="obj">The visual to style.</param>
    /// <param name="style">The style binding.</param>
    /// <returns>The same instance for chaining.</returns>
    public static T Style<T, TStyle>(this T obj, Binding<TStyle> style) where T : Visual where TStyle : IStyle<TStyle>
    {
        ArgumentNullException.ThrowIfNull(obj);
        if (style.IsEmpty)
        {
            throw new ArgumentException("The style binding cannot be empty.", nameof(style));
        }
        obj.VerifyAccess();
        obj.SetStyle(style);
        return obj;
    }

    /// <summary>
    /// Applies a style state to a visual by storing it in the visual environment and returns the same instance.
    /// </summary>
    /// <typeparam name="T">The visual type.</typeparam>
    /// <typeparam name="TStyle">The style type.</typeparam>
    /// <param name="obj">The visual to style.</param>
    /// <param name="style">The state that provides style values.</param>
    /// <returns>The same instance for chaining.</returns>
    public static T Style<T, TStyle>(this T obj, State<TStyle> style) where T : Visual where TStyle : IStyle<TStyle>
    {
        ArgumentNullException.ThrowIfNull(style);
        return obj.Style((Binding<TStyle>)style);
    }
}
