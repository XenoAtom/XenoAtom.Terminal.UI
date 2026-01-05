using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;

using var session = Terminal.Open();

var name = new TextBox { Text = "Type here (Ctrl+A, Shift+Arrows, Ctrl+Left/Right)" };
var accept = new CheckBox("Accept terms");
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

var root = new VStack { Spacing = 1 };
root.Add(new TextBlock("Fullscreen demo: Tab focus, mouse click, wheel scroll, Esc quit"));
root.Add(name);
root.Add(accept);
root.Add(new TextBlock("Pick one (mouse wheel supported):"));
root.Add(list);
root.Add(new TextBlock("ScrollViewer (focus + wheel):"));
root.Add(scroll);
root.Add(progress);
root.Add(button);
root.Add(status);

root.SetEnvironmentValue(Theme.Key, new Theme
{
    Foreground = Theme.Default.Foreground,
    Background = Theme.Default.Background,
    Border = Theme.Default.Border,
    FocusBorder = Theme.Default.FocusBorder,
    Accent = Theme.Default.Accent,
    Selection = 11, // bright green
    Disabled = Theme.Default.Disabled,
});

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
