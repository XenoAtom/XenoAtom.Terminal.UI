using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.ControlsDemo;

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
        Popup? longPopup = null;

        var placement = new Select<PopupPlacement>()
            .Items([PopupPlacement.Below, PopupPlacement.Above, PopupPlacement.Right, PopupPlacement.Left]);

        PopupPlacement GetPlacement()
        {
            if (placement.Items.Count == 0)
            {
                return PopupPlacement.Below;
            }

            var index = Math.Clamp(placement.SelectedIndex, 0, placement.Items.Count - 1);
            return placement.Items[index];
        }

        var anchor = new Button("Show popup");
        anchor.Click(() =>
        {
            popup ??= CreatePopup();
            popup.Anchor = anchor;
            popup.Placement = GetPlacement();
            popup.Show();
        });

        var longAnchor = new Button("Show long popup");
        longAnchor.Click(() =>
        {
            longPopup ??= CreateLongPopup();
            longPopup.Anchor = longAnchor;
            longPopup.Placement = GetPlacement();
            longPopup.Show();
        });

        Popup CreatePopup()
        {
            var p = new Popup();
            p.Content(new Border(new VStack(
                    DemoUi.Title("Popup"),
                    DemoUi.Hint("Click outside or press Tab/Esc to close."),
                    new Button("Close").Click(p.Close))
                .Spacing(1)));

            p.Closed((_, _) => context.Log("Popup closed"));
            return p;
        }

        Popup CreateLongPopup()
        {
            var list = new VStack();
            for (var i = 0; i < 50; i++)
            {
                list.Add($"Item {i:00}");
            }

            var p = new Popup();
            p.Content(new Border(new VStack(
                    DemoUi.Title("Scrollable popup"),
                    DemoUi.Hint("This popup hosts large content; use mouse wheel/scrollbars."),
                    list)
                .Spacing(1)));

            p.Closed((_, _) => context.Log("Long popup closed"));
            return p;
        }

        var root = new VStack(
                DemoUi.Hint("Popups are useful for dropdowns and context menus."),
                "This is a line of text to give space",
                "This is another line",
                "And another line",
                "Now you can try it:",
                new HStack(anchor, longAnchor, "Placement:", placement).Spacing(1))
            .Spacing(1);

        return root.InScreenshot(context, () =>
        {
            longPopup ??= CreateLongPopup();
            longPopup.Anchor = longAnchor;
            longPopup.Placement = PopupPlacement.Below;
            longPopup.Show();
        });
    }
}
