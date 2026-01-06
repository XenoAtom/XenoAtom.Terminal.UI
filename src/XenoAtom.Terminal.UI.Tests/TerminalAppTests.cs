// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Visuals;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalAppTests
{
    private sealed class ProbeFocusable : Visual
    {
        public ProbeFocusable(string text)
        {
            Focusable = true;
            Text = text;
        }

        public string Text { get; }

        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(availableSize.Width, 10), 1);

        protected override void ArrangeOverride(Rectangle finalRect) => Bounds = finalRect;

        protected override void RenderOverride(CellBuffer buffer)
        {
            buffer.WriteText(Bounds.X, Bounds.Y, Text.AsSpan(), ReferenceEquals(App?.FocusedElement, this) ? (Cell.None | TextStyle.Invert) : Cell.None);
        }
    }

    private sealed class KeyBindingProbe : Visual
    {
        public int Count { get; private set; }

        public KeyBindingProbe()
        {
            Focusable = true;
            AddKeyBinding(new Input.TerminalKeyGesture('k', TerminalModifiers.Ctrl), () => Count++);
        }

        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(availableSize.Width, 10), 1);

        protected override void ArrangeOverride(Rectangle finalRect) => Bounds = finalRect;

        protected override void RenderOverride(CellBuffer buffer)
        {
            buffer.WriteText(Bounds.X, Bounds.Y, $"Count:{Count}".AsSpan(), Cell.None);
        }
    }

    [TestMethod]
    public async Task Renders_TextBlock_In_InlineHost()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var root = new VStack();
        root.Add(new TextBlock("Hello"));

        var app = new TerminalApp(root, session.Instance);

        var runTask = app.RunAsync();
        await Task.Delay(10);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });

        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "Hello");
    }

    [TestMethod]
    public async Task VirtualBackend_Delivers_Pushed_KeyEvents()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        session.Instance.StartInput();
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });

        var ev = await session.Instance.ReadEventAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsInstanceOfType<TerminalKeyEvent>(ev);
    }

    [TestMethod]
    public async Task Button_Raises_Click_On_Enter()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var button = new Button("OK");
        var clicked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        button.Click += (_, _) => clicked.TrySetResult();

        var root = new VStack();
        root.Add(button);

        var app = new TerminalApp(root, session.Instance);
        var runTask = app.RunAsync();

        await Task.Delay(10);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });

        await clicked.Task.WaitAsync(TimeSpan.FromSeconds(2));

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task Button_Raises_Click_On_Mouse()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var button = new Button("OK");
        var clicked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        button.Click += (_, _) => clicked.TrySetResult();

        var root = new ZStack();
        root.Add(button, new ComputedVisual(static () => null));

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunAsync();

        await Task.Delay(10);
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 1, Y = 0 });
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 1, Y = 0 });

        await clicked.Task.WaitAsync(TimeSpan.FromSeconds(2));

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task Button_Does_Not_Click_When_Released_Outside()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var button = new Button("OK");
        var clicked = false;
        button.Click += (_, _) => clicked = true;

        var root = new VStack();
        root.Add(button);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunAsync();

        await Task.Delay(10);
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 1, Y = 0 });
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 19, Y = 9 });

        await Task.Delay(30);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsFalse(clicked);
    }

    [TestMethod]
    public async Task Hover_Sets_IsHovered_On_HitTest_Target()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var button = new Button("OK");
        var root = new VStack();
        root.Add(button);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunAsync();

        await Task.Delay(10);

        static async Task WaitUntil(Func<bool> condition)
        {
            var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (!condition())
            {
                if (DateTime.UtcNow >= timeout)
                {
                    Assert.Fail("Timed out waiting for condition.");
                }
                await Task.Delay(10);
            }
        }

        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = 1, Y = 0 });
        await WaitUntil(() => button.IsHovered);

        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = 19, Y = 9 });
        await WaitUntil(() => !button.IsHovered);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task Dialog_Can_Be_Dragged_With_Mouse()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var dialog = new Dialog
        {
            Title = "T",
            Width = 10,
            Height = 5,
            Child = new TextBlock("Hello"),
        };

        var root = new ZStack();
        root.Add(dialog);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunAsync();

        await Task.Delay(10);

        static async Task WaitUntil(Func<bool> condition)
        {
            var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (!condition())
            {
                if (DateTime.UtcNow >= timeout)
                {
                    Assert.Fail("Timed out waiting for condition.");
                }
                await Task.Delay(10);
            }
        }

        // Centered: left=5, top=2 for a 20x10 slot and 10x5 dialog.
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 6, Y = 2 });
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = 9, Y = 4 });
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 9, Y = 4 });

        await WaitUntil(() => dialog.Left == 8 && dialog.Top == 4);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task WindowLayer_Brings_Clicked_Window_To_Front()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var layer = new WindowLayer { Content = new TextBlock("Base") };

        var a = new Dialog { Title = "A", Width = 8, Height = 4, Left = 1, Top = 1, Child = new TextBlock("A") };
        var b = new Dialog { Title = "B", Width = 8, Height = 4, Left = 10, Top = 1, Child = new TextBlock("B") };
        layer.AddWindow(a);
        layer.AddWindow(b);

        var app = new TerminalApp(layer, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunAsync();

        await Task.Delay(20);

        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 2, Y = 1 });
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 2, Y = 1 });

        await Task.Delay(20);

        Assert.AreSame(a, layer.Children[^1]);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task ModalDialog_Blocks_Clicks_Behind()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 12));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var clicked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var button = new Button("Click");
        button.Click += (_, _) => clicked.TrySetResult();

        var content = new VStack();
        content.Add(button);

        var layer = new WindowLayer { Content = content };
        var modal = new Dialog
        {
            Title = "Modal",
            IsModal = true,
            Width = 12,
            Height = 5,
            Left = 10,
            Top = 3,
            Child = new TextBlock("Modal"),
        };
        layer.AddWindow(modal);

        var app = new TerminalApp(layer, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunAsync();

        await Task.Delay(20);

        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 1, Y = 1 });
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 1, Y = 1 });

        await Task.Delay(50);
        Assert.IsFalse(clicked.Task.IsCompleted);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task RadioButton_Unchecks_Others_In_Group()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 8));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        static async Task WaitUntil(Func<bool> condition)
        {
            var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (!condition())
            {
                if (DateTime.UtcNow >= timeout)
                {
                    Assert.Fail("Timed out waiting for condition.");
                }
                await Task.Delay(10);
            }
        }

        var group = new object();
        var a = new RadioButton("A", group);
        var b = new RadioButton("B", group);

        var root = new VStack();
        root.Add(a);
        root.Add(b);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunAsync();

        await Task.Delay(20);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Space });
        await WaitUntil(() => a.IsChecked);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        await Task.Delay(20);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Space });
        await WaitUntil(() => b.IsChecked && !a.IsChecked);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task TabControl_Changes_SelectedIndex_On_ArrowKeys()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        static async Task WaitUntil(Func<bool> condition)
        {
            var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (!condition())
            {
                if (DateTime.UtcNow >= timeout)
                {
                    Assert.Fail("Timed out waiting for condition.");
                }
                await Task.Delay(10);
            }
        }

        var tabs = new TabControl();
        tabs.AddTab("First", new TextBlock("First"));
        tabs.AddTab("Second", new TextBlock("Second"));

        var root = new VStack();
        root.Add(tabs);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunAsync();

        await Task.Delay(20);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        await WaitUntil(() => tabs.SelectedIndex == 1);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left });
        await WaitUntil(() => tabs.SelectedIndex == 0);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task RoutedEventArgs_Sets_Source_And_OriginalSource()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var button = new Button("OK");
        var root = new PointerProbe();
        root.AddChild(button);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunAsync();

        await Task.Delay(20);

        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 1, Y = 0 });
        await Task.Delay(20);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.AreSame(button, root.SeenOriginal);
        Assert.AreSame(root, root.SeenSource);
    }

    private sealed class PointerProbe : Visual
    {
        public Visual? SeenOriginal { get; private set; }

        public Visual? SeenSource { get; private set; }

        public PointerProbe()
        {
            AddHandler(Visual.PointerPressedEvent, (_, e) =>
            {
                SeenOriginal = e.OriginalSource;
                SeenSource = e.Source;
            });
        }
    }

    [TestMethod]
    public async Task TextBox_Shows_Cursor_And_Sets_Position()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 5));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var textBox = new TextBox();
        var root = new VStack();
        root.Add(textBox);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunAsync();

        static async Task WaitUntil(Func<bool> condition)
        {
            var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (!condition())
            {
                if (DateTime.UtcNow >= timeout)
                {
                    Assert.Fail("Timed out waiting for condition.");
                }
                await Task.Delay(10);
            }
        }

        await WaitUntil(() => session.Instance.GetCursorVisible());
        await WaitUntil(() => session.Instance.Cursor.Position.Equals(new TerminalPosition(2, 1)));

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task KeyBinding_Executes_On_Ctrl_Gesture()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var probe = new KeyBindingProbe();
        var root = new VStack();
        root.Add(probe);

        var app = new TerminalApp(root, session.Instance);
        var runTask = app.RunAsync();

        await Task.Delay(10);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = 'k', Modifiers = TerminalModifiers.Ctrl });
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = 'k', Modifiers = TerminalModifiers.Ctrl });

        await Task.Delay(50);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.AreEqual(2, probe.Count);
    }

    [TestMethod]
    public async Task InlineHost_Delivers_Mouse_To_LiveRegion()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var button = new Button("OK");
        var clicked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        button.Click += (_, _) => clicked.TrySetResult();

        var root = new VStack();
        root.Add(button);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Inline });
        var runTask = app.RunAsync();

        await Task.Delay(30);

        var cursor = session.Instance.Cursor.Position;
        var liveTop = cursor.Row - 1;

        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 1, Y = liveTop });
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 1, Y = liveTop });

        await clicked.Task.WaitAsync(TimeSpan.FromSeconds(2));

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task Tab_Skips_Invisible_And_Disabled()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var a = new ProbeFocusable("A");
        var b = new ProbeFocusable("B") { IsVisible = false };
        var c = new ProbeFocusable("C") { IsEnabled = false };
        var d = new ProbeFocusable("D");

        var root = new VStack();
        root.Add(a, b, c, d);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunAsync();

        await Task.Delay(30);
        Assert.AreSame(a, app.FocusedElement);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        await Task.Delay(30);

        Assert.AreSame(d, app.FocusedElement);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task CheckBox_Toggles_On_Space()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var checkBox = new CheckBox("A", isChecked: false);
        var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(object owner, string name)
        {
            if (ReferenceEquals(owner, checkBox) && name == "IsChecked")
            {
                changed.TrySetResult();
            }
        }

        BindingManager.Current.ValueChanged += Handler;
        try
        {
            var root = new VStack();
            root.Add(checkBox);

            var app = new TerminalApp(root, session.Instance);
            var runTask = app.RunAsync();

            await Task.Delay(10);
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Space });

            await changed.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.IsTrue(checkBox.IsChecked);

            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            BindingManager.Current.ValueChanged -= Handler;
        }
    }

    [TestMethod]
    public async Task TextBox_Edits_Text_And_Uses_Clipboard_Paste()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        session.Instance.Clipboard.Text = "xyz";

        var textBox = new TextBox();
        var root = new VStack();
        root.Add(textBox);

        var app = new TerminalApp(root, session.Instance);
        var runTask = app.RunAsync();

        await Task.Delay(10);

        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(object owner, string name)
        {
            if (ReferenceEquals(owner, textBox) && name == "Text" && textBox.Text == "axyzc")
            {
                reached.TrySetResult();
            }
        }

        BindingManager.Current.ValueChanged += Handler;
        try
        {
            backend.PushEvent(new TerminalTextEvent { Text = "a" });
            backend.PushEvent(new TerminalTextEvent { Text = "b" });
            backend.PushEvent(new TerminalTextEvent { Text = "c" });
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left });
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Backspace });
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = 'v', Modifiers = TerminalModifiers.Ctrl });

            await reached.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.AreEqual("axyzc", textBox.Text);

            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            BindingManager.Current.ValueChanged -= Handler;
        }
    }

    [TestMethod]
    public async Task TextBox_Can_Select_And_Copy()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var textBox = new TextBox();
        var root = new VStack();
        root.Add(textBox);

        var app = new TerminalApp(root, session.Instance);
        var runTask = app.RunAsync();

        await Task.Delay(10);

        backend.PushEvent(new TerminalTextEvent { Text = "a" });
        backend.PushEvent(new TerminalTextEvent { Text = "b" });
        backend.PushEvent(new TerminalTextEvent { Text = "c" });

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left, Modifiers = TerminalModifiers.Shift });
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = 'c', Modifiers = TerminalModifiers.Ctrl });

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.AreEqual("c", session.Instance.Clipboard.Text);
    }

    [TestMethod]
    public async Task TextBox_Handles_TerminalPasteEvent()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var textBox = new TextBox();
        var root = new VStack();
        root.Add(textBox);

        var app = new TerminalApp(root, session.Instance);
        var runTask = app.RunAsync();

        await Task.Delay(10);

        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(object owner, string name)
        {
            if (ReferenceEquals(owner, textBox) && name == "Text" && textBox.Text == "hello")
            {
                reached.TrySetResult();
            }
        }

        BindingManager.Current.ValueChanged += Handler;
        try
        {
            backend.PushEvent(new TerminalPasteEvent { Text = "hello" });
            await reached.Task.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            BindingManager.Current.ValueChanged -= Handler;
        }

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task TextBox_Supports_Ctrl_Kill_And_Yank()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var textBox = new TextBox { Text = "hello world", CaretIndex = 6 };
        var root = new VStack();
        root.Add(textBox);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunAsync();

        static async Task WaitUntil(Func<bool> condition)
        {
            var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (!condition())
            {
                if (DateTime.UtcNow >= timeout)
                {
                    Assert.Fail("Timed out waiting for condition.");
                }
                await Task.Delay(10);
            }
        }

        await WaitUntil(() => ReferenceEquals(app.FocusedElement, textBox));

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = 'k', Modifiers = TerminalModifiers.Ctrl });
        await WaitUntil(() => textBox.Text == "hello ");

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = 'y', Modifiers = TerminalModifiers.Ctrl });
        await WaitUntil(() => textBox.Text == "hello world");

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task ComputedVisual_Rebuilds_On_BindingChange()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var model = new TextBox { Text = "A" };
        var computed = new ComputedVisual(() => new TextBlock($"Computed:{model.Text}"));

        var root = new VStack { Spacing = 1 };
        root.Add(model);
        root.Add(computed);

        var app = new TerminalApp(root, session.Instance);

        var runTask = app.RunAsync();
        await Task.Delay(20);

        app.Post(() => model.Text = "B");
        await Task.Delay(50);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "Computed:A");
        StringAssert.Contains(outText, "Computed:B");
    }

    [TestMethod]
    public async Task InlineApp_Can_Append_Flow_Visual()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var progress = new ProgressBar { Label = "Work", Value = 0.0 };
        var root = new VStack();
        root.Add(progress);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Inline });

        var runTask = app.RunAsync();
        await Task.Delay(20);

        app.Post(() => app.Append(new TextBlock("Flow: Hello")));
        await Task.Delay(50);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "Flow: Hello");
        StringAssert.Contains(outText, "Work");
    }

    [TestMethod]
    public async Task EnvironmentValue_Invalidates_ComputedVisual()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var key = new EnvironmentKey<string>("Title", "A");

        ComputedVisual? view = null;
        view = new ComputedVisual(() => new TextBlock($"Env:{view!.GetEnvironmentValue(key)}"));

        var root = new VStack();
        root.Add(view);

        var app = new TerminalApp(root, session.Instance);
        var runTask = app.RunAsync();

        await Task.Delay(20);
        app.Post(() => root.SetEnvironmentValue(key, "B"));
        await Task.Delay(50);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "Env:A");
        StringAssert.Contains(outText, "Env:B");
    }

    [TestMethod]
    public async Task ListBox_Changes_Selection_On_Down()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var listBox = new ListBox
        {
            Items = new[] { "A", "B", "C" },
            SelectedIndex = 0,
            Height = 3,
        };

        var selected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(object owner, string name)
        {
            if (ReferenceEquals(owner, listBox) && name == "SelectedIndex" && listBox.SelectedIndex == 1)
            {
                selected.TrySetResult();
            }
        }

        BindingManager.Current.ValueChanged += Handler;
        try
        {
            var root = new VStack();
            root.Add(listBox);

            var app = new TerminalApp(root, session.Instance);
            var runTask = app.RunAsync();

            await Task.Delay(10);
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });

            await selected.Task.WaitAsync(TimeSpan.FromSeconds(2));

            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            BindingManager.Current.ValueChanged -= Handler;
        }
    }

    [TestMethod]
    public async Task ScrollViewer_Scrolls_On_Wheel()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var content = new VStack();
        for (var i = 0; i < 10; i++)
        {
            content.Add(new TextBlock($"Item {i}"));
        }

        var scroll = new ScrollViewer { Child = content, Height = 4 };

        var root = new VStack { Spacing = 1 };
        root.Add(scroll);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunAsync();

        await Task.Delay(20);
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Wheel, Button = TerminalMouseButton.Wheel, WheelDelta = -1, X = 1, Y = 0 });
        await Task.Delay(20);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsGreaterThan(0, scroll.VerticalOffset);
    }

    [TestMethod]
    public async Task Table_Renders_Headers_And_Cells()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var table = new Table
        {
            Headers = new[] { "Name", "Value" },
            Rows = new[] { new[] { "A", "1" }, new[] { "B", "2" } },
        };

        var root = new VStack();
        root.Add(table);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunAsync();

        await Task.Delay(30);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "Name");
        StringAssert.Contains(outText, "Value");
        StringAssert.Contains(outText, "A");
        StringAssert.Contains(outText, "2");
    }

    [TestMethod]
    public async Task StatusBar_Renders_Left_And_Right()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 5));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var status = new StatusBar { LeftText = "L", RightText = "R" };
        var layout = new DockLayout { Content = new TextBlock("X"), Bottom = status };

        var app = new TerminalApp(layout, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunAsync();

        await Task.Delay(30);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "L");
        StringAssert.Contains(outText, "R");
    }
}
