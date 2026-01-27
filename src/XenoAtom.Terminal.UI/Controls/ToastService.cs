// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Provides helpers for showing toast notifications from anywhere in a running application.
/// </summary>
public static class ToastService
{
    /// <summary>
    /// Shows a toast with plain text content.
    /// </summary>
    /// <param name="message">The toast content.</param>
    /// <param name="severity">The toast severity.</param>
    /// <returns>The created toast, or <see langword="null"/> if no host is available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
    public static Toast? Show(string message, ToastSeverity severity = ToastSeverity.Info)
    {
        ArgumentNullException.ThrowIfNull(message);

        var host = FindHost();
        if (host is null)
        {
            return null;
        }

        return host.Show(new TextBlock(message), severity);
    }

    /// <summary>
    /// Shows a toast created by a factory callback.
    /// </summary>
    /// <param name="build">The factory used to construct the toast.</param>
    /// <returns>The created toast, or <see langword="null"/> if no host is available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="build"/> is <see langword="null"/>.</exception>
    public static Toast? Show(Func<Toast> build)
    {
        ArgumentNullException.ThrowIfNull(build);

        var host = FindHost();
        return host is null ? null : host.Show(build);
    }

    /// <summary>
    /// Shows an informational toast.
    /// </summary>
    /// <param name="message">The toast content.</param>
    /// <returns>The created toast, or <see langword="null"/> if no host is available.</returns>
    public static Toast? Info(string message) => Show(message, ToastSeverity.Info);

    /// <summary>
    /// Shows a success toast.
    /// </summary>
    /// <param name="message">The toast content.</param>
    /// <returns>The created toast, or <see langword="null"/> if no host is available.</returns>
    public static Toast? Success(string message) => Show(message, ToastSeverity.Success);

    /// <summary>
    /// Shows a warning toast.
    /// </summary>
    /// <param name="message">The toast content.</param>
    /// <returns>The created toast, or <see langword="null"/> if no host is available.</returns>
    public static Toast? Warning(string message) => Show(message, ToastSeverity.Warning);

    /// <summary>
    /// Shows an error toast.
    /// </summary>
    /// <param name="message">The toast content.</param>
    /// <returns>The created toast, or <see langword="null"/> if no host is available.</returns>
    public static Toast? Error(string message) => Show(message, ToastSeverity.Error);

    private static ToastHost? FindHost()
    {
        var app = Dispatcher.Current.AttachedApp;
        if (app is null)
        {
            return null;
        }

        return FindHost(app.Root);
    }

    private static ToastHost? FindHost(Visual root)
    {
        var stack = new Stack<Visual>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var visual = stack.Pop();
            if (visual is ToastHost host)
            {
                return host;
            }

            var count = visual.GetChildrenCount();
            for (var i = 0; i < count; i++)
            {
                stack.Push(visual.GetChildUnsafe(i));
            }
        }

        return null;
    }
}
