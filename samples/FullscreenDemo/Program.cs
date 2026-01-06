using XenoAtom.Ansi;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Visuals;

using var session = Terminal.Open();

var name = new TextBox { Text = "Type here (Ctrl+A, Shift+Arrows, Ctrl+Left/Right)" };
var accept = new CheckBox("Accept terms");
var showModal = new CheckBox("Show modal");
var list = new ListBox { Items = new[] { "First", "Second", "Third", "Fourth", "Fifth", "Sixth" }, Height = 6 };
var progress = new ProgressBar { Label = "Work", Value = 0.0 };

var button = new Button("Click me (mouse or Enter)");
var status = new TextBlock("Status: ready");
button.Click += (_, _) => status.Text = "Status: click received";

var scrollContent = new VStack();
for (var i = 0; i < 20; i++)
{
    scrollContent.Add(new TextBlock($"Log line {i}"));
}
var scroll = new ScrollViewer { Child = scrollContent, Height = 5 };

var table = new Table
{
    Headers = new[] { "Task", "Status" },
    Rows = new[]
    {
        new[] { "Download", "Running" },
        new[] { "Render", "OK" },
        new[] { "Tests", "OK" },
    },
};

var main = new VStack { Spacing = 1 };
main.Add(new TextBlock("Fullscreen demo: Tab focus, mouse click, wheel scroll, F12 debug, Esc quit"));
main.Add(name);
main.Add(accept);
main.Add(showModal);
main.Add(table);
main.Add(new TextBlock("Pick one (mouse wheel supported):"));
main.Add(list);
main.Add(new TextBlock("ScrollViewer (focus + wheel):"));
main.Add(scroll);
main.Add(progress);
main.Add(button);
main.Add(status);


// Disabling this part for now, as the custom color on the selection is not nice with the default theme.
//main.SetEnvironmentValue(Theme.Key, new Theme
//{
//    Foreground = Theme.Default.Foreground,
//    Background = Theme.Default.Background,
//    Border = Theme.Default.Border,
//    FocusBorder = Theme.Default.FocusBorder,
//    Accent = Theme.Default.Accent,
//    Selection = AnsiColor.Rgb(0x00, 0xFF, 0x00),
//    Disabled = Theme.Default.Disabled,
//});

var statusBar = new StatusBar
{
    LeftText = "Tab focus | Mouse click | Wheel scroll | F12 debug | Esc quit",
    RightText = "XenoAtom.Terminal.UI",
};

var layout = new DockLayout
{
    Content = main,
    Bottom = statusBar,
};

var overlay = new ComputedVisual(() =>
{
    if (!showModal.IsChecked)
    {
        return null;
    }

    var close = new Button("Close");
    close.Click += (_, _) => showModal.IsChecked = false;

    var dialogContent = new VStack { Spacing = 1 };
    dialogContent.Add(new TextBlock("Modal dialog"));
    dialogContent.Add(new TextBlock("This is a wrapped paragraph demonstrating document-style text rendering.") { Wrap = true });
    dialogContent.Add(close);

    var dialog = new Dialog
    {
        Title = "Modal dialog",
        IsModal = true,
        Padding = new Thickness(1),
        Width = 60,
        Child = dialogContent,
    };

    var panel = new ZStack();
    panel.Add(new Backdrop(), dialog);
    return panel;
});

var root = new WindowLayer { Content = layout };
root.AddWindow(overlay);

var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });

using var cts = new CancellationTokenSource();
_ = Task.Run(async () =>
{
    var t = 0.0;
    while (!cts.IsCancellationRequested)
    {
        t += 0.02;
        var v = (Math.Sin(t) + 1.0) / 2.0;
        app.Post(() => progress.Value = v);
        await Task.Delay(50, cts.Token).ConfigureAwait(false);
    }
}, cts.Token);

await app.RunAsync();
cts.Cancel();
