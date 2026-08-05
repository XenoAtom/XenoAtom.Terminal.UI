// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using System.Reflection;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ToastHostTests
{
    [TestMethod]
    public void Toast_Renders_CloseButton_OnRight_When_TitleMissing()
    {
        var host = new ToastHost(new VStack())
        {
            Position = ToastPosition.TopLeft,
        };

        using var driver = new TerminalAppTestDriver(host, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        driver.App.Post(() => host.Show(new TextBlock("Message")));
        driver.Tick();

        Assert.HasCount(1, host.VisibleToasts);
        var toast = host.VisibleToasts[0];
        var rect = toast.Bounds;
        Assert.IsTrue(rect.Width > 0 && rect.Height > 0);

        var style = toast.GetStyle<ToastStyle>();
        var padding = style.Padding;

        var buffer = (CellBuffer)typeof(TerminalApp).GetField("_renderBuffer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(driver.App)!;
        var closeRune = style.CloseIcon.EnumerateRunes().FirstOrDefault(new Rune('X'));

        // Close button is rendered on the first content line (after border+padding). Border has a fixed inset of 1 cell.
        var y = rect.Y + (padding.Top + 1);
        var closeX = -1;
        for (var x = rect.X; x < rect.Right; x++)
        {
            var index = (y * buffer.Width) + x;
            if (buffer.UnsafeScalars[index] == closeRune.Value)
            {
                closeX = x;
            }
        }

        Assert.AreNotEqual(-1, closeX, "Expected toast close button glyph to be rendered on the header line.");

        var expectedRight = rect.Right - padding.Right - 2;
        Assert.IsGreaterThanOrEqualTo(expectedRight - 2, closeX, $"Expected toast close button near the right edge. closeX={closeX} expectedRight={expectedRight} rect={rect}");
    }

    [TestMethod]
    public void ToastHost_Dismisses_Oldest_When_MaxVisible_Reached()
    {
        var host = new ToastHost(new VStack())
        {
            MaxVisible = 2,
        };

        using var driver = new TerminalAppTestDriver(host, TerminalHostKind.Fullscreen);
        driver.Tick();

        Toast? first = null;
        ToastDismissReason? dismissedReason = null;

        driver.App.Post(() =>
        {
            first = host.Show(new TextBlock("First"));
            first.Dismissed((_, e) => dismissedReason = e.Reason);
        });
        driver.Tick();

        driver.App.Post(() => host.Show(new TextBlock("Second")));
        driver.Tick();

        driver.App.Post(() => host.Show(new TextBlock("Third")));
        driver.Tick();

        Assert.HasCount(2, host.VisibleToasts);
        Assert.AreEqual(ToastDismissReason.Overflow, dismissedReason);
        Assert.IsFalse(host.VisibleToasts.Any(t => ReferenceEquals(t, first)));
    }

    [TestMethod]
    public void ToastHost_AutoDismisses_After_Default_Duration()
    {
        var host = new ToastHost(new VStack())
        {
            DefaultDuration = TimeSpan.FromMilliseconds(30),
        };

        using var driver = new TerminalAppTestDriver(host, TerminalHostKind.Fullscreen);
        driver.Tick();

        ToastDismissReason? dismissedReason = null;

        driver.App.Post(() =>
        {
            var toast = host.Show(new TextBlock("Auto"));
            toast.Dismissed((_, e) => dismissedReason = e.Reason);
        });
        driver.Tick();

        driver.TickUntil(() => host.VisibleToasts.Count == 0, maxTicks: 20);

        Assert.AreEqual(ToastDismissReason.Timeout, dismissedReason);
    }

    [TestMethod]
    public void ToastHost_NewToast_After_Idle_Gap_Is_Not_Dismissed_Immediately()
    {
        // Regression test for: clicking the same toast-triggering button twice
        // with a delay between clicks used to surface only the first toast,
        // because the animation clock kept a stale timestamp from the last
        // frame and treated the new toast as having been on screen for the
        // entire idle gap.
        var host = new ToastHost(new VStack())
        {
            DefaultDuration = TimeSpan.FromMilliseconds(30),
        };

        using var driver = new TerminalAppTestDriver(host, TerminalHostKind.Fullscreen);
        driver.Tick();

        // First click: show a toast and let it auto-dismiss.
        driver.App.Post(() => host.Show(new TextBlock("First")));
        driver.Tick();

        driver.TickUntil(() => host.VisibleToasts.Count == 0, maxTicks: 50);
        Assert.HasCount(0, host.VisibleToasts);

        // Idle gap: the user waits before clicking again. The ToastHost stays
        // registered for animations but its NextAnimationTick is long.MaxValue
        // once the entry list is empty, so AdvanceAnimation isn't called and
        // its internal clock keeps the stale timestamp from the last frame.
        driver.Tick(20);

        // Second click on the same button. Without the fix, the stale clock
        // would be applied as the delta on the next AdvanceAnimation and
        // dismiss this toast immediately.
        driver.App.Post(() => host.Show(new TextBlock("Second")));
        driver.Tick(2);

        Assert.HasCount(1, host.VisibleToasts);
    }
}
