// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;
using System.Globalization;
using System.Numerics;

namespace XenoAtom.Terminal.UI.Templating;

/// <summary>
/// Provides an environment-scoped registry of data templates used to convert data values into visuals.
/// </summary>
/// <remarks>
/// <para>
/// The registry is immutable and is typically overridden by creating a derived registry and setting it in a subtree via
/// <see cref="Visual.SetStyle{T}(T)"/>.
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

        if (role == DataTemplateRole.Editor && !template.HasEditor)
        {
            throw new ArgumentException("Template must provide an editor factory for editor registrations.", nameof(template));
        }

        if (role == DataTemplateRole.Display && !template.HasDisplay)
        {
            throw new ArgumentException("Template must provide a display factory for display registrations.", nameof(template));
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
    /// Creates a derived registry that registers default templates for an enum type.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="displayTemplate">Optional display template.</param>
    /// <param name="editorTemplate">Optional editor template.</param>
    /// <param name="itemTemplate">Optional item template used by the editor dropdown.</param>
    /// <returns>A new registry whose <see cref="Parent"/> is the current registry.</returns>
    public DataTemplates RegisterEnum<TEnum>(DataTemplate<TEnum> displayTemplate = default, DataTemplate<TEnum> editorTemplate = default, DataTemplate<TEnum> itemTemplate = default)
        where TEnum : struct, Enum
        => Derive(builder => builder.RegisterEnum(displayTemplate, editorTemplate, itemTemplate));

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

        if (role == DataTemplateRole.Editor && typeof(T).IsEnum)
        {
            template = new DataTemplate<T>(
                Display: null,
                Editor: static (Binding<T> binding, in DataTemplateContext context) => new EnumEditorTextBox<T>(binding, context.Owner));
            return true;
        }

        template = default;
        return false;
    }

    private static DataTemplates CreateDefault()
    {
        var display = new Dictionary<Type, object>();
        var editor = new Dictionary<Type, object>();

        static void RegisterDisplay<T>(Dictionary<Type, object> table, DataTemplate<T> template) => table[typeof(T)] = template;

        static void RegisterEditor<T>(Dictionary<Type, object> table, DataTemplate<T> template) => table[typeof(T)] = template;

        static DataTemplate<T> DisplayOnly<T>(DataTemplateDisplayFactory<T> factory) => new(factory, null);

        static DataTemplate<T> EditorOnly<T>(DataTemplateEditorFactory<T> factory) => new(null, factory);

        static Visual DisplayNullableString(DataTemplateValue<string?> value, in DataTemplateContext _)
            => new TextBlock(() => value.GetValue() ?? string.Empty);

        static Visual DisplayString(DataTemplateValue<string> value, in DataTemplateContext _)
            => new TextBlock(() => value.GetValue());

        static Visual EditBindingNullableString(Binding<string?> binding, in DataTemplateContext _)
            => new TextBox().Text(binding);

        static Visual EditBindingString(Binding<string> binding, in DataTemplateContext _)
            => new TextBox().Text(AsNullable(binding));

        static Binding<string?> AsNullable(Binding<string> binding)
            // The binding points to a string instance at runtime. We surface it as nullable to match TextBox.Text.
            => new(binding.Owner, (BindingAccessor<string?>)(object)binding.Accessor);

        static Visual DisplayBool(DataTemplateValue<bool> value, in DataTemplateContext _)
            => new CheckBox(value.GetBinding()).IsEnabled(false);

        static Visual EditBindingBool(Binding<bool> binding, in DataTemplateContext _)
            => new CheckBox(binding);

        static Visual DisplayFormattable<T>(DataTemplateValue<T> value, in DataTemplateContext context)
        {
            var owner = context.Owner;
            return new TextBlock(() => owner.ToStringValue(value.GetValue()));
        }

        static Visual EditBindingNumber<T>(Binding<T> binding, in DataTemplateContext context) where T : struct, INumber<T>
        {
            _ = context;
            var box = new NumberBox<T>().Value(binding);

            // Prime the binding so the editor text reflects the current value immediately.
            // Otherwise, the NumberBox might render the default value until something reads Value.
            _ = box.Value;

            return box;
        }

        static Visual EditBindingChar(Binding<char> binding, in DataTemplateContext _)
            => new BoundTextBox<char>(binding,
                static (char value, CultureInfo _) => value.ToString(),
                static (string text, CultureInfo _, out char value) =>
                {
                    if (!string.IsNullOrEmpty(text) && text.Length == 1)
                    {
                        value = text[0];
                        return true;
                    }

                    value = default;
                    return false;
                });

        static Visual EditBindingGuid(Binding<Guid> binding, in DataTemplateContext _)
            => new BoundTextBox<Guid>(binding,
                static (Guid value, CultureInfo _) => value.ToString("D"),
                static (string text, CultureInfo _, out Guid value) => Guid.TryParse(text, out value));

        static Visual EditBindingDateOnly(Binding<DateOnly> binding, in DataTemplateContext _)
            => new BoundTextBox<DateOnly>(binding,
                static (DateOnly value, CultureInfo _) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                static (string text, CultureInfo culture, out DateOnly value)
                    => DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
                       || DateOnly.TryParse(text, culture, DateTimeStyles.None, out value));

        static Visual EditBindingTimeOnly(Binding<TimeOnly> binding, in DataTemplateContext _)
            => new BoundTextBox<TimeOnly>(binding,
                static (TimeOnly value, CultureInfo _) => value.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                static (string text, CultureInfo culture, out TimeOnly value)
                    => TimeOnly.TryParseExact(text, ["HH:mm:ss", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
                       || TimeOnly.TryParse(text, culture, DateTimeStyles.None, out value));

        static Visual EditBindingTimeSpan(Binding<TimeSpan> binding, in DataTemplateContext _)
            => new BoundTextBox<TimeSpan>(binding,
                static (TimeSpan value, CultureInfo _) => value.ToString("c", CultureInfo.InvariantCulture),
                static (string text, CultureInfo culture, out TimeSpan value)
                    => TimeSpan.TryParseExact(text, "c", CultureInfo.InvariantCulture, out value)
                       || TimeSpan.TryParse(text, culture, out value));

        static Visual EditBindingDateTime(Binding<DateTime> binding, in DataTemplateContext _)
            => new BoundTextBox<DateTime>(binding,
                static (DateTime value, CultureInfo _) => value.ToString("O", CultureInfo.InvariantCulture),
                static (string text, CultureInfo culture, out DateTime value)
                    => DateTime.TryParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value)
                       || DateTime.TryParse(text, culture, DateTimeStyles.RoundtripKind, out value));

        static Visual EditBindingDateTimeOffset(Binding<DateTimeOffset> binding, in DataTemplateContext _)
            => new BoundTextBox<DateTimeOffset>(binding,
                static (DateTimeOffset value, CultureInfo _) => value.ToString("O", CultureInfo.InvariantCulture),
                static (string text, CultureInfo culture, out DateTimeOffset value)
                    => DateTimeOffset.TryParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value)
                       || DateTimeOffset.TryParse(text, culture, DateTimeStyles.RoundtripKind, out value));

        RegisterDisplay(display, DisplayOnly<string?>(DisplayNullableString));
        RegisterEditor(editor, EditorOnly<string?>(EditBindingNullableString));

        RegisterDisplay(display, DisplayOnly<string>(DisplayString));
        RegisterEditor(editor, EditorOnly<string>(EditBindingString));

        RegisterDisplay(display, DisplayOnly<bool>(DisplayBool));
        RegisterEditor(editor, EditorOnly<bool>(EditBindingBool));

        RegisterDisplay(display, DisplayOnly<char>(DisplayFormattable<char>));
        RegisterEditor(editor, EditorOnly<char>(EditBindingChar));

        RegisterDisplay(display, DisplayOnly<Guid>(DisplayFormattable<Guid>));
        RegisterEditor(editor, EditorOnly<Guid>(EditBindingGuid));

        RegisterDisplay(display, DisplayOnly<sbyte>(DisplayFormattable<sbyte>));
        RegisterEditor(editor, EditorOnly<sbyte>(EditBindingNumber<sbyte>));

        RegisterDisplay(display, DisplayOnly<byte>(DisplayFormattable<byte>));
        RegisterEditor(editor, EditorOnly<byte>(EditBindingNumber<byte>));

        RegisterDisplay(display, DisplayOnly<short>(DisplayFormattable<short>));
        RegisterEditor(editor, EditorOnly<short>(EditBindingNumber<short>));

        RegisterDisplay(display, DisplayOnly<ushort>(DisplayFormattable<ushort>));
        RegisterEditor(editor, EditorOnly<ushort>(EditBindingNumber<ushort>));

        RegisterDisplay(display, DisplayOnly<int>(DisplayFormattable<int>));
        RegisterEditor(editor, EditorOnly<int>(EditBindingNumber<int>));

        RegisterDisplay(display, DisplayOnly<uint>(DisplayFormattable<uint>));
        RegisterEditor(editor, EditorOnly<uint>(EditBindingNumber<uint>));

        RegisterDisplay(display, DisplayOnly<long>(DisplayFormattable<long>));
        RegisterEditor(editor, EditorOnly<long>(EditBindingNumber<long>));

        RegisterDisplay(display, DisplayOnly<ulong>(DisplayFormattable<ulong>));
        RegisterEditor(editor, EditorOnly<ulong>(EditBindingNumber<ulong>));

        RegisterDisplay(display, DisplayOnly<float>(DisplayFormattable<float>));
        RegisterEditor(editor, EditorOnly<float>(EditBindingNumber<float>));

        RegisterDisplay(display, DisplayOnly<double>(DisplayFormattable<double>));
        RegisterEditor(editor, EditorOnly<double>(EditBindingNumber<double>));

        RegisterDisplay(display, DisplayOnly<decimal>(DisplayFormattable<decimal>));
        RegisterEditor(editor, EditorOnly<decimal>(EditBindingNumber<decimal>));

        RegisterDisplay(display, DisplayOnly<DateOnly>(DisplayFormattable<DateOnly>));
        RegisterEditor(editor, EditorOnly<DateOnly>(EditBindingDateOnly));

        RegisterDisplay(display, DisplayOnly<TimeOnly>(DisplayFormattable<TimeOnly>));
        RegisterEditor(editor, EditorOnly<TimeOnly>(EditBindingTimeOnly));

        RegisterDisplay(display, DisplayOnly<TimeSpan>(DisplayFormattable<TimeSpan>));
        RegisterEditor(editor, EditorOnly<TimeSpan>(EditBindingTimeSpan));

        RegisterDisplay(display, DisplayOnly<DateTime>(DisplayFormattable<DateTime>));
        RegisterEditor(editor, EditorOnly<DateTime>(EditBindingDateTime));

        RegisterDisplay(display, DisplayOnly<DateTimeOffset>(DisplayFormattable<DateTimeOffset>));
        RegisterEditor(editor, EditorOnly<DateTimeOffset>(EditBindingDateTimeOffset));

        return new DataTemplates(display, editor, null);
    }

    private sealed class BoundTextBox<T> : TextBox
    {
        private readonly Binding<T> _binding;
        private readonly Func<T, CultureInfo, string> _format;
        private readonly TryParseWithCulture _tryParse;
        private bool _updatingFromBinding;

        public BoundTextBox(Binding<T> binding, Func<T, CultureInfo, string> format, TryParseWithCulture tryParse)
        {
            _binding = binding;
            _format = format;
            _tryParse = tryParse;

            TextDocument.Changed += OnTextDocumentChanged;

            // Initialize immediately (before first layout).
            SyncFromBinding(force: true);
        }

        protected override void PrepareChildren()
        {
            base.PrepareChildren();
            SyncFromBinding(force: false);
        }

        private void SyncFromBinding(bool force)
        {
            if (_updatingFromBinding) return;

            // Avoid overwriting user edits while the editor is focused.
            if (!force && HasFocus)
            {
                return;
            }

            var culture = GetCulture();
            var value = _binding.GetValue();
            var formatted = _format(value, culture);

            _updatingFromBinding = true;
            try
            {
                Text = formatted;
            }
            finally
            {
                _updatingFromBinding = false;
            }
        }

        private void OnTextDocumentChanged(object? sender, TextDocumentChangedEventArgs e)
        {
            _ = sender;
            _ = e;
            if (_updatingFromBinding)
            {
                return;
            }

            var text = Text ?? string.Empty;
            var culture = GetCulture();
            if (_tryParse(text, culture, out var parsed))
            {
                _binding.SetValue(parsed);
            }
        }

        public delegate bool TryParseWithCulture(string text, CultureInfo culture, out T value);
    }

    private sealed class EnumEditorTextBox<T> : TextBox
    {
        private readonly Binding<T> _binding;
        private bool _updatingFromBinding;

        public EnumEditorTextBox(Binding<T> binding, Visual _)
        {
            _binding = binding;

            TextDocument.Changed += OnTextDocumentChanged;
            SyncFromBinding(force: true);
        }

        protected override void PrepareChildren()
        {
            base.PrepareChildren();

            SyncFromBinding(force: false);
        }

        private void SyncFromBinding(bool force)
        {
            if (_updatingFromBinding) return;

            if (!force && HasFocus)
            {
                return;
            }

            var value = _binding.GetValue();
            var formatted = value?.ToString() ?? string.Empty;

            _updatingFromBinding = true;
            try
            {
                Text = formatted;
            }
            finally
            {
                _updatingFromBinding = false;
            }
        }

        private void OnTextDocumentChanged(object? sender, TextDocumentChangedEventArgs e)
        {
            _ = sender;
            _ = e;
            if (_updatingFromBinding)
            {
                return;
            }

            var text = Text ?? string.Empty;
            if (Enum.TryParse(typeof(T), text, ignoreCase: true, out var parsed) && parsed is T typed)
            {
                _binding.SetValue(typed);
            }
        }
    }
}
