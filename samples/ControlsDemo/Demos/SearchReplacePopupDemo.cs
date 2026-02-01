using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.ControlsDemo;

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
            .VerticalAlignment(Align.Stretch)
            .HorizontalAlignment(Align.Stretch);

        var root = new VStack(
                DemoUi.Hint("SearchReplacePopup is a reusable component hosted by controls like TextArea and LogControl."),
                new Group("TextArea").Padding(1).Content(new Border(editor).HorizontalAlignment(Align.Stretch).VerticalAlignment(Align.Stretch))
                    .HorizontalAlignment(Align.Stretch)
                    .VerticalAlignment(Align.Stretch))
            .Spacing(1)
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch);

        return root.InScreenshot(context, () => editor.OpenReplace("foo"));
    }
}

