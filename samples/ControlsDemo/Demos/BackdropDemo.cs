using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Backdrop", "Overlays", Description = "Fullscreen dimming surface typically used behind modal content.")]
public sealed class BackdropDemo : ControlsDemoBase
{
    public BackdropDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var actions = new OptionList<OptionListItem>().ActivateOnClick(true);
        actions.Items.Add(new OptionListItem("Build", "Ctrl+B"));
        actions.Items.Add(new OptionListItem("Run", "F5"));
        actions.Items.Add(new OptionListItem("Test", "Ctrl+T"));
        actions.Items.Add(new OptionListItem("Publish", "Ctrl+P"));

        var backgroundContent = new Group("Behind")
            .Content(new VStack(
                "Background content (will be dimmed by Backdrop)",
                new HStack(new Button("Success").Tone(ControlTone.Success), new Button("Secondary").Tone(ControlTone.Warning)).Spacing(1),
                new TextBox().Text("Search + filters..."),
                actions).Spacing(1))
            .Padding(1).Stretch();

        var dialog = new Dialog()
            .Title("Foreground dialog")
            .Content(new VStack(
                    "This dialog is rendered above the Backdrop.",
                    "The backdrop is only a subtle dim layer over background content.",
                    new Button("Close"))
                .Spacing(1));

        var showBackdrop = new State<bool>(true);

        var layeredScene = new Border(
                new ZStack(
                    backgroundContent,
                    new Backdrop().IsVisible(showBackdrop),
                    dialog)
            )
            .MinWidth(70)
            .MaxWidth(70)
            .MinHeight(20)
            .MaxHeight(20);


        // Layering: background content -> backdrop -> foreground dialog.
        return new VStack(
            DemoUi.Hint("Backdrop renders between background content and foreground overlays to improve readability."),
            new CheckBox().Text("Show Backdrop").IsChecked(showBackdrop),
            layeredScene).Spacing(1);
    }
}
