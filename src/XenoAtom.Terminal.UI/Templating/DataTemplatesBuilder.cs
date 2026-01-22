// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using XenoAtom.Terminal.UI.Controls;

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
        }
        else
        {
            (Display ??= new Dictionary<Type, object>())[type] = template;
        }
        return this;
    }

    /// <summary>
    /// Registers default templates for an enum type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default editor template uses a <see cref="Select{T}"/> populated with <see cref="Enum.GetValues{TEnum}"/>, avoiding
    /// text parsing and providing a better UX for enum selection.
    /// </para>
    /// <para>
    /// When a template argument is empty, a default implementation is used.
    /// </para>
    /// </remarks>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="displayTemplate">Optional display template.</param>
    /// <param name="editorTemplate">Optional editor template.</param>
    /// <param name="itemTemplate">Optional item template used by the editor dropdown.</param>
    /// <returns>This builder.</returns>
    public DataTemplatesBuilder RegisterEnum<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum>(
        DataTemplate<TEnum> displayTemplate = default,
        DataTemplate<TEnum> editorTemplate = default,
        DataTemplate<TEnum> itemTemplate = default)
        where TEnum : struct, Enum
    {
        if (displayTemplate.IsEmpty)
        {
            displayTemplate = new DataTemplate<TEnum>(static (Binding<TEnum> binding, in DataTemplateContext context) =>
            {
                var owner = context.Owner;
                return new TextBlock(() => owner.ToStringObject(binding.GetValue()));
            });
        }

        if (itemTemplate.IsEmpty)
        {
            itemTemplate = displayTemplate;
        }

        if (editorTemplate.IsEmpty)
        {
            editorTemplate = new DataTemplate<TEnum>(new EnumEditorFactory<TEnum>(itemTemplate).Create);
        }

        Register(DataTemplateRole.Display, displayTemplate);
        Register(DataTemplateRole.Editor, editorTemplate);
        return this;
    }

    private sealed class EnumEditorFactory<TEnum>(DataTemplate<TEnum> itemTemplate) where TEnum : struct, Enum
    {
        public Visual Create(Binding<TEnum> binding, in DataTemplateContext context)
        {
            _ = context;
            var select = new EnumSelect<TEnum>().Value(binding);
            if (!itemTemplate.IsEmpty)
            {
                select.ItemTemplate = itemTemplate;
            }
            return select;
        }
    }
}
