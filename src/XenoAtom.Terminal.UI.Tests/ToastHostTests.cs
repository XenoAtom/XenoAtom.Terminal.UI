// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ToastHostTests
{
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
}
