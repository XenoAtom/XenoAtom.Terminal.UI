using System.Linq;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("SelectionList", "Input", Description = "Multi-select list widget (Space toggles).")]
public sealed class SelectionListDemo : ControlsDemoBase
{
    public SelectionListDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var list = new SelectionList<string>()
            .AddItem("Alpha")
            .AddItem("Beta")
            .AddItem("Gamma")
            .AddItem("Delta")
            .AddItem("Epsilon");

        var longList = new SelectionList<string>();
        for (var i = 0; i < 40; i++)
        {
            longList.AddItem($"Item {i:00}");
        }

        var checkedCount = new TextBlock(() =>
        {
            var count = list.Checked.Count(i => i);
            return $"Checked: {count}";
        });

        return new VStack(
                DemoUi.Hint("Use Space/Enter to toggle an item. Use arrows to move."),
                list,
                checkedCount,
                DemoUi.Hint("With many items, wrap SelectionList in a ScrollViewer."),
                new ScrollViewer(longList).MinHeight(8).MaxHeight(8),
                new Button("Log").Click(() => context.Log($"Checked: {list.Checked.Count(i => i)}")))
            .Spacing(1);
    }
}
