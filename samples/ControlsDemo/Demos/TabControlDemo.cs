using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("TabControl", "Layout", Description = "Closable tabs, mutable tab pages, and single-line overflow navigation.")]
public sealed class TabControlDemo : ControlsDemoBase
{
    public TabControlDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var progress = context.Runtime.Progress01;
        var renameCount = new State<int>(1);
        var closeAttempts = new State<int>(0);

        var statusPage = new TabPage(
            header: new HStack("Status", new TextBlock(() => $"({(int)(progress.Value * 100)}%)")).Spacing(1),
            content: DemoUi.Hint("Loading status..."))
        {
            Data = "primary",
        };
        statusPage.Content = new VStack(
                DemoUi.Hint("The selected content stays attached while the header can be any visual."),
                new ProgressBar().Value(progress),
                new TextBlock(() => $"Status data: {statusPage.Data ?? "<null>"}"))
            .Spacing(1);

        var logsPage = new TabPage(
            header: "Logs",
            content: new VStack(
                    DemoUi.Hint("This page can replace its header and content without recreating the whole tab."),
                    new TextBlock(() => $"Rename count: {renameCount.Value}"),
                    new TextBlock(() => $"Close attempts: {closeAttempts.Value}"))
                .Spacing(1))
        {
            Data = "logs",
            ShowCloseButton = true,
        };

        logsPage.RequestClosing += (_, e) =>
        {
            closeAttempts.Value++;
            if (closeAttempts.Value == 1)
            {
                e.Cancel = true;
                logsPage.Content = new VStack(
                        DemoUi.Hint("The first close request is cancelled by the page callback."),
                        new TextBlock(() => $"Close attempts: {closeAttempts.Value}"))
                    .Spacing(1);
            }
        };

        var metricsPage = new TabPage(
            header: "Metrics",
            content: DemoUi.Hint("Loading metrics..."))
        {
            Data = 42,
            ShowCloseButton = true,
        };
        metricsPage.Content = new VStack(
                DemoUi.Hint("Close buttons inherit state-aware styling and can be toggled dynamically."),
                new TextBlock(() => $"Metrics data: {metricsPage.Data ?? "<null>"}"))
            .Spacing(1);

        var tabs = new TabControl(
            statusPage,
            logsPage,
            metricsPage,
            new TabPage("Search", DemoUi.Hint("Add enough tabs to force the strip to scroll.")) { ShowCloseButton = true },
            new TabPage("Preview", DemoUi.Hint("Use the left/right overflow arrows to browse hidden headers.")) { ShowCloseButton = true },
            new TabPage("History", DemoUi.Hint("Overflow keeps the strip on a single row.")) { ShowCloseButton = true })
            .MinHeight(9)
            .MaxHeight(9)
            .MaxWidth(42);

        return new VStack(
                DemoUi.Hint("Tab pages are bindable models: mutate Header, Content, Data, IsEnabled, or ShowCloseButton directly."),
                new HStack(
                        new Button("Rename Logs").Click(() =>
                        {
                            renameCount.Value++;
                            logsPage.Header = new TextBlock($"Logs v{renameCount.Value}");
                        }),
                        new Button("Toggle Metrics Close").Click(() => metricsPage.ShowCloseButton = !metricsPage.ShowCloseButton),
                        new Button("Disable Metrics").Click(() => metricsPage.IsEnabled = !metricsPage.IsEnabled))
                    .Spacing(1),
                tabs)
            .Spacing(1);
    }
}
