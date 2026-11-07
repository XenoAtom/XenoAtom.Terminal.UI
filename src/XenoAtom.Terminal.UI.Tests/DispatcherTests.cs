// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
[DoNotParallelize] // Uses the process-wide Terminal instance (Terminal.Open).
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

            dispatcher.BindToCurrentThread(app);
            Assert.IsInstanceOfType<DispatcherSynchronizationContext>(SynchronizationContext.Current);
            Assert.AreSame(app, dispatcher.AttachedApp);

            dispatcher.DetachFromThread(app);
            Assert.AreSame(previous, SynchronizationContext.Current);
            Assert.IsNull(dispatcher.AttachedApp);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }
    }
}
