using SkiaSharp;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.Screenshot;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ScreenshotClipboardCommandTests
{
    [TestMethod]
    public void TryCopyScreenshotToClipboard_Writes_Png_Data()
    {
        var root = new Border(new TextBlock("Hello"))
            .Style(BorderStyle.Single)
            .Padding(1);

        root.SetStyle(Theme.Default);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 10));
        driver.Tick();

        Assert.IsTrue(driver.App.TryCopyScreenshotToClipboard());
        AssertClipboardContainsPng(driver.Terminal.Clipboard);
    }

    [TestMethod]
    public void Visual_Registered_Screenshot_Command_Uses_Default_CtrlF12_Shortcut()
    {
        var root = new Border(new TextBlock("Hello"))
            .Style(BorderStyle.Single)
            .Padding(1);

        root.SetStyle(Theme.Default);
        root.RegisterClipboardScreenshotCommand();

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 10));
        driver.Tick();

        var command = driver.App.GlobalCommands.Single(x => string.Equals(x.Id, ScreenshotClipboardCommandOptions.Default.CommandId, StringComparison.Ordinal));
        Assert.AreEqual(ScreenshotClipboardCommandOptions.Default.Gesture, command.Gesture);
        Assert.AreEqual(ScreenshotClipboardCommandOptions.Default.Presentation, command.Presentation);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F12, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        AssertClipboardContainsPng(driver.Terminal.Clipboard);
    }

    [TestMethod]
    public void Visual_Registered_Screenshot_Command_Works_With_Modal_Dialog_Open()
    {
        var root = new Border(new TextBlock("Hello"))
            .Style(BorderStyle.Single)
            .Padding(1);

        root.SetStyle(Theme.Default);
        root.RegisterClipboardScreenshotCommand();

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 10));
        driver.Tick();

        var dialog = new Dialog
        {
            Title = "Modal",
            IsModal = true,
            Content = new Button("OK"),
        };

        dialog.Show();
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F12, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        AssertClipboardContainsPng(driver.Terminal.Clipboard);
    }

    [TestMethod]
    public void TerminalApp_Registers_Global_Screenshot_Command_With_Custom_Options()
    {
        var root = new Border(new TextBlock("Hello"))
            .Style(BorderStyle.Single)
            .Padding(1);

        root.SetStyle(Theme.Default);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 10));
        driver.App.RegisterClipboardScreenshotCommand(new ScreenshotClipboardCommandOptions
        {
            CommandId = "Demo.ScreenshotClipboard",
            LabelMarkup = "Snap",
            Gesture = new KeyGesture(TerminalKey.F11),
            Presentation = CommandPresentation.None,
            ImageOptions = new CellBufferImageExportOptions
            {
                AutoCrop = true,
                Padding = new Thickness(1),
            },
        });

        var command = driver.App.GlobalCommands.Single(x => string.Equals(x.Id, "Demo.ScreenshotClipboard", StringComparison.Ordinal));
        Assert.AreEqual("Snap", command.LabelMarkup);
        Assert.AreEqual(new KeyGesture(TerminalKey.F11), command.Gesture);
        Assert.AreEqual(CommandPresentation.None, command.Presentation);
    }

    [TestMethod]
    public void TerminalApp_Registered_Screenshot_Command_Works_With_Modal_Dialog_Open()
    {
        var root = new Border(new TextBlock("Hello"))
            .Style(BorderStyle.Single)
            .Padding(1);

        root.SetStyle(Theme.Default);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 10));
        driver.App.RegisterClipboardScreenshotCommand();
        driver.Tick();

        var dialog = new Dialog
        {
            Title = "Modal",
            IsModal = true,
            Content = new Button("OK"),
        };

        dialog.Show();
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F12, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        AssertClipboardContainsPng(driver.Terminal.Clipboard);
    }

    [TestMethod]
    public void Visual_Registered_Screenshot_Command_Appears_In_CommandBar_By_Default()
    {
        var focus = new Button("Hello");
        var root = new DockLayout
        {
            Content = focus,
            Bottom = new Footer().Left("Tab").Center("Theme").Right(new CommandBar()),
        };

        root.RegisterClipboardScreenshotCommand();

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 6));
        driver.App.Focus(focus);
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "Ctrl+F12");
        StringAssert.Contains(outText, "Screenshot");
    }

    private static void AssertClipboardContainsPng(TerminalClipboard clipboard)
    {
        Assert.IsTrue(clipboard.TryGetFormats(out var formats));
        CollectionAssert.Contains(formats.ToArray(), TerminalClipboardFormats.Png);

        Assert.IsTrue(clipboard.TryGetData(TerminalClipboardFormats.Png, out var pngBytes));
        CollectionAssert.AreEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, pngBytes[..4]);

        using var bitmap = SKBitmap.Decode(pngBytes);
        Assert.IsNotNull(bitmap);
        Assert.IsTrue(bitmap.Width > 0);
        Assert.IsTrue(bitmap.Height > 0);
    }
}
