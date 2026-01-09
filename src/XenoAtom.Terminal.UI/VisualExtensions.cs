// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI;

public static partial class VisualExtensions
{
    public static T Update<T>(this T obj, Action<T> configure) where T : Visual
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(configure);
        obj.RegisterDynamicUpdate(x => configure((T)x));
        return obj;
    }

    [Obsolete("Use Update(...) for dynamic updates.")]
    public static T With<T>(this T obj, Action<T> configure) where T : Visual
        => Update(obj, configure);

    public static T Add<T>(this T obj, params Visual[] visuals) where T : Panel
    {
        ArgumentNullException.ThrowIfNull(obj);
        obj.VerifyAccess();
        obj.AddRange(visuals);
        return obj;
    }

    public static ListBox Items(this ListBox obj, params Visual[] items)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(items);
        obj.VerifyAccess();
        obj.Items.Clear();
        obj.Items.AddRange(items);
        return obj;
    }

    public static Table AddHeader(this Table obj, Visual header)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(header);
        obj.VerifyAccess();
        obj.HeaderCells.Add(header);
        return obj;
    }

    public static Table Headers(this Table obj, params Visual[] headers)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(headers);
        obj.VerifyAccess();
        obj.HeaderCells.Clear();
        obj.HeaderCells.AddRange(headers);
        return obj;
    }

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

    public static T Style<T, TStyle>(this T obj, TStyle style) where T : Visual where TStyle : IStyle<TStyle>
    {
        ArgumentNullException.ThrowIfNull(obj);
        obj.VerifyAccess();
        obj.Set(style);
        return obj;
    }
}
