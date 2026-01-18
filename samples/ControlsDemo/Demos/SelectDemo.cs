using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Select (Dropdown)", "Input", Description = "Compact single-choice input with popup behavior.")]
public sealed class SelectDemo : ControlsDemoBase
{
    public SelectDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var selected = new State<int>(0);
        var select = new Select<string>()
            .Items(["First", "Second", "Third", "Fourth", "Fifth"])
            .SelectedIndex(selected);

        return new VStack(
                DemoUi.Hint("Select opens a popup; click outside, press Tab or Esc to close."),
                select,
                new TextBlock(() => $"SelectedIndex: {selected.Value}"),
                new Button("Log selection").Click(() => context.Log($"SelectedIndex: {selected.Value}")))
            .Spacing(1);
    }
}
