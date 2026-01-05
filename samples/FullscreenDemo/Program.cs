using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;

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

var main = new VStack { Spacing = 1 };
main.Add(new TextBlock("Fullscreen demo: Tab focus, mouse click, wheel scroll, Esc quit"));
main.Add(name);
main.Add(accept);
main.Add(showModal);
main.Add(new TextBlock("Pick one (mouse wheel supported):"));
main.Add(list);
main.Add(new TextBlock("ScrollViewer (focus + wheel):"));
main.Add(scroll);
main.Add(progress);
main.Add(button);
main.Add(status);

main.SetEnvironmentValue(Theme.Key, new Theme
{
    Foreground = Theme.Default.Foreground,
    Background = Theme.Default.Background,
    Border = Theme.Default.Border,
    FocusBorder = Theme.Default.FocusBorder,
    Accent = Theme.Default.Accent,
    Selection = 11, // bright green
    Disabled = Theme.Default.Disabled,
});

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
    dialogContent.Add(new TextBlock("Click Close or toggle the checkbox."));
    dialogContent.Add(close);

    var dialog = new Border { Padding = new Thickness(1), Child = dialogContent };

    var center = new Center { Child = dialog };

    var panel = new ZStack();
    panel.Add(new Backdrop(), center);
    return panel;
});

var root = new ZStack();
root.Add(main, overlay);

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
