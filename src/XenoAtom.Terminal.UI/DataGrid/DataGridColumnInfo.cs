// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.DataGrid;

/// <summary>
/// Describes a column from a document/view snapshot.
/// </summary>
/// <param name="Key">The stable column key.</param>
/// <param name="HeaderText">The header text.</param>
/// <param name="ValueType">The optional CLR type of values in the column.</param>
/// <param name="ReadOnly">Whether the underlying data source is read-only for this column.</param>
/// <param name="Accessor">The accessor used to create bindings against a row model.</param>
public readonly record struct DataGridColumnInfo(
    string Key,
    string HeaderText,
    Type? ValueType,
    bool ReadOnly,
    BindingAccessor Accessor);

/// <summary>
/// Describes a column from a document/view snapshot with a typed accessor.
/// </summary>
/// <typeparam name="T">The CLR type of values in the column.</typeparam>
/// <param name="Key">The stable column key.</param>
/// <param name="HeaderText">The header text.</param>
/// <param name="ReadOnly">Whether the underlying data source is read-only for this column.</param>
/// <param name="Accessor">The typed accessor used to create bindings against a row model.</param>
public readonly record struct DataGridColumnInfo<T>(
    string Key,
    string HeaderText,
    bool ReadOnly,
    BindingAccessor<T> Accessor)
{
    /// <summary>
    /// Converts a typed column description to an untyped <see cref="DataGridColumnInfo"/>.
    /// </summary>
    /// <param name="info">The typed column description.</param>
    public static implicit operator DataGridColumnInfo(DataGridColumnInfo<T> info)
        => new(info.Key, info.HeaderText, typeof(T), info.ReadOnly, info.Accessor);

    /// <summary>
    /// Creates a <see cref="DataGridColumnInfo{T}"/> from a <see cref="BindingAccessor{T}"/>.
    /// </summary>
    /// <param name="accessor">The accessor.</param>
    public static implicit operator DataGridColumnInfo<T>(BindingAccessor<T> accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        return new DataGridColumnInfo<T>(
            accessor.Name,
            accessor.Name,
            accessor.IsReadOnly,
            accessor);
    }
}
