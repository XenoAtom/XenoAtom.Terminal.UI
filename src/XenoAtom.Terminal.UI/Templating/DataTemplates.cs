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

        Type? bestInterface = null;
        DataTemplate<object?> bestTemplate = default;

        foreach (var kvp in dict)
        {
            var candidateType = kvp.Key;
            if (!candidateType.IsInterface || !candidateType.IsAssignableFrom(valueType))
            {
                continue;
            }

            if (kvp.Value is not DataTemplate<object?> candidateTemplate)
            {
                continue;
            }

            if (bestInterface is null)
            {
                bestInterface = candidateType;
                bestTemplate = candidateTemplate;
                continue;
            }

            // Prefer the most specific interface: if A is assignable from B, then B is more specific than A.
            if (bestInterface.IsAssignableFrom(candidateType))
            {
                bestInterface = candidateType;
                bestTemplate = candidateTemplate;
                continue;
            }

            if (!candidateType.IsAssignableFrom(bestInterface))
            {
                // Unrelated interfaces: pick deterministically by name.
                var bestName = bestInterface.FullName ?? bestInterface.Name;
                var candidateName = candidateType.FullName ?? candidateType.Name;
                if (string.CompareOrdinal(candidateName, bestName) < 0)
                {
                    bestInterface = candidateType;
                    bestTemplate = candidateTemplate;
                }
            }
        }

        if (bestInterface is not null)
        {
            template = bestTemplate;
            resolvedDataType = bestInterface;
            return true;
        }

        template = default;
        resolvedDataType = null;
        return false;
    }

    private static DataTemplates CreateDefault()
    {
        var display = new Dictionary<Type, object>();
        var editor = new Dictionary<Type, object>();
        var displayUntyped = new Dictionary<Type, object>();
        var editorUntyped = new Dictionary<Type, object>();

        static void RegisterDisplay<T>(Dictionary<Type, object> table, Dictionary<Type, object> tableUntyped, DataTemplate<T> template)
        {
            table[typeof(T)] = template;
            tableUntyped[typeof(T)] = ToUntyped(template);
        }

        static void RegisterEditor<T>(Dictionary<Type, object> table, Dictionary<Type, object> tableUntyped, DataTemplate<T> template)
        {
            table[typeof(T)] = template;
            tableUntyped[typeof(T)] = ToUntyped(template);
        }

        static Visual DisplayString(string? value, in DataTemplateContext _) => new TextBlock(value ?? string.Empty);
        static bool TryUpdateString(Visual visual, string? value, in DataTemplateContext _)
        {
            if (visual is TextBlock textBlock)
            {
                textBlock.Text = value ?? string.Empty;
                return true;
            }

            return false;
        }
        var stringTemplate = new DataTemplate<string?>(DisplayString, TryUpdateString);
        RegisterDisplay(display, displayUntyped, stringTemplate);

        static Visual DisplayBool(bool value, in DataTemplateContext _) => new TextBlock(value ? "True" : "False");
        static bool TryUpdateBool(Visual visual, bool value, in DataTemplateContext _)
        {
            if (visual is TextBlock textBlock)
            {
                textBlock.Text = value ? "True" : "False";
                return true;
            }

            return false;
        }
        var boolTemplate = new DataTemplate<bool>(DisplayBool, TryUpdateBool);
        RegisterDisplay(display, displayUntyped, boolTemplate);

        static Visual DisplayVisual(Visual value, in DataTemplateContext _) => value;
        var visualTemplate = new DataTemplate<Visual>(DisplayVisual);
        RegisterDisplay(display, displayUntyped, visualTemplate);
        RegisterEditor(editor, editorUntyped, visualTemplate);

        static Visual DisplayInt32(int value, in DataTemplateContext _) => new TextBlock(value.ToString());
        static bool TryUpdateInt32(Visual visual, int value, in DataTemplateContext _)
        {
            if (visual is TextBlock textBlock)
            {
                textBlock.Text = value.ToString();
                return true;
            }
            return false;
        }
        RegisterDisplay(display, displayUntyped, new DataTemplate<int>(DisplayInt32, TryUpdateInt32));

        static Visual DisplayDouble(double value, in DataTemplateContext _) => new TextBlock(value.ToString());
        static bool TryUpdateDouble(Visual visual, double value, in DataTemplateContext _)
        {
            if (visual is TextBlock textBlock)
            {
                textBlock.Text = value.ToString();
                return true;
            }
            return false;
        }
        RegisterDisplay(display, displayUntyped, new DataTemplate<double>(DisplayDouble, TryUpdateDouble));

        static Visual DisplayDecimal(decimal value, in DataTemplateContext _) => new TextBlock(value.ToString());
        static bool TryUpdateDecimal(Visual visual, decimal value, in DataTemplateContext _)
        {
            if (visual is TextBlock textBlock)
            {
                textBlock.Text = value.ToString();
                return true;
            }
            return false;
        }
        RegisterDisplay(display, displayUntyped, new DataTemplate<decimal>(DisplayDecimal, TryUpdateDecimal));

        static Visual DisplayInt64(long value, in DataTemplateContext _) => new TextBlock(value.ToString());
        static bool TryUpdateInt64(Visual visual, long value, in DataTemplateContext _)
        {
            if (visual is TextBlock textBlock)
            {
                textBlock.Text = value.ToString();
                return true;
            }
            return false;
        }
        RegisterDisplay(display, displayUntyped, new DataTemplate<long>(DisplayInt64, TryUpdateInt64));

        static Visual DisplayStateString(State<string> state, in DataTemplateContext _) => new TextBlock(() => state.Value);
        RegisterDisplay(display, displayUntyped, new DataTemplate<State<string>>(DisplayStateString));

        static Visual DisplayStateNullableString(State<string?> state, in DataTemplateContext _) => new TextBlock(() => state.Value ?? string.Empty);
        RegisterDisplay(display, displayUntyped, new DataTemplate<State<string?>>(DisplayStateNullableString));

        static Visual DisplayStateInt32(State<int> state, in DataTemplateContext _) => new TextBlock(() => state.Value.ToString());
        RegisterDisplay(display, displayUntyped, new DataTemplate<State<int>>(DisplayStateInt32));

        static Visual DisplayStateBool(State<bool> state, in DataTemplateContext _) => new TextBlock(() => state.Value ? "True" : "False");
        RegisterDisplay(display, displayUntyped, new DataTemplate<State<bool>>(DisplayStateBool));

        static Visual DisplayBindingString(Binding<string> binding, in DataTemplateContext _) => new TextBlock(() => binding.GetValue());
        RegisterDisplay(display, displayUntyped, new DataTemplate<Binding<string>>(DisplayBindingString));

        static Visual DisplayBindingNullableString(Binding<string?> binding, in DataTemplateContext _) => new TextBlock(() => binding.GetValue() ?? string.Empty);
        RegisterDisplay(display, displayUntyped, new DataTemplate<Binding<string?>>(DisplayBindingNullableString));

        static Visual DisplayBindingInt32(Binding<int> binding, in DataTemplateContext _) => new TextBlock(() => binding.GetValue().ToString());
        RegisterDisplay(display, displayUntyped, new DataTemplate<Binding<int>>(DisplayBindingInt32));

        static Visual DisplayBindingBool(Binding<bool> binding, in DataTemplateContext _) => new TextBlock(() => binding.GetValue() ? "True" : "False");
        RegisterDisplay(display, displayUntyped, new DataTemplate<Binding<bool>>(DisplayBindingBool));

        static Visual EditStateNullableString(State<string?> state, in DataTemplateContext _) => new TextBox().Text(state);
        RegisterEditor(editor, editorUntyped, new DataTemplate<State<string?>>(EditStateNullableString));

        static Visual EditBindingNullableString(Binding<string?> binding, in DataTemplateContext _) => new TextBox().Text(binding);
        RegisterEditor(editor, editorUntyped, new DataTemplate<Binding<string?>>(EditBindingNullableString));

        static Visual EditStateInt32(State<int> state, in DataTemplateContext _) => new NumberBox<int>().Value(state);
        RegisterEditor(editor, editorUntyped, new DataTemplate<State<int>>(EditStateInt32));

        static Visual EditBindingInt32(Binding<int> binding, in DataTemplateContext _) => new NumberBox<int>().Value(binding);
        RegisterEditor(editor, editorUntyped, new DataTemplate<Binding<int>>(EditBindingInt32));

        static Visual EditStateBool(State<bool> state, in DataTemplateContext _) => new Switch().IsOn(state);
        RegisterEditor(editor, editorUntyped, new DataTemplate<State<bool>>(EditStateBool));

        static Visual EditBindingBool(Binding<bool> binding, in DataTemplateContext _) => new Switch().IsOn(binding);
        RegisterEditor(editor, editorUntyped, new DataTemplate<Binding<bool>>(EditBindingBool));

        return new DataTemplates(display, editor, displayUntyped, editorUntyped, null);
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
