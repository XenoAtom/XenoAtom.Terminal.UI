using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Select (Dropdown)", "Input", Description = "Compact single-choice input with popup behavior.")]
public sealed class SelectDemo : ControlsDemoBase
{
    public SelectDemo() : base(DemoSource.Get())
    {
    }

    private enum SizeMode
    {
        Small,
        Medium,
        Large,
    }

    public override Visual Build(DemoContext context)
    {
        var selected = new State<int>(0);
        var select = new Select<string>()
            .Items(["First", "Second", "Third", "Fourth", "Fifth"])
            .SelectedIndex(selected);

        var enumValue = new State<SizeMode>(SizeMode.Medium);
        var enumSelect = new EnumSelect<SizeMode>().Value(enumValue);

        return new VStack(
                DemoUi.Hint("Select opens a popup; click outside, press Tab or Esc to close."),
                select,
                new TextBlock(() => $"SelectedIndex: {selected.Value}"),
                new Button("Log selection").Click(() => context.Log($"SelectedIndex: {selected.Value}")),
                new Rule(),
                DemoUi.Title("EnumSelect<TEnum>"),
                DemoUi.Hint("EnumSelect is a Select specialized for enums, mapping SelectedIndex to a bindable Value."),
                enumSelect,
                new TextBlock(() => $"Enum value: {enumValue.Value}"))
            .Spacing(1);
    }
}
