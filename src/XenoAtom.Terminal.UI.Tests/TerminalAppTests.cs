// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using System.Text;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalAppTests
{
    private static async Task WaitUntilUi(TerminalApp app, Func<bool> condition)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(condition);

        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (true)
        {
            if (DateTime.UtcNow >= timeout)
            {
                Assert.Fail("Timed out waiting for condition.");
            }

            var ok = await app.Dispatcher.InvokeAsync(condition);
            if (ok)
            {
                return;
            }

            await Task.Delay(10);
        }
    }

    private sealed class ProbeFocusable : Visual
    {
        public ProbeFocusable(string text)
        {
            Focusable = true;
            Text = text;
        }

        public string Text { get; }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints) => SizeHints.Fixed(constraints.Clamp(new Size(10, 1)));

        protected override void ArrangeCore(in Rectangle finalRect) { }

        protected override void RenderOverride(CellBuffer buffer)
        {
            buffer.WriteText(Bounds.X, Bounds.Y, Text.AsSpan(), ReferenceEquals(App?.FocusedElement, this) ? (CellStyle.None | TextStyle.Invert) : CellStyle.None);
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

        protected override SizeHints MeasureCore(in LayoutConstraints constraints) => SizeHints.Fixed(constraints.Clamp(new Size(10, 1)));

        protected override void ArrangeCore(in Rectangle finalRect) { }

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

        var root = new VStack { "Hello" };

        var app = new TerminalApp(root, session.Instance);

        var runTask = app.RunInBackgroundAsync();
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
        button.Click((_, _) => clicked.TrySetResult());

        var root = new VStack { button };

        var app = new TerminalApp(root, session.Instance);
        var runTask = app.RunInBackgroundAsync();

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
        button.Click((_, _) => clicked.TrySetResult());

        var root = new ZStack { button, new ComputedVisual(static () => null) };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

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
        button.Click((_, _) => clicked = true);

        var root = new VStack { button };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

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
        var root = new VStack { button };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(10);

        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = 1, Y = 0 });
        await WaitUntilUi(app, () => button.IsHovered);

        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = 19, Y = 9 });
        await WaitUntilUi(app, () => !button.IsHovered);

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
            Content = new TextBlock("Hello"),
        };

        var root = new ZStack();
        root.Add(dialog);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(10);

        // Centered: left=5, top=2 for a 20x10 slot and 10x5 dialog.
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 6, Y = 2 });
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = 9, Y = 4 });
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 9, Y = 4 });

        await WaitUntilUi(app, () => dialog.Left == 8 && dialog.Top == 4);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task WindowLayer_Brings_Clicked_Window_To_Front()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var layer = new WindowLayer { Content = new TextBlock("Base") };

        var a = new Dialog { Title = "A", Width = 10, Height = 4, Left = 1, Top = 1, Content = new TextBlock("A") };
        var b = new Dialog { Title = "B", Width = 10, Height = 4, Left = 3, Top = 2, Content = new TextBlock("B") };
        layer.AddWindow(a);
        layer.AddWindow(b);

        var app = new TerminalApp(layer, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(20);

        static Visual GetRootChild(WindowLayer layer, Visual visual)
        {
            var rootChild = visual;
            while (rootChild.Parent is not null && !ReferenceEquals(rootChild.Parent, layer))
            {
                rootChild = rootChild.Parent;
            }

            return rootChild;
        }

        await app.Dispatcher.InvokeAsync(() =>
        {
            var initialHit = layer.HitTest(4, 3);
            Assert.IsNotNull(initialHit);
            Assert.AreSame(b, GetRootChild(layer, initialHit));
        });

        // Click within A only (not overlapped by B) to bring it to front.
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 2, Y = 2 });
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 2, Y = 2 });

        await Task.Delay(20);

        await app.Dispatcher.InvokeAsync(() =>
        {
            var postClickHit = layer.HitTest(4, 3);
            Assert.IsNotNull(postClickHit);
            Assert.AreSame(a, GetRootChild(layer, postClickHit));
        });

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
        button.Click((_, _) => clicked.TrySetResult());

        var content = new VStack();
        content.Add(button);

        var modal = new Dialog
        {
            Title = "Modal",
            IsModal = true,
            Width = 12,
            Height = 5,
            Left = 10,
            Top = 3,
            Content = new TextBlock("Modal"),
        };

        var app = new TerminalApp(content, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        try
        {
            await Task.Delay(20);

            await app.Dispatcher.InvokeAsync(modal.Show);

            backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 1, Y = 0 });
            backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 1, Y = 0 });

            await Task.Delay(50);
            Assert.IsFalse(clicked.Task.IsCompleted);

            await app.Dispatcher.InvokeAsync(modal.Close);

            backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 1, Y = 0 });
            backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 1, Y = 0 });

            await clicked.Task.WaitAsync(TimeSpan.FromSeconds(1));
        }
        finally
        {
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    [TestMethod]
    public async Task RadioButton_Unchecks_Others_In_Group()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 8));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var group = new object();
        var a = new RadioButton("A", group);
        var b = new RadioButton("B", group);

        var root = new VStack();
        root.Add(a);
        root.Add(b);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(20);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Space });
        await WaitUntilUi(app, () => a.IsChecked);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        await Task.Delay(20);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Space });
        await WaitUntilUi(app, () => b.IsChecked && !a.IsChecked);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task TabControl_Changes_SelectedIndex_On_ArrowKeys()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var tabs = new TabControl();
        tabs.AddTab("First", new TextBlock("First"));
        tabs.AddTab("Second", new TextBlock("Second"));

        var root = new VStack();
        root.Add(tabs);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(20);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        await WaitUntilUi(app, () => tabs.SelectedIndex == 1);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left });
        await WaitUntilUi(app, () => tabs.SelectedIndex == 0);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task RoutedEventArgs_Sets_Source_And_OriginalSource()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var button = new Button("OK");
        var root = new PointerProbe { Content = button };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

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
        private Visual? _child;

        public Visual? SeenOriginal { get; private set; }

        public Visual? SeenSource { get; private set; }

        public Visual? Content
        {
            get => _child;
            init
            {
                if (value is null)
                {
                    return;
                }

                _child = value;
                AttachChild(value);
            }
        }

        public PointerProbe()
        {
            AddHandler(Visual.PointerPressedEvent, (_, e) =>
            {
                SeenOriginal = e.OriginalSource;
                SeenSource = e.Source;
            });
        }

        protected override int ChildrenCount => _child is null ? 0 : 1;

        protected override Visual GetChild(int index)
        {
            if (index == 0 && _child is not null)
            {
                return _child;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    private sealed class CursorProbe : Visual, XenoAtom.Terminal.UI.Input.ICursorProvider
    {
        private readonly int _x;
        private readonly int _y;

        public CursorProbe(int x, int y)
        {
            Focusable = true;
            _x = x;
            _y = y;
        }

        public bool TryGetCursorCell(out int x, out int y)
        {
            x = _x;
            y = _y;
            return true;
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints) => SizeHints.Fixed(constraints.Clamp(new Size(1, 1)));
    }

    [TestMethod]
    public async Task TextBox_Shows_Cursor_And_Sets_Position()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 5));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var probe = new CursorProbe(x: 6, y: 3);
        var root = new VStack(probe);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

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

        await WaitUntil(() => backend.GetOutText().Contains("\x1b[4;7H\x1b[?25h", StringComparison.Ordinal));

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task Fluent_Bindable_Extensions_Are_Applied_During_Initialization()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 8));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var textBox = new TextBox().Text("Hello");
        var root = new VStack(textBox).Spacing(2);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();
        await WaitUntilUi(app, () => textBox.Text == "Hello" && root.Spacing == 2);

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
        var runTask = app.RunInBackgroundAsync();

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
        button.Click((_, _) => clicked.TrySetResult());

        var root = new VStack();
        root.Add(button);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Inline });
        var runTask = app.RunInBackgroundAsync();

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

        await WaitUntil(() => backend.GetOutText().Contains("\x1b[s", StringComparison.Ordinal));

        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 1, Y = 0 });
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 1, Y = 0 });

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
        root.Add(a);
        root.Add(b);
        root.Add(c);
        root.Add(d);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(30);
        await app.Dispatcher.InvokeAsync(() => Assert.AreSame(a, app.FocusedElement));

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        await Task.Delay(30);

        await app.Dispatcher.InvokeAsync(() => Assert.AreSame(d, app.FocusedElement));

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

        void Handler(Binding binding)
        {
            if (ReferenceEquals(binding.Owner, checkBox) && binding.Accessor.Name == "IsChecked")
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
            var runTask = app.RunInBackgroundAsync();
            try
            {
                await Task.Delay(10);
                backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Space });

                await changed.Task.WaitAsync(TimeSpan.FromSeconds(2));
                await app.Dispatcher.InvokeAsync(() => Assert.IsTrue(checkBox.IsChecked));
            }
            finally
            {
                backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
                await runTask.WaitAsync(TimeSpan.FromSeconds(2));
            }
        }
        finally
        {
            BindingManager.Current.ValueChanged -= Handler;
        }
    }

    [TestMethod]
    public void CheckBox_Renders_Space_Between_Glyph_And_Text()
    {
        var checkBox = new CheckBox("A", isChecked: true);

        // Use a wide glyph to ensure the label offset accounts for rune width.
        var wideGlyph = new Rune(0x1F600); // 😀
        checkBox.Set(CheckBoxStyle.Key, new CheckBoxStyle
        {
            CheckedGlyph = wideGlyph,
        });

        checkBox.Measure(new Size(10, 1));
        checkBox.Arrange(new Rectangle(0, 0, 10, 1));

        var buffer = new CellBuffer(10, 1);
        buffer.Clear();
        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(checkBox, new object[] { buffer });

        var scalars = (int[])typeof(CellBuffer).GetField("_scalars", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;
        var cells = (CellStyle[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        Assert.AreEqual(wideGlyph.Value, scalars[0]);

        var glyphWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(wideGlyph));
        if (glyphWidth > 1)
        {
            Assert.IsTrue((bool)typeof(CellStyle).GetProperty("IsContinuation", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(cells[1])!);
        }

        var labelIndex = Array.IndexOf(scalars, 'A');
        Assert.IsTrue(labelIndex >= 0, "Expected the label text to be rendered.");
        Assert.IsTrue(labelIndex >= glyphWidth + 1, "Expected at least one space after the checkbox glyph.");

        for (var i = glyphWidth; i < labelIndex; i++)
        {
            Assert.AreEqual(' ', scalars[i], "Expected padding between the checkbox glyph and the label.");
        }
    }

    [TestMethod]
    public void TabControl_Supports_Visual_Headers()
    {
        var tabControl = new TabControl();

        var header = new HStack(new TextBlock("A"), new TextBlock("!")).Spacing(1);
        var content = new TextBlock("Content");

        tabControl.AddTab(header, content);

        Assert.AreSame(tabControl, header.Parent);
        Assert.AreSame(tabControl, content.Parent);

        var visuals = tabControl.EnumerateVisualsDepthFirst().ToList();
        CollectionAssert.Contains(visuals, header);
        CollectionAssert.Contains(visuals, content);
    }

    [TestMethod]
    [Ignore("Invalid for now for TabControl")]
    public async Task TabControl_DoesNotDuplicateTabs_When_SelectedIndex_Changes()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(80, 25));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var tabControl = new TabControl()
            .Update(tabs =>
            {
                tabs.AddTab(new TextBlock("One"), new TextBlock("A"));
                tabs.AddTab(new TextBlock("Two"), new TextBlock("B"));
            });

        var root = new VStack(tabControl);
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        try
        {
            await Task.Delay(20);

            await app.Dispatcher.InvokeAsync(() => Assert.AreEqual(2, tabControl.Tabs.Count));

            await app.Dispatcher.InvokeAsync(() => tabControl.SelectedIndex = 1);
            await Task.Delay(50);

            await app.Dispatcher.InvokeAsync(() =>
                Assert.AreEqual(2, tabControl.Tabs.Count, "Tabs should not be re-added when SelectedIndex changes."));
        }
        finally
        {
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    [TestMethod]
    public void TabControl_Sets_Bounds_On_Arrange()
    {
        var tabControl = new TabControl(
            new TabPage("One", new TextBlock("A")),
            new TabPage("Two", new TextBlock("B")))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);

        tabControl.Measure(new Size(80, 25));
        tabControl.Arrange(new Rectangle(0, 0, 80, 25));

        Assert.AreEqual(new Rectangle(0, 0, 80, 25), tabControl.Bounds);
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
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(10);

        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(Binding binding)
        {
            if (ReferenceEquals(binding.Owner, textBox) && binding.Accessor.Name == "Text" && textBox.Text == "axyzc")
            {
                reached.TrySetResult();
            }
        }

        BindingManager.Current.ValueChanged += Handler;
        try
        {
            try
            {
                backend.PushEvent(new TerminalTextEvent { Text = "a" });
                backend.PushEvent(new TerminalTextEvent { Text = "b" });
                backend.PushEvent(new TerminalTextEvent { Text = "c" });
                backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left });
                backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Backspace });
                backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = 'v', Modifiers = TerminalModifiers.Ctrl });

                await reached.Task.WaitAsync(TimeSpan.FromSeconds(2));
                await app.Dispatcher.InvokeAsync(() => Assert.AreEqual("axyzc", textBox.Text));
            }
            finally
            {
                backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
                await runTask.WaitAsync(TimeSpan.FromSeconds(2));
            }
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
        var runTask = app.RunInBackgroundAsync();

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
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(10);

        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(Binding binding)
        {
            if (ReferenceEquals(binding.Owner, textBox) && binding.Accessor.Name == "Text" && textBox.Text == "hello")
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
        var runTask = app.RunInBackgroundAsync();
        await WaitUntilUi(app, () => ReferenceEquals(app.FocusedElement, textBox));

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = 'k', Modifiers = TerminalModifiers.Ctrl });
        await WaitUntilUi(app, () => textBox.Text == "hello ");

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = 'y', Modifiers = TerminalModifiers.Ctrl });
        await WaitUntilUi(app, () => textBox.Text == "hello world");

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

        var runTask = app.RunInBackgroundAsync();
        await Task.Delay(20);

        app.Post(() => model.Text = "B");
        await Task.Delay(50);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "Computed:A");

        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(outText);
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "Computed:B");
    }

    [TestMethod]
    public async Task DynamicUpdates_Clear_Lists_Before_Reapply()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(80, 25));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var countState = new State<int>(1);

        var stack = new VStack()
            .Update(v =>
            {
                var count = countState.Value;
                for (var i = 0; i < count; i++)
                {
                    v.Add($"Item {i}");
                }
            });

        var app = new TerminalApp(stack, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        try
        {
            await Task.Delay(20);
            await app.Dispatcher.InvokeAsync(() => Assert.AreEqual(1, stack.Children.Count));

            await app.Dispatcher.InvokeAsync(() => countState.Value = 3);
            await Task.Delay(50);
            await app.Dispatcher.InvokeAsync(() => Assert.AreEqual(3, stack.Children.Count));

            await app.Dispatcher.InvokeAsync(() => countState.Value = 2);
            await Task.Delay(50);
            await app.Dispatcher.InvokeAsync(() => Assert.AreEqual(2, stack.Children.Count));
        }
        finally
        {
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    [TestMethod]
    public void DynamicUpdates_Cannot_Mutate_StaticallyInitialized_List()
    {
        var stack = new VStack();
        stack.Add("Static");

        stack.Update(v =>
        {
            v.Add("Dynamic");
        });

        try
        {
            stack.Measure(new Size(80, 25));
            Assert.Fail("Expected an InvalidOperationException when mixing static list initialization with dynamic updates.");
        }
        catch (InvalidOperationException)
        {
        }
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

        var runTask = app.RunInBackgroundAsync();
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

        var key = new StyleKey<string>("Title", "A");

        ComputedVisual? view = null;
        view = new ComputedVisual(() => new TextBlock($"Env:{view!.Get(key)}"));

        var root = new VStack();
        root.Add(view);

        var app = new TerminalApp(root, session.Instance);
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(20);
        app.Post(() => root.Set(key, "B"));
        await Task.Delay(50);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "Env:A");

        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(outText);
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "Env:B");
    }

    [TestMethod]
    public async Task ListBox_Changes_Selection_On_Down()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var listBox = new ListBox
        {
            SelectedIndex = 0,
            MinHeight = 3,
            MaxHeight = 3,
        };
        listBox.Items.AddRange("A", "B", "C");

        var selected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(Binding binding)
        {
            if (ReferenceEquals(binding.Owner, listBox) && binding.Accessor.Name == "SelectedIndex" && listBox.SelectedIndex == 1)
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
            var runTask = app.RunInBackgroundAsync();

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

        var scroll = new ScrollViewer { Content = content };

        var root = new VStack { Spacing = 1 };
        root.Add(scroll);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

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
        };
        table.Headers("Name", "Value")
            .AddRow("A", "1")
            .AddRow("B", "2");

        var root = new VStack { table };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

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
    public async Task Group_Renders_Corner_Texts()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 8));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var group = new Group
        {
            Padding = new Thickness(1),
            TopLeftText = "TL",
            TopRightText = "TR",
            BottomLeftText = "BL",
            BottomRightText = "BR",
            Content = new TextBlock("X"),
        };

        var root = new VStack { group };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(30);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "TL");
        StringAssert.Contains(outText, "TR");
        StringAssert.Contains(outText, "BL");
        StringAssert.Contains(outText, "BR");
    }

    [TestMethod]
    public async Task StatusBar_Renders_Left_And_Right()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 5));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var status = new StatusBar { LeftText = "L", RightText = "R" };
        var layout = new DockLayout { Content = new TextBlock("X"), Bottom = status };

        var app = new TerminalApp(layout, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(30);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "L");
        StringAssert.Contains(outText, "R");
    }
}
