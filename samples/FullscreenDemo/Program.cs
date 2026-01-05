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

var root = new VStack { Spacing = 1 };
root.Add(new TextBlock("Fullscreen demo: Tab focus, mouse click, wheel scroll, Esc quit"));
root.Add(name);
root.Add(accept);
root.Add(new TextBlock("Pick one (mouse wheel supported):"));
root.Add(list);
root.Add(progress);
root.Add(button);
root.Add(status);

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
