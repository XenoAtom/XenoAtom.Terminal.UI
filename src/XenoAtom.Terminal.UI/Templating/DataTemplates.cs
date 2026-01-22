// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

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
/// <see cref="Visual.SetStyle{T}"/>.
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

    private DataTemplates(Dictionary<Type, object>? display, Dictionary<Type, object>? editor, DataTemplates? parent)
    {
        _display = display;
        _editor = editor;
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
        return new DataTemplates(builder.Display, builder.Editor, this);
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
            return new DataTemplates(null, editor, this);
        }
        else
        {
            var display = new Dictionary<Type, object>(1) { [type] = template };
            return new DataTemplates(display, null, this);
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

    private static DataTemplates CreateDefault()
    {
        var display = new Dictionary<Type, object>();
        var editor = new Dictionary<Type, object>();

        static void RegisterDisplay<T>(Dictionary<Type, object> table, DataTemplate<T> template)
        {
            table[typeof(T)] = template;
        }

        static void RegisterEditor<T>(Dictionary<Type, object> table, DataTemplate<T> template)
        {
            table[typeof(T)] = template;
        }

        static Visual EditBindingNullableString(Binding<string?> binding, in DataTemplateContext _) => new TextBox().Text(binding);

        static Visual EditBindingInt32(Binding<int> binding, in DataTemplateContext _) => new NumberBox<int>().Value(binding);

        static Visual EditBindingBool(Binding<bool> binding, in DataTemplateContext _) => new Switch().IsOn(binding);

        static Visual DisplayString(Binding<string?> binding, in DataTemplateContext _) => new TextBlock(() => binding.GetValue() ?? string.Empty);
        RegisterDisplay(display, new DataTemplate<string?>(DisplayString));
        RegisterEditor(editor, new DataTemplate<string?>(EditBindingNullableString));

        static Visual DisplayBool(Binding<bool> binding, in DataTemplateContext _) => new TextBlock(() => binding.GetValue() ? "True" : "False");
        RegisterDisplay(display, new DataTemplate<bool>(DisplayBool));
        RegisterEditor(editor, new DataTemplate<bool>(EditBindingBool));

        static Visual DisplayInt32(Binding<int> binding, in DataTemplateContext context)
        {
            var owner = context.Owner;
            return new TextBlock(() => owner.ToStringValue(binding.GetValue()));
        }
        RegisterDisplay(display, new DataTemplate<int>(DisplayInt32));
        RegisterEditor(editor, new DataTemplate<int>(EditBindingInt32));

        static Visual DisplayInt64(Binding<long> binding, in DataTemplateContext context)
        {
            var owner = context.Owner;
            return new TextBlock(() => owner.ToStringValue(binding.GetValue()));
        }
        RegisterDisplay(display, new DataTemplate<long>(DisplayInt64));

        static Visual DisplayDouble(Binding<double> binding, in DataTemplateContext context)
        {
            var owner = context.Owner;
            return new TextBlock(() => owner.ToStringValue(binding.GetValue()));
        }
        RegisterDisplay(display, new DataTemplate<double>(DisplayDouble));

        static Visual DisplayDecimal(Binding<decimal> binding, in DataTemplateContext context)
        {
            var owner = context.Owner;
            return new TextBlock(() => owner.ToStringValue(binding.GetValue()));
        }
        RegisterDisplay(display, new DataTemplate<decimal>(DisplayDecimal));

        return new DataTemplates(display, editor, null);
    }
}
