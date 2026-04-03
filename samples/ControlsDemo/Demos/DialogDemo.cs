using XenoAtom.Ansi;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.ControlsDemo;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Dialog", "Overlays", Description = "Movable, resizable window with optional modal behavior.")]
public sealed class DialogDemo : ControlsDemoBase
{
    public DialogDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        Dialog CreateBasicDialog()
        {
            Dialog? dlg = null;
            var cornerButton = new Button("Log")
                .Style(ButtonStyle.Default with { Padding = 0 })
                .Click(() => context.Log("Dialog corner button clicked"));

            dlg = new Dialog()
                .Title("Modal dialog")
                .TopRightText(new HStack(
                        new TextBlock("Resizable").Style(new TextBlockStyle { Foreground = Colors.DeepSkyBlue }),
                        cornerButton)
                    .Spacing(1))
                .BottomLeftText("Min 36x9")
                .BottomRightText("Drag edge")
                .IsModal(true)
                .Padding(1)
                .MinWidth(36)
                .MinHeight(9)
                .Width(52)
                .Height(12)
                .Style(DialogStyle.Rounded with
                {
                    ResizeHandleHoverStyle = Style.None.WithBackground(Colors.DeepSkyBlue.WithAlpha(0x40)),
                })
                .Content(new VStack(
                        "This is a dialog window.",
                        DemoUi.Hint("Drag the title bar to move."),
                        DemoUi.Hint("Hover the left/right/bottom edges or the bottom-right corner to resize."),
                        new Group("Notes")
                            .TopRightText("DialogStyle")
                            .Content(new VStack(
                                    "Resizable is enabled by default.",
                                    "Minimum size uses the inherited MinWidth / MinHeight properties.")
                                .Spacing(1))
                            .Padding(1),
                        new Button("Close").Click(() => dlg!.Close()))
                    .Spacing(1));

            return dlg;
        }

        Dialog CreateOverlayDialog(bool openPopupOnShow = false)
        {
            Dialog? dlg = null;
            Popup? anchoredPopup = null;

            var selectChoices = new[] { "Stable", "Preview", "Experimental", "Archived" };
            var variantSelect = new Select<string>()
                .Items(selectChoices)
                .SelectedIndex(1)
                .HorizontalAlignment(Align.Stretch);
            variantSelect.SelectionChanged((_, e) =>
            {
                if (e.NewIndex < 0 || e.NewIndex >= selectChoices.Length)
                {
                    return;
                }

                context.Log($"Dialog select changed to {selectChoices[e.NewIndex]}");
            });

            Button? popupAnchor = null;
            void ToggleAnchoredPopup()
            {
                if (popupAnchor is null)
                {
                    return;
                }

                if (anchoredPopup is { App: not null })
                {
                    anchoredPopup.Close();
                    return;
                }

                anchoredPopup = new Popup
                {
                    Anchor = popupAnchor,
                    MatchAnchorWidth = false,
                    Placement = PopupPlacement.Below,
                    Content = new Border(new VStack(
                            DemoUi.Title("Nested popup"),
                            DemoUi.Hint("This popup is anchored to a control inside the dialog."),
                            new Button("Close popup").Click(() => anchoredPopup?.Close()))
                        .Spacing(1))
                        .Padding(1),
                };

                anchoredPopup.Closed((_, _) =>
                {
                    anchoredPopup = null;
                    context.Log("Dialog popup closed");
                });

                anchoredPopup.Show();
            }

            popupAnchor = new Button("Toggle popup")
                .Click(ToggleAnchoredPopup);

            var tooltipButton = new Button("Hover for tooltip")
                .Tooltip(new VStack(
                        "Tooltips also float above dialogs.",
                        DemoUi.Hint("Move away or click another control to dismiss it."))
                    .Spacing(1))
                .ShowDelayMilliseconds(context.IsScreenshot ? 0 : 200);

            var contextMenuTarget = new TextBlock("Right-click here for a context menu from inside the dialog.")
                .Wrap(true);
            contextMenuTarget.ContextMenuFactory = _ =>
            {
                var more = new MenuItem("More");
                more.Items.Add(new MenuItem("Log current select", () =>
                {
                    var index = Math.Clamp(variantSelect.SelectedIndex, 0, selectChoices.Length - 1);
                    context.Log($"Context menu saw {selectChoices[index]}");
                }));

                return
                [
                    new MenuItem("Say hello", () => context.Log("Dialog context menu invoked")),
                    MenuItem.Separator(),
                    more
                ];
            };

            var editor = new TextArea("Focus here and press Ctrl+F or Ctrl+H.\nSearchReplacePopup should also work while the dialog is open.")
                .MinHeight(4)
                .MaxHeight(4);

            var content = new VStack(
                    "This dialog hosts several overlay-producing controls.",
                    DemoUi.Hint("Use the Select, tooltip, right-click context menu, and nested popup button below."),
                    new Group("Dropdown").Padding(1).Content(new VStack(
                            new TextBlock(() =>
                            {
                                var index = Math.Clamp(variantSelect.SelectedIndex, 0, selectChoices.Length - 1);
                                return $"Current variant: {selectChoices[index]}";
                            }),
                            variantSelect)
                        .Spacing(1)),
                    new Group("Nested overlays").Padding(1).Content(new VStack(
                            new HStack(popupAnchor, tooltipButton).Spacing(1),
                            contextMenuTarget)
                        .Spacing(1)),
                    new Group("Search popup").Padding(1).Content(new VStack(
                            DemoUi.Hint("Focus the editor, then press Ctrl+F or Ctrl+H."),
                            editor)
                        .Spacing(1)),
                    new Button("Close").Click(() => dlg!.Close()))
                .Spacing(1);

            if (openPopupOnShow)
            {
                var opened = false;
                content.Update(_ =>
                {
                    if (opened)
                    {
                        return;
                    }

                    opened = true;
                    ToggleAnchoredPopup();
                });
            }

            dlg = new Dialog()
                .Title("Dialog with nested popups")
                .TopRightText("Select / Tooltip / ContextMenu")
                .BottomLeftText("Try Ctrl+F in the editor")
                .BottomRightText("Overlay stress case")
                .IsModal(true)
                .Padding(1)
                .MinWidth(58)
                .MinHeight(16)
                .Width(72)
                .Height(20)
                .Style(DialogStyle.Rounded with
                {
                    ResizeHandleHoverStyle = Style.None.WithBackground(Colors.DeepSkyBlue.WithAlpha(0x40)),
                })
                .Content(content);

            return dlg;
        }

        var showModal = new Button("Show modal dialog").Click(() =>
        {
            var dlg = CreateBasicDialog();
            dlg.Show();
        });

        var showOverlayDialog = new Button("Show dialog with popups").Click(() =>
        {
            var dlg = CreateOverlayDialog();
            dlg.Show();
        });

        var root = new VStack(
                DemoUi.Hint("Dialogs are supported in fullscreen apps (WindowLayer)."),
                DemoUi.Hint("Use the second button to try popup-producing controls while the dialog is open."),
                new HStack(showModal, showOverlayDialog).Spacing(1))
            .Spacing(1);

        return root.InScreenshot(context, () =>
        {
            var dlg = CreateOverlayDialog(openPopupOnShow: true);
            dlg.Show();
        });
    }
}

