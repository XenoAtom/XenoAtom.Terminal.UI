using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("OptionList", "Navigation", Description = "List items with activation, shortcuts, and disabled entries.")]
public sealed class OptionListDemo : ControlsDemoBase
{
    public OptionListDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var selected = new State<int>(0);

        var list = new OptionList()
            .Height(8)
            .ActivateOnClick(true)
            .SelectedIndex(selected);

        list.Items.Add(new OptionListItem("Open", "Ctrl+O"));
        list.Items.Add(new OptionListItem("Save", "Ctrl+S"));
        list.Items.Add(new OptionListItem("Disabled item") { IsEnabled = false });
        list.Items.Add(new OptionListItem("Quit", "Esc"));

        list.SelectionChanged((_, e) => context.Log($"SelectionChanged: {e.OldIndex} -> {e.NewIndex}"));
        list.ItemActivated((_, e) => context.Log($"ItemActivated: {e.Index}"));

        return new VStack(
                DemoUi.Hint("OptionList supports a selected index, disabled items, and activation."),
                list,
                new TextBlock(() => $"SelectedIndex: {selected.Value}"))
            .Spacing(1);
    }
}

