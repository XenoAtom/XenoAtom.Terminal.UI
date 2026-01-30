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

