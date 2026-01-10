using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo;

internal static class ControlsDemoApp
{
    public static Visual Build(out Func<bool> onUpdate)
    {
        var demos = DemoRegistry.Load();

        var selectedIndex = new State<int>(0);
        var themeIndex = new State<int>(0);
        var runtime = new DemoRuntime();
        onUpdate = runtime.Advance;

        var commandPalette = new CommandPalette();

        void NavigateToId(string id)
        {
            for (var i = 0; i < demos.Count; i++)
            {
                if (string.Equals(demos[i].Metadata.Id, id, StringComparison.Ordinal))
                {
                    selectedIndex.Value = i;
                    return;
                }
            }
        }

        // Populate command palette.
        for (var i = 0; i < demos.Count; i++)
        {
            var demo = demos[i];
            commandPalette.Items.Add(new CommandPaletteItem(
                $"{demo.Metadata.Name}",
                () => selectedIndex.Value = i)
            {
                DescriptionFactory = () => demo.Metadata.Category,
            });
        }

        commandPalette.Items.Add(new CommandPaletteItem("Toggle theme", () => themeIndex.Value = 1 - themeIndex.Value)
        {
            ShortcutFactory = () => "Ctrl+T",
            DescriptionFactory = () => "Switch between dark/light demo themes",
        });

        // Root is computed so theme can be swapped without using dynamic updates.
        return new ComputedVisual(() =>
        {
            var theme = themeIndex.Value == 0 ? DemoThemes.Dark : DemoThemes.Light;

            var header = new Header()
                .Left("XenoAtom.Terminal.UI ControlsDemo")
                .Right("Ctrl+P palette | F12 debug | Esc quit");

            var menu = BuildMenuBar(commandPalette, themeIndex);

            var footer = new Footer()
                .Left("Tab focus | Mouse | Resize")
                .Right("XenoAtom.Terminal.UI");

            var searchBox = new TextBox()
                .Placeholder("Search controls, demos, tags…")
                .HorizontalAlignment(HorizontalAlignment.Stretch);

            var sidebarList = new ComputedVisual(() =>
                    BuildSidebarList(demos, selectedIndex, query: searchBox.Text ?? string.Empty))
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch);

            var sidebar = new VStack(
                     new Group()
                         .TopLeftText("Browse")
                         .Padding(0)
                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                        .Content(new VStack(
                                searchBox,
                                sidebarList)
                            .Spacing(1)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)))
                .Spacing(1)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch);

            var page = new ComputedVisual(() =>
            {
                var i = Math.Clamp(selectedIndex.Value, 0, Math.Max(0, demos.Count - 1));
                var demo = demos.Count == 0 ? null : demos[i];

                if (demo is null)
                {
                    return new Center().Content("No demos found.");
                }

                return DemoPage.Build(demo, new DemoContext
                {
                    NavigateToDemoId = NavigateToId,
                    Log = _ => { },
                    Runtime = runtime,
                });
            })
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);

            var layout = new DockLayout()
                .Top(new VStack(header, menu).Spacing(0))
                .Bottom(new VStack(new Rule(), footer).Spacing(0))
                .Content(new HSplitter(sidebar, page)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch))
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .Style(theme);

            layout.AddKeyBinding(new global::XenoAtom.Terminal.UI.Input.TerminalKeyGesture('p', TerminalModifiers.Ctrl), commandPalette.Show);
            layout.AddKeyBinding(new global::XenoAtom.Terminal.UI.Input.TerminalKeyGesture('t', TerminalModifiers.Ctrl), () => themeIndex.Value = 1 - themeIndex.Value);

            return layout;
        })
        .HorizontalAlignment(HorizontalAlignment.Stretch)
        .VerticalAlignment(VerticalAlignment.Stretch);
    }

    private static MenuBar BuildMenuBar(CommandPalette commandPalette, State<int> themeIndex)
    {
        var menuBar = new MenuBar();

        var menuView = new MenuItem("View");
        menuView.Items.Add(new MenuItem("Command palette", commandPalette.Show) { Shortcut = "Ctrl+P" });
        menuView.Items.Add(new MenuItem("Toggle theme", () => themeIndex.Value = 1 - themeIndex.Value) { Shortcut = "Ctrl+T" });

        var menuHelp = new MenuItem("Help");
        menuHelp.Items.Add(new MenuItem("About", () => commandPalette.Show()));

        menuBar.Items.AddRange(menuView, menuHelp);
        return menuBar;
    }

    private static Visual BuildSidebarList(IReadOnlyList<IControlsDemo> demos, State<int> selectedIndex, string query)
    {
        var list = new OptionList()
            .Height(24)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var normalizedQuery = query.Trim();
        var hasQuery = normalizedQuery.Length > 0;

        var filteredToOriginal = new List<int>(demos.Count);
        for (var i = 0; i < demos.Count; i++)
        {
            var demo = demos[i];
            var meta = demo.Metadata;

            if (hasQuery && !DemoSearch.Matches(meta, normalizedQuery))
            {
                continue;
            }

            var item = new OptionListItem(
                new TextBlock(meta.Name),
                new Markup($"[dim]{meta.Category}[/]"))
            {
                SearchText = $"{meta.Name} {meta.Category} {string.Join(' ', meta.Tags)}",
            };

            list.Items.Add(item);
            filteredToOriginal.Add(i);
        }

        // Keep selection bound to the global demo list even when the sidebar is filtered.
        var localSelected = 0;
        for (var i = 0; i < filteredToOriginal.Count; i++)
        {
            if (filteredToOriginal[i] == selectedIndex.Value)
            {
                localSelected = i;
                break;
            }
        }

        list.SelectedIndex(localSelected);
        list.SelectionChanged((_, e) =>
        {
            if ((uint)e.NewIndex < (uint)filteredToOriginal.Count)
            {
                selectedIndex.Value = filteredToOriginal[e.NewIndex];
            }
        });

        return list;
    }
}
