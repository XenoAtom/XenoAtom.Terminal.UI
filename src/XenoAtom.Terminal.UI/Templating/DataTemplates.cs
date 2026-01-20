// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Templating;

/// <summary>
/// Provides an environment-scoped registry of data templates used to convert data values into visuals.
/// </summary>
/// <remarks>
/// <para>
/// The registry is immutable and is typically overridden by creating a derived registry and setting it in a subtree via
/// <see cref="Visual.Set{T}(T)"/>.
/// </para>
/// <para>
/// A registry can be layered by setting <see cref="Parent"/>; resolution checks the current registry first and then
/// walks the parent chain.
/// </para>
/// </remarks>
public sealed record DataTemplates : IStyle<DataTemplates>
{
    private readonly Dictionary<Type, object>? _display;
    private readonly Dictionary<Type, object>? _editor;
    private readonly Dictionary<Type, object>? _displayUntyped;
    private readonly Dictionary<Type, object>? _editorUntyped;

    private DataTemplates(Dictionary<Type, object>? display, Dictionary<Type, object>? editor, Dictionary<Type, object>? displayUntyped, Dictionary<Type, object>? editorUntyped, DataTemplates? parent)
    {
        _display = display;
        _editor = editor;
        _displayUntyped = displayUntyped;
        _editorUntyped = editorUntyped;
        Parent = parent;
    }

    /// <summary>
    /// Gets the default data templates.
    /// </summary>
    public static DataTemplates Default { get; } = CreateDefault();

    /// <summary>
    /// Gets the environment key for the <see cref="DataTemplates"/> registry.
    /// </summary>
    public static StyleKey<DataTemplates> Key { get; } = new(nameof(DataTemplates), Default);

    /// <summary>
    /// Gets an optional parent registry used for overlay chaining.
    /// </summary>
    public DataTemplates? Parent { get; init; }

    /// <summary>
    /// Creates a derived registry by applying registrations through a builder.
    /// </summary>
    /// <param name="configure">A function that registers templates on a builder and returns it.</param>
    /// <returns>A new registry whose <see cref="Parent"/> is the current registry.</returns>
    public DataTemplates Derive(Func<DataTemplatesBuilder, DataTemplatesBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new DataTemplatesBuilder();
        builder = configure(builder);
        ArgumentNullException.ThrowIfNull(builder);
        return new DataTemplates(builder.Display, builder.Editor, builder.DisplayUntyped, builder.EditorUntyped, this);
    }

    /// <summary>
    /// Creates a derived registry that overrides a single template.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    /// <param name="role">The template role.</param>
    /// <param name="template">The template.</param>
    /// <returns>A new registry whose <see cref="Parent"/> is the current registry.</returns>
    public DataTemplates Register<T>(DataTemplateRole role, DataTemplate<T> template)
    {
        if (template.IsEmpty)
        {
            throw new ArgumentException("Template must not be empty.", nameof(template));
        }

        var type = typeof(T);
        if (role == DataTemplateRole.Editor)
        {
            var editor = new Dictionary<Type, object>(1) { [type] = template };
            var editorUntyped = new Dictionary<Type, object>(1) { [type] = ToUntyped(template) };
            return new DataTemplates(null, editor, null, editorUntyped, this);
        }
        else
        {
            var display = new Dictionary<Type, object>(1) { [type] = template };
            var displayUntyped = new Dictionary<Type, object>(1) { [type] = ToUntyped(template) };
            return new DataTemplates(display, null, displayUntyped, null, this);
        }
    }

    /// <summary>
    /// Attempts to resolve a template for the specified role and data type.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    /// <param name="role">The template role.</param>
    /// <param name="template">The resolved template.</param>
    /// <returns><see langword="true"/> if a template was found; otherwise <see langword="false"/>.</returns>
    public bool TryResolve<T>(DataTemplateRole role, out DataTemplate<T> template)
    {
        for (var r = this; r is not null; r = r.Parent)
        {
            var dict = role == DataTemplateRole.Editor ? r._editor : r._display;
            if (dict is not null && dict.TryGetValue(typeof(T), out var boxed) && boxed is DataTemplate<T> typed)
            {
                template = typed;
                return true;
            }
        }

        template = default;
        return false;
    }

