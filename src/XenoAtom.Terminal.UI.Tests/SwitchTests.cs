// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Hosting;
using System.Reflection;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class SwitchTests
{
    [TestMethod]
    public void Toggled_Event_Is_Raised_With_Old_And_New_Values()
    {
        var sw = new Switch();

        ToggleChangedEventArgs? args = null;
        sw.ToggledRouted += (_, e) => args = e;

        sw.IsOn = true;

        Assert.IsNotNull(args);
        Assert.AreEqual(false, args.OldValue);
        Assert.AreEqual(true, args.NewValue);
    }

    [TestMethod]
    public async Task Space_Key_Toggles_Switch()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var sw = new Switch();
        var root = new VStack { sw };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(50);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Space });
        await Task.Delay(50);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });

        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreEqual(true, sw.IsOn);
    }

    [TestMethod]
    public void Switch_Renders_Segmented_Track_With_Different_Left_And_Right_Backgrounds()
    {
        var theme = Theme.FromScheme(XenoAtom.Terminal.UI.Styling.AnsiColorScheme.RootLoopsDark);

        var sw = new Switch();
        sw.Set(Theme.Key, theme);

        sw.Measure(new Size(10, 1));
        sw.Arrange(new Rectangle(0, 0, 10, 1));

        var buffer = new CellBuffer(10, 1);
        buffer.Clear();
        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(sw, new object[] { buffer });

        var cells = (CellStyle[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        Assert.IsTrue(cells[0].TryGetBackground(out var leftOffBg));
        Assert.IsTrue(cells[3].TryGetBackground(out var rightOffBg));
        Assert.AreNotEqual(leftOffBg, rightOffBg);

        // Thumb cell should keep the underlying track background.
        Assert.IsTrue(cells[1].TryGetBackground(out var thumbOffBg));
        Assert.AreEqual(leftOffBg, thumbOffBg);

        sw.IsOn = true;
        buffer.Clear();
        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(sw, new object[] { buffer });
        cells = (CellStyle[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        Assert.IsTrue(cells[0].TryGetBackground(out var leftOnBg));
        Assert.IsTrue(cells[3].TryGetBackground(out var rightOnBg));
        Assert.AreNotEqual(leftOnBg, rightOnBg);

        Assert.IsTrue(cells[2].TryGetBackground(out var thumbOnBg));
        Assert.AreEqual(rightOnBg, thumbOnBg);
    }
}
