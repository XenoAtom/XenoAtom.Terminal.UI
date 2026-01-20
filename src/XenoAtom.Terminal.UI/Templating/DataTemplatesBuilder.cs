// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Templating;

/// <summary>
/// Builder used to create an overlay <see cref="DataTemplates"/> registry.
/// </summary>
/// <remarks>
/// This type exists to allow registering multiple templates without repeatedly copying internal tables.
/// The builder mutates temporary tables, and the resulting registry is immutable.
/// </remarks>
public sealed class DataTemplatesBuilder
{
    internal DataTemplatesBuilder()
    {
    }

    internal Dictionary<Type, object>? Display { get; private set; }

    internal Dictionary<Type, object>? Editor { get; private set; }

    internal Dictionary<Type, object>? DisplayUntyped { get; private set; }

    internal Dictionary<Type, object>? EditorUntyped { get; private set; }

    /// <summary>
    /// Registers a template for the specified role and data type.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    /// <param name="role">The template role.</param>
    /// <param name="template">The template.</param>
    /// <returns>This builder.</returns>
    public DataTemplatesBuilder Register<T>(DataTemplateRole role, DataTemplate<T> template)
    {
        if (template.IsEmpty)
        {
            throw new ArgumentException("Template must not be empty.", nameof(template));
        }

        var type = typeof(T);
        if (role == DataTemplateRole.Editor)
        {
            (Editor ??= new Dictionary<Type, object>())[type] = template;
            (EditorUntyped ??= new Dictionary<Type, object>())[type] = DataTemplates.ToUntyped(template);
        }
        else
        {
            (Display ??= new Dictionary<Type, object>())[type] = template;
            (DisplayUntyped ??= new Dictionary<Type, object>())[type] = DataTemplates.ToUntyped(template);
        }
        return this;
    }
}
