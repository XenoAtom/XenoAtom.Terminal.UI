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
        context.AllowPageScrollViewer = false;

        // For list-like scenarios, RadioButtonList provides arrow-key navigation and ScrollViewer integration.
        var selected = new State<int>(0);
        var list = new RadioButtonList<string>().SelectedIndex(selected);
        list.Items.AddRange("Choice A", "Choice B", "Choice C");

        var longSelected = new State<int>(0);
        var longList = new RadioButtonList<string>().SelectedIndex(longSelected);
        for (var i = 0; i < 40; i++)
        {
            longList.Items.Add($"Option {i:00}");
        }

        return new VStack(
                DemoUi.Hint("RadioButtonList supports keyboard navigation and a selected index."),
                list,
                new TextBlock(() => $"SelectedIndex: {selected.Value}"),
                DemoUi.Hint("With many options, wrap RadioButtonList in a ScrollViewer."),
                new Border(new ScrollViewer(longList)).MinHeight(8).MaxHeight(8),
                new TextBlock(() => $"Long list SelectedIndex: {longSelected.Value}"),
                new Button("Log selection").Click(() => context.Log($"SelectedIndex: {selected.Value}")))
            .Spacing(1);
    }
}
