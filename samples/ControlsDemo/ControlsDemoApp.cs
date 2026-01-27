using System.Linq;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.ControlsDemo.Demos;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo;

internal static class ControlsDemoApp
{
    public static Visual Build(out Func<TerminalLoopResult> onUpdate)
    {
        var demos = DemoRegistry.Load();

        var defaultId = string.Empty;
        var welcomeId = typeof(WelcomeDemo).FullName ?? string.Empty;
        for (var i = 0; i < demos.Count; i++)
        {
            if (string.Equals(demos[i].Metadata.Id, welcomeId, StringComparison.Ordinal))
            {
                defaultId = welcomeId;
                break;
            }
        }

        if (defaultId.Length == 0 && demos.Count > 0)
        {
            defaultId = demos[0].Metadata.Id;
        }

        var selectedDemoId = new State<string>(defaultId);
        var runtime = new DemoRuntime();
        onUpdate = () =>
        {
            _ = runtime.Advance();
            return TerminalLoopResult.Continue;
        };

        void NavigateToId(string id) => selectedDemoId.Value = id;

        var searchBox = new TextBox().Placeholder("Search controls…");

        var sidebarDemoIdForIndex = new List<string?>(demos.Count);

        var sidebarList = new OptionList<OptionListItem>()
            .ActivateOnClick(true)
            .VerticalAlignment(Align.Stretch);

        // Make category headers stand out by giving disabled rows a stronger style.
        // (Category headers are represented as disabled items so they cannot be selected.)
        var theme = DemoThemes.Dark;
        var headerStyle = Style.None;
        if (theme.Foreground is { } headerFg)
        {
            headerStyle = headerStyle.WithForeground(headerFg);
        }
        if ((theme.SurfaceAlt ?? theme.ControlFill ?? theme.Surface ?? theme.Background) is { } headerBg)
        {
            headerStyle = headerStyle.WithBackground(headerBg);
        }
        headerStyle |= TextStyle.Bold;

        sidebarList.Style(OptionListStyle.Default with { Disabled = headerStyle });

        void SelectSidebarIndex(int index)
        {
            if ((uint)index >= (uint)sidebarDemoIdForIndex.Count)
            {
                return;
            }

            if (sidebarDemoIdForIndex[index] is { } id)
            {
                selectedDemoId.Value = id;
            }
        }

        sidebarList.SelectionChanged((_, e) => SelectSidebarIndex(e.NewIndex));
        sidebarList.ItemActivated((_, e) => SelectSidebarIndex(e.Index));

        // Keep the sidebar list stable (no ComputedVisual) and rebuild its items via dynamic update so focus isn't reset.
        sidebarList.Update(_ => RebuildSidebarList(sidebarList, sidebarDemoIdForIndex, demos, selectedDemoId, query: searchBox.Text ?? string.Empty));

        var sidebar = new VStack(
                "Browse",
                searchBox,
                new ScrollViewer(sidebarList))
            .Spacing(1);

        // Initialize color scheme selector.
        var colorSchemes = new Select<ColorScheme>().Items(ColorScheme.GetPredefinedSchemes());
        colorSchemes.SelectedIndex = colorSchemes.Items.IndexOf(ColorScheme.ElderberryDarkSoft);

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

            var colorScheme = colorSchemes.Items[colorSchemes.SelectedIndex];
            var theme = Theme.FromScheme(colorScheme);

            return demo is null
                ? new Center().Content("No demos found.")
                : DemoPage.Build(demo, new DemoContext { NavigateToDemoId = NavigateToId, Log = _ => { }, Runtime = runtime, Theme = theme });
        }).Pad(1).HorizontalAlignment(Align.Stretch).VerticalAlignment(Align.Stretch);


        var root = new DockLayout()
            .Content(new HSplitter(sidebar, page).Ratio(0.16))
            .Bottom(
                new Footer().Left("Tab focus | Mouse | Resize")
                    .Center(new HStack("🎨 Select Theme: ", colorSchemes))
                    .Right(new CommandBar()));

        root.Update(c =>
        {
            var colorScheme = colorSchemes.Items[colorSchemes.SelectedIndex];
            c.Style(Theme.FromScheme(colorScheme));
        });

        return root;
    }

    private static void RebuildSidebarList(OptionList<OptionListItem> list, List<string?> demoIdForIndex, IReadOnlyList<IControlsDemo> demos, State<string> selectedDemoId, string query)
    {
        var normalizedQuery = query.Trim();
        var hasQuery = normalizedQuery.Length > 0;

        list.Items.Clear();
        demoIdForIndex.Clear();

        // Add welcome page as the first item (outside categories).
        var welcomeId = typeof(WelcomeDemo).FullName ?? string.Empty;
        if (welcomeId.Length > 0)
        {
            IControlsDemo? welcome = null;
            for (var i = 0; i < demos.Count; i++)
            {
                if (string.Equals(demos[i].Metadata.Id, welcomeId, StringComparison.Ordinal))
                {
                    welcome = demos[i];
                    break;
                }
            }

            if (welcome is not null && (!hasQuery || DemoSearch.Matches(welcome.Metadata, normalizedQuery)))
            {
                list.Items.Add(new OptionListItem("🏠 Welcome"));
                demoIdForIndex.Add(welcomeId);
            }
        }

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
                if (string.Equals(demo.Metadata.Id, welcomeId, StringComparison.Ordinal))
                {
                    continue;
                }

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

            list.Items.Add(new OptionListItem(CreateCategoryHeader(category)) { IsEnabled = false });
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
    }

    private static Visual CreateCategoryHeader(string category)
    {
        var label = category switch
        {
            "Content" => $"📄 {category}",
            "Input" => $"⌨️ {category}",
            "Layout" => $"🧩 {category}",
            "Navigation" => $"🧭 {category}",
            "Overlays" => $"🪟 {category}",
            "Patterns" => $"🧠 {category}",
            "Visualization" => $"📊 {category}",
            _ => $"📁 {category}",
        };

        // Category headers are rendered as disabled items, so they inherit the disabled row style.
        return new Markup($"[cyan]{label}[/]");
    }
}
