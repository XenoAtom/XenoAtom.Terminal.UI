using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("ListBox", "Navigation", Description = "Simple single selection list.")]
public sealed class ListBoxDemo : ControlsDemoBase
{
    public ListBoxDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var selected = new State<int>(1);

        var list = new ListBox<string>().SelectedIndex(selected);
        list.Items.AddRange("First", "Second", "Third", "Fourth", "Fifth", "Sixth");

        return new VStack(
                DemoUi.Hint("ListBox supports keyboard navigation and a selected index."),
                list,
                new TextBlock(() => $"SelectedIndex: {selected.Value}"),
                new Button("Log selection").Click(() => context.Log($"SelectedIndex: {selected.Value}")))
            .Spacing(1);
    }
}
