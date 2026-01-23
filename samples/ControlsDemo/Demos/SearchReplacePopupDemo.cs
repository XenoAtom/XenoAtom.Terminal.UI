using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Search / Replace", "Input", Description = "Reusable search popup used by TextArea (Ctrl+F / Ctrl+H) and LogControl (Ctrl+F). Alt+Arrow moves the popup.")]
public sealed class SearchReplacePopupDemo : ControlsDemoBase
{
    public SearchReplacePopupDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var editor = new TextArea(
                """
                This is a TextArea.

                Use Ctrl+F to open Find.
                Use Ctrl+H to open Replace.
                Use Enter / Shift+Enter (or F3) to navigate matches.
                Use Alt+Arrow to move the popup.

                foo bar foo
                """)
            .MinHeight(10)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var log = new LogControl { MaxCapacity = 2000 }.WrapText(true);
        log.AppendLine("LogControl search is read-only.");
        log.AppendLine("Try Ctrl+F, then type: foo");
        log.AppendLine("foo bar foo");

        return new VStack(
                DemoUi.Hint("SearchReplacePopup is a reusable component hosted by controls like TextArea and LogControl."),
                new HStack(
                        new Group("TextArea").Padding(1).Content(new Border(editor).HorizontalAlignment(HorizontalAlignment.Stretch).VerticalAlignment(VerticalAlignment.Stretch)),
                        new Group("LogControl").Padding(1).Content(new Border(log).HorizontalAlignment(HorizontalAlignment.Stretch).VerticalAlignment(VerticalAlignment.Stretch).MinHeight(10)))
                    .Spacing(2)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch))
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);
    }
}

