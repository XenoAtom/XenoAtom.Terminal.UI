using System.Linq;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo;

internal static class ControlsDemoApp
{
    public static Visual Build(out Func<TerminalLoopResult> onUpdate)
    {
        var demos = DemoRegistry.Load();

        var selectedDemoId = new State<string>(demos.Count > 0 ? demos[0].Metadata.Id : string.Empty);
        var runtime = new DemoRuntime();
        onUpdate = () =>
        {
            _ = runtime.Advance();
            return TerminalLoopResult.Continue;
        };

        void NavigateToId(string id) => selectedDemoId.Value = id;

        var searchBox = new TextBox().Placeholder("Search controls…");

        var sidebarList = new ComputedVisual(() =>
            BuildSidebarList(demos, selectedDemoId, query: searchBox.Text ?? string.Empty));

        var sidebar = new VStack(
                "Browse",
                searchBox,
                sidebarList)
            .Spacing(1);

        var page = new ComputedVisual(() =>
        {
            var id = selectedDemoId.Value;
            IControlsDemo? demo = null;
            for (var i = 0; i < demos.Count; i++)
            {
                if (string.Equals(demos[i].Metadata.Id, id, StringComparison.Ordinal))
                {
                    demo = demos[i];
                    break;
                }
            }

            return demo is null
                ? new Center().Content("No demos found.")
                : DemoPage.Build(demo, new DemoContext { NavigateToDemoId = NavigateToId, Log = _ => { }, Runtime = runtime });
        });

        return new DockLayout()
            .Content(new HSplitter(sidebar, page).Ratio(0.16))
            .Bottom(new Footer().Left("Tab focus | Mouse | Resize").Right("F12 debug | Ctrl+Q quit"))
            .Style(DemoThemes.Dark);
    }

    private static Visual BuildSidebarList(IReadOnlyList<IControlsDemo> demos, State<string> selectedDemoId, string query)
    {
        var normalizedQuery = query.Trim();
        var hasQuery = normalizedQuery.Length > 0;

        var list = new OptionList()
            .ActivateOnClick(true)
            .VerticalAlignment(VerticalAlignment.Stretch);

        var demoIdForIndex = new List<string?>(demos.Count);

        // Group by category and keep everything expanded (flat list with category headers).
        var categories = demos
            .Select(static d => d.Metadata.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static s => s, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        for (var c = 0; c < categories.Length; c++)
        {
            var category = categories[c];

            var matches = new List<IControlsDemo>();
            for (var i = 0; i < demos.Count; i++)
            {
                var demo = demos[i];
                if (!string.Equals(demo.Metadata.Category, category, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (hasQuery && !DemoSearch.Matches(demo.Metadata, normalizedQuery))
                {
                    continue;
                }

                matches.Add(demo);
            }

            if (matches.Count == 0)
            {
                continue;
            }

            list.Items.Add(new OptionListItem(new Markup($"[dim]{category}[/]")) { IsEnabled = false });
            demoIdForIndex.Add(null);

            for (var i = 0; i < matches.Count; i++)
            {
                var demo = matches[i];
                var meta = demo.Metadata;

                var item = new OptionListItem(meta.Name)
                {
                    SearchText = $"{meta.Name} {meta.Category} {meta.Description}",
                };

                list.Items.Add(item);
                demoIdForIndex.Add(meta.Id);
            }
        }

        // Sync selection (sidebar index) from selected demo id.
        var selectedIndex = 0;
        for (var i = 0; i < demoIdForIndex.Count; i++)
        {
            if (demoIdForIndex[i] is { } id && string.Equals(id, selectedDemoId.Value, StringComparison.Ordinal))
            {
                selectedIndex = i;
                break;
            }
        }

        list.SelectedIndex(selectedIndex);
        list.SelectionChanged((_, e) =>
        {
            if ((uint)e.NewIndex >= (uint)demoIdForIndex.Count)
            {
                return;
            }

            if (demoIdForIndex[e.NewIndex] is { } id)
            {
                selectedDemoId.Value = id;
            }
        });

        return list;
    }
}
