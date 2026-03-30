// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using XenoAtom.Terminal.UI.DataGrid;
using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Defines a column for <see cref="DataGridControl"/>.
/// </summary>
public abstract partial class DataGridColumn : IVisualElement
{
    private DataGridControl? _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridColumn"/> class.
    /// </summary>
    protected DataGridColumn()
    {
        Key = string.Empty;
        Width = GridLength.Star(1);
        MinWidth = 0;
        MaxWidth = int.MaxValue;
        Visible = true;
        HeaderAlignment = TextAlignment.Left;
        CellAlignment = TextAlignment.Left;
        ReadOnly = false;
    }

    /// <summary>
    /// Gets the owning <see cref="TerminalApp"/> if attached.
    /// </summary>
    /// <remarks>
    /// This property must not depend on bindable properties (e.g. <see cref="Header"/>) to avoid
    /// recursion when the binding system needs the owning app to register reads.
    /// </remarks>
    public TerminalApp? App => _owner?.App;

    /// <summary>
    /// Gets or sets the stable column key.
    /// </summary>
    [Bindable]
    public partial string Key { get; set; }

    /// <summary>
    /// Gets or sets the header visual for this column.
    /// </summary>
    [Bindable]
    public partial Visual? Header { get; set; }

    /// <summary>
    /// Gets or sets the width settings for this column.
    /// </summary>
    [Bindable]
    public partial GridLength Width { get; set; }

    /// <summary>
    /// Gets or sets the minimum width for this column.
    /// </summary>
    [Bindable]
    public partial int MinWidth { get; set; }

    /// <summary>
    /// Gets or sets the maximum width for this column.
    /// </summary>
    [Bindable]
    public partial int MaxWidth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is visible.
    /// </summary>
    [Bindable]
    public partial bool Visible { get; set; }

    /// <summary>
    /// Gets or sets the text alignment used when the header is rendered as text.
    /// </summary>
    [Bindable]
    public partial TextAlignment HeaderAlignment { get; set; }

    /// <summary>
    /// Gets or sets the default text alignment for cells in this column.
    /// </summary>
    /// <remarks>
    /// This is used by the fast rendering path and as a default when templates render plain text.
    /// Typical defaults: strings = <see cref="TextAlignment.Left"/>, numbers = <see cref="TextAlignment.Right"/>.
    /// </remarks>
    [Bindable]
    public partial TextAlignment CellAlignment { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether cells in this column are read-only (not editable).
    /// </summary>
    /// <remarks>
    /// This is a UI-level affordance that SHOULD be combined with schema/data-source read-only information
    /// (e.g. <see cref="DataGridColumnInfo.ReadOnly"/>) to compute an effective read-only state.
    /// </remarks>
    [Bindable]
    public partial bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this column participates in sorting.
    /// </summary>
    /// <remarks>
    /// Sorting UI is only shown when the owning <see cref="DataGridControl"/> is bound to a sortable view.
    /// </remarks>
    [Bindable]
    public partial bool Sortable { get; set; }

    /// <summary>
    /// Gets the CLR type of the cell values in this column.
    /// </summary>
    public abstract Type ValueType { get; }

    /// <summary>
    /// Gets the accessor used to create bindings against a row model for this column.
    /// </summary>
    public abstract BindingAccessor ValueAccessor { get; }

    partial void OnKeyChanged(string value)
    {
        _ = value;
        NotifySortConfigurationChanged();
    }

    partial void OnSortableChanged(bool value)
    {
        _ = value;
        NotifySortConfigurationChanged();
    }

    internal void Attach(DataGridControl owner) => _owner = owner;

    internal void Detach(DataGridControl owner)
    {
        if (ReferenceEquals(_owner, owner))
        {
            _owner = null;
        }
    }

    internal void NotifySortConfigurationChanged() => _owner?.OnColumnSortConfigurationChanged();

    internal abstract string FormatValue(Visual owner, object rowModel, CultureInfo culture);

    internal virtual IComparer<object?>? CreateSortComparer() => null;

    internal abstract bool HasDisplayTemplate(Visual owner, bool effectiveReadOnly);

    internal abstract Visual CreateOrUpdateDisplayVisual(
        Visual owner,
        object rowModel,
        in DataTemplateContext context,
        bool effectiveReadOnly,
        Visual? reused,
        out bool updated);

    internal abstract bool TryCreateEditorVisual(
        Visual owner,
        object rowModel,
        in DataTemplateContext context,
        out Visual? editor);
}

/// <summary>
/// A typed column that carries typed templates and a typed accessor to avoid boxing for value types.
/// </summary>
/// <typeparam name="T">The cell value type.</typeparam>
public sealed partial class DataGridColumn<T> : DataGridColumn
{
    /// <summary>
    /// Gets or sets the accessor used to create <see cref="Binding{T}"/> instances for this column.
    /// </summary>
    [Bindable]
    public partial BindingAccessor<T> TypedValueAccessor { get; set; }

    /// <inheritdoc />
    public override Type ValueType => typeof(T);

    /// <inheritdoc />
    public override BindingAccessor ValueAccessor => TypedValueAccessor;

    /// <summary>
    /// Gets or sets the display template used for cells in this column.
    /// </summary>
    [Bindable]
    public partial DataTemplate<T> CellTemplate { get; set; }

    /// <summary>
    /// Gets or sets the display template used for read-only cells in this column.
    /// </summary>
    /// <remarks>
    /// When empty, the column falls back to <see cref="CellTemplate"/>.
    /// </remarks>
    [Bindable]
    public partial DataTemplate<T> ReadOnlyCellTemplate { get; set; }

