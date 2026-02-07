// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Presents a single data value using a resolved data template.
/// </summary>
/// <typeparam name="T">The type of data presented by this control.</typeparam>
public sealed partial class DataPresenter<T> : Visual
{
    private Visual? _content;
    private DataTemplateRole _lastRole;
    private DataTemplate<T> _lastResolvedTemplate;
    private bool _hasLastTemplate;
    private bool _isVisualContent;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataPresenter{T}"/> class.
    /// </summary>
    public DataPresenter()
    {
        Focusable = false;
        Role = DataTemplateRole.Display;
        Value = default!;
        _lastRole = Role;
        _lastResolvedTemplate = default;
        _hasLastTemplate = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataPresenter{T}"/> class with an initial value.
    /// </summary>
    /// <param name="value">The initial value to present.</param>
    public DataPresenter(T value) : this()
    {
        this.Value(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataPresenter{T}"/> class with a dynamic value provider.
    /// </summary>
    /// <param name="value">A delegate that provides the value to present.</param>
    public DataPresenter(Func<T> value) : this()
    {
        this.Value(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataPresenter{T}"/> class bound to a value binding.
    /// </summary>
    /// <param name="value">The binding that provides the value to present.</param>
    public DataPresenter(Binding<T> value) : this()
    {
        this.BindValue(value);
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

    /// <inheritdoc />
    protected override int ChildrenCount => EnsureContent() is null ? 0 : 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index)
    {
        var content = EnsureContent();
        return index == 0 && content is not null ? content : throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var content = EnsureContent();
        return content?.Measure(constraints) ?? SizeHints.Fixed(Size.Zero);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        EnsureContent()?.Arrange(finalRect);
    }
    
    private Visual? EnsureContent()
    {
        var owner = (Visual)this;
        var role = Role;
        var binding = this.Bind.Value;
        var value = binding.GetValue();

        var ctx = new DataTemplateContext(owner, role, -1, DataTemplateItemState.None);

        // Special-case Visual values: present the visual directly (no templating). This is the common "already a Visual" scenario.
        if (value is Visual visual)
        {
            if (!_isVisualContent || !ReferenceEquals(_content, visual))
            {
                if (_content is not null)
                {
                    DetachChild(_content);
                }

                _content = visual;
                AttachChild(_content);
                _isVisualContent = true;
            }

            _hasLastTemplate = false;
            return _content;
        }

        _isVisualContent = false;

        var template = Template;
        if (template.IsEmpty)
        {
            var templates = GetStyle<DataTemplates>();
            templates.TryResolve(role, out template);
        }

        if (_content is not null && _hasLastTemplate && _lastRole == role && _lastResolvedTemplate.Equals(template))
        {
            return _content;
        }

        _lastRole = role;
        _lastResolvedTemplate = template;
        _hasLastTemplate = true;

        if (_content is not null)
        {
            DetachChild(_content);
            _content = null;
        }

        if (role == DataTemplateRole.Editor)
        {
            _content = template.IsEmpty || template.Editor is null
                ? new TextBlock(() => owner.ToStringObject(binding.GetValue()))
                : template.Editor(binding, ctx);
        }
        else
        {
            var displayValue = new DataTemplateValue<T>(binding);
            _content = template.IsEmpty || template.Display is null
                ? new TextBlock(() => owner.ToStringObject(displayValue.GetValue()))
                : template.Display(displayValue, ctx);
        }

        AttachChild(_content);
        return _content;
    }
}
