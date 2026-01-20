// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Presents a single data value using a resolved data template.
/// </summary>
/// <typeparam name="T">The type of data presented by this control.</typeparam>
public sealed partial class DataPresenter<T> : ContentVisual
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataPresenter{T}"/> class.
    /// </summary>
    public DataPresenter()
    {
        Focusable = false;
        Role = DataTemplateRole.Display;
        Value = default!;
        this.Update(_ => UpdateContent());
    }

    /// <summary>
    /// Gets or sets the value presented by this control.
    /// </summary>
    [Bindable]
    public partial T Value { get; set; }

    /// <summary>
    /// Gets or sets the template role used for template resolution.
    /// </summary>
    [Bindable]
    public partial DataTemplateRole Role { get; set; }

    /// <summary>
    /// Gets or sets an optional per-instance template override.
    /// </summary>
    /// <remarks>
    /// When this value is empty, templates are resolved from <see cref="DataTemplates"/> in the environment.
    /// </remarks>
    [Bindable]
    public partial DataTemplate<T> Template { get; set; }

    private void UpdateContent()
    {
        var owner = (Visual)this;
        var role = Role;
        var ctx = new DataTemplateContext(owner, role, -1, DataTemplateItemState.None);

        var value = Value;
        if (value is Visual visual)
        {
            Content = visual;
            return;
        }

        var content = Content;
        var template = Template;

        DataTemplate<object?> templateUntyped = default;
        var useUntyped = false;

        if (template.IsEmpty)
        {
            var templates = Get<DataTemplates>();
            if (!templates.TryResolve(role, out template))
            {
                // For reference types, attempt runtime-type resolution to support derived-type templates.
                if (!typeof(T).IsValueType && templates.TryResolveForValue(value, role, out templateUntyped, out _))
                {
                    useUntyped = true;
                }
            }
        }

        if (useUntyped)
        {
            if (content is not null && templateUntyped.TryUpdate is { } updater && updater(content, value, ctx))
            {
                return;
            }

            var create = templateUntyped.Create;
            Content = create is null ? new TextBlock(value?.ToString() ?? string.Empty) : create(value, ctx);
            return;
        }

        if (template.IsEmpty)
        {
            Content = new TextBlock(value?.ToString() ?? string.Empty);
            return;
        }

        if (content is not null && template.TryUpdate is { } typedUpdater && typedUpdater(content, value, ctx))
        {
            return;
        }

        var typedCreate = template.Create;
        Content = typedCreate is null ? new TextBlock(value?.ToString() ?? string.Empty) : typedCreate(value, ctx);
    }
}