    /// <summary>
    /// Gets or sets the editor template used when a cell enters edit mode.
    /// </summary>
    [Bindable]
    public partial DataTemplate<T> CellEditorTemplate { get; set; }

    /// <summary>
    /// Gets or sets the optional comparer used when this sortable column is sorted.
    /// </summary>
    /// <remarks>
    /// When <see cref="DataGridColumn.Sortable"/> is <see langword="true"/> and this property is not set,
    /// the grid uses <see cref="Comparer{T}.Default"/>.
    /// </remarks>
    [Bindable]
    public partial IComparer<T>? SortComparer { get; set; }

    private IComparer<object?>? _cachedSortComparer;
    private IComparer<T>? _cachedTypedSortComparer;
    private bool _cachedUsesDefaultComparer;

    partial void OnTypedValueAccessorChanging(ref BindingAccessor<T> value) => ArgumentNullException.ThrowIfNull(value);

    partial void OnSortComparerChanged(IComparer<T>? value)
    {
        _ = value;
        _cachedSortComparer = null;
        _cachedTypedSortComparer = null;
        _cachedUsesDefaultComparer = false;
        NotifySortConfigurationChanged();
    }

    internal override string FormatValue(Visual owner, object rowModel, CultureInfo culture)
    {
        _ = owner;
        _ = culture;
        var binding = new Binding<T>(rowModel, TypedValueAccessor);
        var value = binding.GetValue();
        if (value is null)
        {
            return string.Empty;
        }

        return value is string s ? s : value.ToString() ?? string.Empty;
    }

    internal override bool HasDisplayTemplate(Visual owner, bool effectiveReadOnly)
    {
        var template = ResolveDisplayTemplate(owner, effectiveReadOnly);
        return !template.IsEmpty && template.Display is not null;
    }

    internal override Visual CreateOrUpdateDisplayVisual(
        Visual owner,
        object rowModel,
        in DataTemplateContext context,
        bool effectiveReadOnly,
        Visual? reused,
        out bool updated)
    {
        var template = ResolveDisplayTemplate(owner, effectiveReadOnly);
        var binding = new Binding<T>(rowModel, TypedValueAccessor);
        var templateValue = new DataTemplateValue<T>(binding);

        if (reused is not null && template.TryUpdate is { } updater && updater(reused, templateValue, context))
        {
            updated = true;
            return reused;
        }

        if (reused is not null && template.Release is { } release)
        {
            release(reused);
        }

        updated = false;
        return template.IsEmpty || template.Display is null
            ? new TextBlock(() => owner.ToStringObject(templateValue.GetValue()))
            : template.Display(templateValue, context);
    }

    internal override bool TryCreateEditorVisual(Visual owner, object rowModel, in DataTemplateContext context, out Visual? editor)
    {
        var template = ResolveEditorTemplate(owner);
        var binding = new Binding<T>(rowModel, TypedValueAccessor);

        if (!template.IsEmpty && template.Editor is not null)
        {
            editor = template.Editor(binding, context);
            return true;
        }

        var templates = owner.GetStyle<DataTemplates>();
        if (templates.TryResolve<T>(DataTemplateRole.Editor, out var resolved) && !resolved.IsEmpty && resolved.Editor is not null)
        {
            editor = resolved.Editor(binding, context);
            return true;
        }

        editor = null;
        return false;
    }

    internal override IComparer<object?>? CreateSortComparer()
    {
        if (!Sortable)
        {
            return null;
        }

        var typedComparer = SortComparer;
        var usesDefaultComparer = typedComparer is null;
        if (_cachedSortComparer is not null &&
            ReferenceEquals(_cachedTypedSortComparer, typedComparer) &&
            _cachedUsesDefaultComparer == usesDefaultComparer)
        {
            return _cachedSortComparer;
        }

        var effectiveComparer = typedComparer ?? Comparer<T>.Default;
        _cachedTypedSortComparer = typedComparer;
        _cachedUsesDefaultComparer = usesDefaultComparer;
        _cachedSortComparer = new UntypedSortComparer(effectiveComparer);
        return _cachedSortComparer;
    }

    private DataTemplate<T> ResolveDisplayTemplate(Visual owner, bool effectiveReadOnly)
    {
        // DataGrid is optimized for fast rendering: it draws plain text directly unless a per-column
        // display template is explicitly provided. Bool is the notable built-in exception because its
        // default template is intentionally non-textual (checkbox glyphs).
        if (effectiveReadOnly)
        {
            if (!ReadOnlyCellTemplate.IsEmpty)
            {
                return ReadOnlyCellTemplate;
            }

            if (!CellTemplate.IsEmpty)
            {
                return CellTemplate;
            }

            if (typeof(T) != typeof(bool))
            {
                return default;
            }
        }
        else if (!CellTemplate.IsEmpty)
        {
            return CellTemplate;
        }

        if (typeof(T) == typeof(bool))
        {
            var templates = owner.GetStyle<DataTemplates>();
            if (templates.TryResolve<T>(DataTemplateRole.Display, out var template) && !template.IsEmpty && template.Display is not null)
            {
                return template;
            }
        }

        return default;
    }

    private DataTemplate<T> ResolveEditorTemplate(Visual owner)
    {
        var template = CellEditorTemplate;
        if (!template.IsEmpty)
        {
            return template;
        }

        var templates = owner.GetStyle<DataTemplates>();
        if (templates.TryResolve(DataTemplateRole.Editor, out template))
        {
            return template;
        }

        return default;
    }

    private sealed class UntypedSortComparer : IComparer<object?>
    {
        private readonly IComparer<T> _comparer;

        public UntypedSortComparer(IComparer<T> comparer)
        {
            _comparer = comparer;
        }

        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            return _comparer.Compare((T)x, (T)y);
        }
    }
}
