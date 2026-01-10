using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Lists and navigation", "Navigation", Description = "ListBox, OptionList, SelectionList, Select, TreeView and TabControl.", Tags = ["ListBox", "OptionList", "SelectionList", "Select", "TreeView", "TabControl"], Order = 0)]
public sealed class ListsAndNavigationDemo : ControlsDemoBase
{
    public ListsAndNavigationDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var listBox = new ListBox().MinHeight(6).MaxHeight(6);
        listBox.Items.AddRange("First", "Second", "Third", "Fourth", "Fifth", "Sixth");

        var optionList = new OptionList().MinHeight(8).MaxHeight(8);
        optionList.Items.AddRange(
            new OptionListItem("Open", "Ctrl+O") { Description = "Open a file" },
            new OptionListItem("Search", "Ctrl+F") { Description = "Find in the current view" },
            new OptionListItem("Command palette", "Ctrl+P") { Description = "Quick open actions/demos" },
            new OptionListItem("Quit", "Esc") { Description = "Exit the app" });
        optionList.ItemActivated((_, e) => context.Log($"OptionList activated: {e.Index}"));
        optionList.SelectionChanged((_, e) => context.Log($"OptionList selection: {e.NewIndex}"));

        var selectionList = new SelectionList().MinHeight(7).MaxHeight(7);
        selectionList.Items.AddRange(
            new SelectionListItem("Arrakis", isChecked: true),
            new SelectionListItem("Caladan"),
            new SelectionListItem("Chusuk"),
            new SelectionListItem("Giedi Prime"),
            new SelectionListItem("Ginaz"));

        var select = new Select();
        select.Items.AddRange(
            new SelectItem("Alpha"),
            new SelectItem("Beta"),
            new SelectItem("Gamma"),
            new SelectItem("Delta"));

        var tree = new TreeView().MinHeight(10).MaxHeight(10);
        var root = new TreeNode("XenoAtom") { Icon = TreeNodeIcon.Folder, IsExpanded = true };
        var src = new TreeNode("src") { Icon = TreeNodeIcon.Folder, IsExpanded = true };
        src.Children.Add(new TreeNode("Program.cs") { Icon = TreeNodeIcon.File });
        src.Children.Add(new TreeNode("readme.md") { Icon = TreeNodeIcon.Document });
        root.Children.Add(src);
        tree.Roots.Add(root);

        var tabs = new TabControl(
            new TabPage(new Markup("[bold]ListBox[/]"), listBox),
            new TabPage(new Markup("[bold]OptionList[/]"), optionList),
            new TabPage(new Markup("[bold]Select[/]"), new VStack(select, new Markup("[dim]Click to open; Esc closes.[/]").Wrap(true)).Spacing(1)),
            new TabPage(new Markup("[bold]TreeView[/]"), tree),
            new TabPage(new Markup("[bold]SelectionList[/]"), selectionList))
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        return tabs;
    }
}
