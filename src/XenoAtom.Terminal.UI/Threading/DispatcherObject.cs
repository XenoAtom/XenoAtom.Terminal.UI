// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Threading;

/// <summary>
/// Base type for objects that are associated with the UI dispatcher thread.
/// </summary>
public abstract class DispatcherObject
{
    /// <summary>
    /// Gets the application dispatcher.
    /// </summary>
    public Dispatcher Dispatcher => Dispatcher.Current;

    /// <summary>
    /// Checks whether the current thread has access to this object.
    /// </summary>
    public bool CheckAccess() => Dispatcher.CheckAccess();

    /// <summary>
    /// Verifies that the current thread has access to this object.
    /// </summary>
    public void VerifyAccess() => Dispatcher.VerifyAccess();
}