    /// <summary>
    /// Attempts to resolve a template for the runtime type of <paramref name="value"/>.
    /// </summary>
    /// <remarks>
    /// This method is intended for heterogeneous values and reference-type scenarios. It may box value types.
    /// </remarks>
    /// <param name="value">The value to resolve for.</param>
    /// <param name="role">The template role.</param>
    /// <param name="template">The resolved template.</param>
    /// <param name="resolvedDataType">The data type that matched the resolved template.</param>
    /// <returns><see langword="true"/> if a template was found; otherwise <see langword="false"/>.</returns>
    public bool TryResolveForValue(object? value, DataTemplateRole role, out DataTemplate<object?> template, [NotNullWhen(true)] out Type? resolvedDataType)
    {
        var valueType = value?.GetType();
        if (valueType is null)
        {
            template = default;
            resolvedDataType = null;
            return false;
        }

        for (var r = this; r is not null; r = r.Parent)
        {
            var dict = role == DataTemplateRole.Editor ? r._editorUntyped : r._displayUntyped;
            if (dict is null)
            {
                continue;
            }

            if (TryResolveInDictionary(valueType, dict, out template, out resolvedDataType))
            {
                return true;
            }
        }

        template = default;
        resolvedDataType = null;
        return false;
    }

    private static bool TryResolveInDictionary(Type valueType, Dictionary<Type, object> dict, out DataTemplate<object?> template, [NotNullWhen(true)] out Type? resolvedDataType)
    {
        if (dict.TryGetValue(valueType, out var boxed) && boxed is DataTemplate<object?> t0)
        {
            template = t0;
            resolvedDataType = valueType;
            return true;
        }

        for (var baseType = valueType.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (dict.TryGetValue(baseType, out boxed) && boxed is DataTemplate<object?> tb)
            {
                template = tb;
                resolvedDataType = baseType;
                return true;
            }
        }

        template = default;
        resolvedDataType = null;
        return false;
    }

    private static DataTemplates CreateDefault()
    {
        var display = new Dictionary<Type, object>();
        var displayUntyped = new Dictionary<Type, object>();

        static Visual DisplayString(string? value, in DataTemplateContext _) => new TextBlock(value ?? string.Empty);
        var stringTemplate = new DataTemplate<string?>(DisplayString);
        display[typeof(string)] = stringTemplate;
        displayUntyped[typeof(string)] = ToUntyped(stringTemplate);

        static Visual DisplayBool(bool value, in DataTemplateContext _) => new TextBlock(value ? "True" : "False");
        var boolTemplate = new DataTemplate<bool>(DisplayBool);
        display[typeof(bool)] = boolTemplate;
        displayUntyped[typeof(bool)] = ToUntyped(boolTemplate);

        static Visual DisplayVisual(Visual value, in DataTemplateContext _) => value;
        var visualTemplate = new DataTemplate<Visual>(DisplayVisual);
        display[typeof(Visual)] = visualTemplate;
        displayUntyped[typeof(Visual)] = ToUntyped(visualTemplate);

        return new DataTemplates(display, null, displayUntyped, null, null);
    }

    internal static DataTemplate<object?> ToUntyped<T>(DataTemplate<T> template)
    {
        if (template.IsEmpty)
        {
            return default;
        }

        var create = template.Create;
        if (create is null)
        {
            return default;
        }

        DataTemplateFactory<object?> createUntyped = (object? value, in DataTemplateContext context) => create((T)value!, context);

        DataTemplateUpdater<object?>? updateUntyped = null;
        if (template.TryUpdate is { } tryUpdate)
        {
            updateUntyped = (Visual visual, object? value, in DataTemplateContext context) => tryUpdate(visual, (T)value!, context);
        }

        return new DataTemplate<object?>(createUntyped, updateUntyped, template.Release);
    }
}
