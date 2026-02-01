using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.ControlsDemo;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("ContextMenu", "Overlays", Description = "Right-click context menus from ContextMenuFactory or CommandPresentation.ContextMenu.")]
public sealed class ContextMenuDemo : ControlsDemoBase
{
    public ContextMenuDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var text = new TextArea("Right-click in this editor to open a context menu.")
            .MinHeight(6)
            .MaxHeight(6);

        text.AddCommand(new Command
        {
            Id = "Demo.InsertLine",
            LabelMarkup = "[primary]Insert sample line[/]",
            Presentation = CommandPresentation.ContextMenu,
            Execute = _ =>
            {
                text.Text = (text.Text ?? string.Empty) + "Inserted from context menu.\n";
            },
        });

        var custom = new TextArea("This editor uses ContextMenuFactory (custom items).")
            .MinHeight(6)
            .MaxHeight(6);

        custom.ContextMenuFactory = _ => new[]
        {
            new MenuItem("Log selection", () => context.Log("Context menu invoked")),
            MenuItem.Separator(),
            new MenuItem("Clear", () => custom.Text = string.Empty),
        };

        var root = new VStack(
                DemoUi.Title("Context menus"),
                new TextBlock("Right-click to open a context menu. If ContextMenuFactory is not provided, the framework discovers commands with CommandPresentation.ContextMenu.")
                    .Wrap(true),
                new Group().TopLeftText("Command-based").Padding(1).Content(text),
                new Group().TopLeftText("Factory-based").Padding(1).Content(custom))
            .Spacing(1);

        return root.InScreenshot(context, () =>
        {
            var items = custom.ContextMenuFactory?.Invoke(custom) ?? Array.Empty<MenuItem>();
            _ = ContextMenuService.Show(custom, items, uiX: 6, uiY: 8);
        });
    }
}
