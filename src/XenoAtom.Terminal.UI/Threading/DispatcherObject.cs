// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace XenoAtom.Terminal.UI.Threading;

/// <summary>
/// Base type for objects that are associated with the UI dispatcher thread.
/// </summary>
/// <remarks>
/// Thread affinity follows ownership, not construction: an object belongs to the dispatcher of the <see cref="TerminalApp"/>
/// that uses it. A <see cref="Visual"/> is owned by the app it is attached to, a <see cref="TerminalApp"/> by the thread it runs on,
/// and other objects (for example <see cref="State{T}"/>) by the running app that first reads them while tracking dependencies.
/// An object without a running owner can be accessed from any thread, so a tree can be built on one thread and run on another.
/// </remarks>
public abstract class DispatcherObject
{
    private Dispatcher? _dispatcher;

    /// <summary>
    /// Gets the dispatcher this object is associated with.
    /// </summary>
    /// <remarks>
    /// Returns the dispatcher of the owning app (for a <see cref="TerminalApp"/>, its own dispatcher while it runs).
    /// When the object has no owner yet (for example a <see cref="TerminalApp"/> before it starts running, or a visual that is not attached),
    /// this returns <see cref="Threading.Dispatcher.Current"/>, the dispatcher of the calling thread.
    /// </remarks>
    public Dispatcher Dispatcher => _dispatcher ?? GetVisualElementDispatcher() ?? Dispatcher.Current;

    internal Dispatcher? OwnerDispatcher => _dispatcher;

    /// <summary>
    /// Checks whether the current thread has access to this object.
    /// </summary>
    /// <returns><see langword="true"/> if the object has no running owner or the caller runs on the owner's thread; otherwise <see langword="false"/>.</returns>
    public bool CheckAccess()
    {
        var dispatcher = _dispatcher;
        if (dispatcher is null)
        {
            return CheckAccessWithoutOwner();
        }

        return dispatcher.ThreadId == Environment.CurrentManagedThreadId || CheckAccessFromOtherThread(dispatcher);
    }

    /// <summary>
    /// Verifies that the current thread has access to this object.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the object is owned by a running app on another thread.</exception>
    public void VerifyAccess()
    {
        if (!CheckAccess())
        {
            Dispatcher.ThrowInvalidThreadAccess();
        }
    }

    internal void SetDispatcher(Dispatcher? dispatcher) => _dispatcher = dispatcher;

    internal void ClearDispatcher() => _dispatcher = null;

    internal void TryAcquireDispatcher(Dispatcher dispatcher) => Interlocked.CompareExchange(ref _dispatcher, dispatcher, null);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool CheckAccessWithoutOwner()
    {
        // Visual elements that are not visuals (ProgressTask, BarChartItem, BreakdownSegment) follow the app of their owner visual.
        var dispatcher = GetVisualElementDispatcher();
        return dispatcher is null || dispatcher.ThreadId == Environment.CurrentManagedThreadId || !dispatcher.IsRunning;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool CheckAccessFromOtherThread(Dispatcher dispatcher)
    {
        if (dispatcher.IsRunning)
        {
            return false;
        }

        // The owning app is no longer running: drop the stale owner so that the next app using this object can acquire it.
        Interlocked.CompareExchange(ref _dispatcher, null, dispatcher);
        return true;
    }

    private Dispatcher? GetVisualElementDispatcher()
    {
        return this is IVisualElement element && element.App is { } app && !ReferenceEquals(app, this) ? app.OwnerDispatcher : null;
    }
}

