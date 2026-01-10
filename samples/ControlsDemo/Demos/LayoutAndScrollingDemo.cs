using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Layout and scrolling", "Layout", Description = "Stacks, Grid, DockLayout, splitters, Border/Group, ScrollViewer and ScrollBar.", Tags = ["VStack", "HStack", "ZStack", "Grid", "DockLayout", "Splitter", "ScrollViewer", "ScrollBar", "Border", "Group"], Order = 0)]
public sealed class LayoutAndScrollingDemo : ControlsDemoBase
{
    public LayoutAndScrollingDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var longList = new VStack().Spacing(0);
        for (var i = 0; i < 200; i++)
        {
            longList.Add($"Log line {i}");
        }

        var scroll = new ScrollViewer()
            .Content(longList)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);

        var leftTop = new Group()
            .TopLeftText("Center + Border")
            .Padding(1)
            .Content(new Border()
                .Padding(new Thickness(1))
                .Content(new Center().Content("Centered content")));

        var leftBottom = new Group()
            .TopLeftText("ZStack")
            .Padding(1)
            .Content(new ZStack(
                new Border().Padding(1).Content("Bottom"),
                new Border().Padding(new Thickness(2)).Content("Top")));

        var left = new VSplitter(leftTop, leftBottom)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);

        var right = new VStack(
                new Group()
                    .TopLeftText("ScrollViewer")
                    .Padding(0)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .Content(scroll))
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);

        var splitter = new HSplitter(left, right)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);

        // Grid + DockLayout quick showcase.
        var modeSelect = new Select();
        modeSelect.Items.AddRange(new SelectItem("Dev"), new SelectItem("Prod"));

        var grid = new Grid()
            .Columns(
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star(1) })
            .Rows(
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto })
            .ColumnGap(1)
            .Add(
                new TextBlock("Name:").Row(0).Column(0),
                new TextBox().Text("Alex").Row(0).Column(1).HorizontalAlignment(HorizontalAlignment.Stretch),
                new TextBlock("Mode:").Row(1).Column(0),
                modeSelect.Row(1).Column(1).HorizontalAlignment(HorizontalAlignment.Stretch))
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var dock = new DockLayout()
            .Top(new Markup("[dim]DockLayout.Top[/]"))
            .Bottom(new Markup("[dim]DockLayout.Bottom[/]"))
            .Content(new Center().Content(new Markup("[dim]DockLayout.Content[/]")));

        var explicitScrollBarValue = new State<int>(0);
        var scrollBar = new ScrollBar()
            .Minimum(0)
            .Maximum(100)
            .Value(explicitScrollBarValue.Value)
            .ValueChanged((_, e) =>
            {
                explicitScrollBarValue.Value = e.NewValue;
                context.Log($"ScrollBar: {explicitScrollBarValue.Value}");
            });

        var scrollBarHost = new HStack(
                new Markup("[dim]ScrollBar:[/]"),
                scrollBar,
                new Markup(() => $"[dim]{explicitScrollBarValue.Value}[/]"))
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var statusBar = new StatusBar()
            .LeftText("StatusBar left")
            .RightText(new Markup("[dim]StatusBar right[/]"))
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        return new VStack(
                new Group().TopLeftText("Grid").Padding(1).HorizontalAlignment(HorizontalAlignment.Stretch).Content(grid),
                new Group().TopLeftText("DockLayout").Padding(1).HorizontalAlignment(HorizontalAlignment.Stretch).Content(dock),
                new Group().TopLeftText("ScrollBar").Padding(1).HorizontalAlignment(HorizontalAlignment.Stretch).Content(scrollBarHost),
                new Group().TopLeftText("StatusBar").Padding(1).HorizontalAlignment(HorizontalAlignment.Stretch).Content(statusBar),
                splitter)
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);
    }
}
