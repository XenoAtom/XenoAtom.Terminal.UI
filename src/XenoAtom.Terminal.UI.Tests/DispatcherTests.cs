// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class DispatcherTests
{
    [TestMethod]
    public void BindToCurrentThread_Restores_Previous_SynchronizationContext()
    {
        var original = SynchronizationContext.Current;
        var previous = new SynchronizationContext();

        try
        {
            SynchronizationContext.SetSynchronizationContext(previous);

            var backend = new InMemoryTerminalBackend(new TerminalSize(10, 5));
            using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = false }, force: true);

            var app = new TerminalApp(new VStack(), session.Instance);

            var dispatcher = Dispatcher.Current;
            var bind = typeof(Dispatcher).GetMethod("BindToCurrentThread", BindingFlags.Instance | BindingFlags.NonPublic);
            var detach = typeof(Dispatcher).GetMethod("DetachFromThread", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(bind);
            Assert.IsNotNull(detach);

            bind.Invoke(dispatcher, [app]);
            Assert.AreEqual("DispatcherSynchronizationContext", SynchronizationContext.Current?.GetType().Name);

            detach.Invoke(dispatcher, [app]);
            Assert.AreSame(previous, SynchronizationContext.Current);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }
    }
}
