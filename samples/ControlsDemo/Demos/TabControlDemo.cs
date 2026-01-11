using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("TabControl", "Navigation", Description = "Tabs with Visual headers and dynamic header content.")]
public sealed class TabControlDemo : ControlsDemoBase
{
    public TabControlDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var progress = context.Runtime.Progress01;

        var tabs = new TabControl(
            new TabPage(
                header: new HStack("Status", new TextBlock(() => $"({(int)(progress.Value * 100)}%)")).Spacing(1),
                content: new VStack(
                        DemoUi.Hint("This is the first tab."),
                        new ProgressBar().Value(progress).Label("Work"))
                    .Spacing(1)),
            new TabPage(
                header: "Logs",
                content: new VStack(
                        DemoUi.Hint("Second tab content."),
                        "Log line 0",
                        "Log line 1",
                        "Log line 2")
                    .Spacing(0)));

        return new VStack(
                DemoUi.Hint("Tab headers are visuals, so they can include dynamic content."),
                tabs.MinHeight(8).MaxHeight(8))
            .Spacing(1);
    }
}
