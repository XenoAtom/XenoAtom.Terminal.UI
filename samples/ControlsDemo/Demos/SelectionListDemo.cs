using System.Linq;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("SelectionList", "Navigation", Description = "Multi-select list widget (Space toggles).")]
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

        var checkedCount = new TextBlock(() =>
        {
            var count = list.Checked.Count(i => i);
            return $"Checked: {count}";
        });

        return new VStack(
                DemoUi.Hint("Use Space/Enter to toggle an item. Use arrows to move."),
                list,
                checkedCount,
                new Button("Log").Click(() => context.Log($"Checked: {list.Checked.Count(i => i)}")))
            .Spacing(1);
    }
}
