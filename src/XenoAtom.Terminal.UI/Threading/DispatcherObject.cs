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
    // Owner marker for visual elements that are not visuals (ProgressTask, BarChartItem, BreakdownSegment): they follow the app
    // of their owner visual, resolved through IVisualElement.App on the slow path. Its thread id never matches a managed thread.
    private static readonly Dispatcher.Ownership VisualElementOwner = new(Dispatcher.CreateUnbound());

    private Dispatcher.Ownership? _dispatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="DispatcherObject"/> class.
    /// </summary>
    protected DispatcherObject()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DispatcherObject"/> class for a visual element that is not a <see cref="Visual"/>
    /// (for example <see cref="Controls.ProgressTask"/>) and follows the app of its owner visual through <see cref="IVisualElement.App"/>.
    /// </summary>
    /// <param name="followsOwnerVisual">Must be <see langword="true"/>; the object resolves its dispatcher through <see cref="IVisualElement.App"/>.</param>
    private protected DispatcherObject(bool followsOwnerVisual)
    {
        if (followsOwnerVisual)
        {
            _dispatcher = VisualElementOwner;
        }
    }

    /// <summary>
    /// Gets the dispatcher this object is associated with.
    /// </summary>
    /// <remarks>
    /// Returns the dispatcher of the owning app (for a <see cref="TerminalApp"/>, its own dispatcher while it runs).
    /// When the object has no owner yet (for example a <see cref="TerminalApp"/> before it starts running, or a visual that is not attached),
    /// this returns <see cref="Threading.Dispatcher.Current"/>, the dispatcher of the calling thread.
    /// </remarks>
    public Dispatcher Dispatcher
    {
        get
        {
            var dispatcher = OwnerDispatcher;
            if (ReferenceEquals(dispatcher, VisualElementOwner.Dispatcher))
            {
                dispatcher = GetVisualElementDispatcher();
            }

            return dispatcher ?? Dispatcher.Current;
        }
    }

    /// <summary>
    /// Gets the active owner of this object (a marker for visual elements that follow the app of their owner visual).
    /// </summary>
    internal Dispatcher? OwnerDispatcher
    {
        get
        {
            var ownership = _dispatcher;
            if (ownership is null)
            {
                return null;
            }

            if (!ReferenceEquals(ownership, VisualElementOwner) && !ownership.IsRunning)
            {
                Interlocked.CompareExchange(ref _dispatcher, null, ownership);
                return null;
            }

            return ownership.Dispatcher;
        }
    }

    /// <summary>
    /// Checks whether the current thread has access to this object.
    /// </summary>
    /// <returns><see langword="true"/> if the object has no running owner or the caller runs on the owner's thread; otherwise <see langword="false"/>.</returns>
    public bool CheckAccess()
    {
        var dispatcher = _dispatcher;
        return dispatcher is null || dispatcher.ThreadId == Environment.CurrentManagedThreadId || CheckAccessFromOtherThread(dispatcher);
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

    internal void SetDispatcher(Dispatcher? dispatcher) => _dispatcher = dispatcher?.CurrentOwnership;

    internal void ClearDispatcher() => _dispatcher = null;

    internal void TryAcquireDispatcher(Dispatcher dispatcher)
    {
        var ownership = dispatcher.CurrentOwnership ?? throw new InvalidOperationException(Dispatcher.NotRunningMessage);
        Interlocked.CompareExchange(ref _dispatcher, ownership, null);
        // Another running app may have won the first-read race after RegisterRead checked access.
        VerifyAccess();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool CheckAccessFromOtherThread(Dispatcher.Ownership dispatcher)
    {
        if (ReferenceEquals(dispatcher, VisualElementOwner))
        {
            var owner = GetVisualElementDispatcher();
            return owner is null || owner.ThreadId == Environment.CurrentManagedThreadId || !owner.IsRunning;
        }

        if (dispatcher.IsRunning)
        {
            return false;
        }

        // The owning app is no longer running: drop the stale owner so that the next app using this object can acquire it.
        Interlocked.CompareExchange(ref _dispatcher, null, dispatcher);
        return true;
    }

    private Dispatcher? GetVisualElementDispatcher() => (this as IVisualElement)?.App?.OwnerDispatcher;
}

