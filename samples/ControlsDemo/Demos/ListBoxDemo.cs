using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("ListBox", "Input", Description = "Simple single selection list.")]
public sealed class ListBoxDemo : ControlsDemoBase
{
    public ListBoxDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        context.AllowPageScrollViewer = false;

        var selected = new State<int>(1);

        var list = new ListBox<string>().SelectedIndex(selected);
        list.Items.AddRange("First", "Second", "Third", "Fourth", "Fifth", "Sixth");

        var longSelected = new State<int>(0);
        var longList = new ListBox<string>().SelectedIndex(longSelected);
        for (var i = 0; i < 40; i++)
        {
            longList.Items.Add($"Item {i:00}");
        }

        return new VStack(
                DemoUi.Hint("ListBox supports keyboard navigation and a selected index."),
                new Group("Simple List").Content(list),
                new TextBlock(() => $"SelectedIndex: {selected.Value}"),
                DemoUi.Hint("With many items, wrap ListBox in a ScrollViewer."),
                new Border(new ScrollViewer(longList).MinHeight(8).MaxHeight(8)),
                new TextBlock(() => $"Long list SelectedIndex: {longSelected.Value}"),
                new Button("Log selection").Click(() => context.Log($"SelectedIndex: {selected.Value}")))
            .Spacing(1);
    }
}
