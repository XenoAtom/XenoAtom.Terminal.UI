using System.Text;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("TabControl", "Layout", Description = "Attached tabs by default, mutable tab pages, drag reordering, and single-line overflow navigation.")]
public sealed class TabControlDemo : ControlsDemoBase
{
    private enum TabStylePreset
    {
        Default,
        Compact,
        LegacySingleBox,
        NoBorder,
        RoundedBox,
        DoubleBox,
        HeavyBox,
        AsciiBox,
        AsciiHeavyBox,
        DashedBox,
    }

    public TabControlDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var progress = context.Runtime.Progress01;
        var renameCount = new State<int>(1);
        var closeAttempts = new State<int>(0);
        var addedTabCount = new State<int>(0);
        var stylePreset = new State<TabStylePreset>(TabStylePreset.Default);

        var statusPage = new TabPage(
            header: new TextBlock(() => $"Status ({(int)(progress.Value * 100)}%)"),
            content: DemoUi.Hint("Loading status..."))
        {
            Data = "primary",
        };
        statusPage.Content = new VStack(
                new Markup(() => $"[dim]{DescribePreset(stylePreset.Value)}[/]").Wrap(true),
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
            .Style(() => ResolveStyle(stylePreset.Value))
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch);

        var root = new Grid()
            .Rows(
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star(1) })
            .Columns(new ColumnDefinition { Width = GridLength.Star(1) })
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch);

        root
            .Cell(
                DemoUi.Hint("Tab pages are bindable models: mutate Header, Content, Data, IsEnabled, or ShowCloseButton directly. Drag any tab header to reorder it, or use the move buttons below. Resize the terminal to exercise overflow, then switch presets to compare the chrome."),
                0,
                0)
            .Cell(
                new VStack(
                        new HStack(
                                new TextBlock("Style"),
                                new EnumSelect<TabStylePreset>().Value(stylePreset),
                                new Button("Rename Logs").Click(() =>
                                {
                                    renameCount.Value++;
                                    logsPage.Header = new TextBlock($"Logs v{renameCount.Value}");
                                }),
                                new Button("Add Tab").Click(() =>
                                {
                                    addedTabCount.Value++;
                                    var tabNumber = addedTabCount.Value;
                                    tabs.AddTab(new TabPage(
                                        $"Tab-{tabNumber:00}",
                                        new VStack(
                                                DemoUi.Hint("Add enough tabs, scroll the strip, then drag or move a visible tab to confirm the order updates in place."),
                                                new TextBlock($"Dynamic tab #{tabNumber}"))
                                            .Spacing(1))
                                    {
                                        ShowCloseButton = true,
                                    });
                                }),
                                new Button("Toggle Metrics Close").Click(() => metricsPage.ShowCloseButton = !metricsPage.ShowCloseButton))
                            .Spacing(1),
                        new HStack(
                                new Button("Disable Metrics").Click(() => metricsPage.IsEnabled = !metricsPage.IsEnabled),
                                new Button("Move Selected Left").Click(() => MoveSelectedTab(tabs, -1)),
                                new Button("Move Selected Right").Click(() => MoveSelectedTab(tabs, 1)))
                            .Spacing(1),
                        new TextBlock(() => $"Order: {DescribeTabOrder(tabs)}"),
                        new TextBlock(() => $"Selected: {DescribeSelectedTab(tabs)}"),
                        DemoUi.Hint("Mouse drag reordering is enabled by default. Drag visible headers horizontally to reorganize the strip."))
                    .Spacing(1),
                1,
                0)
            .Cell(tabs, 2, 0);

        return root;
    }

    private static string DescribePreset(TabStylePreset preset)
        => preset switch
        {
            TabStylePreset.Default => "Default: attached rounded tabs with unwrapped content.",
            TabStylePreset.Compact => "Compact: tighter attached tabs with single-line glyphs.",
            TabStylePreset.LegacySingleBox => "LegacySingleBox: the original flat strip with a single-line content border.",
            TabStylePreset.NoBorder => "NoBorder: legacy flat strip with no additional content wrapper.",
            TabStylePreset.RoundedBox => "RoundedBox: legacy strip with a rounded content border.",
            TabStylePreset.DoubleBox => "DoubleBox: legacy strip with a double-line content border.",
            TabStylePreset.HeavyBox => "HeavyBox: legacy strip with a heavy content border.",
            TabStylePreset.AsciiBox => "AsciiBox: legacy strip with an ASCII content border.",
            TabStylePreset.AsciiHeavyBox => "AsciiHeavyBox: legacy strip with a heavy ASCII content border.",
            TabStylePreset.DashedBox => "DashedBox: legacy strip with a dashed content border.",
            _ => "Tab style preset.",
        };

    private static TabControlStyle ResolveStyle(TabStylePreset preset)
        => preset switch
        {
            TabStylePreset.Default => TabControlStyle.Default,
            TabStylePreset.Compact => TabControlStyle.Compact,
            TabStylePreset.LegacySingleBox => TabControlStyle.Legacy,
            TabStylePreset.NoBorder => TabControlStyle.NoBorder,
            TabStylePreset.RoundedBox => TabControlStyle.Rounded,
            TabStylePreset.DoubleBox => TabControlStyle.Double,
            TabStylePreset.HeavyBox => TabControlStyle.Heavy,
            TabStylePreset.AsciiBox => TabControlStyle.Ascii,
            TabStylePreset.AsciiHeavyBox => TabControlStyle.AsciiHeavy,
            TabStylePreset.DashedBox => TabControlStyle.Dashed,
            _ => TabControlStyle.Default,
        };

    private static void MoveSelectedTab(TabControl tabs, int delta)
    {
        var selectedIndex = tabs.SelectedIndex;
        var newIndex = selectedIndex + delta;
        if ((uint)selectedIndex >= (uint)tabs.Tabs.Count || (uint)newIndex >= (uint)tabs.Tabs.Count)
        {
            return;
        }

        tabs.MoveTab(selectedIndex, newIndex);
    }

    private static string DescribeSelectedTab(TabControl tabs)
    {
        if ((uint)tabs.SelectedIndex >= (uint)tabs.Tabs.Count)
        {
            return "<none>";
        }

        return $"#{tabs.SelectedIndex}: {GetTabTitle(tabs.Tabs[tabs.SelectedIndex])}";
    }

    private static string DescribeTabOrder(TabControl tabs)
    {
        if (tabs.Tabs.Count == 0)
        {
            return "<empty>";
        }

        var builder = new StringBuilder();
        for (var i = 0; i < tabs.Tabs.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(" → ");
            }

            builder.Append(GetTabTitle(tabs.Tabs[i]));
        }

        return builder.ToString();
    }

    private static string GetTabTitle(TabPage page)
        => page.Header switch
        {
            TextBlock textBlock => textBlock.Text ?? string.Empty,
            _ => page.Data?.ToString() ?? page.Header.GetType().Name,
        };
}
