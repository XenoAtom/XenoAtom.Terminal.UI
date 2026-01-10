using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Popups and dialogs", "Overlays", Description = "Popup placement, dialog windows, backdrop, and window layering.", Tags = ["Popup", "Dialog", "Backdrop", "WindowLayer"], Order = 0)]
public sealed class OverlaysDemo : ControlsDemoBase
{
    public OverlaysDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        Popup? popup = null;
        var placement = new Select();
        placement.Items.AddRange(
            new SelectItem("Below"),
            new SelectItem("Above"),
            new SelectItem("Right"),
            new SelectItem("Left"));

        PopupPlacement GetPlacement()
            => placement.SelectedIndex switch
            {
                1 => PopupPlacement.Above,
                2 => PopupPlacement.Right,
                3 => PopupPlacement.Left,
                _ => PopupPlacement.Below,
            };

        var anchor = new Button("Show popup");
        anchor.Click(() =>
        {
            popup ??= CreatePopup();
            popup.Anchor = anchor;
            popup.Placement = GetPlacement();
            popup.Show();
        });

        var popupHost = new HStack(
                anchor,
                new Markup("[dim]Placement:[/]"),
                placement)
            .Spacing(1);

        Popup CreatePopup()
        {
            var p = new Popup();
            var close = new Button("Close").Click(() => p.Close());

            p.Content(new VStack(
                    new Markup("[bold]Popup[/]"),
                    new Markup("[dim]Click outside or press Tab/Esc to close.[/]").Wrap(true),
                    close)
                .Spacing(1)
                .HorizontalAlignment(HorizontalAlignment.Stretch));

            p.Closed((_, _) => context.Log("Popup closed"));
            return p;
        }

        var showDialog = new Button("Show modal dialog")
            .Click(() =>
            {
                Dialog? dlg = null;
                dlg = new Dialog()
                    .Title("Modal dialog")
                    .IsModal(true)
                    .Padding(1)
                    .Width(60)
                    .Content(new VStack(
                            new Markup("This is a [bold]dialog[/] shown as a window."),
                            new Markup("[dim]Drag the title bar to move.[/]").Wrap(true),
                            new Button("Close").HorizontalAlignment(HorizontalAlignment.Stretch).Click(() => dlg!.Close()))
                        .Spacing(1)
                        .HorizontalAlignment(HorizontalAlignment.Stretch));

                dlg.Show();
            });

        var layering = new Group()
            .TopLeftText("WindowLayer (embedded)")
            .Padding(0)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(BuildEmbeddedLayer());

        Visual BuildEmbeddedLayer()
        {
            var layer = new WindowLayer
            {
                Content = new ZStack(
                    new Border().Padding(1).Content(new Markup("[dim]This WindowLayer is embedded inside the demo page.[/]")),
                    new Backdrop())
            };

            var dialog1 = new Dialog().Title("Window A").Width(30).Height(6).Left(2).Top(1).Content(new Center().Content("A"));
            var dialog2 = new Dialog().Title("Window B").Width(30).Height(6).Left(10).Top(4).Content(new Center().Content("B"));
            layer.AddWindow(dialog1);
            layer.AddWindow(dialog2);

            return new VStack(
                    new Markup("[dim]Click a window to bring it to front.[/]").Wrap(true),
                    new Border().Padding(0).Content(layer).MinHeight(12).MaxHeight(12))
                .Spacing(1)
                .HorizontalAlignment(HorizontalAlignment.Stretch);
        }

        return new VStack(
                new Group().TopLeftText("Popup").Padding(1).HorizontalAlignment(HorizontalAlignment.Left).Content(popupHost),
                new Group().TopLeftText("Dialog").Padding(1).HorizontalAlignment(HorizontalAlignment.Left).Content(showDialog),
                layering)
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);
    }
}
