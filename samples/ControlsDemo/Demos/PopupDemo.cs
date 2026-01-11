using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Popup", "Overlays", Description = "Floating popup anchored to a control.")]
public sealed class PopupDemo : ControlsDemoBase
{
    public PopupDemo() : base(DemoSource.Get())
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

        Popup CreatePopup()
        {
            var p = new Popup();
            p.Content(new VStack(
                    DemoUi.Title("Popup"),
                    DemoUi.Hint("Click outside or press Tab/Esc to close."),
                    new Button("Close").Click(p.Close))
                .Spacing(1));

            p.Closed((_, _) => context.Log("Popup closed"));
            return p;
        }

        return new VStack(
                DemoUi.Hint("Popups are useful for dropdowns and context menus."),
                new HStack(anchor, "Placement:", placement).Spacing(1))
            .Spacing(1);
    }
}
