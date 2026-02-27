using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("OptionList", "Input", Description = "List items with activation, shortcuts, and disabled entries.")]
public sealed class OptionListDemo : ControlsDemoBase
{
    public OptionListDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        context.AllowPageScrollViewer = false;

        var selected = new State<int>(0);

        var list = new OptionList<OptionListItem>()
            .ActivateOnClick(true)
            .SelectedIndex(selected);

        list.Items.Add(new OptionListItem("Open", "Ctrl+O"));
        list.Items.Add(new OptionListItem("Save", "Ctrl+S"));
        list.Items.Add(new OptionListItem("Disabled item") { IsEnabled = false });
        list.Items.Add(new OptionListItem("Quit", "Esc"));

        list.SelectionChanged((_, e) => context.Log($"SelectionChanged: {e.OldIndex} -> {e.NewIndex}"));
        list.ItemActivated((_, e) => context.Log($"ItemActivated: {e.Index}"));

        var longList = new OptionList<OptionListItem>().ActivateOnClick(true);
        for (var i = 0; i < 30; i++)
        {
            longList.Items.Add(new OptionListItem($"Item {i:00}", "Enter"));
        }

        return new VStack(
                DemoUi.Hint("OptionList supports a selected index, disabled items, and activation."),
                list,
                new TextBlock(() => $"SelectedIndex: {selected.Value}"),
                DemoUi.Hint("With many items, wrap OptionList in a ScrollViewer."),
                new Border(new ScrollViewer(longList)).MinHeight(8).MaxHeight(8))
            .Spacing(1);
    }
}
