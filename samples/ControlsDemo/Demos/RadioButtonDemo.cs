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

        var a = new RadioButton("Choice A").GroupBy(group).IsChecked(true);
        var b = new RadioButton("Choice B").GroupBy(group);
        var c = new RadioButton("Choice C").GroupBy(group);

        string Selected()
        {
            if (a.IsChecked) return "A";
            if (b.IsChecked) return "B";
            if (c.IsChecked) return "C";
            return "<none>";
        }

        var longGroup = new object();
        var longStack = new VStack().Spacing(0);
        for (var i = 0; i < 30; i++)
        {
            longStack.Children.Add(new RadioButton($"Option {i:00}").GroupBy(longGroup));
        }

        return new VStack(
                DemoUi.Hint("Only one item can be selected per group."),
                a,
                b,
                c,
                new TextBlock(() => $"Selected: {Selected()}"),
                DemoUi.Hint("With many options, wrap a stack of radio buttons in a ScrollViewer."),
                new ScrollViewer(longStack).MinHeight(8).MaxHeight(8),
                new Button("Log selection").Click(() => context.Log($"Selected: {Selected()}")))
            .Spacing(1);
    }
}
