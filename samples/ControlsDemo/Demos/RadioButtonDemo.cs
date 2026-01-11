using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("RadioButton", "Input", Description = "Mutually exclusive selection with a shared group.")]
public sealed class RadioButtonDemo : ControlsDemoBase
{
    public RadioButtonDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        // Radio buttons are grouped by an arbitrary key object.
        var group = new object();

        var a = new RadioButton("Choice A").Group(group).IsChecked(true);
        var b = new RadioButton("Choice B").Group(group);
        var c = new RadioButton("Choice C").Group(group);

        string Selected()
        {
            if (a.IsChecked) return "A";
            if (b.IsChecked) return "B";
            if (c.IsChecked) return "C";
            return "<none>";
        }

        return new VStack(
                DemoUi.Hint("Only one item can be selected per group."),
                a,
                b,
                c,
                new TextBlock(() => $"Selected: {Selected()}"),
                new Button("Log selection").Click(() => context.Log($"Selected: {Selected()}")))
            .Spacing(1);
    }
}

