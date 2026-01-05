// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalAppTests
{
    private sealed class KeyBindingProbe : Visual
    {
        public int Count { get; private set; }

        public KeyBindingProbe()
        {
            Focusable = true;
            AddKeyBinding(new TerminalKeyGesture('k', TerminalModifiers.Ctrl), () => Count++);
        }

        protected override CellSize MeasureOverride(CellSize availableSize) => new(Math.Min(availableSize.Width, 10), 1);

        protected override void ArrangeOverride(CellRect finalRect) => Bounds = finalRect;

        protected override void RenderOverride(CellBuffer buffer)
        {
            buffer.WriteText(Bounds.X, Bounds.Y, $"Count:{Count}".AsSpan(), CellStyle.None);
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

        var root = new VStack();
        root.Add(button);

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
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = 'a' });
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = 'b' });
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = 'c' });
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

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = 'a' });
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = 'b' });
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = 'c' });

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
}
